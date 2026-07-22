const assert = require("node:assert/strict");
const { access, readFile, writeFile } = require("node:fs/promises");
const Module = require("node:module");
const { dirname, resolve } = require("node:path");
const test = require("node:test");
const vm = require("node:vm");

const {
  loadEndpointCatalog,
  parseEndpointCatalog,
} = require("../dist/cli/endpointCatalog.js");
const { buildDashboardTuneArguments } = require("../dist/cli/tuneArguments.js");
const {
  EndpointCatalogSession,
} = require("../dist/cli/endpointCatalogSession.js");
const {
  defaultReadOnlyConnectionStringForHarness,
  sampleAppHarnessPath,
  sampleAppReadOnlyConnectionString,
} = require("../dist/constants/sampleAppDefaults.js");

const endpoint = {
  stableOperationKey: "GET /api/products/{id}",
  httpMethod: "GET",
  route: "/api/products/{id}",
  controllerType: "Sample.ProductsController",
  methodName: "GetById",
};

test("prefills the localhost connection only for the sample app harness", () => {
  assert.equal(
    defaultReadOnlyConnectionStringForHarness(sampleAppHarnessPath),
    sampleAppReadOnlyConnectionString,
  );
  assert.equal(
    defaultReadOnlyConnectionStringForHarness(
      ".\\tests\\Sqloom\\Sqloom.TestApp\\default\\Harness.cs",
    ),
    sampleAppReadOnlyConnectionString,
  );
  assert.equal(
    defaultReadOnlyConnectionStringForHarness("tests/Sample/Harness.cs"),
    "",
  );
});

test("keeps the sample connection aligned with the test app", async () => {
  const appSettingsPath = resolve(
    __dirname,
    "../../../tests/Sqloom.TestApp/appsettings.json",
  );
  const appSettings = JSON.parse(await readFile(appSettingsPath, "utf8"));

  assert.equal(
    appSettings.ConnectionStrings.DefaultConnection,
    sampleAppReadOnlyConnectionString,
  );
});

test("initializes the dashboard connection for the detected sample app", async () => {
  const dashboardStatePath = require.resolve(
    "../dist/dashboard/dashboardState.js",
  );
  const originalLoad = Module._load;

  try {
    Module._load = function (request, parent, isMain) {
      if (request === "vscode") {
        return {
          Uri: {
            joinPath: (base, ...segments) => ({
              fsPath: [base.fsPath, ...segments].join("/"),
            }),
          },
          workspace: {
            workspaceFolders: [
              {
                name: "sqloom",
                uri: { fsPath: "C:/repo/sqloom" },
              },
            ],
            getConfiguration: () => ({
              get: (_key, defaultValue) => defaultValue,
            }),
            fs: {
              stat: async () => ({}),
            },
          },
        };
      }
      if (request === "../cli/cliStatus") {
        return {
          checkCliAvailability: async (cliPath) => ({
            cliPath,
            ready: true,
            detail: "Available",
          }),
        };
      }

      return originalLoad.call(this, request, parent, isMain);
    };
    delete require.cache[dashboardStatePath];
    const { createDashboardState } = require(dashboardStatePath);
    const state = await createDashboardState();
    const connectionField = state.setupFields.find(
      (field) => field.id === "readOnlyConnectionString",
    );

    assert.ok(connectionField);
    assert.equal(connectionField.value, sampleAppReadOnlyConnectionString);
    assert.equal(connectionField.status, "ready");
  } finally {
    Module._load = originalLoad;
    delete require.cache[dashboardStatePath];
  }
});

test("loads endpoints through a caller-owned JSON file and cleans up", async () => {
  let outputPath;
  const result = await loadEndpointCatalog(
    {
      cliPath: "sqloom-local",
      harnessPath: "tests/Sample/Harness.cs",
      cwd: "C:\\repo\\sample",
    },
    async (executable, args, cwd) => {
      assert.equal(executable, "sqloom-local");
      assert.equal(cwd, "C:\\repo\\sample");
      assert.deepEqual(args.slice(0, 3), [
        "endpoints",
        "tests/Sample/Harness.cs",
        "--json-output-file",
      ]);
      outputPath = args[3];
      await writeFile(outputPath, JSON.stringify([endpoint]), "utf8");
      return { exitCode: 0, stdout: "catalog", stderr: "" };
    },
  );

  assert.equal(result.status, "loaded");
  assert.deepEqual(result.endpoints, [endpoint]);
  await assert.rejects(access(dirname(outputPath)));
});

test("returns an empty loaded catalog without inventing a selection", async () => {
  const result = await loadEndpointCatalog(
    { cliPath: "sqloom", harnessPath: "Harness.cs", cwd: "C:\\repo" },
    async (_executable, args) => {
      await writeFile(args[3], "[]", "utf8");
      return { exitCode: 0, stdout: "", stderr: "" };
    },
  );

  assert.equal(result.status, "loaded");
  assert.deepEqual(result.endpoints, []);
});

test("fails closed for invalid endpoint JSON", () => {
  assert.throws(
    () => parseEndpointCatalog(JSON.stringify({ endpoints: [] })),
    /must be a JSON array/,
  );
  assert.throws(
    () =>
      parseEndpointCatalog(
        JSON.stringify([{ httpMethod: "GET", route: "/api/products" }]),
      ),
    /stableOperationKey/,
  );
});

test("cleans up after malformed endpoint output", async () => {
  let outputPath;
  const result = await loadEndpointCatalog(
    { cliPath: "sqloom", harnessPath: "Harness.cs", cwd: "C:\\repo" },
    async (_executable, args) => {
      outputPath = args[3];
      await writeFile(outputPath, '{"endpoints":[]}', "utf8");
      return { exitCode: 0, stdout: "", stderr: "" };
    },
  );

  assert.equal(result.status, "failed");
  assert.match(result.message, /must be a JSON array/);
  await assert.rejects(access(dirname(outputPath)));
});

test("reports discovery failures and cleans up", async () => {
  let outputPath;
  const result = await loadEndpointCatalog(
    { cliPath: "sqloom", harnessPath: "Harness.cs", cwd: "C:\\repo" },
    async (_executable, args) => {
      outputPath = args[3];
      return { exitCode: 2, stdout: "", stderr: "invalid harness" };
    },
  );

  assert.equal(result.status, "failed");
  assert.match(result.message, /exited with code 2/);
  await assert.rejects(access(dirname(outputPath)));
});

test("passes the selected stable operation key to tune exactly once", () => {
  const args = buildDashboardTuneArguments({
    harnessPath: "Harness.cs",
    target: endpoint.stableOperationKey,
    modelProvider: "openai",
    openAiApiKey: "test-key",
    openAiModel: "gpt-test",
    replayDataAgent: "required",
    readOnlyConnectionString: "Server=localhost",
    artifactDir: "artifacts/sqloom/tune/dashboard-test",
  });

  assert.equal(args.filter((value) => value === "--target").length, 1);
  assert.equal(args[args.indexOf("--target") + 1], endpoint.stableOperationKey);
  assert.equal(args[args.indexOf("--replay-data-agent-model") + 1], "gpt-test");
});

test("rejects dashboard tune arguments without an endpoint", () => {
  assert.throws(
    () =>
      buildDashboardTuneArguments({
        harnessPath: "Harness.cs",
        target: " ",
        modelProvider: "openai",
        openAiApiKey: "test-key",
        openAiModel: "gpt-test",
        replayDataAgent: "required",
        readOnlyConnectionString: "Server=localhost",
        artifactDir: "artifacts/sqloom/tune/dashboard-test",
      }),
    /endpoint target is required/,
  );
});

test("endpoint session revokes stale keys while refreshing", () => {
  const session = new EndpointCatalogSession();
  const context = "sqloom\nHarness.cs";
  const firstGeneration = session.beginLoad();
  assert.equal(
    session.completeLoad(firstGeneration, context, [endpoint]),
    true,
  );
  assert.equal(session.canRun(context, endpoint.stableOperationKey), true);

  const refreshGeneration = session.beginLoad();
  assert.equal(session.canRun(context, endpoint.stableOperationKey), false);
  assert.equal(
    session.completeLoad(firstGeneration, context, [endpoint]),
    false,
  );
  assert.equal(session.canRun(context, endpoint.stableOperationKey), false);
  assert.equal(
    session.completeLoad(refreshGeneration, context, [endpoint]),
    true,
  );
  assert.equal(session.canRun(context, endpoint.stableOperationKey), true);
  assert.equal(session.canRun(context, "POST /api/products"), false);
  session.clear();
  assert.equal(session.canRun(context, endpoint.stableOperationKey), false);
});

test("renders syntactically valid dashboard client script", async () => {
  const rendererPath =
    require.resolve("../dist/dashboard/dashboardRenderer.js");
  const originalLoad = Module._load;
  const state = {
    title: "Sqloom Tune",
    subtitle: "Test dashboard",
    runNote: "Test run",
    readinessLabel: "Ready",
    stages: [],
    setupSummaryItems: [],
    setupFields: [
      {
        id: "cliPath",
        label: "CLI",
        value: "sqloom",
        kind: "text",
        status: "ready",
      },
      {
        id: "harnessPath",
        label: "Harness",
        value: "Harness.cs",
        kind: "text",
        status: "ready",
      },
    ],
    runStatusTitle: "Run tune",
    runStatusSubtitle: "Ready",
    runStatusChecks: [],
    editStatusTitle: "Edit setup",
    editStatusSubtitle: "Ready",
    editStatusChecks: [],
    recentRunsTitle: "Recent runs",
    recentRunsAction: "View all",
    recentRunsEmptyTitle: "No runs",
    recentRunsEmptyDetail: "No runs yet",
    recentRuns: [],
    summaryItems: [],
    configFields: [],
    readinessChecks: [],
    artifacts: [],
  };

  try {
    Module._load = function (request, parent, isMain) {
      if (request === "vscode") {
        return {
          Uri: {
            joinPath: (...parts) => parts.join("/"),
          },
        };
      }
      if (request === "./dashboardState") {
        return {
          createDashboardState: async () => state,
        };
      }

      return originalLoad.call(this, request, parent, isMain);
    };
    delete require.cache[rendererPath];
    const { renderDashboardHtml } = require(rendererPath);
    const html = await renderDashboardHtml(
      { extensionUri: "extension" },
      {
        asWebviewUri: (value) => value,
        cspSource: "vscode-resource:",
      },
      [],
      {
        selectedRunId: "",
        selectedArtifactDir: "",
        selectedRunLabel: "",
        workspaceFolderUri: "",
        artifactCountLabel: "0 artifacts",
        footerPrimary: "Latest run: --",
        footerSecondary: "Artifacts will appear here after you run a tune.",
        emptyTitle: "No artifacts yet",
        emptyDetail: "Run a tune or select a recent run to inspect artifacts.",
        artifacts: [],
      },
    );
    const script = html.match(/<script nonce="[^"]+">([\s\S]*?)<\/script>/);
    assert.ok(script, "Dashboard client script was not rendered.");
    assert.doesNotThrow(() => new vm.Script(script[1]));
  } finally {
    Module._load = originalLoad;
    delete require.cache[rendererPath];
  }
});
