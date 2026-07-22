import type {
  DashboardStageId,
  DashboardStageStatus,
  DashboardTuneProgressEvent,
} from "../sharedInterfaces/dashboard";

export const TUNE_STAGE_ORDER: readonly DashboardStageId[] = [
  "replay",
  "observe",
  "correlate",
  "advise",
];

export const STAGE_COMPLETION_ARTIFACTS: Record<DashboardStageId, string> = {
  replay: "replay/replay-summary.json",
  observe: "query-store-snapshot.json",
  correlate: "replay/query-store-correlation.json",
  advise: "replay/sql-tuning-proposal.sql",
};

export function getDashboardArtifactDir(runId: string): string {
  return `artifacts/sqloom/tune/dashboard-${runId}`;
}

export function normalizeArtifactRelativePath(value: string): string {
  return value.replace(/\\/g, "/").replace(/^\/+/, "");
}

export function isPathUnderRoot(rootPath: string, filePath: string): boolean {
  const root = normalizeArtifactRelativePath(rootPath).toLowerCase();
  const file = normalizeArtifactRelativePath(filePath).toLowerCase();
  return file === root || file.startsWith(`${root}/`);
}

export function getRelativePathUnderRoot(
  rootPath: string,
  filePath: string,
): string | undefined {
  if (!isPathUnderRoot(rootPath, filePath)) {
    return undefined;
  }

  const root = normalizeArtifactRelativePath(rootPath);
  const file = normalizeArtifactRelativePath(filePath);
  if (file.length === root.length) {
    return "";
  }

  return file.slice(root.length + 1);
}

export function getStageForArtifactRelativePath(
  relativePath: string,
): DashboardStageId | undefined {
  const normalized = normalizeArtifactRelativePath(relativePath);
  for (const stage of TUNE_STAGE_ORDER) {
    const artifactPath = STAGE_COMPLETION_ARTIFACTS[stage];
    if (normalized === artifactPath || normalized.endsWith(`/${artifactPath}`)) {
      return stage;
    }
  }

  return undefined;
}

export function getNextStage(
  stage: DashboardStageId,
): DashboardStageId | undefined {
  const index = TUNE_STAGE_ORDER.indexOf(stage);
  if (index < 0 || index >= TUNE_STAGE_ORDER.length - 1) {
    return undefined;
  }

  return TUNE_STAGE_ORDER[index + 1];
}

export class TuneRunProgressStateMachine {
  private readonly completedStages = new Set<DashboardStageId>();
  private readonly pendingCompletions = new Map<
    DashboardStageId,
    string | undefined
  >();
  private activeStage: DashboardStageId | null = null;
  private failed = false;

  start(onProgress: (event: DashboardTuneProgressEvent) => void): void {
    this.emitStage(onProgress, "replay", "active");
  }

  completeStage(
    stage: DashboardStageId,
    detail: string | undefined,
    onProgress: (event: DashboardTuneProgressEvent) => void,
  ): void {
    if (this.failed || this.completedStages.has(stage)) {
      return;
    }

    if (this.activeStage !== stage) {
      if (!this.pendingCompletions.has(stage)) {
        this.pendingCompletions.set(stage, detail);
      }
      return;
    }

    this.processStageCompletion(stage, detail, onProgress);
  }

  private processStageCompletion(
    stage: DashboardStageId,
    detail: string | undefined,
    onProgress: (event: DashboardTuneProgressEvent) => void,
  ): void {
    this.completedStages.add(stage);
    this.pendingCompletions.delete(stage);
    this.emitStage(onProgress, stage, "completed", detail);

    const nextStage = getNextStage(stage);
    if (!nextStage) {
      this.activeStage = null;
      return;
    }

    this.emitStage(onProgress, nextStage, "active");
    if (this.pendingCompletions.has(nextStage)) {
      this.processStageCompletion(
        nextStage,
        this.pendingCompletions.get(nextStage),
        onProgress,
      );
    }
  }

  finish(
    exitCode: number,
    onProgress: (event: DashboardTuneProgressEvent) => void,
  ): void {
    if (exitCode === 0 || this.failed) {
      return;
    }

    this.failed = true;
    const stageToFail = this.activeStage ?? "replay";
    this.emitStage(onProgress, stageToFail, "failed");
  }

  private emitStage(
    onProgress: (event: DashboardTuneProgressEvent) => void,
    stage: DashboardStageId,
    status: DashboardStageStatus,
    detail?: string,
  ): void {
    if (status === "active") {
      this.activeStage = stage;
    }

    onProgress({ stage, status, detail });
  }
}
