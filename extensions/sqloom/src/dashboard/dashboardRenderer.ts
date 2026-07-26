import * as vscode from "vscode";
import { DashboardArtifactsPanelState } from "../sharedInterfaces/dashboard";
import type { TuneRunRecord } from "../recentRuns/recentRunsStore";
import { escapeHtml, getNonce, serializeForScript } from "../utils/webview";
import { createDashboardState } from "./dashboardState";

/** Renders the server-side HTML/JS shell for the tune dashboard webview. */
export async function renderDashboardHtml(
  context: vscode.ExtensionContext,
  webview: vscode.Webview,
  recentRuns: readonly TuneRunRecord[] = [],
  artifactsPanel: DashboardArtifactsPanelState,
): Promise<string> {
  const nonce = getNonce();
  const state = await createDashboardState(recentRuns);
  const logoUri = webview.asWebviewUri(
    vscode.Uri.joinPath(context.extensionUri, "images", "extensionIcon.png"),
  );

  // Script and Style URIs from dist/views
  const scriptUri = webview.asWebviewUri(
    vscode.Uri.joinPath(context.extensionUri, "dist", "views", "dashboard.js"),
  );
  const styleUri = webview.asWebviewUri(
    vscode.Uri.joinPath(context.extensionUri, "dist", "views", "dashboard.css"),
  );

  return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${webview.cspSource} https: data:; style-src ${webview.cspSource} 'unsafe-inline'; script-src 'nonce-${nonce}' ${webview.cspSource};">
    <title>${escapeHtml(state.title)}</title>
    <link href="${styleUri}" rel="stylesheet" />
</head>
<body>
    <div id="root"></div>
    <script nonce="${nonce}">
        window.__INITIAL_STATE__ = ${serializeForScript(state)};
        window.__INITIAL_ARTIFACTS__ = ${serializeForScript(artifactsPanel)};
        window.__LOGO_URI__ = "${logoUri}";
    </script>
    <script nonce="${nonce}" src="${scriptUri}" type="module"></script>
</body>
</html>`;
}
