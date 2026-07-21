import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import * as vscode from "vscode";
import { loadEndpointCatalog } from "./cli/endpointCatalog";
import { buildDashboardTuneArguments } from "./cli/tuneArguments";
import { getDashboardArtifactDir } from "./cli/tuneRunProgress";
import { TuneRunProgressTracker } from "./cli/tuneRunProgressTracker";
import {
  defaultOpenAiModel,
  isKnownModelProvider,
  isKnownOpenAiModel,
} from "./constants/modelOptions";
import {
  defaultReadOnlyConnectionStringForHarness,
  sampleAppHarnessPath,
} from "./constants/sampleAppDefaults";
import { openDashboard, registerDashboardLauncher } from "./dashboard";
import {
  DashboardEndpointRequest,
  DashboardEndpointResult,
  DashboardTuneProgressEvent,
  DashboardTuneRequest,
  DashboardTuneRunResult,
} from "./sharedInterfaces/dashboard";

const outputChannel = vscode.window.createOutputChannel("Sqloom");

export function activate(context: vscode.ExtensionContext) {
  const cliService = new CliService();
  const dashboardCallbacks = {
    runTune: (
      request: DashboardTuneRequest,
      runId: string,
      onProgress: (event: DashboardTuneProgressEvent) => void,
    ) => runTuneFromDashboard(cliService, request, runId, onProgress),
    loadEndpoints: (request: DashboardEndpointRequest) =>
      loadDashboardEndpoints(request),
  };

  context.subscriptions.push(
    outputChannel,
    registerDashboardLauncher(context, dashboardCallbacks),
    vscode.commands.registerCommand("sqloom.openDashboard", () =>
      openDashboard(context, dashboardCallbacks),
    ),
    vscode.commands.registerCommand("sqloom.init", () => runInit(cliService)),
    vscode.commands.registerCommand("sqloom.tune", () => runTune(cliService)),
    vscode.commands.registerCommand("sqloom.selectCliPath", selectCliPath),
  );
}

export function deactivate() {
  outputChannel.dispose();
}

class CliService {
  async run(
    args: string[],
    workspaceFolder: vscode.WorkspaceFolder,
    cliPathOverride?: string,
  ): Promise<boolean> {
    const result = await this.runWithExitCode(args, workspaceFolder, {
      cliPathOverride,
    });
    return result.exitCode === 0;
  }

  async runWithExitCode(
    args: string[],
    workspaceFolder: vscode.WorkspaceFolder,
    options?: {
      cliPathOverride?: string;
      quietCompletionToasts?: boolean;
    },
  ): Promise<{ exitCode: number; launchFailureMessage?: string }> {
    const cliPath =
      options?.cliPathOverride?.trim() ||
      getConfiguration().get<string>("cli.path", "sqloom").trim() ||
      "sqloom";
    const redactedCommand = [cliPath, ...redactArgs(args)].join(" ");

    outputChannel.show(true);
    outputChannel.appendLine(`> ${redactedCommand}`);
    outputChannel.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
    outputChannel.appendLine("");

    let launchFailureMessage: string | undefined;
    const exitCode = await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: `Sqloom ${args[0]}`,
      },
      () =>
        new Promise<number>((resolve) => {
          let failedToStart = false;
          let child: ChildProcessWithoutNullStreams;
          try {
            child = spawn(cliPath, args, {
              cwd: workspaceFolder.uri.fsPath,
              env: process.env,
              shell: false,
            });
          } catch (error) {
            launchFailureMessage = formatLaunchFailureMessage(cliPath, error);
            outputChannel.appendLine(launchFailureMessage);
            resolve(-1);
            return;
          }

          child.stdout.on("data", (chunk: Buffer) => {
            outputChannel.append(chunk.toString());
          });

          child.stderr.on("data", (chunk: Buffer) => {
            outputChannel.append(chunk.toString());
          });

          child.on("error", (error) => {
            failedToStart = true;
            launchFailureMessage = formatLaunchFailureMessage(cliPath, error);
            outputChannel.appendLine("");
            outputChannel.appendLine(launchFailureMessage);
            resolve(-1);
          });

          child.on("close", (code) => {
            if (failedToStart) {
              return;
            }

            outputChannel.appendLine("");
            outputChannel.appendLine(`Sqloom exited with code ${code ?? -1}.`);
            resolve(code ?? -1);
          });
        }),
    );

    if (!options?.quietCompletionToasts) {
      if (exitCode === 0) {
        vscode.window.showInformationMessage("Sqloom command completed.");
      } else {
        vscode.window.showErrorMessage(
          launchFailureMessage ??
            "Sqloom command failed. See the Sqloom output channel.",
        );
      }
    }

    return { exitCode, launchFailureMessage };
  }
}

async function runTuneFromDashboard(
  cliService: CliService,
  request: DashboardTuneRequest,
  runId: string,
  onProgress: (event: DashboardTuneProgressEvent) => void,
): Promise<DashboardTuneRunResult> {
  const workspaceFolder = getDashboardWorkspaceFolder();
  if (!workspaceFolder) {
    vscode.window.showWarningMessage("Open a workspace before running Sqloom.");
    return { success: false, artifactDir: "" };
  }

  const harnessPath =
    (request.harnessPath ?? "").trim() ||
    (await defaultHarnessPath(workspaceFolder));
  if (harnessPath.length === 0) {
    vscode.window.showWarningMessage(
      "Sqloom dashboard tune requires the default harness path in this workspace.",
    );
    return { success: false, artifactDir: "" };
  }

  const modelProvider = (request.modelProvider ?? "").trim();
  if (!isKnownModelProvider(modelProvider)) {
    vscode.window.showWarningMessage(
      "Sqloom dashboard tune requires the OpenAI model provider.",
    );
    return { success: false, artifactDir: "" };
  }

  const openAiModel = (request.openAiModel ?? "").trim();
  if (!isKnownOpenAiModel(openAiModel)) {
    vscode.window.showWarningMessage(
      "Select an OpenAI model before running Sqloom tune.",
    );
    return { success: false, artifactDir: "" };
  }

  const openAiApiKey =
    (request.openAiApiKey ?? "").trim() ||
    (process.env.OPENAI_API_KEY ?? "").trim();
  if (openAiApiKey.length === 0) {
    vscode.window.showWarningMessage(
      "Enter an OpenAI API key or set OPENAI_API_KEY before running Sqloom tune from the dashboard.",
    );
    return { success: false, artifactDir: "" };
  }

  const replayDataAgent = getConfiguration().get<string>(
    "replayDataAgent",
    "required",
  );
  const readOnlyConnectionString = (
    request.readOnlyConnectionString ?? ""
  ).trim();
  if (readOnlyConnectionString.length === 0) {
    vscode.window.showWarningMessage(
      "Enter a read-only SQL Server connection string before running Sqloom tune from the dashboard.",
    );
    return { success: false, artifactDir: "" };
  }

  const target = (request.target ?? "").trim();
  if (target.length === 0) {
    vscode.window.showWarningMessage(
      "Select an endpoint before running Sqloom tune from the dashboard.",
    );
    return { success: false, artifactDir: "" };
  }

  const artifactDir = getDashboardArtifactDir(runId);
  const args = buildDashboardTuneArguments({
    harnessPath,
    target,
    modelProvider,
    openAiApiKey,
    openAiModel,
    replayDataAgent,
    readOnlyConnectionString,
    artifactDir,
  });

  const tracker = new TuneRunProgressTracker(
    workspaceFolder,
    artifactDir,
    onProgress,
  );
  await tracker.start();

  try {
    const result = await cliService.runWithExitCode(args, workspaceFolder, {
      cliPathOverride: request.cliPath,
      quietCompletionToasts: true,
    });
    await tracker.finish(result.exitCode);
    if (result.exitCode === 0) {
      vscode.window.showInformationMessage("Sqloom tune completed.");
      return { success: true, artifactDir };
    }

    vscode.window.showErrorMessage(
      result.launchFailureMessage ??
        "Sqloom tune failed. See the Sqloom output channel.",
    );
    return { success: false, artifactDir };
  } finally {
    tracker.dispose();
  }
}

async function loadDashboardEndpoints(
  request: DashboardEndpointRequest,
): Promise<DashboardEndpointResult> {
  const workspaceFolder = getDashboardWorkspaceFolder();
  if (!workspaceFolder) {
    return {
      status: "failed",
      message: "Open a workspace before loading Sqloom endpoints.",
    };
  }

  const harnessPath = (request.harnessPath ?? "").trim();
  if (harnessPath.length === 0) {
    return {
      status: "failed",
      message: "Enter or detect a harness path before loading endpoints.",
    };
  }

  const cliPath =
    (request.cliPath ?? "").trim() ||
    getConfiguration().get<string>("cli.path", "sqloom").trim() ||
    "sqloom";
  const result = await loadEndpointCatalog({
    cliPath,
    harnessPath,
    cwd: workspaceFolder.uri.fsPath,
  });
  outputChannel.appendLine(
    `> ${[cliPath, ...result.command].map(quoteArg).join(" ")}`,
  );
  outputChannel.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
  if (result.stdout.length > 0) {
    outputChannel.append(result.stdout);
  }
  if (result.stderr.length > 0) {
    outputChannel.append(result.stderr);
  }
  outputChannel.appendLine("");

  if (result.status === "failed") {
    outputChannel.appendLine(result.message);
    return { status: "failed", message: result.message };
  }

  return { status: "loaded", endpoints: result.endpoints };
}

async function runInit(cliService: CliService): Promise<void> {
  const workspaceFolder = await pickWorkspaceFolder();
  if (!workspaceFolder) {
    return;
  }

  const agent = await vscode.window.showQuickPick(
    ["codex", "claude", "copilot", "all"],
    {
      title: "Sqloom Agent Target",
      placeHolder: "Select the agent skill target to scaffold.",
    },
  );
  if (!agent) {
    return;
  }

  const overwrite = await vscode.window.showQuickPick(["No", "Yes"], {
    title: "Overwrite changed scaffolded files?",
    placeHolder: "Choose whether to pass --overwrite.",
  });
  if (!overwrite) {
    return;
  }

  const args = ["init", "--agent", agent];
  if (overwrite === "Yes") {
    args.push("--overwrite");
  }

  await cliService.run(args, workspaceFolder);
}

async function runTune(cliService: CliService): Promise<void> {
  const workspaceFolder = await pickWorkspaceFolder();
  if (!workspaceFolder) {
    return;
  }

  const harnessPath = await vscode.window.showInputBox({
    title: "Sqloom Harness Path",
    prompt:
      "C# harness file, harness project, assembly, solution, solution filter, or directory.",
    value: await defaultHarnessPath(workspaceFolder),
    ignoreFocusOut: true,
  });
  if (!harnessPath) {
    return;
  }

  const target = await vscode.window.showInputBox({
    title: "Replay Target",
    prompt:
      "Optional exact operation, for example GET /api/products/by-category.",
    ignoreFocusOut: true,
  });
  if (target === undefined) {
    return;
  }

  const readOnlyConnectionString = await vscode.window.showInputBox({
    title: "Read-only SQL Server Connection String",
    prompt:
      "Required for Query Store reads and schema export. The extension does not store this value.",
    value: defaultReadOnlyConnectionStringForHarness(harnessPath),
    password: true,
    ignoreFocusOut: true,
  });
  if (readOnlyConnectionString === undefined) {
    return;
  }
  const trimmedReadOnlyConnectionString = readOnlyConnectionString.trim();
  if (trimmedReadOnlyConnectionString.length === 0) {
    vscode.window.showWarningMessage(
      "Sqloom tune requires a read-only SQL Server connection string.",
    );
    return;
  }

  const openAiApiKey =
    process.env.OPENAI_API_KEY ??
    (await vscode.window.showInputBox({
      title: "OpenAI API Key",
      prompt:
        "Required by the current sqloom tune CLI surface. The extension does not store this value.",
      password: true,
      ignoreFocusOut: true,
    }));
  if (!openAiApiKey) {
    vscode.window.showWarningMessage("Sqloom tune requires an OpenAI API key.");
    return;
  }

  const configuration = getConfiguration();
  const openAiModel = configuration.get<string>(
    "openai.model",
    defaultOpenAiModel,
  );
  const replayDataAgent = configuration.get<string>(
    "replayDataAgent",
    "required",
  );

  const args = [
    "tune",
    harnessPath,
    "--model-provider",
    "openai",
    "--openai-api-key",
    openAiApiKey,
    "--openai-model",
    openAiModel,
    "--replay-data-agent",
    replayDataAgent,
    "--replay-data-agent-model",
    openAiModel,
    "--read-only-connection-string",
    trimmedReadOnlyConnectionString,
  ];

  if (target.trim().length > 0) {
    args.push("--target", target.trim());
  }

  await cliService.run(args, workspaceFolder);
}

async function selectCliPath(): Promise<void> {
  const configuration = getConfiguration();
  const current = configuration.get<string>("cli.path", "sqloom");
  const next = await vscode.window.showInputBox({
    title: "Sqloom CLI Path",
    prompt:
      "Use sqloom when the CLI is available on PATH, or enter an absolute executable path.",
    value: current,
    ignoreFocusOut: true,
  });

  if (!next) {
    return;
  }

  await configuration.update(
    "cli.path",
    next,
    vscode.ConfigurationTarget.Workspace,
  );
}

async function pickWorkspaceFolder(): Promise<
  vscode.WorkspaceFolder | undefined
> {
  const folders = vscode.workspace.workspaceFolders;

  if (!folders || folders.length === 0) {
    vscode.window.showWarningMessage("Open a workspace before running Sqloom.");
    return undefined;
  }

  if (folders.length === 1) {
    return folders[0];
  }

  const selected = await vscode.window.showQuickPick(
    folders.map((folder) => ({
      label: folder.name,
      description: folder.uri.fsPath,
      folder,
    })),
    {
      title: "Sqloom Workspace",
      placeHolder: "Select the workspace folder for this Sqloom command.",
    },
  );

  return selected?.folder;
}

function getConfiguration(): vscode.WorkspaceConfiguration {
  return vscode.workspace.getConfiguration("sqloom");
}

function getDashboardWorkspaceFolder(): vscode.WorkspaceFolder | undefined {
  return vscode.workspace.workspaceFolders?.[0];
}

async function defaultHarnessPath(
  workspaceFolder: vscode.WorkspaceFolder,
): Promise<string> {
  const sampleHarnessUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(sampleAppHarnessPath),
  );

  try {
    await vscode.workspace.fs.stat(sampleHarnessUri);
    return sampleAppHarnessPath;
  } catch {
    return "";
  }
}

function splitPath(value: string): string[] {
  return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}

function redactArgs(args: readonly string[]): string[] {
  const sensitiveFlags = new Set([
    "--openai-api-key",
    "--read-only-connection-string",
  ]);
  const redacted: string[] = [];
  let redactNext = false;

  for (const arg of args) {
    if (redactNext) {
      redacted.push("<redacted>");
      redactNext = false;
      continue;
    }

    redacted.push(quoteArg(arg));
    if (sensitiveFlags.has(arg)) {
      redactNext = true;
    }
  }

  return redacted;
}

function formatLaunchFailureMessage(cliPath: string, error: unknown): string {
  if (isNodeError(error) && error.code === "ENOENT") {
    return `Sqloom CLI was not found at ${quoteArg(cliPath)}. Install the sqloom .NET tool or update sqloom.cli.path.`;
  }

  const message =
    error instanceof Error && error.message.length > 0
      ? error.message
      : "Could not start the Sqloom CLI.";

  return `Failed to start Sqloom: ${message}`;
}

function isNodeError(error: unknown): error is NodeJS.ErrnoException {
  return error instanceof Error && "code" in error;
}

function quoteArg(arg: string): string {
  if (/^[A-Za-z0-9._:/\\-]+$/.test(arg)) {
    return arg;
  }

  return JSON.stringify(arg);
}
