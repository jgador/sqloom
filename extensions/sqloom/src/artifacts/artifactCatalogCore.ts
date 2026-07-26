import { formatRelativeTime } from "../recentRuns/formatRecentRun";
import type { DashboardArtifact } from "../sharedInterfaces/dashboard";
import { getArtifactMetadata, normalizeArtifactCatalogPath } from "./artifactMetadata";

/** Workspace-relative artifact file discovered under a tune run directory. */
export type ScannedArtifactFile = {
  artifactRelativePath: string;
  workspaceRelativePath: string;
  sizeBytes: number;
  modifiedAtUtc: string;
};

export function mapScannedFilesToArtifacts(
  files: readonly ScannedArtifactFile[],
  nowMs: number = Date.now(),
): DashboardArtifact[] {
  return [...files]
    .map((file) => toDashboardArtifact(file, nowMs))
    .sort(compareArtifacts);
}

function toDashboardArtifact(
  file: ScannedArtifactFile,
  nowMs: number,
): DashboardArtifact {
  const metadata = getArtifactMetadata(file.artifactRelativePath);
  const name = file.artifactRelativePath.split("/").at(-1) ?? file.artifactRelativePath;

  return {
    name,
    description: metadata.description,
    type: metadata.type,
    typeTone: metadata.typeTone,
    size: formatFileSize(file.sizeBytes),
    updated: formatRelativeTime(file.modifiedAtUtc, nowMs),
    summary: metadata.summary,
    relativePath: file.workspaceRelativePath,
    sortOrder: metadata.sortOrder,
    stage: metadata.stage,
  };
}

function compareArtifacts(left: DashboardArtifact, right: DashboardArtifact): number {
  const sortOrderDelta = left.sortOrder - right.sortOrder;
  if (sortOrderDelta !== 0) {
    return sortOrderDelta;
  }

  return left.relativePath.localeCompare(right.relativePath);
}

export function formatFileSize(sizeBytes: number): string {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`;
  }

  if (sizeBytes < 1024 * 1024) {
    return `${Math.round(sizeBytes / 1024)} KB`;
  }

  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function buildArtifactSortKey(relativePath: string): number {
  return getArtifactMetadata(relativePath).sortOrder;
}

export function isKnownArtifactPath(relativePath: string): boolean {
  const normalized = normalizeArtifactCatalogPath(relativePath);
  return normalized.length > 0;
}
