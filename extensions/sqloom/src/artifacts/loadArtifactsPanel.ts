import * as vscode from "vscode";
import type { DashboardArtifactsPanelState } from "../sharedInterfaces/dashboard";
import type { TuneRunRecord } from "../recentRuns/recentRunsStore";
import {
  mapScannedFilesToArtifacts,
  type ScannedArtifactFile,
} from "./artifactCatalogCore";

const emptyPanelDefaults = {
  selectedRunId: "",
  selectedArtifactDir: "",
  selectedRunLabel: "",
  workspaceFolderUri: "",
  artifactCountLabel: "0 artifacts",
  footerPrimary: "Latest run: --",
  footerSecondary: "Artifacts will appear here after you run a tune.",
  emptyTitle: "No artifacts yet",
  emptyDetail: "Run a tune or select a recent run to inspect artifacts.",
  artifacts: [],
} satisfies DashboardArtifactsPanelState;

/** Scans a tune run directory and maps files into dashboard artifact rows. */
export async function loadArtifactsPanelForRun(
  workspaceFolder: vscode.WorkspaceFolder,
  run: Pick<TuneRunRecord, "id" | "target" | "artifactDir"> | undefined,
  nowMs: number = Date.now(),
): Promise<DashboardArtifactsPanelState> {
  if (!run) {
    return { ...emptyPanelDefaults };
  }

  const scannedFiles = await scanArtifactDirectory(
    workspaceFolder,
    run.artifactDir,
  );
  const artifacts = mapScannedFilesToArtifacts(scannedFiles, nowMs);
  const runLabel = run.target.trim().length > 0 ? run.target : "Tune run";

  if (artifacts.length === 0) {
    return {
      selectedRunId: run.id,
      selectedArtifactDir: run.artifactDir,
      selectedRunLabel: runLabel,
      workspaceFolderUri: workspaceFolder.uri.toString(),
      artifactCountLabel: "0 artifacts",
      footerPrimary: `Selected run: ${runLabel}`,
      footerSecondary: `No files were found under ${run.artifactDir}.`,
      emptyTitle: "No artifacts found",
      emptyDetail: `This run directory is empty or missing: ${run.artifactDir}.`,
      artifacts: [],
    };
  }

  return {
    selectedRunId: run.id,
    selectedArtifactDir: run.artifactDir,
    selectedRunLabel: runLabel,
    workspaceFolderUri: workspaceFolder.uri.toString(),
    artifactCountLabel: `${artifacts.length} artifact${artifacts.length === 1 ? "" : "s"}`,
    footerPrimary: `Selected run: ${runLabel}`,
    footerSecondary: run.artifactDir,
    emptyTitle: emptyPanelDefaults.emptyTitle,
    emptyDetail: emptyPanelDefaults.emptyDetail,
    artifacts,
  };
}

export async function loadArtifactsPanelForLatestRun(
  workspaceFolder: vscode.WorkspaceFolder,
  runs: readonly TuneRunRecord[],
  nowMs: number = Date.now(),
): Promise<DashboardArtifactsPanelState> {
  return loadArtifactsPanelForRun(workspaceFolder, runs[0], nowMs);
}

async function scanArtifactDirectory(
  workspaceFolder: vscode.WorkspaceFolder,
  artifactDirRelative: string,
): Promise<ScannedArtifactFile[]> {
  const normalizedArtifactDir = artifactDirRelative
    .replace(/\\/g, "/")
    .replace(/^\/+/, "");
  if (normalizedArtifactDir.length === 0) {
    return [];
  }

  const artifactRootUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(normalizedArtifactDir),
  );

  try {
    const rootStat = await vscode.workspace.fs.stat(artifactRootUri);
    if (rootStat.type !== vscode.FileType.Directory) {
      return [];
    }
  } catch {
    return [];
  }

  const files: ScannedArtifactFile[] = [];
  await walkDirectory(
    artifactRootUri,
    normalizedArtifactDir,
    "",
    files,
  );
  return files;
}

async function walkDirectory(
  directoryUri: vscode.Uri,
  artifactDirRelative: string,
  relativePrefix: string,
  files: ScannedArtifactFile[],
): Promise<void> {
  const entries = await vscode.workspace.fs.readDirectory(directoryUri);
  for (const [name, fileType] of entries) {
    const artifactRelativePath = relativePrefix
      ? `${relativePrefix}/${name}`
      : name;
    const entryUri = vscode.Uri.joinPath(directoryUri, name);
    if (fileType === vscode.FileType.Directory) {
      await walkDirectory(
        entryUri,
        artifactDirRelative,
        artifactRelativePath,
        files,
      );
      continue;
    }

    if (fileType !== vscode.FileType.File) {
      continue;
    }

    const stat = await vscode.workspace.fs.stat(entryUri);
    files.push({
      artifactRelativePath,
      workspaceRelativePath: `${artifactDirRelative}/${artifactRelativePath}`,
      sizeBytes: stat.size,
      modifiedAtUtc: new Date(stat.mtime).toISOString(),
    });
  }
}

function splitPath(value: string): string[] {
  return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}
