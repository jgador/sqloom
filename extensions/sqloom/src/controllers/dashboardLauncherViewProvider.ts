import * as vscode from "vscode";
import { dashboardLauncherViewId } from "../constants/dashboardConstants";
import { renderDashboardLauncherHtml } from "../dashboard/dashboardLauncherRenderer";
import {
  DashboardMessage,
  DashboardTuneRequest,
} from "../sharedInterfaces/dashboard";
import { openDashboard } from "./dashboardWebviewController";

export function registerDashboardLauncher(
  context: vscode.ExtensionContext,
  runTune: (request: DashboardTuneRequest) => Promise<void>,
): vscode.Disposable {
  return vscode.window.registerWebviewViewProvider(
    dashboardLauncherViewId,
    new DashboardLauncherViewProvider(context, runTune),
    {
      webviewOptions: {
        retainContextWhenHidden: true,
      },
    },
  );
}

class DashboardLauncherViewProvider implements vscode.WebviewViewProvider {
  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly runTune: (request: DashboardTuneRequest) => Promise<void>,
  ) {}

  resolveWebviewView(webviewView: vscode.WebviewView): void {
    webviewView.webview.options = {
      enableScripts: true,
      localResourceRoots: [
        vscode.Uri.joinPath(this.context.extensionUri, "images"),
      ],
    };
    webviewView.webview.html = renderDashboardLauncherHtml(
      this.context,
      webviewView.webview,
    );

    const messageSubscription = webviewView.webview.onDidReceiveMessage(
      (message: DashboardMessage) => void this.handleMessage(message),
    );

    webviewView.onDidDispose(() => messageSubscription.dispose());
  }

  private async handleMessage(message: DashboardMessage): Promise<void> {
    switch (message.command) {
      case "openDashboard":
        await openDashboard(this.context, this.runTune);
        return;
      default:
        return;
    }
  }
}
