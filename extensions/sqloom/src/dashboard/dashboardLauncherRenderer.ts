import * as vscode from "vscode";
import { escapeHtml, getNonce } from "../utils/webview";

export function renderDashboardLauncherHtml(
  context: vscode.ExtensionContext,
  webview: vscode.Webview,
): string {
  const nonce = getNonce();
  const logoUri = webview.asWebviewUri(
    vscode.Uri.joinPath(context.extensionUri, "images", "extensionIcon.png"),
  );

  return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${webview.cspSource}; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';">
    <title>Sqloom</title>
    <style>
        :root {
            color-scheme: light dark;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            min-width: 180px;
            color: var(--vscode-sideBar-foreground, var(--vscode-foreground, #000000));
            background: var(--vscode-sideBar-background, var(--vscode-editor-background, transparent));
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
        }

        body.vscode-dark {
            color: var(--vscode-sideBar-foreground, var(--vscode-foreground, #ffffff));
        }

        body.vscode-light {
            color: var(--vscode-sideBar-foreground, var(--vscode-foreground, #000000));
        }

        body.vscode-high-contrast {
            color: var(--vscode-foreground, #ffffff);
        }

        body.vscode-high-contrast-light {
            color: var(--vscode-foreground, #000000);
        }

        button {
            font: inherit;
        }

        .launcher {
            display: grid;
            gap: 14px;
            padding: 18px 14px;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 10px;
            min-width: 0;
        }

        .brand img {
            width: 32px;
            height: 32px;
            border-radius: 6px;
            flex: 0 0 auto;
        }

        .title {
            margin: 0;
            overflow-wrap: anywhere;
            font-size: 14px;
            line-height: 1.25;
            font-weight: 700;
            color: inherit;
        }

        .caption {
            color: var(--vscode-descriptionForeground);
            line-height: 1.35;
        }

        .open-dashboard {
            width: 100%;
            min-height: 32px;
            border: 0;
            border-radius: 4px;
            padding: 7px 10px;
            color: var(--vscode-button-foreground);
            background: var(--vscode-button-background);
            cursor: pointer;
            font-weight: 600;
        }

        .open-dashboard:hover {
            background: var(--vscode-button-hoverBackground);
        }
    </style>
</head>
<body>
    <main class="launcher" aria-label="Sqloom">
        <section class="brand">
            <img src="${logoUri}" alt="">
            <h1 class="title">${escapeHtml("Sqloom")}</h1>
        </section>
        <div class="caption">Tune dashboard preview</div>
        <button class="open-dashboard" type="button" data-command="openDashboard">Open Tune Dashboard</button>
    </main>
    <script nonce="${nonce}">
        const vscode = acquireVsCodeApi();
        document.querySelector("[data-command='openDashboard']").addEventListener("click", () => {
            vscode.postMessage({ command: "openDashboard" });
        });
    </script>
</body>
</html>`;
}
