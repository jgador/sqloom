import * as vscode from "vscode";
import {
  defaultOpenAiModel,
  isKnownOpenAiModel,
  modelProviderOptions,
  openAiModelOptions,
} from "../constants/modelOptions";
import {
  DashboardCheck,
  DashboardConfigField,
  DashboardSetupField,
  DashboardSetupSummaryItem,
  DashboardState,
  DashboardStatus,
  DashboardStatusCheck,
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
  const workspaceReady = workspaceFolder !== undefined;
  const cliReady = effectiveCliPath.length > 0;
  const modelReady = openAiModel.length > 0;
  const setupReady =
    workspaceReady &&
    cliReady &&
    modelReady &&
    openAiApiKeyEnvironmentReady &&
    harnessReady &&
    replayDataAgentReady;

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
      detail: "Executable or alias in PATH.",
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
      detail: harnessReady ? harnessPath : "Default harness path was not found.",
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
      value: "Optional",
      detail: "Used only for live validation.",
      status: "neutral",
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
      note: "CLI executable or alias in PATH.",
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
      label: "SQL connection (optional)",
      value: "",
      kind: "password",
      placeholder: "Optional read-only SQL Server connection string",
      note: "Optional connection string for live validation.",
      status: "neutral",
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
      detail: harnessReady ? harnessPath : "Default harness path was not found.",
      status: harnessReady ? "ready" : "warning",
    },
    {
      id: "preflight",
      label: warningCheckCount === 0 ? "Preflight checks passed" : "Preflight checks need review",
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
        : "Workspace, CLI, model, harness, and API key are required.",
      status: setupReady ? "ready" : "warning",
    },
    {
      id: "harness",
      label: harnessReady ? "Harness detected" : "Harness not detected",
      detail: harnessReady ? harnessPath : "Default harness path was not found.",
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
      note: "CLI executable or alias in PATH.",
      status: cliReady ? "ready" : "warning",
      statusLabel: cliReady ? "Configured" : "Required",
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
      value: "",
      kind: "password",
      editable: true,
      placeholder: "Optional read-only SQL Server connection string",
      note: "Optional. Masked and never saved.",
      status: "neutral",
      statusLabel: "Optional",
    },
  ];

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
    configFields: legacyConfigFields,
    readinessChecks,
    artifacts: [
      {
        name: "SQL tuning proposal",
        description: "AI-generated tuning recommendations",
        type: "Markdown",
        typeTone: "markdown",
        size: "128 KB",
        updated: "2m ago",
        summary:
          "Prioritized recommendations with rationale and expected impact.",
      },
      {
        name: "tune-summary.json",
        description: "Summary of tuning run and key metrics",
        type: "JSON",
        typeTone: "json",
        size: "36 KB",
        updated: "2m ago",
        summary: "Run configuration, metrics, and high-level outcomes.",
      },
      {
        name: "query-store-snapshot.json",
        description: "Captured query store snapshot",
        type: "JSON",
        typeTone: "json",
        size: "212 KB",
        updated: "2m ago",
        summary: "Query store data used for analysis and tuning.",
      },
      {
        name: "recommended-changes.sql",
        description: "Suggested SQL and index changes",
        type: "SQL",
        typeTone: "sql",
        size: "96 KB",
        updated: "2m ago",
        summary: "T-SQL scripts with suggested indexes and rewrites.",
      },
      {
        name: "impact-estimates.json",
        description: "Estimated impact of recommendations",
        type: "JSON",
        typeTone: "json",
        size: "48 KB",
        updated: "2m ago",
        summary: "Estimated gains, confidence scores, and trade-offs.",
      },
      {
        name: "execution-plan-diffs.html",
        description: "Before/after execution plan comparisons",
        type: "HTML",
        typeTone: "html",
        size: "320 KB",
        updated: "2m ago",
        summary: "Visual plan diffs highlighting improvements.",
      },
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
