export type DashboardStatus = "ready" | "warning" | "neutral" | "idle";

export type DashboardStage = {
  number: number;
  label: string;
  detail?: string;
  active?: boolean;
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

export type DashboardArtifact = {
  label: string;
};

export type DashboardState = {
  title: string;
  subtitle: string;
  runNote: string;
  readinessLabel: string;
  stages: DashboardStage[];
  summaryItems: DashboardSummaryItem[];
  configFields: DashboardConfigField[];
  readinessChecks: DashboardCheck[];
  artifacts: DashboardArtifact[];
};

export type DashboardTuneRequest = {
  modelProvider?: string;
  openAiModel?: string;
  openAiApiKey?: string;
  readOnlyConnectionString?: string;
};

export type DashboardMessage = {
  command?: string;
  payload?: DashboardTuneRequest;
};
