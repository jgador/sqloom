import type { DashboardRecentRun } from "../sharedInterfaces/dashboard";
import type { TuneRunRecord } from "./recentRunsStore";

const minuteMs = 60_000;
const hourMs = 60 * minuteMs;
const dayMs = 24 * hourMs;

export function formatRelativeTime(
  isoTimestamp: string,
  nowMs: number = Date.now(),
): string {
  const timestampMs = Date.parse(isoTimestamp);
  if (Number.isNaN(timestampMs)) {
    return "Unknown time";
  }

  const elapsedMs = Math.max(0, nowMs - timestampMs);
  if (elapsedMs < minuteMs) {
    return "Just now";
  }

  const minutes = Math.floor(elapsedMs / minuteMs);
  if (minutes < 60) {
    return `${minutes}m ago`;
  }

  const hours = Math.floor(elapsedMs / hourMs);
  if (hours < 24) {
    return `${hours}h ago`;
  }

  const days = Math.floor(elapsedMs / dayMs);
  if (days < 7) {
    return `${days}d ago`;
  }

  return new Date(timestampMs).toLocaleString();
}

export function toDashboardRecentRun(
  record: TuneRunRecord,
  nowMs: number = Date.now(),
): DashboardRecentRun {
  return {
    id: record.id,
    target: record.target,
    status: record.success ? "completed" : "failed",
    statusLabel: record.success ? "Completed" : "Failed",
    startedRelative: formatRelativeTime(record.finishedAtUtc, nowMs),
    artifactDir: record.artifactDir,
  };
}

export function formatLastTuneRunSummary(
  records: readonly TuneRunRecord[],
  nowMs: number = Date.now(),
): { detail: string; status: "ready" | "warning" | "idle" } {
  const latest = records[0];
  if (!latest) {
    return { detail: "Never run", status: "idle" };
  }

  const relative = formatRelativeTime(latest.finishedAtUtc, nowMs);
  if (latest.success) {
    return {
      detail: `${relative} · ${latest.target}`,
      status: "ready",
    };
  }

  return {
    detail: `${relative} · failed · ${latest.target}`,
    status: "warning",
  };
}
