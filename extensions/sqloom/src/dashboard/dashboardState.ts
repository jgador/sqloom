import * as vscode from "vscode";
import {
  DashboardCheck,
  DashboardState,
  DashboardStatus,
} from "../sharedInterfaces/dashboard";

export async function createDashboardState(): Promise<DashboardState> {
  const configuration = getConfiguration();
  const workspaceFolder = getPrimaryWorkspaceFolder();
  const cliPath = configuration.get<string>("cli.path", "sqloom").trim();
  const effectiveCliPath = cliPath.length > 0 ? cliPath : "sqloom";
  const openAiModel = configuration
    .get<string>("openai.model", "gpt-5.4-mini")
    .trim();
  const replayDataAgent = configuration
    .get<string>("replayDataAgent", "required")
    .trim();
  const replayDataAgentReady = isReplayDataAgent(replayDataAgent);
  const openAiApiKeyReady =
    (process.env.OPENAI_API_KEY ?? "").trim().length > 0;
  const harnessPath = workspaceFolder
    ? await defaultHarnessPath(workspaceFolder)
    : "";
  const harnessReady = harnessPath.length > 0;

  const readinessChecks: DashboardCheck[] = [
    {
      label: "Workspace",
      badge: workspaceFolder ? "Open" : "Not open",
      detail: workspaceFolder
        ? workspaceFolder.uri.fsPath
        : "Open a folder before running Sqloom.",
      status: workspaceFolder ? "ready" : "warning",
      trailing: workspaceFolder?.name,
    },
    {
      label: "Sqloom CLI path",
      badge: cliPath.length > 0 ? "Configured" : "Default",
      detail:
        cliPath.length > 0
          ? effectiveCliPath
          : "Using sqloom until a path is configured.",
      status: cliPath.length > 0 ? "ready" : "warning",
    },
    {
      label: "OpenAI model",
      badge: openAiModel.length > 0 ? "Configured" : "Missing",
      detail:
        openAiModel.length > 0
          ? openAiModel
          : "Set sqloom.openai.model before running tune.",
      status: openAiModel.length > 0 ? "ready" : "warning",
    },
    {
      label: "Replay data agent",
      badge: replayDataAgentReady ? "Configured" : "Review",
      detail: replayDataAgentReady
        ? replayDataAgent
        : "Expected required, auto, or off.",
      status: replayDataAgentReady ? "ready" : "warning",
    },
    {
      label: "OpenAI API key",
      badge: openAiApiKeyReady ? "Environment" : "Prompt later",
      detail: openAiApiKeyReady
        ? "OPENAI_API_KEY is available to the extension host."
        : "The run command can prompt for this value later.",
      status: openAiApiKeyReady ? "ready" : "neutral",
    },
    {
      label: "Default harness",
      badge: harnessReady ? "Detected" : "Not detected",
      detail: harnessReady
        ? harnessPath
        : "Default harness path was not found in this workspace.",
      status: harnessReady ? "ready" : "neutral",
    },
  ];
  const readyCheckCount = readinessChecks.filter(
    (check) => check.status === "ready",
  ).length;
  const warningCheckCount = readinessChecks.filter(
    (check) => check.status === "warning",
  ).length;
  const readinessLabel = `${readyCheckCount} of ${readinessChecks.length} checks ready`;
  const configurationStatus: DashboardStatus =
    warningCheckCount > 0 ? "warning" : "ready";

  return {
    title: "Sqloom Tune",
    subtitle: "Tune SQL for performance with confidence.",
    readinessLabel,
    stages: [
      { number: 1, label: "Observe", active: true },
      { number: 2, label: "Replay" },
      { number: 3, label: "Capture", detail: "Capture SQL evidence" },
      { number: 4, label: "Correlate" },
      { number: 5, label: "Advise" },
    ],
    summaryItems: [
      {
        label: "Configuration",
        detail:
          warningCheckCount > 0
            ? `${warningCheckCount} value${warningCheckCount === 1 ? "" : "s"} to review`
            : "No blocking values found",
        status: configurationStatus,
      },
      {
        label: "Workspace",
        detail: workspaceFolder?.name ?? "No folder open",
        status: workspaceFolder ? "ready" : "warning",
      },
      {
        label: "Harness",
        detail: harnessReady ? "Default harness detected" : "Not detected",
        status: harnessReady ? "ready" : "neutral",
      },
      {
        label: "Last tune run",
        detail: "Never run",
        status: "idle",
      },
    ],
    configFields: [
      {
        label: "Workspace",
        value: workspaceFolder?.name ?? "No workspace open",
        kind: "text",
        note: workspaceFolder?.uri.fsPath ?? "Open a folder before running tune.",
        status: workspaceFolder ? "ready" : "warning",
        statusLabel: workspaceFolder ? "Ready" : "Needs folder",
      },
      {
        label: "Sqloom CLI path",
        value: effectiveCliPath,
        kind: "text",
        note:
          cliPath.length > 0
            ? "Read from sqloom.cli.path."
            : "Defaulting to sqloom until a path is configured.",
        status: cliPath.length > 0 ? "ready" : "warning",
        statusLabel: cliPath.length > 0 ? "Configured" : "Default",
      },
      {
        label: "OpenAI advice model",
        value: openAiModel.length > 0 ? openAiModel : "Not configured",
        kind: "text",
        note: "Read from sqloom.openai.model.",
        status: openAiModel.length > 0 ? "ready" : "warning",
        statusLabel: openAiModel.length > 0 ? "Configured" : "Review",
      },
      {
        label: "Replay data agent",
        value: replayDataAgent.length > 0 ? replayDataAgent : "Not configured",
        kind: "text",
        note: "Allowed values are required, auto, and off.",
        status: replayDataAgentReady ? "ready" : "warning",
        statusLabel: replayDataAgentReady ? "Configured" : "Review",
      },
      {
        label: "OpenAI API key",
        value: openAiApiKeyReady
          ? "Available in environment"
          : "Prompt when running",
        kind: "text",
        note: "Presence only; the extension never stores this value.",
        status: openAiApiKeyReady ? "ready" : "neutral",
        statusLabel: openAiApiKeyReady ? "Available" : "Prompt later",
      },
      {
        label: "Default harness",
        value: harnessReady ? harnessPath : "Not detected",
        kind: "text",
        note: "Checks tests/Sqloom/Sqloom.TestApp/default/Harness.cs.",
        status: harnessReady ? "ready" : "neutral",
        statusLabel: harnessReady ? "Detected" : "Not detected",
      },
      {
        label: "Read-only connection string",
        value: "Prompt when running",
        kind: "text",
        note: "Uses the harness session connection when omitted.",
        status: "neutral",
        statusLabel: "Optional",
      },
    ],
    readinessChecks,
    artifacts: [
      { label: "SQL tuning proposal" },
      { label: "tune-summary.json" },
      { label: "query-store-snapshot.json" },
    ],
  };
}

function getConfiguration(): vscode.WorkspaceConfiguration {
  return vscode.workspace.getConfiguration("sqloom");
}

function getPrimaryWorkspaceFolder(): vscode.WorkspaceFolder | undefined {
  return vscode.workspace.workspaceFolders?.[0];
}

async function defaultHarnessPath(
  workspaceFolder: vscode.WorkspaceFolder,
): Promise<string> {
  const sampleHarness = "tests/Sqloom/Sqloom.TestApp/default/Harness.cs";
  const sampleHarnessUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(sampleHarness),
  );

  return (await pathExists(sampleHarnessUri)) ? sampleHarness : "";
}

async function pathExists(uri: vscode.Uri): Promise<boolean> {
  try {
    await vscode.workspace.fs.stat(uri);
    return true;
  } catch {
    return false;
  }
}

function splitPath(value: string): string[] {
  return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}

function isReplayDataAgent(value: string): boolean {
  return value === "required" || value === "auto" || value === "off";
}
