import * as path from "node:path";
import * as vscode from "vscode";
import type { DashboardTuneProgressEvent } from "../sharedInterfaces/dashboard";
import {
  STAGE_COMPLETION_ARTIFACTS,
  TUNE_STAGE_ORDER,
  TuneRunProgressStateMachine,
  getRelativePathUnderRoot,
  getStageForArtifactRelativePath,
  normalizeArtifactRelativePath,
} from "./tuneRunProgress";

const pollIntervalMs = 1000;

export class TuneRunProgressTracker implements vscode.Disposable {
  private readonly stateMachine = new TuneRunProgressStateMachine();
  private readonly disposables: vscode.Disposable[] = [];
  private readonly artifactDirRelative: string;
  private readonly workspaceRoot: string;
  private pollTimer: ReturnType<typeof setInterval> | undefined;

  constructor(
    private readonly workspaceFolder: vscode.WorkspaceFolder,
    artifactDirRelative: string,
    private readonly onProgress: (event: DashboardTuneProgressEvent) => void,
  ) {
    this.artifactDirRelative = normalizeArtifactRelativePath(artifactDirRelative);
    this.workspaceRoot = workspaceFolder.uri.fsPath;
  }

  async start(): Promise<void> {
    this.stateMachine.start(this.onProgress);
    const pattern = new vscode.RelativePattern(
      this.workspaceFolder,
      `${this.artifactDirRelative}/**`,
    );
    const watcher = vscode.workspace.createFileSystemWatcher(pattern);
    watcher.onDidCreate((uri) => void this.handleUri(uri));
    watcher.onDidChange((uri) => void this.handleUri(uri));
    this.disposables.push(watcher);
    this.pollTimer = setInterval(() => {
      void this.scanExistingArtifacts();
    }, pollIntervalMs);
    await this.scanExistingArtifacts();
  }

  async finish(exitCode: number): Promise<void> {
    await this.scanExistingArtifacts();
    this.stateMachine.finish(exitCode, this.onProgress);
  }

  dispose(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = undefined;
    }

    for (const disposable of this.disposables) {
      disposable.dispose();
    }
    this.disposables.length = 0;
  }

  private async scanExistingArtifacts(): Promise<void> {
    for (const stage of TUNE_STAGE_ORDER) {
      const artifactRelativePath = STAGE_COMPLETION_ARTIFACTS[stage];
      const uri = vscode.Uri.joinPath(
        this.workspaceFolder.uri,
        ...splitPath(`${this.artifactDirRelative}/${artifactRelativePath}`),
      );
      if (await fileHasContent(uri)) {
        this.stateMachine.completeStage(
          stage,
          path.basename(uri.fsPath),
          this.onProgress,
        );
      }
    }
  }

  private async handleUri(uri: vscode.Uri): Promise<void> {
    const relativeToWorkspace = getRelativePathUnderRoot(
      this.workspaceRoot,
      uri.fsPath,
    );
    if (relativeToWorkspace === undefined) {
      return;
    }

    const relativeToArtifactDir = relativeToWorkspace.startsWith(
      `${this.artifactDirRelative}/`,
    )
      ? relativeToWorkspace.slice(this.artifactDirRelative.length + 1)
      : relativeToWorkspace === this.artifactDirRelative
        ? ""
        : undefined;
    if (relativeToArtifactDir === undefined) {
      return;
    }

    const stage = getStageForArtifactRelativePath(relativeToArtifactDir);
    if (!stage || !(await fileHasContent(uri))) {
      return;
    }

    this.stateMachine.completeStage(
      stage,
      path.basename(uri.fsPath),
      this.onProgress,
    );
  }
}

async function fileHasContent(uri: vscode.Uri): Promise<boolean> {
  try {
    const stat = await vscode.workspace.fs.stat(uri);
    return stat.size > 0;
  } catch {
    return false;
  }
}

function splitPath(value: string): string[] {
  return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}
