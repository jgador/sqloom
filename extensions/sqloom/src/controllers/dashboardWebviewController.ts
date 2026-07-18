import * as vscode from "vscode";
import {
  tuneDashboardTitle,
  tuneDashboardViewType,
} from "../constants/dashboardConstants";
import { renderDashboardHtml } from "../dashboard/dashboardRenderer";
import {
  DashboardMessage,
  DashboardTuneRequest,
} from "../sharedInterfaces/dashboard";

let dashboardController: DashboardWebviewController | undefined;

export async function openDashboard(
  context: vscode.ExtensionContext,
  runTune: (request: DashboardTuneRequest) => Promise<void>,
): Promise<void> {
  dashboardController ??= new DashboardWebviewController(
    context,
    runTune,
    () => {
      dashboardController = undefined;
    },
  );

  await dashboardController.show();
}

class DashboardWebviewController implements vscode.Disposable {
  private panel: vscode.WebviewPanel | undefined;
  private messageSubscription: vscode.Disposable | undefined;

  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly runTune: (request: DashboardTuneRequest) => Promise<void>,
    private readonly onDisposed: () => void,
  ) {}

  async show(): Promise<void> {
    if (this.panel) {
      this.panel.reveal(vscode.ViewColumn.One);
      await this.refresh();
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
    panel.onDidDispose(() => this.dispose());
  }

  dispose(): void {
    this.messageSubscription?.dispose();
    this.messageSubscription = undefined;
    this.panel = undefined;
    this.onDisposed();
  }

  private async refresh(): Promise<void> {
    if (!this.panel) {
      return;
    }

    this.panel.webview.html = await renderDashboardHtml(
      this.context,
      this.panel.webview,
    );
  }

  private async handleMessage(message: DashboardMessage): Promise<void> {
    switch (message.command) {
      case "runTune":
        await this.runTune(message.payload ?? {});
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
}
