import type * as vscode from "vscode";
import type { DashboardRecentRun } from "../sharedInterfaces/dashboard";
import {
  formatLastTuneRunSummary,
  toDashboardRecentRun,
} from "./formatRecentRun";

const workspaceStateKey = "sqloom.dashboard.recentTuneRuns";
const maxRecentRuns = 10;

export type TuneRunRecord = {
  id: string;
  startedAtUtc: string;
  finishedAtUtc: string;
  success: boolean;
  target: string;
  artifactDir: string;
  workspaceFolderUri: string;
  openAiModel: string;
  source: "dashboard";
};

export type TuneRunRecordInput = Omit<
  TuneRunRecord,
  "startedAtUtc" | "finishedAtUtc" | "source"
> & {
  startedAtUtc: string;
  finishedAtUtc?: string;
};

/** Persists recent dashboard tune runs in workspace state for the artifacts panel. */
export class RecentRunsStore {
  constructor(private readonly workspaceState: vscode.Memento) {}

  list(): TuneRunRecord[] {
    const stored = this.workspaceState.get<unknown>(workspaceStateKey, []);
    if (!Array.isArray(stored)) {
      return [];
    }

    return stored
      .map(normalizeRecord)
      .filter((record): record is TuneRunRecord => record !== undefined);
  }

  listForDashboard(nowMs: number = Date.now()): DashboardRecentRun[] {
    return this.list().map((record) => toDashboardRecentRun(record, nowMs));
  }

  getLastTuneRunSummary(nowMs: number = Date.now()): {
    detail: string;
    status: "ready" | "warning" | "idle";
  } {
    return formatLastTuneRunSummary(this.list(), nowMs);
  }

  async record(input: TuneRunRecordInput): Promise<TuneRunRecord> {
    const record: TuneRunRecord = {
      ...input,
      finishedAtUtc: input.finishedAtUtc ?? new Date().toISOString(),
      source: "dashboard",
    };
    const nextRuns = [
      record,
      ...this.list().filter((item) => item.id !== record.id),
    ]
      .sort(
        (left, right) =>
          Date.parse(right.finishedAtUtc) - Date.parse(left.finishedAtUtc),
      )
      .slice(0, maxRecentRuns);
    await this.workspaceState.update(workspaceStateKey, nextRuns);
    return record;
  }
}

function normalizeRecord(value: unknown): TuneRunRecord | undefined {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  const record = value as Partial<TuneRunRecord>;
  if (
    typeof record.id !== "string" ||
    typeof record.startedAtUtc !== "string" ||
    typeof record.finishedAtUtc !== "string" ||
    typeof record.success !== "boolean" ||
    typeof record.target !== "string" ||
    typeof record.artifactDir !== "string" ||
    typeof record.openAiModel !== "string" ||
    record.source !== "dashboard"
  ) {
    return undefined;
  }

  return {
    id: record.id,
    startedAtUtc: record.startedAtUtc,
    finishedAtUtc: record.finishedAtUtc,
    success: record.success,
    target: record.target,
    artifactDir: record.artifactDir,
    workspaceFolderUri:
      typeof record.workspaceFolderUri === "string"
        ? record.workspaceFolderUri
        : "",
    openAiModel: record.openAiModel,
    source: "dashboard",
  };
}
