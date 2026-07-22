import * as vscode from "vscode";
import { dashboardLauncherViewId } from "../constants/dashboardConstants";
import { renderDashboardLauncherHtml } from "../dashboard/dashboardLauncherRenderer";
import { DashboardMessage } from "../sharedInterfaces/dashboard";
import {
  DashboardCallbacks,
  openDashboard,
} from "./dashboardWebviewController";

export function registerDashboardLauncher(
  context: vscode.ExtensionContext,
  callbacks: DashboardCallbacks,
): vscode.Disposable {
  return vscode.window.registerWebviewViewProvider(
    dashboardLauncherViewId,
    new DashboardLauncherViewProvider(context, callbacks),
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
    private readonly callbacks: DashboardCallbacks,
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
        await openDashboard(this.context, this.callbacks);
        return;
      default:
        return;
    }
  }
}
