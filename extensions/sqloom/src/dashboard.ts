import * as vscode from "vscode";

let dashboardPanel: vscode.WebviewPanel | undefined;

type DashboardStatus = "ready" | "warning" | "neutral" | "idle";

type DashboardStage = {
  number: number;
  label: string;
  detail?: string;
  active?: boolean;
};

type DashboardSummaryItem = {
  label: string;
  detail: string;
  status: DashboardStatus;
};

type DashboardCheck = {
  label: string;
  badge: string;
  detail: string;
  status: DashboardStatus;
  trailing?: string;
  action?: string;
};

type DashboardConfigField = {
  label: string;
  value: string;
  kind: "select" | "password" | "toggle";
  note?: string;
};

type DashboardArtifact = {
  label: string;
};

type DashboardState = {
  title: string;
  subtitle: string;
  readinessLabel: string;
  stages: DashboardStage[];
  summaryItems: DashboardSummaryItem[];
  configFields: DashboardConfigField[];
  readinessChecks: DashboardCheck[];
  artifacts: DashboardArtifact[];
};

type DashboardMessage = {
  command?: string;
};

const dashboardLauncherViewId = "sqloom.dashboardLauncher";

export function registerDashboardLauncher(
  context: vscode.ExtensionContext,
): vscode.Disposable {
  return vscode.window.registerWebviewViewProvider(
    dashboardLauncherViewId,
    new DashboardLauncherViewProvider(context),
    {
      webviewOptions: {
        retainContextWhenHidden: true,
      },
    },
  );
}

class DashboardLauncherViewProvider implements vscode.WebviewViewProvider {
  constructor(private readonly context: vscode.ExtensionContext) {}

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
      async (message: DashboardMessage) => {
        switch (message.command) {
          case "openDashboard":
            await openDashboard(this.context);
            return;
          default:
            return;
        }
      },
    );

    webviewView.onDidDispose(() => messageSubscription.dispose());
  }
}

export async function openDashboard(
  context: vscode.ExtensionContext,
): Promise<void> {
  if (dashboardPanel) {
    dashboardPanel.reveal(vscode.ViewColumn.One);
    return;
  }

  const panel = vscode.window.createWebviewPanel(
    "sqloom.tuneDashboard",
    "Sqloom Tune",
    vscode.ViewColumn.One,
    {
      enableScripts: true,
      retainContextWhenHidden: true,
      localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, "images")],
    },
  );

  dashboardPanel = panel;
  panel.iconPath = {
    light: vscode.Uri.joinPath(
      context.extensionUri,
      "images",
      "extensionIcon.png",
    ),
    dark: vscode.Uri.joinPath(
      context.extensionUri,
      "images",
      "extensionIcon.png",
    ),
  };
  panel.webview.html = renderDashboardHtml(context, panel.webview);

  const messageSubscription = panel.webview.onDidReceiveMessage(
    (message: DashboardMessage) => handleDashboardMessage(message),
  );
  panel.onDidDispose(() => {
    dashboardPanel = undefined;
    messageSubscription.dispose();
  });
}

async function handleDashboardMessage(
  message: DashboardMessage,
): Promise<void> {
  switch (message.command) {
    case "runTune":
      vscode.window.showWarningMessage(
        "Complete harness setup before running Sqloom tune from the dashboard preview.",
      );
      return;
    case "refreshChecks":
      vscode.window.showInformationMessage(
        "Dashboard checks are static in this preview. Live checks will be wired later.",
      );
      return;
    case "createHarness":
      vscode.window.showInformationMessage(
        "Harness creation is not wired into the dashboard preview yet.",
      );
      return;
    default:
      return;
  }
}

function renderDashboardLauncherHtml(
  context: vscode.ExtensionContext,
  webview: vscode.Webview,
): string {
  const nonce = createNonce();
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
            color: var(--vscode-sideBar-foreground);
            background: var(--vscode-sideBar-background);
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
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
            <h1 class="title">Sqloom</h1>
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

function createDashboardState(): DashboardState {
  return {
    title: "Sqloom Tune",
    subtitle: "Tune SQL for performance with confidence.",
    readinessLabel: "4 of 5 checks ready",
    stages: [
      { number: 1, label: "Observe", active: true },
      { number: 2, label: "Replay" },
      { number: 3, label: "Capture", detail: "Capture SQL evidence" },
      { number: 4, label: "Correlate" },
      { number: 5, label: "Advise" },
    ],
    summaryItems: [
      {
        label: "Repository readiness",
        detail: "4 of 5 checks ready",
        status: "ready",
      },
      {
        label: "Harness",
        detail: "Not detected",
        status: "warning",
      },
      {
        label: "OpenAI model",
        detail: "gpt-5.4-mini",
        status: "ready",
      },
      {
        label: "Last tune run",
        detail: "Never run",
        status: "idle",
      },
    ],
    configFields: [
      {
        label: "Target method",
        value: "Discover after Harness.cs is created",
        kind: "select",
      },
      {
        label: "Read-only connection string",
        value: "************************",
        kind: "password",
        note: "Uses harness session connection when omitted",
      },
      {
        label: "Model provider",
        value: "OpenAI",
        kind: "select",
      },
      {
        label: "OpenAI API key",
        value: "******************",
        kind: "password",
      },
      {
        label: "OpenAI advice model",
        value: "gpt-5.4-mini",
        kind: "select",
      },
      {
        label: "Debug",
        value: "Show per-stage diagnostics",
        kind: "toggle",
      },
    ],
    readinessChecks: [
      {
        label: "Sqloom CLI",
        badge: "Installed",
        detail: "sqloom",
        status: "ready",
        trailing: "v0.4.0",
      },
      {
        label: "Sqloom skill",
        badge: "Installed",
        detail: ".agents/skills/sqloom",
        status: "ready",
      },
      {
        label: ".NET SDK",
        badge: "Ready",
        detail: ".NET 10",
        status: "ready",
      },
      {
        label: "Sqloom.Testing",
        badge: "Added with harness",
        detail: "Match v0.4.0",
        status: "neutral",
      },
      {
        label: "Harness.cs",
        badge: "Not detected",
        detail: "Required for target discovery and tune",
        status: "warning",
        action: "Create Harness",
      },
    ],
    artifacts: [
      { label: "SQL tuning proposal" },
      { label: "tune-summary.json" },
      { label: "query-store-snapshot.json" },
    ],
  };
}

function renderDashboardHtml(
  context: vscode.ExtensionContext,
  webview: vscode.Webview,
): string {
  const nonce = createNonce();
  const state = createDashboardState();
  const logoUri = webview.asWebviewUri(
    vscode.Uri.joinPath(context.extensionUri, "images", "extensionIcon.png"),
  );

  return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${webview.cspSource}; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';">
    <title>${escapeHtml(state.title)}</title>
    <style>
        :root {
            color-scheme: light dark;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            min-width: 320px;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
        }

        button,
        input,
        select {
            font: inherit;
        }

        .shell {
            min-height: 100vh;
            padding: 28px 32px 24px;
        }

        .surface {
            max-width: 1420px;
            margin: 0 auto;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 8px;
            background: var(--vscode-editor-background);
            box-shadow: 0 12px 28px rgba(0, 0, 0, 0.08);
            overflow: hidden;
        }

        .top {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 24px;
            padding: 24px 28px 8px;
            align-items: start;
        }

        .brand {
            display: flex;
            gap: 18px;
            align-items: center;
            min-width: 0;
        }

        .brand img {
            width: 58px;
            height: 58px;
            border-radius: 8px;
            flex: 0 0 auto;
        }

        .title {
            margin: 0 0 4px;
            font-size: 24px;
            line-height: 1.2;
            font-weight: 700;
        }

        .subtitle,
        .caption,
        .muted {
            color: var(--vscode-descriptionForeground);
        }

        .primary-action {
            min-width: 160px;
            border: 0;
            border-radius: 4px;
            padding: 10px 18px;
            color: var(--vscode-button-foreground);
            background: var(--vscode-button-background);
            font-weight: 600;
            cursor: pointer;
        }

        .primary-action:hover {
            background: var(--vscode-button-hoverBackground);
        }

        .run-note {
            margin-top: 8px;
            text-align: center;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }

        .stepper {
            display: grid;
            grid-template-columns: repeat(5, minmax(92px, 1fr));
            gap: 0;
            padding: 20px 120px 16px;
        }

        .step {
            position: relative;
            display: grid;
            justify-items: center;
            text-align: center;
            min-width: 0;
        }

        .step:not(:last-child)::after {
            content: "";
            position: absolute;
            top: 16px;
            left: calc(50% + 28px);
            right: calc(-50% + 28px);
            height: 1px;
            background: var(--vscode-panel-border);
        }

        .step-index {
            display: grid;
            place-items: center;
            width: 34px;
            height: 34px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 999px;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
            font-weight: 700;
            z-index: 1;
        }

        .step.active .step-index {
            border-color: var(--vscode-focusBorder);
            color: var(--vscode-focusBorder);
        }

        .step-label {
            margin-top: 8px;
            font-weight: 600;
        }

        .step-detail {
            margin-top: 2px;
            min-height: 16px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }

        .summary-strip {
            display: grid;
            grid-template-columns: repeat(4, minmax(160px, 1fr)) auto;
            gap: 0;
            margin: 0 24px 14px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 8px;
            overflow: hidden;
        }

        .summary-item {
            display: grid;
            grid-template-columns: auto minmax(0, 1fr);
            gap: 12px;
            align-items: center;
            min-height: 58px;
            padding: 12px 18px;
            border-right: 1px solid var(--vscode-panel-border);
        }

        .summary-label,
        .row-title,
        .field-label {
            font-weight: 600;
        }

        .summary-detail {
            margin-top: 2px;
            font-size: 12px;
            color: var(--vscode-descriptionForeground);
        }

        .refresh {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            padding: 0 22px;
            border: 0;
            color: var(--vscode-textLink-foreground);
            background: transparent;
            cursor: pointer;
            font-weight: 600;
            white-space: nowrap;
        }

        .content {
            display: grid;
            grid-template-columns: minmax(280px, 360px) minmax(0, 1fr);
            gap: 16px;
            padding: 0 24px 20px;
        }

        .panel {
            border: 1px solid var(--vscode-panel-border);
            border-radius: 8px;
            background: var(--vscode-sideBar-background, var(--vscode-editor-background));
            min-width: 0;
        }

        .config {
            padding: 18px 20px;
        }

        .section-title {
            margin: 0 0 16px;
            font-size: 18px;
            line-height: 1.25;
        }

        .field {
            margin-bottom: 14px;
        }

        .field-label {
            margin-bottom: 6px;
            font-size: 12px;
        }

        .field-note {
            margin-top: 6px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }

        .control {
            display: flex;
            align-items: center;
            min-height: 32px;
            width: 100%;
            border: 1px solid var(--vscode-input-border, var(--vscode-panel-border));
            border-radius: 4px;
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            padding: 0 10px;
        }

        .control-value {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            min-width: 0;
            flex: 1 1 auto;
        }

        .control-icon {
            color: var(--vscode-descriptionForeground);
            margin-left: 8px;
            flex: 0 0 auto;
        }

        .toggle-row {
            display: flex;
            align-items: center;
            gap: 8px;
            min-height: 32px;
        }

        .toggle {
            position: relative;
            width: 30px;
            height: 16px;
            border-radius: 999px;
            background: var(--vscode-inputOption-activeBorder, #9aa0a6);
            opacity: 0.8;
        }

        .toggle::after {
            content: "";
            position: absolute;
            width: 12px;
            height: 12px;
            top: 2px;
            left: 2px;
            border-radius: 999px;
            background: var(--vscode-editor-background);
            box-shadow: 0 1px 2px rgba(0, 0, 0, 0.24);
        }

        .main-panel {
            overflow: hidden;
        }

        .readiness {
            padding: 18px 22px 0;
        }

        .section-heading {
            display: flex;
            gap: 12px;
            align-items: baseline;
            margin-bottom: 10px;
        }

        .section-heading .section-title {
            margin: 0;
        }

        .check-row {
            display: grid;
            grid-template-columns: 30px minmax(150px, 1.1fr) minmax(120px, 0.8fr) minmax(180px, 1.4fr) auto;
            gap: 14px;
            align-items: center;
            min-height: 44px;
            border-top: 1px solid var(--vscode-panel-border);
        }

        .check-row:first-of-type {
            border-top: 0;
        }

        .status-dot {
            display: grid;
            place-items: center;
            width: 20px;
            height: 20px;
            border-radius: 999px;
            border: 1px solid currentColor;
            font-size: 12px;
            font-weight: 700;
        }

        .status-ready {
            color: #22a447;
        }

        .status-warning {
            color: #f97316;
        }

        .status-neutral,
        .status-idle {
            color: var(--vscode-descriptionForeground);
        }

        .badge {
            justify-self: start;
            border-radius: 4px;
            padding: 4px 10px;
            font-size: 12px;
            background: var(--vscode-badge-background);
            color: var(--vscode-badge-foreground);
        }

        .badge.status-ready {
            background: rgba(34, 164, 71, 0.12);
            color: #15803d;
        }

        .badge.status-warning {
            background: rgba(249, 115, 22, 0.12);
            color: #ea580c;
        }

        .badge.status-neutral {
            background: var(--vscode-input-background);
            color: var(--vscode-descriptionForeground);
        }

        .row-action {
            border: 0;
            background: transparent;
            color: var(--vscode-textLink-foreground);
            cursor: pointer;
            font-weight: 600;
            white-space: nowrap;
        }

        .row-trailing {
            color: var(--vscode-editor-foreground);
            white-space: nowrap;
        }

        .results {
            margin-top: 8px;
            border-top: 1px solid var(--vscode-panel-border);
            padding: 16px 12px 10px;
        }

        .results .section-title {
            margin: 0;
        }

        .artifact {
            display: grid;
            grid-template-columns: 24px minmax(0, 1fr) 120px 28px;
            gap: 12px;
            align-items: center;
            min-height: 36px;
            margin-top: 8px;
            padding: 0 8px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 4px;
            background: var(--vscode-editor-background);
        }

        .file-icon {
            width: 14px;
            height: 18px;
            border: 1px solid var(--vscode-descriptionForeground);
            border-radius: 2px;
            position: relative;
        }

        .file-icon::after {
            content: "";
            position: absolute;
            right: -1px;
            top: -1px;
            width: 5px;
            height: 5px;
            border-left: 1px solid var(--vscode-descriptionForeground);
            border-bottom: 1px solid var(--vscode-descriptionForeground);
            background: var(--vscode-editor-background);
        }

        .skeleton {
            height: 5px;
            border-radius: 999px;
            background: var(--vscode-panel-border);
            opacity: 0.9;
        }

        .more {
            border: 0;
            background: transparent;
            color: var(--vscode-descriptionForeground);
            cursor: pointer;
            font-size: 18px;
        }

        @media (max-width: 1050px) {
            .top,
            .content,
            .summary-strip {
                grid-template-columns: 1fr;
            }

            .stepper {
                padding-left: 28px;
                padding-right: 28px;
                overflow-x: auto;
            }

            .summary-item {
                border-right: 0;
                border-bottom: 1px solid var(--vscode-panel-border);
            }

            .refresh {
                min-height: 44px;
            }

            .check-row {
                grid-template-columns: 30px minmax(120px, 1fr);
                gap: 8px 12px;
                padding: 10px 0;
            }

            .badge,
            .check-detail,
            .row-trailing,
            .row-action {
                grid-column: 2;
            }
        }

        @media (max-width: 620px) {
            .shell {
                padding: 16px;
            }

            .surface {
                border-radius: 0;
            }

            .top {
                padding: 18px;
            }

            .brand {
                align-items: flex-start;
            }

            .brand img {
                width: 48px;
                height: 48px;
            }

            .stepper {
                grid-template-columns: repeat(5, 112px);
            }

            .content,
            .summary-strip {
                margin-left: 16px;
                margin-right: 16px;
                padding-left: 0;
                padding-right: 0;
            }

            .artifact {
                grid-template-columns: 24px minmax(0, 1fr) 28px;
            }

            .artifact .skeleton {
                display: none;
            }
        }
    </style>
</head>
<body>
    <main class="shell">
        <section class="surface" aria-label="Sqloom Tune dashboard">
            <header class="top">
                <div class="brand">
                    <img src="${logoUri}" alt="">
                    <div>
                        <h1 class="title">${escapeHtml(state.title)}</h1>
                        <div class="subtitle">${escapeHtml(state.subtitle)}</div>
                    </div>
                </div>
                <div>
                    <button class="primary-action" type="button" data-command="runTune">&#9655;&nbsp;&nbsp;Run tune</button>
                    <div class="run-note">Complete harness setup to run</div>
                </div>
            </header>

            <div class="stepper" aria-label="Tune workflow stages">
                ${state.stages.map(renderStage).join("")}
            </div>

            <section class="summary-strip" aria-label="Dashboard summary">
                ${state.summaryItems.map(renderSummaryItem).join("")}
                <button class="refresh" type="button" data-command="refreshChecks">&#8635;&nbsp;Refresh checks</button>
            </section>

            <section class="content">
                <aside class="panel config" aria-label="Configuration">
                    <h2 class="section-title">Configuration</h2>
                    ${state.configFields.map(renderConfigField).join("")}
                </aside>

                <section class="panel main-panel" aria-label="Repository readiness and tune results">
                    <div class="readiness">
                        <div class="section-heading">
                            <h2 class="section-title">Repository readiness</h2>
                            <span class="caption">${escapeHtml(state.readinessLabel)}</span>
                        </div>
                        ${state.readinessChecks.map(renderReadinessCheck).join("")}
                    </div>

                    <div class="results">
                        <h2 class="section-title">Tune results</h2>
                        <div class="caption">Artifacts will appear after a successful run</div>
                        ${state.artifacts.map(renderArtifact).join("")}
                    </div>
                </section>
            </section>
        </section>
    </main>
    <script nonce="${nonce}">
        const vscode = acquireVsCodeApi();
        document.querySelectorAll("[data-command]").forEach((element) => {
            element.addEventListener("click", () => {
                vscode.postMessage({ command: element.getAttribute("data-command") });
            });
        });
    </script>
</body>
</html>`;
}

function renderStage(stage: DashboardStage): string {
  const activeClass = stage.active ? " active" : "";
  const detail = stage.detail ? escapeHtml(stage.detail) : "";
  return `<div class="step${activeClass}">
        <div class="step-index">${stage.number}</div>
        <div class="step-label">${escapeHtml(stage.label)}</div>
        <div class="step-detail">${detail}</div>
    </div>`;
}

function renderSummaryItem(item: DashboardSummaryItem): string {
  return `<div class="summary-item">
        ${renderStatusDot(item.status)}
        <div>
            <div class="summary-label">${escapeHtml(item.label)}</div>
            <div class="summary-detail">${escapeHtml(item.detail)}</div>
        </div>
    </div>`;
}

function renderConfigField(field: DashboardConfigField): string {
  if (field.kind === "toggle") {
    return `<div class="field">
            <div class="field-label">${escapeHtml(field.label)}</div>
            <div class="toggle-row">
                <span class="toggle" aria-hidden="true"></span>
                <span class="caption">${escapeHtml(field.value)}</span>
            </div>
        </div>`;
  }

  const icon = field.kind === "password" ? "&#128065;" : "&#8964;";
  const note = field.note
    ? `<div class="field-note">${escapeHtml(field.note)}</div>`
    : "";
  return `<div class="field">
        <div class="field-label">${escapeHtml(field.label)}</div>
        <div class="control" role="textbox" aria-readonly="true">
            <span class="control-value">${escapeHtml(field.value)}</span>
            <span class="control-icon" aria-hidden="true">${icon}</span>
        </div>
        ${note}
    </div>`;
}

function renderReadinessCheck(check: DashboardCheck): string {
  const trailing = check.action
    ? `<button class="row-action" type="button" data-command="createHarness">+&nbsp;${escapeHtml(check.action)}</button>`
    : `<span class="row-trailing">${escapeHtml(check.trailing ?? "")}</span>`;
  return `<div class="check-row">
        ${renderStatusDot(check.status)}
        <div class="row-title">${escapeHtml(check.label)}</div>
        <span class="badge status-${check.status}">${escapeHtml(check.badge)}</span>
        <div class="check-detail">${escapeHtml(check.detail)}</div>
        ${trailing}
    </div>`;
}

function renderArtifact(artifact: DashboardArtifact): string {
  return `<div class="artifact">
        <span class="file-icon" aria-hidden="true"></span>
        <span>${escapeHtml(artifact.label)}</span>
        <span class="skeleton" aria-hidden="true"></span>
        <button class="more" type="button" aria-label="Artifact actions">&#8942;</button>
    </div>`;
}

function renderStatusDot(status: DashboardStatus): string {
  const symbol =
    status === "ready"
      ? "&#10003;"
      : status === "warning"
        ? "!"
        : status === "idle"
          ? "&#9711;"
          : "-";
  return `<span class="status-dot status-${status}" aria-hidden="true">${symbol}</span>`;
}

function createNonce(): string {
  const alphabet =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  let nonce = "";
  for (let index = 0; index < 32; index++) {
    nonce += alphabet.charAt(Math.floor(Math.random() * alphabet.length));
  }

  return nonce;
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (character) => {
    switch (character) {
      case "&":
        return "&amp;";
      case "<":
        return "&lt;";
      case ">":
        return "&gt;";
      case '"':
        return "&quot;";
      case "'":
        return "&#39;";
      default:
        return character;
    }
  });
}
