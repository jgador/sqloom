import * as vscode from "vscode";
import { defaultOpenAiModel } from "../constants/modelOptions";
import { defaultReadOnlyConnectionStringForHarness } from "../constants/sampleAppDefaults";
import { CliService } from "./cliService";
import { getSqloomConfiguration } from "./sqloomConfigurationService";
import {
  defaultHarnessPath,
  pickWorkspaceFolder,
} from "./workspacePromptService";

/** Command Palette workflows that prompt in VS Code and delegate to the CLI. */
export class CommandWorkflowService {
  constructor(private readonly cliService: CliService) {}

  async runInit(): Promise<void> {
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

    await this.cliService.run(args, workspaceFolder);
  }

  async runTune(): Promise<void> {
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
      vscode.window.showWarningMessage(
        "Sqloom tune requires an OpenAI API key.",
      );
      return;
    }

    const configuration = getSqloomConfiguration();
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

    await this.cliService.run(args, workspaceFolder);
  }

  async selectCliPath(): Promise<void> {
    const configuration = getSqloomConfiguration();
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
}
