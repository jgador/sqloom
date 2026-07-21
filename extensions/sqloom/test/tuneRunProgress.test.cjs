const assert = require("node:assert/strict");
const test = require("node:test");

const {
  TuneRunProgressStateMachine,
  getDashboardArtifactDir,
  getNextStage,
  getStageForArtifactRelativePath,
} = require("../dist/cli/tuneRunProgress.js");
const { buildDashboardTuneArguments } = require("../dist/cli/tuneArguments.js");

test("maps tune completion artifacts to workflow stages", () => {
  assert.equal(
    getStageForArtifactRelativePath("replay/replay-summary.json"),
    "replay",
  );
  assert.equal(
    getStageForArtifactRelativePath("query-store-snapshot.json"),
    "observe",
  );
  assert.equal(
    getStageForArtifactRelativePath("replay/query-store-correlation.json"),
    "correlate",
  );
  assert.equal(
    getStageForArtifactRelativePath("replay/sql-tuning-proposal.sql"),
    "advise",
  );
  assert.equal(
    getStageForArtifactRelativePath("replay/tuning-advice.json"),
    undefined,
  );
  assert.equal(getStageForArtifactRelativePath("replay/replay-plan.json"), undefined);
});

test("advances stages in TuneWorkflowRunner order", () => {
  const events = [];
  const machine = new TuneRunProgressStateMachine();
  machine.start((event) => events.push(event));
  machine.completeStage("replay", "replay-summary.json", (event) =>
    events.push(event),
  );
  machine.completeStage("observe", "query-store-snapshot.json", (event) =>
    events.push(event),
  );

  assert.deepEqual(
    events.map((event) => `${event.stage}:${event.status}`),
    [
      "replay:active",
      "replay:completed",
      "observe:active",
      "observe:completed",
      "correlate:active",
    ],
  );
  assert.equal(getNextStage("correlate"), "advise");
  assert.equal(getNextStage("advise"), undefined);
});

test("marks the active stage failed on nonzero exit", () => {
  const events = [];
  const machine = new TuneRunProgressStateMachine();
  machine.start((event) => events.push(event));
  machine.finish(1, (event) => events.push(event));

  assert.deepEqual(
    events.map((event) => ({ stage: event.stage, status: event.status })),
    [
      { stage: "replay", status: "active" },
      { stage: "replay", status: "failed" },
    ],
  );
});

test("builds dashboard tune arguments with artifact directory", () => {
  const args = buildDashboardTuneArguments({
    harnessPath: "Harness.cs",
    target: "GET /api/products",
    modelProvider: "openai",
    openAiApiKey: "test-key",
    openAiModel: "gpt-test",
    replayDataAgent: "required",
    readOnlyConnectionString: "Server=localhost",
    artifactDir: "artifacts/sqloom/tune/dashboard-test",
  });

  assert.equal(args.at(-2), "--artifact-dir");
  assert.equal(args.at(-1), "artifacts/sqloom/tune/dashboard-test");
});

test("matches artifact paths case-insensitively on Windows-style roots", () => {
  const {
    getRelativePathUnderRoot,
    isPathUnderRoot,
  } = require("../dist/cli/tuneRunProgress.js");

  assert.equal(
    isPathUnderRoot(
      "c:\\repo\\GitHub\\sqloom",
      "C:\\repo\\GitHub\\sqloom\\artifacts\\sqloom\\tune\\dashboard-test\\replay\\replay-summary.json",
    ),
    true,
  );
  assert.equal(
    getRelativePathUnderRoot(
      "c:\\repo\\GitHub\\sqloom",
      "C:\\repo\\GitHub\\sqloom\\artifacts\\sqloom\\tune\\dashboard-test\\replay\\replay-summary.json",
    ),
    "artifacts/sqloom/tune/dashboard-test/replay/replay-summary.json",
  );
});

test("buffers out-of-order artifact completions until the active stage catches up", () => {
  const events = [];
  const machine = new TuneRunProgressStateMachine();
  machine.start((event) => events.push(event));
  machine.completeStage("observe", "query-store-snapshot.json", (event) =>
    events.push(event),
  );
  machine.completeStage("replay", "replay-summary.json", (event) =>
    events.push(event),
  );

  assert.deepEqual(
    events.map((event) => `${event.stage}:${event.status}`),
    [
      "replay:active",
      "replay:completed",
      "observe:active",
      "observe:completed",
      "correlate:active",
    ],
  );
});

test("marks advise failed on nonzero exit when proposal artifacts are incomplete", () => {
  const events = [];
  const machine = new TuneRunProgressStateMachine();
  machine.start((event) => events.push(event));
  for (const stage of ["replay", "observe", "correlate"]) {
    machine.completeStage(stage, `${stage}.json`, (event) => events.push(event));
  }
  machine.finish(1, (event) => events.push(event));

  assert.deepEqual(
    events
      .filter((event) => event.status === "failed")
      .map((event) => ({ stage: event.stage, status: event.status })),
    [{ stage: "advise", status: "failed" }],
  );
});

test("marks replay failed on nonzero exit after all stages complete", () => {
  const events = [];
  const machine = new TuneRunProgressStateMachine();
  machine.start((event) => events.push(event));
  for (const stage of ["replay", "observe", "correlate", "advise"]) {
    machine.completeStage(stage, `${stage}.json`, (event) => events.push(event));
  }
  machine.finish(1, (event) => events.push(event));

  assert.deepEqual(
    events
      .filter((event) => event.status === "failed")
      .map((event) => ({ stage: event.stage, status: event.status })),
    [{ stage: "replay", status: "failed" }],
  );
});

test("creates predictable dashboard artifact directories", () => {
  assert.equal(
    getDashboardArtifactDir("abc123"),
    "artifacts/sqloom/tune/dashboard-abc123",
  );
});
