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

export type DashboardConfigField = {
  label: string;
  value: string;
  kind: "select" | "password" | "toggle" | "text";
  note?: string;
  status?: DashboardStatus;
  statusLabel?: string;
};

export type DashboardArtifact = {
  label: string;
};

export type DashboardState = {
  title: string;
  subtitle: string;
  readinessLabel: string;
  stages: DashboardStage[];
  summaryItems: DashboardSummaryItem[];
  configFields: DashboardConfigField[];
  readinessChecks: DashboardCheck[];
  artifacts: DashboardArtifact[];
};

export type DashboardMessage = {
  command?: string;
};
