import * as vscode from "vscode";
import { checkCliAvailability } from "../cli/cliStatus";
import {
  defaultOpenAiModel,
  isKnownOpenAiModel,
  modelProviderOptions,
  openAiModelOptions,
} from "../constants/modelOptions";
import {
  defaultReadOnlyConnectionStringForHarness,
  sampleAppHarnessPath,
} from "../constants/sampleAppDefaults";
import {
  DashboardCheck,
  DashboardConfigField,
  DashboardRecentRun,
  DashboardSetupField,
  DashboardSetupSummaryItem,
  DashboardState,
  DashboardStatus,
  DashboardStatusCheck,
} from "../sharedInterfaces/dashboard";
import {
  formatLastTuneRunSummary,
  toDashboardRecentRun,
} from "../recentRuns/formatRecentRun";
import type { TuneRunRecord } from "../recentRuns/recentRunsStore";

/** Builds the initial dashboard view model from workspace settings and recent runs. */
export async function createDashboardState(
  recentRuns: readonly TuneRunRecord[] = [],
): Promise<DashboardState> {
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
  const [cliStatus, harnessPath] = await Promise.all([
    checkCliAvailability(effectiveCliPath),
    workspaceFolder ? defaultHarnessPath(workspaceFolder) : Promise.resolve(""),
  ]);
  const harnessReady = harnessPath.length > 0;
  const readOnlyConnectionString =
    defaultReadOnlyConnectionStringForHarness(harnessPath);
  const workspaceReady = workspaceFolder !== undefined;
  const cliReady = cliStatus.ready;
  const cliStatusBadge = cliReady
    ? "Verified"
    : cliPath.length > 0
      ? "Unavailable"
      : "Default missing";
  const cliStatusDetail = `${cliStatus.cliPath}: ${cliStatus.detail}`;
  const modelReady = openAiModel.length > 0;
  const readOnlyConnectionStringReady = readOnlyConnectionString.length > 0;
  const setupReady =
    workspaceReady &&
    cliReady &&
    modelReady &&
    openAiApiKeyEnvironmentReady &&
    harnessReady &&
    replayDataAgentReady &&
    readOnlyConnectionStringReady;

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
      badge: cliStatusBadge,
      detail: cliStatusDetail,
      status: cliReady ? "ready" : "warning",
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
      status: openAiApiKeyEnvironmentReady ? "ready" : "warning",
    },
    {
      label: "Default harness",
      badge: harnessReady ? "Detected" : "Not detected",
      detail: harnessReady
        ? harnessPath
        : "Default harness path was not found in this workspace.",
      status: harnessReady ? "ready" : "neutral",
    },
    {
      label: "Read-only SQL connection",
      badge: readOnlyConnectionStringReady ? "Sample default" : "Required",
      detail: readOnlyConnectionStringReady
        ? "Prefilled for the detected Sqloom test app."
        : "Enter a masked read-only SQL Server connection string before running tune.",
      status: readOnlyConnectionStringReady ? "ready" : "warning",
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
  const workspaceName = workspaceFolder?.name ?? "No workspace";
  const workspacePath =
    workspaceFolder?.uri.fsPath ?? "Open a folder before running tune.";
  const setupSummaryItems: DashboardSetupSummaryItem[] = [
    {
      id: "workspace",
      label: "Workspace",
      value: workspaceName,
      detail: workspacePath,
      status: workspaceReady ? "ready" : "warning",
    },
    {
      id: "cliPath",
      label: "CLI",
      value: effectiveCliPath,
      detail: cliStatus.detail,
      status: cliReady ? "ready" : "warning",
    },
    {
      id: "openAiModel",
      label: "Model",
      value: openAiModel,
      detail: "Model used for tuning.",
      status: modelReady ? "ready" : "warning",
    },
    {
      id: "harnessPath",
      label: "Harness",
      value: harnessReady ? "Default harness" : "Not detected",
      detail: harnessReady
        ? harnessPath
        : "Default harness path was not found.",
      status: harnessReady ? "ready" : "warning",
    },
    {
      id: "openAiApiKey",
      label: "API key",
      value: openAiApiKeyEnvironmentReady ? "Configured" : "Required",
      detail: openAiApiKeyEnvironmentReady
        ? "Prefilled for this session."
        : "Enter before running tune.",
      status: openAiApiKeyEnvironmentReady ? "ready" : "warning",
    },
    {
      id: "readOnlyConnectionString",
      label: "SQL connection",
      value: readOnlyConnectionStringReady ? "Configured" : "Required",
      detail: readOnlyConnectionStringReady
        ? "Prefilled for the detected test app."
        : "Enter before running tune.",
      status: readOnlyConnectionStringReady ? "ready" : "warning",
    },
  ];
  const setupFields: DashboardSetupField[] = [
    {
      id: "workspace",
      label: "Workspace",
      value: workspaceReady ? workspaceName : "",
      kind: "text",
      placeholder: "No workspace",
      secondaryValue: workspacePath,
      note: workspaceReady
        ? "Workspace used as the Sqloom command directory."
        : "Open a folder before running tune.",
      status: workspaceReady ? "ready" : "warning",
      readonly: true,
    },
    {
      id: "cliPath",
      label: "CLI",
      value: effectiveCliPath,
      kind: "text",
      note: cliStatus.detail,
      status: cliReady ? "ready" : "warning",
      required: true,
    },
    {
      id: "openAiModel",
      label: "Model",
      value: openAiModel,
      kind: "select",
      options: [...openAiModelOptions],
      note: "Model used for tuning.",
      status: modelReady ? "ready" : "warning",
      required: true,
    },
    {
      id: "harnessPath",
      label: "Harness",
      value: harnessReady ? harnessPath : "",
      kind: "text",
      placeholder: "tests/Sqloom/Sqloom.TestApp/default/Harness.cs",
      actionLabel: "Detect",
      note: harnessReady
        ? "Auto-detected from test project."
        : "Default harness path was not found.",
      status: harnessReady ? "ready" : "warning",
      required: true,
    },
    {
      id: "openAiApiKey",
      label: "API key",
      value: openAiApiKey,
      kind: "password",
      placeholder: openAiApiKeyEnvironmentReady
        ? "Prefilled from OPENAI_API_KEY"
        : "Enter API key for this run",
      note: openAiApiKeyEnvironmentReady
        ? "Masked by default. Edit to override OPENAI_API_KEY for this run."
        : "Masked and passed only to the current dashboard run.",
      status: openAiApiKeyEnvironmentReady ? "ready" : "warning",
      required: true,
    },
    {
      id: "readOnlyConnectionString",
      label: "SQL connection",
      value: readOnlyConnectionString,
      kind: "password",
      placeholder: "Read-only SQL Server connection string",
      note: readOnlyConnectionStringReady
        ? "Prefilled for the test app, masked, and passed only to this dashboard run."
        : "Masked and passed only to the current dashboard run.",
      status: readOnlyConnectionStringReady ? "ready" : "warning",
      required: true,
    },
  ];
  const runStatusChecks: DashboardStatusCheck[] = [
    {
      id: "setup",
      label: setupReady ? "Run setup is valid" : "Run setup needs attention",
      detail: setupReady
        ? "All required settings are configured."
        : "Review required values before running.",
      status: setupReady ? "ready" : "warning",
    },
    {
      id: "harness",
      label: harnessReady ? "Harness detected" : "Harness not detected",
      detail: harnessReady
        ? harnessPath
        : "Default harness path was not found.",
      status: harnessReady ? "ready" : "warning",
    },
    {
      id: "preflight",
      label:
        warningCheckCount === 0
          ? "Preflight checks passed"
          : "Preflight checks need review",
      detail: `${readyCheckCount} of ${readinessChecks.length} checks passed`,
      status: warningCheckCount === 0 ? "ready" : "warning",
    },
  ];
  const editStatusChecks: DashboardStatusCheck[] = [
    {
      id: "required",
      label: setupReady ? "Required values present" : "Required values missing",
      detail: setupReady
        ? "All required fields are filled."
        : "Workspace, CLI, model, harness, API key, and SQL connection are required.",
      status: setupReady ? "ready" : "warning",
    },
    {
      id: "harness",
      label: harnessReady ? "Harness detected" : "Harness not detected",
      detail: harnessReady
        ? harnessPath
        : "Default harness path was not found.",
      status: harnessReady ? "ready" : "warning",
    },
    {
      id: "ready",
      label: setupReady ? "Ready to apply" : "Review before applying",
      detail: setupReady
        ? "Setup is valid and ready to run."
        : "Apply after the required values are present.",
      status: setupReady ? "ready" : "warning",
    },
  ];
  const legacyConfigFields: DashboardConfigField[] = [
    {
      label: "Workspace",
      value: workspaceName,
      kind: "text",
      note: workspacePath,
      status: workspaceReady ? "ready" : "warning",
      statusLabel: workspaceReady ? "Ready" : "Needs folder",
    },
    {
      label: "Sqloom CLI path",
      value: effectiveCliPath,
      kind: "text",
      note: cliStatus.detail,
      status: cliReady ? "ready" : "warning",
      statusLabel: cliReady ? "Verified" : "Unavailable",
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
      note: "Choose the model for this dashboard run.",
      status: "ready",
      statusLabel: "Selectable",
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
      note: "Masked and passed only to the current dashboard run.",
      status: openAiApiKeyEnvironmentReady ? "ready" : "warning",
      statusLabel: openAiApiKeyEnvironmentReady ? "Environment" : "Required",
      required: !openAiApiKeyEnvironmentReady,
    },
    {
      id: "readOnlyConnectionString",
      label: "Read-only connection string",
      value: readOnlyConnectionString,
      kind: "password",
      editable: true,
      placeholder: "Read-only SQL Server connection string",
      note: readOnlyConnectionStringReady
        ? "Prefilled for the test app. Masked and never saved."
        : "Required for tune runs. Masked and never saved.",
      status: readOnlyConnectionStringReady ? "ready" : "warning",
      statusLabel: readOnlyConnectionStringReady
        ? "Sample default"
        : "Required",
      required: true,
    },
  ];

  const lastTuneRunSummary = formatLastTuneRunSummary(recentRuns);
  const dashboardRecentRuns: DashboardRecentRun[] = recentRuns.map((record) =>
    toDashboardRecentRun(record),
  );

  return {
    title: "Sqloom Tune",
    subtitle: "Tune SQL for performance with confidence.",
    runNote: harnessReady
      ? "Uses the detected default harness"
      : "Default harness required to run",
    readinessLabel,
    stages: [
      {
        id: "replay",
        number: 1,
        label: "Replay",
        detail: "Captures SQL evidence during replay",
        status: "pending",
      },
      {
        id: "observe",
        number: 2,
        label: "Observe",
        status: "pending",
      },
      {
        id: "correlate",
        number: 3,
        label: "Correlate",
        status: "pending",
      },
      {
        id: "advise",
        number: 4,
        label: "Advise",
        status: "pending",
      },
    ],
    setupSummaryItems,
    setupFields,
    runStatusTitle: "Run tune",
    runStatusSubtitle: setupReady
      ? "Everything looks good. You're ready to go."
      : "Review setup before running tune.",
    runStatusChecks,
    editStatusTitle: "Edit setup status",
    editStatusSubtitle: setupReady
      ? "Everything looks good."
      : "Required values need attention.",
    editStatusChecks,
    recentRunsTitle: "Recent runs",
    recentRunsAction: "View all",
    recentRunsEmptyTitle: "No runs yet",
    recentRunsEmptyDetail:
      "Your recent tune runs will appear here. Run a tune to get started.",
    recentRuns: dashboardRecentRuns,
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
        detail: lastTuneRunSummary.detail,
        status: lastTuneRunSummary.status,
      },
    ],
    configFields: legacyConfigFields,
    readinessChecks,
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
  const sampleHarnessUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(sampleAppHarnessPath),
  );

  return (await pathExists(sampleHarnessUri)) ? sampleAppHarnessPath : "";
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
