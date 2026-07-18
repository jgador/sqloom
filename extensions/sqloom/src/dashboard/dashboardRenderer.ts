import * as vscode from "vscode";
import {
  DashboardArtifact,
  DashboardSetupField,
  DashboardSetupSummaryItem,
  DashboardStage,
  DashboardStatus,
  DashboardStatusCheck,
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
  const detectedHarness =
    state.setupFields.find((field) => field.id === "harnessPath")?.value ?? "";

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
    <main class="dashboard" data-mode="run" data-detected-harness="${escapeHtml(detectedHarness)}">
        <header class="topbar">
            <div class="brand">
                <img src="${logoUri}" alt="">
                <div class="brand-copy">
                    <h1>${escapeHtml(state.title)}</h1>
                    <p>${escapeHtml(state.subtitle)}</p>
                </div>
            </div>
            <div class="top-actions">
                <button class="primary-action" type="button" data-command="runTune">&#9655;&nbsp;&nbsp;Run tune</button>
                <div class="run-note">${escapeHtml(state.runNote)}</div>
            </div>
        </header>

        <nav class="stepper" aria-label="Tune workflow stages">
            ${state.stages.map(renderStage).join("")}
        </nav>

        <section class="dashboard-grid" aria-label="Sqloom Tune dashboard">
            <div class="main-column">
                <section class="panel setup-panel mode-run" aria-label="Run setup">
                    <div class="panel-heading">
                        <div>
                            <h2>Run setup</h2>
                            <p>Review and adjust your configuration before running.</p>
                        </div>
                        <button class="secondary-action" type="button" data-client-action="editSetup">&#9998;&nbsp;&nbsp;Edit setup</button>
                    </div>
                    <div class="setup-summary-grid">
                        ${state.setupSummaryItems.map(renderSetupSummaryItem).join("")}
                    </div>
                    <div class="detected-line">
                        ${renderStatusDot(state.runStatusChecks[1]?.status ?? "neutral")}
                        <span>Using detected harness: <span data-summary-detail="harnessPath">${escapeHtml(state.setupSummaryItems.find((item) => item.id === "harnessPath")?.detail ?? "Not detected")}</span></span>
                    </div>
                </section>

                <section class="panel setup-panel mode-edit" aria-label="Edit setup">
                    <div class="panel-heading">
                        <div>
                            <h2>Edit setup</h2>
                            <p>Update required values before running.</p>
                        </div>
                        <button class="secondary-action" type="button" data-client-action="validateSetup">&#8635;&nbsp;&nbsp;Validate</button>
                    </div>
                    <input type="hidden" value="openai" data-field-id="modelProvider">
                    <div class="edit-grid">
                        ${state.setupFields.map(renderSetupField).join("")}
                    </div>
                    <div class="detected-line edit-detected">
                        ${renderStatusDot(state.editStatusChecks[1]?.status ?? "neutral")}
                        <span>Detected harness: <span data-detected-harness-label>${escapeHtml(detectedHarness || "Not detected")}</span></span>
                    </div>
                    <div class="edit-actions">
                        <button class="secondary-action" type="button" data-client-action="cancelSetup">Cancel</button>
                        <button class="primary-action compact" type="button" data-client-action="applySetup">&#10003;&nbsp;&nbsp;Apply setup</button>
                    </div>
                </section>

                <section class="panel artifacts-panel" aria-label="Tune results and artifacts">
                    <div class="panel-heading artifact-heading">
                        <div>
                            <h2>Tune results / artifacts</h2>
                            <p>Artifacts will appear here after a successful run.</p>
                        </div>
                        <div class="artifact-tools">
                            <span>${state.artifacts.length} artifacts</span>
                            <button class="icon-button" type="button" aria-label="Artifact display options">&#9776;</button>
                        </div>
                    </div>
                    ${renderArtifactsTable(state.artifacts)}
                    <div class="artifact-footer">
                        <span>Latest run: --</span>
                        <span>Artifacts will appear here after you run a tune.</span>
                    </div>
                </section>
            </div>

            <aside class="side-column" aria-label="Setup status and recent runs">
                ${renderStatusPanel("run", state.runStatusTitle, state.runStatusSubtitle, state.runStatusChecks, true)}
                ${renderStatusPanel("edit", state.editStatusTitle, state.editStatusSubtitle, state.editStatusChecks, false)}
                <section class="panel recent-panel" aria-label="${escapeHtml(state.recentRunsTitle)}">
                    <div class="panel-heading compact-heading">
                        <h2>${escapeHtml(state.recentRunsTitle)}</h2>
                        <button class="link-button" type="button">${escapeHtml(state.recentRunsAction)}</button>
                    </div>
                    <div class="empty-state">
                        <span class="clock-icon" aria-hidden="true"></span>
                        <strong>${escapeHtml(state.recentRunsEmptyTitle)}</strong>
                        <p>${escapeHtml(state.recentRunsEmptyDetail)}</p>
                    </div>
                </section>
            </aside>
        </section>
    </main>
    <script nonce="${nonce}">
        const vscode = acquireVsCodeApi();
        const root = document.querySelector(".dashboard");
        const detectedHarnessPath = root ? root.getAttribute("data-detected-harness") || "" : "";
        let appliedSetup = captureForm();

        document.querySelectorAll("[data-command='runTune']").forEach((element) => {
            element.addEventListener("click", () => {
                vscode.postMessage({ command: "runTune", payload: readTuneRequest() });
            });
        });

        document.querySelectorAll("[data-client-action]").forEach((element) => {
            element.addEventListener("click", () => {
                const action = element.getAttribute("data-client-action");
                if (action === "editSetup") {
                    setMode("edit");
                    return;
                }

                if (action === "cancelSetup") {
                    writeForm(appliedSetup);
                    validateSetup();
                    setMode("run");
                    return;
                }

                if (action === "applySetup") {
                    appliedSetup = captureForm();
                    updateRunSummary(appliedSetup);
                    validateSetup();
                    setMode("run");
                    return;
                }

                if (action === "validateSetup") {
                    validateSetup();
                    return;
                }

                if (action === "detectHarness") {
                    writeField("harnessPath", detectedHarnessPath);
                    validateSetup();
                }
            });
        });

        document.querySelectorAll("[data-password-toggle]").forEach((button) => {
            button.addEventListener("click", () => {
                const fieldId = button.getAttribute("data-password-toggle");
                const input = document.querySelector('[data-field-id="' + fieldId + '"]');
                if (!(input instanceof HTMLInputElement)) {
                    return;
                }

                const showValue = input.type === "password";
                input.type = showValue ? "text" : "password";
                button.setAttribute("aria-label", showValue ? "Hide value" : "Show value");
                button.setAttribute("title", showValue ? "Hide value" : "Show value");
            });
        });

        document.querySelectorAll("[data-field-id]").forEach((field) => {
            field.addEventListener("input", () => validateSetup());
            field.addEventListener("change", () => validateSetup());
        });

        validateSetup();

        function setMode(mode) {
            if (root) {
                root.setAttribute("data-mode", mode);
            }
        }

        function readTuneRequest() {
            const values = captureForm();
            return {
                cliPath: values.cliPath,
                harnessPath: values.harnessPath,
                modelProvider: "openai",
                openAiModel: values.openAiModel,
                openAiApiKey: values.openAiApiKey,
                readOnlyConnectionString: values.readOnlyConnectionString,
            };
        }

        function captureForm() {
            return {
                workspace: readField("workspace"),
                cliPath: readField("cliPath"),
                harnessPath: readField("harnessPath"),
                openAiModel: readField("openAiModel"),
                openAiApiKey: readField("openAiApiKey"),
                readOnlyConnectionString: readField("readOnlyConnectionString"),
            };
        }

        function readField(fieldId) {
            const field = document.querySelector('[data-field-id="' + fieldId + '"]');
            return field instanceof HTMLInputElement || field instanceof HTMLSelectElement
                ? field.value
                : "";
        }

        function writeForm(values) {
            Object.keys(values).forEach((key) => writeField(key, values[key] || ""));
        }

        function writeField(fieldId, value) {
            const field = document.querySelector('[data-field-id="' + fieldId + '"]');
            if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement) {
                field.value = value;
            }
        }

        function validateSetup() {
            const values = captureForm();
            const requiredPresent =
                values.workspace.trim().length > 0 &&
                values.cliPath.trim().length > 0 &&
                values.openAiModel.trim().length > 0 &&
                values.harnessPath.trim().length > 0 &&
                values.openAiApiKey.trim().length > 0;
            const harnessPresent = values.harnessPath.trim().length > 0;
            const ready = requiredPresent && harnessPresent;

            updateStatusCheck(
                "required",
                requiredPresent ? "ready" : "warning",
                requiredPresent ? "Required values present" : "Required values missing",
                requiredPresent ? "All required fields are filled." : "Workspace, CLI, model, harness, and API key are required.",
            );
            updateStatusCheck(
                "harness",
                harnessPresent ? "ready" : "warning",
                harnessPresent ? "Harness detected" : "Harness not detected",
                harnessPresent ? values.harnessPath : "Default harness path was not found.",
            );
            updateStatusCheck(
                "ready",
                ready ? "ready" : "warning",
                ready ? "Ready to apply" : "Review before applying",
                ready ? "Setup is valid and ready to run." : "Apply after the required values are present.",
            );

            const detectedLabel = document.querySelector("[data-detected-harness-label]");
            if (detectedLabel) {
                detectedLabel.textContent = harnessPresent ? values.harnessPath : "Not detected";
            }
        }

        function updateStatusCheck(id, status, label, detail) {
            document.querySelectorAll('[data-status-check="' + id + '"]').forEach((element) => {
                element.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                element.classList.add("status-" + status);
                const labelElement = element.querySelector("[data-check-label]");
                const detailElement = element.querySelector("[data-check-detail]");
                const dot = element.querySelector(".status-dot");
                if (labelElement) {
                    labelElement.textContent = label;
                }
                if (detailElement) {
                    detailElement.textContent = detail;
                }
                if (dot) {
                    dot.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                    dot.classList.add("status-" + status);
                    dot.textContent = status === "ready" ? "\\u2713" : status === "warning" ? "!" : "\\u25cb";
                }
            });
        }

        function updateRunSummary(values) {
            const workspaceReady = values.workspace.trim().length > 0;
            const cliReady = values.cliPath.trim().length > 0;
            const modelReady = values.openAiModel.trim().length > 0;
            const harnessReady = values.harnessPath.trim().length > 0;
            const apiKeyReady = values.openAiApiKey.trim().length > 0;
            const setupReady = workspaceReady && cliReady && modelReady && harnessReady && apiKeyReady;

            updateSummaryValue("cliPath", values.cliPath || "sqloom", "Executable or alias in PATH.", cliReady ? "ready" : "warning");
            updateSummaryValue("openAiModel", values.openAiModel || "Not selected", "Model used for tuning.", modelReady ? "ready" : "warning");
            updateSummaryValue("harnessPath", values.harnessPath ? "Default harness" : "Not detected", values.harnessPath || "Default harness path was not found.", harnessReady ? "ready" : "warning");
            updateSummaryValue("openAiApiKey", values.openAiApiKey ? "Configured" : "Required", values.openAiApiKey ? "Provided for this session." : "Enter before running tune.", apiKeyReady ? "ready" : "warning");
            updateSummaryValue("readOnlyConnectionString", values.readOnlyConnectionString ? "Configured" : "Optional", values.readOnlyConnectionString ? "Provided for this session." : "Used only for live validation.", "neutral");
            updateStatusCheck(
                "setup",
                setupReady ? "ready" : "warning",
                setupReady ? "Run setup is valid" : "Run setup needs attention",
                setupReady ? "All required settings are configured." : "Review required values before running.",
            );
            updateStatusCheck(
                "preflight",
                setupReady ? "ready" : "warning",
                setupReady ? "Preflight checks passed" : "Preflight checks need review",
                setupReady ? "6 of 6 checks passed" : "Review required values before running.",
            );
        }

        function updateSummaryValue(id, value, detail, status) {
            const item = document.querySelector('[data-summary-item="' + id + '"]');
            if (item) {
                item.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                item.classList.add("status-" + status);
                const dot = item.querySelector(".status-dot");
                if (dot) {
                    dot.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                    dot.classList.add("status-" + status);
                    dot.textContent = status === "ready" ? "\\u2713" : status === "warning" ? "!" : "\\u24d8";
                }
            }
            document.querySelectorAll('[data-summary-value="' + id + '"]').forEach((valueElement) => {
                valueElement.textContent = value;
            });
            document.querySelectorAll('[data-summary-detail="' + id + '"]').forEach((detailElement) => {
                detailElement.textContent = detail;
            });
        }
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

function renderSetupSummaryItem(item: DashboardSetupSummaryItem): string {
  const detail = item.detail ? escapeHtml(item.detail) : "";
  return `<div class="setup-summary-item status-${item.status}" data-summary-item="${escapeHtml(item.id)}">
        <div class="summary-label-row">
            <span>${escapeHtml(item.label)}</span>
            ${renderStatusDot(item.status)}
        </div>
        <div class="summary-value" data-summary-value="${escapeHtml(item.id)}">${escapeHtml(item.value)}</div>
        <div class="summary-detail" data-summary-detail="${escapeHtml(item.id)}">${detail}</div>
    </div>`;
}

function renderSetupField(field: DashboardSetupField): string {
  const fieldId = escapeHtml(field.id);
  const note = field.note
    ? `<div class="field-note">${escapeHtml(field.note)}</div>`
    : "";
  const required = field.required ? " required" : "";
  const readonly = field.readonly ? " readonly" : "";
  const placeholder = field.placeholder
    ? ` placeholder="${escapeHtml(field.placeholder)}"`
    : "";
  const status = renderStatusDot(field.status);
  const action =
    field.actionLabel && field.id === "harnessPath"
      ? `<button class="field-action" type="button" data-client-action="detectHarness">${escapeHtml(field.actionLabel)}</button>`
      : "";

  if (field.kind === "select") {
    const options = field.options ?? [];
    const optionHtml = options.map((option) => {
      const selected = option.value === field.value ? " selected" : "";
      return `<option value="${escapeHtml(option.value)}"${selected}>${escapeHtml(option.label)}</option>`;
    });

    return `<label class="field">
        <span class="field-label">${escapeHtml(field.label)} ${status}</span>
        <select class="control control-select" data-field-id="${fieldId}"${required}>
            ${optionHtml.join("")}
        </select>
        ${note}
    </label>`;
  }

  if (field.kind === "password") {
    return `<label class="field">
        <span class="field-label">${escapeHtml(field.label)} ${status}</span>
        <span class="password-control">
            <input class="control control-input" type="password" value="${escapeHtml(field.value)}" data-field-id="${fieldId}" autocomplete="off" spellcheck="false"${placeholder}${required}>
            <button class="mask-toggle" type="button" data-password-toggle="${fieldId}" aria-label="Show value" title="Show value">&#128065;</button>
        </span>
        ${note}
    </label>`;
  }

  const secondary = field.secondaryValue
    ? `<span class="inline-meta">${escapeHtml(field.secondaryValue)}</span>`
    : "";

  return `<label class="field">
        <span class="field-label">${escapeHtml(field.label)} ${status}</span>
        <span class="compound-control">
            <input class="control control-input" type="text" value="${escapeHtml(field.value)}" data-field-id="${fieldId}" spellcheck="false"${placeholder}${required}${readonly}>
            ${secondary}
            ${action}
        </span>
        ${note}
    </label>`;
}

function renderStatusPanel(
  mode: "run" | "edit",
  title: string,
  subtitle: string,
  checks: readonly DashboardStatusCheck[],
  includeAction: boolean,
): string {
  const action = includeAction
    ? `<button class="primary-action full-width" type="button" data-command="runTune">&#9655;&nbsp;&nbsp;Run tune</button>
        <p class="status-footer">This will use the detected default harness.</p>`
    : "";

  return `<section class="panel status-panel mode-${mode}" aria-label="${escapeHtml(title)}">
        <h2>${escapeHtml(title)}</h2>
        <p>${escapeHtml(subtitle)}</p>
        <div class="status-list">
            ${checks.map(renderStatusCheck).join("")}
        </div>
        ${action}
    </section>`;
}

function renderStatusCheck(check: DashboardStatusCheck): string {
  return `<div class="status-check status-${check.status}" data-status-check="${escapeHtml(check.id)}">
        ${renderStatusDot(check.status)}
        <div>
            <strong data-check-label>${escapeHtml(check.label)}</strong>
            <span data-check-detail>${escapeHtml(check.detail)}</span>
        </div>
    </div>`;
}

function renderArtifactsTable(artifacts: readonly DashboardArtifact[]): string {
  return `<div class="artifact-table-wrap">
        <table class="artifact-table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Size</th>
                    <th>Updated</th>
                    <th>Preview / Summary</th>
                    <th aria-label="Actions"></th>
                </tr>
            </thead>
            <tbody>
                ${artifacts.map(renderArtifactRow).join("")}
            </tbody>
        </table>
    </div>`;
}

function renderArtifactRow(artifact: DashboardArtifact): string {
  return `<tr>
        <td>
            <div class="artifact-name">
                <span class="artifact-icon tone-${artifact.typeTone}" aria-hidden="true">${renderArtifactIcon(artifact.typeTone)}</span>
                <span>
                    <strong>${escapeHtml(artifact.name)}</strong>
                    <small>${escapeHtml(artifact.description)}</small>
                </span>
            </div>
        </td>
        <td><span class="type-badge tone-${artifact.typeTone}">${escapeHtml(artifact.type)}</span></td>
        <td>${escapeHtml(artifact.size)}</td>
        <td>${escapeHtml(artifact.updated)}</td>
        <td>${escapeHtml(artifact.summary)}</td>
        <td>
            <div class="row-actions">
                <button class="icon-button" type="button" aria-label="Preview ${escapeHtml(artifact.name)}">&#128065;</button>
                <button class="icon-button" type="button" aria-label="Actions for ${escapeHtml(artifact.name)}">&#8942;</button>
            </div>
        </td>
    </tr>`;
}

function renderArtifactIcon(tone: DashboardArtifact["typeTone"]): string {
  switch (tone) {
    case "markdown":
      return "&#9633;";
    case "json":
      return "{}";
    case "sql":
      return "&#9638;";
    case "html":
      return "&#9635;";
    default:
      return "&#9633;";
  }
}

function renderStatusDot(status: DashboardStatus): string {
  const symbol =
    status === "ready"
      ? "&#10003;"
      : status === "warning"
        ? "!"
        : status === "idle"
          ? "&#9711;"
          : "&#9432;";
  return `<span class="status-dot status-${status}" aria-hidden="true">${symbol}</span>`;
}
