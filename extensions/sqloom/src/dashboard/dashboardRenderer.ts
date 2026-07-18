import * as vscode from "vscode";
import {
  DashboardArtifact,
  DashboardCheck,
  DashboardConfigField,
  DashboardStage,
  DashboardStatus,
  DashboardSummaryItem,
} from "../sharedInterfaces/dashboard";
import { escapeHtml, getNonce } from "../utils/webview";
import { createDashboardState } from "./dashboardState";
import { dashboardStyles } from "./dashboardStyles";

export async function renderDashboardHtml(
  context: vscode.ExtensionContext,
  webview: vscode.Webview,
): Promise<string> {
  const nonce = getNonce();
  const state = await createDashboardState();
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
${dashboardStyles}
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
  const status =
    field.status && field.statusLabel
      ? `<span class="field-status status-${field.status}">${escapeHtml(field.statusLabel)}</span>`
      : "";
  const heading = `<div class="field-heading">
            <div class="field-label">${escapeHtml(field.label)}</div>
            ${status}
        </div>`;

  if (field.kind === "toggle") {
    return `<div class="field">
            ${heading}
            <div class="toggle-row">
                <span class="toggle" aria-hidden="true"></span>
                <span class="caption">${escapeHtml(field.value)}</span>
            </div>
        </div>`;
  }

  const icon =
    field.kind === "password"
      ? "&#128065;"
      : field.kind === "text"
        ? ""
        : "&#8964;";
  const iconElement =
    icon.length > 0
      ? `<span class="control-icon" aria-hidden="true">${icon}</span>`
      : "";
  const note = field.note
    ? `<div class="field-note">${escapeHtml(field.note)}</div>`
    : "";
  return `<div class="field">
        ${heading}
        <div class="control" role="textbox" aria-readonly="true">
            <span class="control-value">${escapeHtml(field.value)}</span>
            ${iconElement}
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
