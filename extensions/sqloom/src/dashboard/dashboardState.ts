import * as vscode from "vscode";
import {
  defaultOpenAiModel,
  isKnownOpenAiModel,
  modelProviderOptions,
  openAiModelOptions,
} from "../constants/modelOptions";
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
  const configuredOpenAiModel = configuration
    .get<string>("openai.model", defaultOpenAiModel)
    .trim();
  const openAiModel = isKnownOpenAiModel(configuredOpenAiModel)
    ? configuredOpenAiModel
    : defaultOpenAiModel;
  const replayDataAgent = configuration
    .get<string>("replayDataAgent", "required")
    .trim();
  const replayDataAgentReady = isReplayDataAgent(replayDataAgent);
  const openAiApiKey = (process.env.OPENAI_API_KEY ?? "").trim();
  const openAiApiKeyEnvironmentReady = openAiApiKey.length > 0;
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
      badge: "Selectable",
      detail: openAiModel,
      status: "ready",
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
      badge: openAiApiKeyEnvironmentReady ? "Environment" : "Dashboard input",
      detail: openAiApiKeyEnvironmentReady
        ? "OPENAI_API_KEY is prefilled for dashboard tune runs."
        : "Enter a masked value before running tune from the dashboard.",
      status: openAiApiKeyEnvironmentReady ? "ready" : "neutral",
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
    runNote: harnessReady
      ? "Uses the detected default harness"
      : "Default harness required to run",
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
        id: "modelProvider",
        label: "Model provider",
        value: "openai",
        kind: "select",
        editable: true,
        options: [...modelProviderOptions],
        note: "Only OpenAI is available in this preview.",
        status: "ready",
        statusLabel: "OpenAI",
      },
      {
        id: "openAiModel",
        label: "OpenAI advice model",
        value: openAiModel,
        kind: "select",
        editable: true,
        options: [...openAiModelOptions],
        note: isKnownOpenAiModel(configuredOpenAiModel)
          ? "Choose the model for this dashboard run."
          : "Unknown configured model ignored for the dashboard dropdown.",
        status: "ready",
        statusLabel: "Selectable",
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
        id: "openAiApiKey",
        label: "OpenAI API key",
        value: openAiApiKey,
        kind: "password",
        editable: true,
        placeholder: openAiApiKeyEnvironmentReady
          ? "Prefilled from OPENAI_API_KEY"
          : "Enter API key for this run",
        note: openAiApiKeyEnvironmentReady
          ? "Masked by default. Edit this value to override OPENAI_API_KEY for this run."
          : "Masked and passed only to the current dashboard run.",
        status: openAiApiKeyEnvironmentReady ? "ready" : "warning",
        statusLabel: openAiApiKeyEnvironmentReady
          ? "Environment"
          : "Required",
        required: !openAiApiKeyEnvironmentReady,
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
        id: "readOnlyConnectionString",
        label: "Read-only connection string",
        value: "",
        kind: "password",
        editable: true,
        placeholder: "Optional read-only SQL Server connection string",
        note: "Optional. Masked and never saved.",
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
