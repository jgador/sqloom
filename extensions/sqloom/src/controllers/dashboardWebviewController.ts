import * as vscode from "vscode";
import { EndpointCatalogSession } from "../cli/endpointCatalogSession";
import {
  tuneDashboardTitle,
  tuneDashboardViewType,
} from "../constants/dashboardConstants";
import { renderDashboardHtml } from "../dashboard/dashboardRenderer";
import {
  DashboardEndpointRequest,
  DashboardEndpointResult,
  DashboardHostMessage,
  DashboardMessage,
  DashboardTuneProgressEvent,
  DashboardTuneRequest,
  DashboardTuneRunResult,
} from "../sharedInterfaces/dashboard";

let dashboardController: DashboardWebviewController | undefined;

export type DashboardCallbacks = {
  runTune: (
    request: DashboardTuneRequest,
    runId: string,
    onProgress: (event: DashboardTuneProgressEvent) => void,
  ) => Promise<DashboardTuneRunResult>;
  loadEndpoints: (
    request: DashboardEndpointRequest,
  ) => Promise<DashboardEndpointResult>;
};

export async function openDashboard(
  context: vscode.ExtensionContext,
  callbacks: DashboardCallbacks,
): Promise<void> {
  dashboardController ??= new DashboardWebviewController(
    context,
    callbacks,
    () => {
      dashboardController = undefined;
    },
  );

  await dashboardController.show();
}

class DashboardWebviewController implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private messageSubscription: vscode.Disposable | undefined;
  private workspaceSubscription: vscode.Disposable | undefined;
  private readonly endpointCatalogSession = new EndpointCatalogSession();
  private activeTuneRunId = "";
  private tuneRunInFlight = false;

  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly callbacks: DashboardCallbacks,
    private readonly onDisposed: () => void,
  ) {}

  async show(): Promise<void> {
    if (this.panel) {
      this.panel.reveal(vscode.ViewColumn.One);
      return;
    }

    const panel = vscode.window.createWebviewPanel(
      tuneDashboardViewType,
      tuneDashboardTitle,
      vscode.ViewColumn.One,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [
          vscode.Uri.joinPath(this.context.extensionUri, "images"),
        ],
      },
    );

    this.panel = panel;
    panel.iconPath = {
      light: vscode.Uri.joinPath(
        this.context.extensionUri,
        "media",
        "sqloom-activity_dark.svg",
      ),
      dark: vscode.Uri.joinPath(
        this.context.extensionUri,
        "media",
        "sqloom-activity_dark.svg",
      ),
    };

    await this.refresh();

    this.messageSubscription = panel.webview.onDidReceiveMessage(
      (message: DashboardMessage) => void this.handleMessage(message),
    );
    this.workspaceSubscription = vscode.workspace.onDidChangeWorkspaceFolders(
      () => void this.refresh(),
    );
    panel.onDidDispose(() => this.dispose());
  }

  dispose(): void {
    this.messageSubscription?.dispose();
    this.messageSubscription = undefined;
    this.workspaceSubscription?.dispose();
    this.workspaceSubscription = undefined;
    this.clearEndpointCatalog();
    this.panel = undefined;
    this.onDisposed();
  }

  private async refresh(): Promise<void> {
    if (!this.panel) {
      return;
    }

    this.clearEndpointCatalog();
    this.panel.webview.html = await renderDashboardHtml(
      this.context,
      this.panel.webview,
    );
  }

  private async handleMessage(message: DashboardMessage): Promise<void> {
    switch (message.command) {
      case "runTune": {
        if (this.tuneRunInFlight) {
          void vscode.window.showWarningMessage(
            "A Sqloom tune run is already in progress.",
          );
          return;
        }

        const request = message.payload ?? {};
        const target = (request.target ?? "").trim();
        const context = endpointContext(request);
        if (!this.endpointCatalogSession.canRun(context, target)) {
          void vscode.window.showWarningMessage(
            "Select an endpoint loaded from the current Sqloom CLI and harness before running tune.",
          );
          return;
        }

        const runId = createTuneRunId();
        this.tuneRunInFlight = true;
        this.activeTuneRunId = runId;
        await this.postMessage({ command: "tuneRunStarted", runId });

        try {
          const result = await this.callbacks.runTune(
            { ...request, target },
            runId,
            (event) => {
              if (this.activeTuneRunId !== runId) {
                return;
              }

              void this.postMessage({
                command: "tuneProgress",
                runId,
                stage: event.stage,
                status: event.status,
                detail: event.detail,
              });
            },
          );

          if (this.activeTuneRunId !== runId) {
            return;
          }

          await this.postMessage({
            command: "tuneRunFinished",
            runId,
            success: result.success,
            artifactDir: result.artifactDir,
          });
        } finally {
          if (this.activeTuneRunId === runId) {
            this.activeTuneRunId = "";
          }
          this.tuneRunInFlight = false;
        }
        return;
      }
      case "loadEndpoints":
        await this.loadEndpoints(message.requestId, message.payload ?? {});
        return;
      case "refreshChecks":
        await this.refresh();
        return;
      case "createHarness":
        void vscode.window.showInformationMessage(
          "Harness creation is not wired into the dashboard preview yet.",
        );
        return;
      default:
        return;
    }
  }

  private async loadEndpoints(
    requestId: string,
    request: DashboardEndpointRequest,
  ): Promise<void> {
    const generation = this.endpointCatalogSession.beginLoad();
    const context = endpointContext(request);
    const result = await this.callbacks.loadEndpoints(request);
    if (!this.endpointCatalogSession.isCurrent(generation)) {
      return;
    }

    if (result.status === "loaded") {
      this.endpointCatalogSession.completeLoad(
        generation,
        context,
        result.endpoints,
      );
      await this.postMessage({
        command: "endpointsLoaded",
        requestId,
        endpoints: result.endpoints,
      });
      return;
    }

    await this.postMessage({
      command: "endpointsFailed",
      requestId,
      message: result.message,
    });
  }

  private async postMessage(message: DashboardHostMessage): Promise<void> {
    await this.panel?.webview.postMessage(message);
  }

  private clearEndpointCatalog(): void {
    this.endpointCatalogSession.clear();
  }
}

function endpointContext(
  request: DashboardEndpointRequest | DashboardTuneRequest,
): string {
  return `${(request.cliPath ?? "").trim()}\n${(request.harnessPath ?? "").trim()}`;
}

function createTuneRunId(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}
