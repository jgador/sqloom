import { spawn } from "node:child_process";
import * as vscode from "vscode";
import {
  defaultOpenAiModel,
  isKnownModelProvider,
  isKnownOpenAiModel,
} from "./constants/modelOptions";
import { openDashboard, registerDashboardLauncher } from "./dashboard";
import { DashboardTuneRequest } from "./sharedInterfaces/dashboard";

const outputChannel = vscode.window.createOutputChannel("Sqloom");

export function activate(context: vscode.ExtensionContext) {
  const cliService = new CliService();
  const runDashboardTune = (request: DashboardTuneRequest) =>
    runTuneFromDashboard(cliService, request);

  context.subscriptions.push(
    outputChannel,
    registerDashboardLauncher(context, runDashboardTune),
    vscode.commands.registerCommand("sqloom.openDashboard", () =>
      openDashboard(context, runDashboardTune),
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
  ): Promise<boolean> {
    const cliPath =
      getConfiguration().get<string>("cli.path", "sqloom").trim() || "sqloom";
    const redactedCommand = [cliPath, ...redactArgs(args)].join(" ");

    outputChannel.show(true);
    outputChannel.appendLine(`> ${redactedCommand}`);
    outputChannel.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
    outputChannel.appendLine("");

    const exitCode = await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: `Sqloom ${args[0]}`,
      },
      () =>
        new Promise<number>((resolve) => {
          const child = spawn(cliPath, args, {
            cwd: workspaceFolder.uri.fsPath,
            env: process.env,
            shell: false,
          });

          child.stdout.on("data", (chunk: Buffer) => {
            outputChannel.append(chunk.toString());
          });

          child.stderr.on("data", (chunk: Buffer) => {
            outputChannel.append(chunk.toString());
          });

          child.on("error", (error) => {
            outputChannel.appendLine("");
            outputChannel.appendLine(
              `Failed to start Sqloom: ${error.message}`,
            );
            resolve(-1);
          });

          child.on("close", (code) => {
            outputChannel.appendLine("");
            outputChannel.appendLine(`Sqloom exited with code ${code ?? -1}.`);
            resolve(code ?? -1);
          });
        }),
    );

    if (exitCode === 0) {
      vscode.window.showInformationMessage("Sqloom command completed.");
      return true;
    }

    vscode.window.showErrorMessage(
      "Sqloom command failed. See the Sqloom output channel.",
    );
    return false;
  }
}

async function runTuneFromDashboard(
  cliService: CliService,
  request: DashboardTuneRequest,
): Promise<void> {
  const workspaceFolder = await pickWorkspaceFolder();
  if (!workspaceFolder) {
    return;
  }

  const harnessPath = await defaultHarnessPath(workspaceFolder);
  if (harnessPath.length === 0) {
    vscode.window.showWarningMessage(
      "Sqloom dashboard tune requires the default harness path in this workspace.",
    );
    return;
  }

  const modelProvider = (request.modelProvider ?? "").trim();
  if (!isKnownModelProvider(modelProvider)) {
    vscode.window.showWarningMessage(
      "Sqloom dashboard tune requires the OpenAI model provider.",
    );
    return;
  }

  const openAiModel = (request.openAiModel ?? "").trim();
  if (!isKnownOpenAiModel(openAiModel)) {
    vscode.window.showWarningMessage(
      "Select an OpenAI model before running Sqloom tune.",
    );
    return;
  }

  const openAiApiKey =
    (request.openAiApiKey ?? "").trim() ||
    (process.env.OPENAI_API_KEY ?? "").trim();
  if (openAiApiKey.length === 0) {
    vscode.window.showWarningMessage(
      "Enter an OpenAI API key or set OPENAI_API_KEY before running Sqloom tune from the dashboard.",
    );
    return;
  }

  const replayDataAgent = getConfiguration().get<string>(
    "replayDataAgent",
    "required",
  );
  const readOnlyConnectionString = (
    request.readOnlyConnectionString ?? ""
  ).trim();
  const args = [
    "tune",
    harnessPath,
    "--model-provider",
    modelProvider,
    "--openai-api-key",
    openAiApiKey,
    "--openai-model",
    openAiModel,
    "--replay-data-agent",
    replayDataAgent,
  ];

  if (readOnlyConnectionString.length > 0) {
    args.push("--read-only-connection-string", readOnlyConnectionString);
  }

  await cliService.run(args, workspaceFolder);
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
      "Optional. Used for Query Store reads and schema export when the harness does not provide one.",
    password: true,
    ignoreFocusOut: true,
  });
  if (readOnlyConnectionString === undefined) {
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
  ];

  if (target.trim().length > 0) {
    args.push("--target", target.trim());
  }

  if (readOnlyConnectionString.trim().length > 0) {
    args.push("--read-only-connection-string", readOnlyConnectionString.trim());
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

async function defaultHarnessPath(
  workspaceFolder: vscode.WorkspaceFolder,
): Promise<string> {
  const sampleHarness = "tests/Sqloom/Sqloom.TestApp/default/Harness.cs";
  const sampleHarnessUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(sampleHarness),
  );

  try {
    await vscode.workspace.fs.stat(sampleHarnessUri);
    return sampleHarness;
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

function quoteArg(arg: string): string {
  if (/^[A-Za-z0-9._:/\\-]+$/.test(arg)) {
    return arg;
  }

  return JSON.stringify(arg);
}
