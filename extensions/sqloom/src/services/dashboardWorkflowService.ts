import * as vscode from "vscode";
import { loadEndpointCatalog } from "../cli/endpointCatalog";
import { buildDashboardTuneArguments } from "../cli/tuneArguments";
import { getDashboardArtifactDir } from "../cli/tuneRunProgress";
import { TuneRunProgressTracker } from "../cli/tuneRunProgressTracker";
import {
  isKnownModelProvider,
  isKnownOpenAiModel,
} from "../constants/modelOptions";
import { loadArtifactsPanelForRun } from "../artifacts/loadArtifactsPanel";
import type { DashboardCallbacks } from "../controllers/dashboardWebviewController";
import { RecentRunsStore } from "../recentRuns/recentRunsStore";
import {
  DashboardArtifactsPanelState,
  DashboardEndpointRequest,
  DashboardEndpointResult,
  DashboardTuneProgressEvent,
  DashboardTuneRequest,
  DashboardTuneRunResult,
} from "../sharedInterfaces/dashboard";
import {
  getPrimaryWorkspaceFolder,
  resolveWorkspaceFolder,
} from "../utils/workspaceFolder";
import { CliService } from "./cliService";
import {
  openArtifactFile,
  revealArtifactDirectory,
  revealArtifactFile,
} from "./artifactPresentationService";
import { getSqloomConfiguration } from "./sqloomConfigurationService";
import { defaultHarnessPath } from "./workspacePromptService";

/** Host-side dashboard actions: tune runs, endpoint discovery, and artifact panels. */
export class DashboardWorkflowService {
  constructor(
    private readonly cliService: CliService,
    private readonly recentRunsStore: RecentRunsStore,
  ) {}

  // Bridges webview controllers to CLI-backed workflows without importing vscode UI types there.
  createCallbacks(): DashboardCallbacks {
    return {
      runTune: (request, runId, onProgress) =>
        this.runTuneFromDashboard(request, runId, onProgress),
      loadEndpoints: (request) => this.loadDashboardEndpoints(request),
      getRecentTuneRuns: () => this.recentRunsStore.list(),
      getRecentRuns: () => this.recentRunsStore.listForDashboard(),
      loadArtifactsPanel: (runId) => this.loadDashboardArtifactsPanel(runId),
      revealArtifactDirectory: (artifactDir) =>
        revealArtifactDirectory(artifactDir),
      openArtifact: (relativePath, workspaceFolderUri) =>
        openArtifactFile(relativePath, workspaceFolderUri),
      revealArtifact: (relativePath, workspaceFolderUri) =>
        revealArtifactFile(relativePath, workspaceFolderUri),
    };
  }

  private async runTuneFromDashboard(
    request: DashboardTuneRequest,
    runId: string,
    onProgress: (event: DashboardTuneProgressEvent) => void,
  ): Promise<DashboardTuneRunResult> {
    const workspaceFolder = getPrimaryWorkspaceFolder();
    if (!workspaceFolder) {
      vscode.window.showWarningMessage(
        "Open a workspace before running Sqloom.",
      );
      return { success: false, artifactDir: "" };
    }

    const harnessPath =
      (request.harnessPath ?? "").trim() ||
      (await defaultHarnessPath(workspaceFolder));
    if (harnessPath.length === 0) {
      vscode.window.showWarningMessage(
        "Sqloom dashboard tune requires the default harness path in this workspace.",
      );
      return { success: false, artifactDir: "" };
    }

    const modelProvider = (request.modelProvider ?? "").trim();
    if (!isKnownModelProvider(modelProvider)) {
      vscode.window.showWarningMessage(
        "Sqloom dashboard tune requires the OpenAI model provider.",
      );
      return { success: false, artifactDir: "" };
    }

    const openAiModel = (request.openAiModel ?? "").trim();
    if (!isKnownOpenAiModel(openAiModel)) {
      vscode.window.showWarningMessage(
        "Select an OpenAI model before running Sqloom tune.",
      );
      return { success: false, artifactDir: "" };
    }

    const openAiApiKey =
      (request.openAiApiKey ?? "").trim() ||
      (process.env.OPENAI_API_KEY ?? "").trim();
    if (openAiApiKey.length === 0) {
      vscode.window.showWarningMessage(
        "Enter an OpenAI API key or set OPENAI_API_KEY before running Sqloom tune from the dashboard.",
      );
      return { success: false, artifactDir: "" };
    }

    const replayDataAgent = getSqloomConfiguration().get<string>(
      "replayDataAgent",
      "required",
    );
    const readOnlyConnectionString = (
      request.readOnlyConnectionString ?? ""
    ).trim();
    if (readOnlyConnectionString.length === 0) {
      vscode.window.showWarningMessage(
        "Enter a read-only SQL Server connection string before running Sqloom tune from the dashboard.",
      );
      return { success: false, artifactDir: "" };
    }

    const target = (request.target ?? "").trim();
    if (target.length === 0) {
      vscode.window.showWarningMessage(
        "Select an endpoint before running Sqloom tune from the dashboard.",
      );
      return { success: false, artifactDir: "" };
    }

    const artifactDir = getDashboardArtifactDir(runId);
    const startedAtUtc = new Date().toISOString();
    const args = buildDashboardTuneArguments({
      harnessPath,
      target,
      modelProvider,
      openAiApiKey,
      openAiModel,
      replayDataAgent,
      readOnlyConnectionString,
      artifactDir,
    });

    const tracker = new TuneRunProgressTracker(
      workspaceFolder,
      artifactDir,
      onProgress,
    );
    await tracker.start();

    try {
      const result = await this.cliService.runWithExitCode(
        args,
        workspaceFolder,
        {
          cliPathOverride: request.cliPath,
          quietCompletionToasts: true,
        },
      );
      await tracker.finish(result.exitCode);
      await this.recentRunsStore.record({
        id: runId,
        startedAtUtc,
        success: result.exitCode === 0,
        target,
        artifactDir,
        workspaceFolderUri: workspaceFolder.uri.toString(),
        openAiModel,
      });
      if (result.exitCode === 0) {
        vscode.window.showInformationMessage("Sqloom tune completed.");
        return { success: true, artifactDir };
      }

      vscode.window.showErrorMessage(
        result.launchFailureMessage ??
          "Sqloom tune failed. See the Sqloom output channel.",
      );
      return { success: false, artifactDir };
    } finally {
      tracker.dispose();
    }
  }

  private async loadDashboardEndpoints(
    request: DashboardEndpointRequest,
  ): Promise<DashboardEndpointResult> {
    const workspaceFolder = getPrimaryWorkspaceFolder();
    if (!workspaceFolder) {
      return {
        status: "failed",
        message: "Open a workspace before loading Sqloom endpoints.",
      };
    }

    const harnessPath = (request.harnessPath ?? "").trim();
    if (harnessPath.length === 0) {
      return {
        status: "failed",
        message: "Enter or detect a harness path before loading endpoints.",
      };
    }

    const cliPath =
      (request.cliPath ?? "").trim() ||
      getSqloomConfiguration().get<string>("cli.path", "sqloom").trim() ||
      "sqloom";
    const result = await loadEndpointCatalog({
      cliPath,
      harnessPath,
      cwd: workspaceFolder.uri.fsPath,
    });
    this.cliService.appendCommandLine(cliPath, result.command);
    this.cliService.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
    if (result.stdout.length > 0) {
      this.cliService.append(result.stdout);
    }
    if (result.stderr.length > 0) {
      this.cliService.append(result.stderr);
    }
    this.cliService.appendLine("");

    if (result.status === "failed") {
      this.cliService.appendLine(result.message);
      return { status: "failed", message: result.message };
    }

    return { status: "loaded", endpoints: result.endpoints };
  }

  private async loadDashboardArtifactsPanel(
    runId?: string,
  ): Promise<DashboardArtifactsPanelState> {
    const runs = this.recentRunsStore.list();
    const selectedRun = runId
      ? (runs.find((run) => run.id === runId) ?? runs[0])
      : runs[0];
    const workspaceFolder = resolveWorkspaceFolder(
      selectedRun?.workspaceFolderUri,
    );
    if (!workspaceFolder) {
      return createEmptyArtifactsPanel();
    }

    return loadArtifactsPanelForRun(workspaceFolder, selectedRun);
  }
}

function createEmptyArtifactsPanel(): DashboardArtifactsPanelState {
  return {
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
  };
}
