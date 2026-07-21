export type DashboardStatus = "ready" | "warning" | "neutral" | "idle";

export type DashboardStageId = "replay" | "observe" | "correlate" | "advise";

export type DashboardStageStatus =
  | "pending"
  | "active"
  | "completed"
  | "failed";

export type DashboardStage = {
  id: DashboardStageId;
  number: number;
  label: string;
  detail?: string;
  status: DashboardStageStatus;
};

export type DashboardSummaryItem = {
  label: string;
  detail: string;
  status: DashboardStatus;
};

export type DashboardCheck = {
  label: string;
  badge: string;
  detail: string;
  status: DashboardStatus;
  trailing?: string;
  action?: string;
};

export type DashboardConfigOption = {
  label: string;
  value: string;
};

export type DashboardConfigField = {
  id?: string;
  label: string;
  value: string;
  kind: "select" | "password" | "toggle" | "text";
  note?: string;
  status?: DashboardStatus;
  statusLabel?: string;
  editable?: boolean;
  placeholder?: string;
  options?: DashboardConfigOption[];
  required?: boolean;
};

export type DashboardSetupSummaryItem = {
  id: string;
  label: string;
  value: string;
  detail?: string;
  status: DashboardStatus;
  statusLabel?: string;
};

export type DashboardSetupField = {
  id: string;
  label: string;
  value: string;
  kind: "select" | "password" | "text";
  note?: string;
  status: DashboardStatus;
  placeholder?: string;
  options?: DashboardConfigOption[];
  required?: boolean;
  actionLabel?: string;
  secondaryValue?: string;
  readonly?: boolean;
};

export type DashboardStatusCheck = {
  id: string;
  label: string;
  detail: string;
  status: DashboardStatus;
};

export type DashboardArtifact = {
  name: string;
  description: string;
  type: string;
  typeTone: "markdown" | "json" | "sql" | "html";
  size: string;
  updated: string;
  summary: string;
};

export type DashboardState = {
  title: string;
  subtitle: string;
  runNote: string;
  readinessLabel: string;
  stages: DashboardStage[];
  setupSummaryItems: DashboardSetupSummaryItem[];
  setupFields: DashboardSetupField[];
  runStatusTitle: string;
  runStatusSubtitle: string;
  runStatusChecks: DashboardStatusCheck[];
  editStatusTitle: string;
  editStatusSubtitle: string;
  editStatusChecks: DashboardStatusCheck[];
  recentRunsTitle: string;
  recentRunsAction: string;
  recentRunsEmptyTitle: string;
  recentRunsEmptyDetail: string;
  summaryItems: DashboardSummaryItem[];
  configFields: DashboardConfigField[];
  readinessChecks: DashboardCheck[];
  artifacts: DashboardArtifact[];
};

export type DashboardTuneRequest = {
  cliPath?: string;
  harnessPath?: string;
  target?: string;
  modelProvider?: string;
  openAiModel?: string;
  openAiApiKey?: string;
  readOnlyConnectionString?: string;
};

export type ReplayEndpoint = {
  stableOperationKey: string;
  httpMethod: string;
  route: string;
  controllerType?: string;
  methodName?: string;
};

export type DashboardEndpointRequest = {
  cliPath?: string;
  harnessPath?: string;
};

export type DashboardEndpointResult =
  | {
      status: "loaded";
      endpoints: ReplayEndpoint[];
    }
  | {
      status: "failed";
      message: string;
    };

export type DashboardMessage =
  | {
      command: "runTune";
      payload?: DashboardTuneRequest;
    }
  | {
      command: "loadEndpoints";
      requestId: string;
      payload?: DashboardEndpointRequest;
    }
  | {
      command: "refreshChecks" | "createHarness" | "openDashboard";
    };

export type DashboardTuneProgressEvent = {
  stage: DashboardStageId;
  status: DashboardStageStatus;
  detail?: string;
};

export type DashboardTuneRunResult = {
  success: boolean;
  artifactDir: string;
};

export type DashboardHostMessage =
  | {
      command: "endpointsLoaded";
      requestId: string;
      endpoints: ReplayEndpoint[];
    }
  | {
      command: "endpointsFailed";
      requestId: string;
      message: string;
    }
  | {
      command: "tuneRunStarted";
      runId: string;
    }
  | {
      command: "tuneProgress";
      runId: string;
      stage: DashboardStageId;
      status: DashboardStageStatus;
      detail?: string;
    }
  | {
      command: "tuneRunFinished";
      runId: string;
      success: boolean;
      artifactDir?: string;
      message?: string;
    };
