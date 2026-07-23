import * as vscode from "vscode";
import {
  DashboardArtifact,
  DashboardArtifactsPanelState,
  DashboardRecentRun,
  DashboardSetupField,
  DashboardSetupSummaryItem,
  DashboardStage,
  DashboardStatus,
  DashboardStatusCheck,
} from "../sharedInterfaces/dashboard";
import type { TuneRunRecord } from "../recentRuns/recentRunsStore";
import { escapeHtml, getNonce, serializeForScript } from "../utils/webview";
import { createDashboardState } from "./dashboardState";
import { dashboardStyles } from "./dashboardStyles";

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
  const cliField = state.setupFields.find((field) => field.id === "cliPath");
  const verifiedCliPath = cliField?.value ?? "sqloom";
  const verifiedCliReady = cliField?.status === "ready";
  const verifiedCliDetail = cliField?.note ?? "";
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
    <main class="dashboard" data-mode="run" data-detected-harness="${escapeHtml(detectedHarness)}" data-verified-cli-path="${escapeHtml(verifiedCliPath)}" data-cli-ready="${verifiedCliReady ? "true" : "false"}" data-cli-detail="${escapeHtml(verifiedCliDetail)}" data-readiness-check-count="${state.readinessChecks.length}">
        <header class="topbar">
            <div class="brand">
                <img src="${logoUri}" alt="">
                <div class="brand-copy">
                    <h1>${escapeHtml(state.title)}</h1>
                    <p>${escapeHtml(state.subtitle)}</p>
                </div>
            </div>
            <div class="top-actions">
                <button class="primary-action" type="button" data-command="runTune" disabled>${renderIcon("play")}<span>Run tune</span></button>
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
                        <button class="secondary-action" type="button" data-client-action="editSetup">${renderIcon("edit")}<span>Edit setup</span></button>
                    </div>
                    <div class="setup-summary-grid">
                        ${state.setupSummaryItems.map(renderSetupSummaryItem).join("")}
                    </div>
                    <div class="detected-line">
                        ${renderStatusDot(state.runStatusChecks[1]?.status ?? "neutral")}
                        <span>Using detected harness: <span data-summary-detail="harnessPath">${escapeHtml(state.setupSummaryItems.find((item) => item.id === "harnessPath")?.detail ?? "Not detected")}</span></span>
                    </div>
                    <div class="endpoint-summary status-warning" data-endpoint-summary>
                        ${renderStatusDot("warning")}
                        <span>Replay endpoint: <strong data-selected-endpoint>Not selected</strong></span>
                        <span class="endpoint-summary-detail" data-endpoint-summary-detail>Loading endpoints…</span>
                    </div>
                </section>

                <section class="panel setup-panel mode-edit" aria-label="Edit setup">
                    <div class="panel-heading">
                        <div>
                            <h2>Edit setup</h2>
                            <p>Update required values before running.</p>
                        </div>
                        <button class="secondary-action" type="button" data-client-action="validateSetup">${renderIcon("refresh")}<span>Validate</span></button>
                    </div>
                    <input type="hidden" value="openai" data-field-id="modelProvider">
                    <div class="edit-grid">
                        ${state.setupFields.map(renderSetupField).join("")}
                        <div class="field endpoint-selector status-warning" data-endpoint-selector>
                            <div class="field-label">
                                <label for="sqloom-replay-endpoint">Replay endpoint <span aria-hidden="true">*</span></label>
                                ${renderStatusDot("warning")}
                            </div>
                            <div class="compound-control">
                                <select id="sqloom-replay-endpoint" class="control control-select" data-field-id="target" required disabled>
                                    <option value="">Loading endpoints…</option>
                                </select>
                                <button class="field-action" type="button" data-client-action="refreshEndpoints">Refresh</button>
                            </div>
                            <div class="field-note" data-endpoint-note>Endpoints come from the selected Sqloom CLI and harness.</div>
                        </div>
                    </div>
                    <div class="detected-line edit-detected">
                        ${renderStatusDot(state.editStatusChecks[1]?.status ?? "neutral")}
                        <span>Detected harness: <span data-detected-harness-label>${escapeHtml(detectedHarness || "Not detected")}</span></span>
                    </div>
                    <div class="edit-actions">
                        <button class="secondary-action" type="button" data-client-action="cancelSetup">Cancel</button>
                        <button class="primary-action compact" type="button" data-client-action="applySetup">${renderIcon("check")}<span>Apply setup</span></button>
                    </div>
                </section>

                <section class="panel artifacts-panel" aria-label="Tune results and artifacts">
                    ${renderArtifactsPanel(artifactsPanel)}
                </section>
            </div>

            <aside class="side-column" aria-label="Setup status and recent runs">
                ${renderStatusPanel("run", state.runStatusTitle, state.runStatusSubtitle, state.runStatusChecks, true)}
                ${renderStatusPanel("edit", state.editStatusTitle, state.editStatusSubtitle, state.editStatusChecks, false)}
                <section class="panel recent-panel" aria-label="${escapeHtml(state.recentRunsTitle)}">
                    <div class="panel-heading compact-heading">
                        <h2>${escapeHtml(state.recentRunsTitle)}</h2>
                        <button class="link-button" type="button" data-command="openRecentRunsFolder">${escapeHtml(state.recentRunsAction)}</button>
                    </div>
                    ${renderRecentRunsPanel(state, artifactsPanel.selectedRunId)}
                </section>
            </aside>
        </section>
    </main>
    <script nonce="${nonce}">
        const vscode = acquireVsCodeApi();
        const root = document.querySelector(".dashboard");
        const detectedHarnessPath = root ? root.getAttribute("data-detected-harness") || "" : "";
        let verifiedCliPath = root ? root.getAttribute("data-verified-cli-path") || "" : "";
        let verifiedCliReady = root ? root.getAttribute("data-cli-ready") === "true" : false;
        let verifiedCliDetail = root ? root.getAttribute("data-cli-detail") || "" : "";
        const readinessCheckCount = root ? Number(root.getAttribute("data-readiness-check-count") || "0") : 0;
        let activeTuneRunId = "";
        let tuneRunInFlight = false;
        let appliedSetup = captureForm();
        let endpointRequestSequence = 0;
        let activeEndpointRequestId = "";
        let activeEndpointRequestContext = "";
        let loadedEndpointContext = "";
        let endpointLoadState = "idle";
        let recentRuns = ${serializeForScript(state.recentRuns)};
        let artifactsPanel = ${serializeForScript(artifactsPanel)};
        let selectedRunId = artifactsPanel.selectedRunId || "";
        const recentRunsEmptyTitle = ${serializeForScript(state.recentRunsEmptyTitle)};
        const recentRunsEmptyDetail = ${serializeForScript(state.recentRunsEmptyDetail)};

        window.addEventListener("message", (event) => {
            const message = event.data;
            if (!message || typeof message !== "object") {
                return;
            }

            if (message.command === "tuneRunStarted" && message.runId) {
                activeTuneRunId = message.runId;
                tuneRunInFlight = true;
                resetTuneStages();
                setTuneStage("replay", "active");
                setRunTuneDisabled(true);
                return;
            }

            if (message.command === "tuneProgress" && message.runId === activeTuneRunId) {
                setTuneStage(message.stage, message.status, message.detail);
                return;
            }

            if (message.command === "tuneRunFinished" && message.runId === activeTuneRunId) {
                tuneRunInFlight = false;
                activeTuneRunId = "";
                setRunTuneDisabled(false);
                if (Array.isArray(message.recentRuns)) {
                    recentRuns = message.recentRuns;
                    renderRecentRunsPanel();
                }
                if (message.artifactsPanel) {
                    applyArtifactsPanel(message.artifactsPanel);
                }
                validateSetup();
                return;
            }

            if (message.command === "artifactsLoaded" && message.panel) {
                if (message.runId && message.runId !== selectedRunId) {
                    return;
                }
                applyArtifactsPanel(message.panel);
                return;
            }

            if (message.command === "recentRunsUpdated" && Array.isArray(message.recentRuns)) {
                recentRuns = message.recentRuns;
                renderRecentRunsPanel();
                return;
            }

            if (message.requestId !== activeEndpointRequestId) {
                return;
            }

            if (activeEndpointRequestContext !== endpointContext(captureForm())) {
                return;
            }

            if (message.command === "endpointsLoaded") {
                const endpoints = Array.isArray(message.endpoints) ? message.endpoints : [];
                loadedEndpointContext = activeEndpointRequestContext;
                verifiedCliPath = captureForm().cliPath.trim();
                verifiedCliReady = true;
                verifiedCliDetail = "Sqloom CLI verified by endpoint discovery.";
                populateEndpointOptions(endpoints);
                if (endpoints.length === 0) {
                    setEndpointCatalogState("empty", "No replayable endpoints were discovered.");
                } else {
                    setEndpointCatalogState(
                        "loaded",
                        endpoints.length + " endpoint" + (endpoints.length === 1 ? "" : "s") + " loaded.",
                    );
                }
                validateSetup();
                return;
            }

            if (message.command === "endpointsFailed") {
                loadedEndpointContext = "";
                populateEndpointOptions([]);
                setEndpointCatalogState("failed", message.message || "Unable to load Sqloom endpoints.");
                validateSetup();
            }
        });

        document.querySelectorAll("[data-command='runTune']").forEach((element) => {
            element.addEventListener("click", () => {
                vscode.postMessage({ command: "runTune", payload: readTuneRequest() });
            });
        });

        document.querySelectorAll("[data-command='openRecentRunsFolder']").forEach((element) => {
            element.addEventListener("click", () => {
                vscode.postMessage({ command: "openRecentRunsFolder" });
            });
        });

        const recentRunsPanel = document.querySelector("[data-recent-runs-panel]");
        const artifactsPanelRoot = document.querySelector("[data-artifacts-panel]");
        recentRunsPanel?.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof Element)) {
                return;
            }

            const button = target.closest("[data-command='selectRecentRun']");
            if (!(button instanceof HTMLElement)) {
                return;
            }

            const runId = button.getAttribute("data-run-id") || "";
            const artifactDir = button.getAttribute("data-artifact-dir") || "";
            if (runId.length === 0 || artifactDir.length === 0) {
                return;
            }

            selectedRunId = runId;
            renderRecentRunsPanel();
            vscode.postMessage({ command: "selectRecentRun", runId, artifactDir });
        });

        artifactsPanelRoot?.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof Element)) {
                return;
            }

            const previewButton = target.closest("[data-command='openArtifact']");
            if (previewButton instanceof HTMLElement) {
                const relativePath = previewButton.getAttribute("data-relative-path") || "";
                if (relativePath.length > 0) {
                    vscode.postMessage({
                        command: "openArtifact",
                        relativePath,
                        workspaceFolderUri: artifactsPanel.workspaceFolderUri || "",
                    });
                }
                return;
            }

            const revealButton = target.closest("[data-command='revealArtifact']");
            if (revealButton instanceof HTMLElement) {
                const relativePath = revealButton.getAttribute("data-relative-path") || "";
                if (relativePath.length > 0) {
                    vscode.postMessage({
                        command: "revealArtifact",
                        relativePath,
                        workspaceFolderUri: artifactsPanel.workspaceFolderUri || "",
                    });
                }
            }
        });

        renderRecentRunsPanel();
        renderArtifactsPanel();

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
                    const nextSetup = captureForm();
                    const contextChanged = endpointContext(nextSetup) !== endpointContext(appliedSetup);
                    const currentCatalogLoaded =
                        endpointLoadState === "loaded" &&
                        loadedEndpointContext === endpointContext(nextSetup);
                    if (contextChanged && !currentCatalogLoaded) {
                        nextSetup.target = "";
                        writeField("target", "");
                    }
                    appliedSetup = nextSetup;
                    updateRunSummary(appliedSetup);
                    validateSetup();
                    setMode("run");
                    if (contextChanged && !currentCatalogLoaded) {
                        loadEndpoints();
                    }
                    return;
                }

                if (action === "validateSetup") {
                    validateSetup();
                    return;
                }

                if (action === "detectHarness") {
                    writeField("harnessPath", detectedHarnessPath);
                    validateSetup();
                    return;
                }

                if (action === "refreshEndpoints") {
                    loadEndpoints();
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
        loadEndpoints();

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
                target: values.target,
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
                target: readField("target"),
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
            const cliStatus = currentCliStatus(values);
            const cliPresent = values.cliPath.trim().length > 0;
            const cliReady = cliPresent && cliStatus.ready;
            const sqlConnectionReady = values.readOnlyConnectionString.trim().length > 0;
            const endpointReady =
                endpointLoadState === "loaded" &&
                loadedEndpointContext === endpointContext(values) &&
                values.target.trim().length > 0;
            const requiredPresent =
                values.workspace.trim().length > 0 &&
                cliPresent &&
                values.openAiModel.trim().length > 0 &&
                values.harnessPath.trim().length > 0 &&
                endpointReady &&
                values.openAiApiKey.trim().length > 0 &&
                sqlConnectionReady;
            const harnessPresent = values.harnessPath.trim().length > 0;
            const ready = requiredPresent && harnessPresent && cliReady;
            const warningDetail = setupWarningDetail(values, cliStatus, cliReady);

            updateStatusCheck(
                "required",
                requiredPresent ? "ready" : "warning",
                requiredPresent ? "Required values present" : "Required values missing",
                requiredPresent ? "All required fields are filled." : warningDetail,
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
                ready ? "Setup is valid and ready to run." : warningDetail,
            );

            const detectedLabel = document.querySelector("[data-detected-harness-label]");
            if (detectedLabel) {
                detectedLabel.textContent = harnessPresent ? values.harnessPath : "Not detected";
            }
            updateEndpointSummary(values, endpointReady);
            setRunTuneDisabled(!ready);
        }

        const defaultStageDetails = {
            replay: "Captures SQL evidence during replay",
            observe: "",
            correlate: "",
            advise: "",
        };

        function resetTuneStages() {
            setTuneStage("replay", "pending", defaultStageDetails.replay);
            setTuneStage("observe", "pending", defaultStageDetails.observe);
            setTuneStage("correlate", "pending", defaultStageDetails.correlate);
            setTuneStage("advise", "pending", defaultStageDetails.advise);
        }

        function setTuneStage(stageId, status, detail) {
            document.querySelectorAll('[data-stage-id="' + stageId + '"]').forEach((element) => {
                element.classList.remove("active", "completed", "failed");
                if (status === "active" || status === "completed" || status === "failed") {
                    element.classList.add(status);
                }
                const detailElement = element.querySelector(".step-detail");
                if (detailElement) {
                    if (detail !== undefined) {
                        detailElement.textContent = detail;
                    } else if (status === "pending") {
                        detailElement.textContent = defaultStageDetails[stageId] || "";
                    }
                }
            });
        }

        function setRunTuneDisabled(disabled) {
            document.querySelectorAll("[data-command='runTune']").forEach((button) => {
                if (button instanceof HTMLButtonElement) {
                    button.disabled = disabled || tuneRunInFlight;
                }
            });
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
                }
            });
        }

        function updateRunSummary(values) {
            const workspaceReady = values.workspace.trim().length > 0;
            const cliStatus = currentCliStatus(values);
            const cliReady = values.cliPath.trim().length > 0 && cliStatus.ready;
            const modelReady = values.openAiModel.trim().length > 0;
            const harnessReady = values.harnessPath.trim().length > 0;
            const apiKeyReady = values.openAiApiKey.trim().length > 0;
            const sqlConnectionReady = values.readOnlyConnectionString.trim().length > 0;
            const endpointReady =
                endpointLoadState === "loaded" &&
                loadedEndpointContext === endpointContext(values) &&
                values.target.trim().length > 0;
            const setupReady = workspaceReady && cliReady && modelReady && harnessReady && endpointReady && apiKeyReady && sqlConnectionReady;
            const warningDetail = setupWarningDetail(values, cliStatus, cliReady);

            updateSummaryValue("cliPath", values.cliPath || "sqloom", cliStatus.detail, cliReady ? "ready" : "warning");
            updateSummaryValue("openAiModel", values.openAiModel || "Not selected", "Model used for tuning.", modelReady ? "ready" : "warning");
            updateSummaryValue("harnessPath", values.harnessPath ? "Default harness" : "Not detected", values.harnessPath || "Default harness path was not found.", harnessReady ? "ready" : "warning");
            updateSummaryValue("openAiApiKey", values.openAiApiKey ? "Configured" : "Required", values.openAiApiKey ? "Provided for this session." : "Enter before running tune.", apiKeyReady ? "ready" : "warning");
            updateSummaryValue("readOnlyConnectionString", sqlConnectionReady ? "Configured" : "Required", sqlConnectionReady ? "Provided for this session." : "Enter before running tune.", sqlConnectionReady ? "ready" : "warning");
            updateStatusCheck(
                "setup",
                setupReady ? "ready" : "warning",
                setupReady ? "Run setup is valid" : "Run setup needs attention",
                setupReady ? "All required settings are configured." : warningDetail,
            );
            updateStatusCheck(
                "preflight",
                setupReady ? "ready" : "warning",
                setupReady ? "Preflight checks passed" : "Preflight checks need review",
                setupReady ? readinessCheckCount + " of " + readinessCheckCount + " checks passed" : warningDetail,
            );
        }

        function setupWarningDetail(values, cliStatus, cliReady) {
            if (!cliReady) {
                return cliStatus.detail;
            }

            if (values.workspace.trim().length === 0) {
                return "Open a workspace before running tune.";
            }

            if (values.openAiModel.trim().length === 0) {
                return "Select an OpenAI model before running tune.";
            }

            if (values.harnessPath.trim().length === 0) {
                return "Enter or detect a harness path before running tune.";
            }

            if (
                endpointLoadState !== "loaded" ||
                loadedEndpointContext !== endpointContext(values) ||
                values.target.trim().length === 0
            ) {
                return endpointStatusDetail(values);
            }

            if (values.openAiApiKey.trim().length === 0) {
                return "Enter an OpenAI API key before running tune.";
            }

            if (values.readOnlyConnectionString.trim().length === 0) {
                return "Enter a read-only SQL Server connection string before running tune.";
            }

            return "Review required values before running.";
        }

        function loadEndpoints() {
            const values = captureForm();
            if (values.cliPath.trim().length === 0 || values.harnessPath.trim().length === 0) {
                loadedEndpointContext = "";
                populateEndpointOptions([]);
                setEndpointCatalogState(
                    "failed",
                    values.cliPath.trim().length === 0
                        ? "Enter the Sqloom CLI executable or path."
                        : "Enter or detect a harness path before loading endpoints.",
                );
                validateSetup();
                return;
            }

            endpointRequestSequence += 1;
            activeEndpointRequestId = "endpoint-" + endpointRequestSequence;
            activeEndpointRequestContext = endpointContext(values);
            setEndpointCatalogState("loading", "Loading endpoints from Sqloom…");
            validateSetup();
            vscode.postMessage({
                command: "loadEndpoints",
                requestId: activeEndpointRequestId,
                payload: {
                    cliPath: values.cliPath,
                    harnessPath: values.harnessPath,
                },
            });
        }

        function populateEndpointOptions(endpoints) {
            const select = document.querySelector('[data-field-id="target"]');
            if (!(select instanceof HTMLSelectElement)) {
                return;
            }

            const previousSelection = select.value;
            select.replaceChildren();
            const placeholder = document.createElement("option");
            placeholder.value = "";
            placeholder.textContent = endpoints.length === 0
                ? "No endpoints available"
                : "Select an endpoint…";
            select.appendChild(placeholder);
            endpoints.forEach((endpoint) => {
                const option = document.createElement("option");
                option.value = endpoint.stableOperationKey;
                option.textContent = endpoint.stableOperationKey;
                select.appendChild(option);
            });
            if (endpoints.some((endpoint) => endpoint.stableOperationKey === previousSelection)) {
                select.value = previousSelection;
            }
        }

        function setEndpointCatalogState(state, detail) {
            endpointLoadState = state;
            const select = document.querySelector('[data-field-id="target"]');
            if (select instanceof HTMLSelectElement) {
                select.disabled = state !== "loaded";
            }

            const status = state === "loaded" ? "ready" : state === "loading" ? "idle" : "warning";
            document.querySelectorAll("[data-endpoint-selector], [data-endpoint-summary]").forEach((element) => {
                element.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                element.classList.add("status-" + status);
                const dot = element.querySelector(".status-dot");
                if (dot) {
                    dot.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                    dot.classList.add("status-" + status);
                }
            });
            document.querySelectorAll("[data-endpoint-note], [data-endpoint-summary-detail]").forEach((element) => {
                element.textContent = detail;
            });
        }

        function updateEndpointSummary(values, endpointReady) {
            const selectedEndpoint = document.querySelector("[data-selected-endpoint]");
            if (selectedEndpoint) {
                selectedEndpoint.textContent = endpointReady ? values.target : "Not selected";
            }
            const status = endpointReady
                ? "ready"
                : endpointLoadState === "loading"
                  ? "idle"
                  : "warning";
            document.querySelectorAll("[data-endpoint-selector], [data-endpoint-summary]").forEach((element) => {
                element.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                element.classList.add("status-" + status);
                const dot = element.querySelector(".status-dot");
                if (dot) {
                    dot.classList.remove("status-ready", "status-warning", "status-neutral", "status-idle");
                    dot.classList.add("status-" + status);
                }
            });
        }

        function endpointStatusDetail(values) {
            const currentContext = endpointContext(values);
            if (
                (loadedEndpointContext.length > 0 && loadedEndpointContext !== currentContext) ||
                (activeEndpointRequestContext.length > 0 && activeEndpointRequestContext !== currentContext)
            ) {
                return "Apply setup or refresh endpoints for the current CLI and harness.";
            }

            if (endpointLoadState === "loading") {
                return "Wait for endpoint discovery to finish.";
            }
            if (endpointLoadState === "empty") {
                return "No replayable endpoints were discovered.";
            }
            if (endpointLoadState === "failed") {
                return "Refresh endpoints after resolving the discovery error.";
            }

            return "Select a replay endpoint before running tune.";
        }

        function endpointContext(values) {
            return values.cliPath.trim() + "\\n" + values.harnessPath.trim();
        }

        function currentCliStatus(values) {
            const cliPath = values.cliPath.trim();
            if (cliPath.length === 0) {
                return { ready: false, detail: "Enter the Sqloom CLI executable or path." };
            }

            if (cliPath === verifiedCliPath.trim()) {
                return {
                    ready: verifiedCliReady,
                    detail: verifiedCliDetail || (verifiedCliReady ? "Sqloom CLI verified." : "Sqloom CLI unavailable."),
                };
            }

            return {
                ready: false,
                detail: "Refresh the dashboard to verify this CLI path.",
            };
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
                }
            }
            document.querySelectorAll('[data-summary-value="' + id + '"]').forEach((valueElement) => {
                valueElement.textContent = value;
            });
            document.querySelectorAll('[data-summary-detail="' + id + '"]').forEach((detailElement) => {
                detailElement.textContent = detail;
            });
        }

        function renderRecentRunsPanel() {
            if (!(recentRunsPanel instanceof HTMLElement)) {
                return;
            }

            if (!Array.isArray(recentRuns) || recentRuns.length === 0) {
                recentRunsPanel.innerHTML = '<div class="empty-state"><span class="clock-icon" aria-hidden="true"></span><strong>'
                    + escapeClientText(recentRunsEmptyTitle)
                    + '</strong><p>'
                    + escapeClientText(recentRunsEmptyDetail)
                    + "</p></div>";
                return;
            }

            recentRunsPanel.innerHTML = '<div class="recent-run-list">'
                + recentRuns.map(renderRecentRunItem).join("")
                + "</div>";
        }

        function renderRecentRunItem(run) {
            const target = escapeClientText(run.target || "Tune run");
            const statusLabel = escapeClientText(run.statusLabel || (run.status === "failed" ? "Failed" : "Completed"));
            const startedRelative = escapeClientText(run.startedRelative || "Just now");
            const artifactDir = escapeClientText(run.artifactDir || "");
            const runId = escapeClientText(run.id || "");
            const status = run.status === "failed" ? "failed" : "completed";
            const selectedClass = run.id === selectedRunId ? " selected" : "";
            return '<button class="recent-run-item' + selectedClass + '" type="button" data-command="selectRecentRun" data-run-id="'
                + runId
                + '" data-artifact-dir="'
                + artifactDir
                + '"><div class="recent-run-header"><strong>'
                + target
                + '</strong><span class="recent-run-badge status-'
                + status
                + '">'
                + statusLabel
                + '</span></div><span class="recent-run-meta">'
                + startedRelative
                + " · "
                + artifactDir
                + "</span></button>";
        }

        function applyArtifactsPanel(panel) {
            artifactsPanel = panel;
            selectedRunId = panel.selectedRunId || selectedRunId;
            renderArtifactsPanel();
            renderRecentRunsPanel();
        }

        function renderArtifactsPanel() {
            if (!(artifactsPanelRoot instanceof HTMLElement)) {
                return;
            }

            artifactsPanelRoot.innerHTML = renderArtifactsPanelMarkup(artifactsPanel);
        }

        function renderArtifactsPanelMarkup(panel) {
            const subtitle = panel.selectedRunId
                ? "Artifacts for the selected tune run."
                : "Artifacts will appear here after a successful run.";
            const tableMarkup = panel.artifacts.length > 0
                ? renderArtifactsTableMarkup(panel.artifacts)
                : '<div class="empty-state compact-empty-state"><strong>'
                    + escapeClientText(panel.emptyTitle)
                    + '</strong><p>'
                    + escapeClientText(panel.emptyDetail)
                    + "</p></div>";

            return '<div class="panel-heading artifact-heading"><div><h2>Tune results / artifacts</h2><p>'
                + escapeClientText(subtitle)
                + '</p></div><div class="artifact-tools"><span data-artifact-count>'
                + escapeClientText(panel.artifactCountLabel)
                + '</span></div></div>'
                + tableMarkup
                + '<div class="artifact-footer"><span data-artifact-footer-primary>'
                + escapeClientText(panel.footerPrimary)
                + '</span><span data-artifact-footer-secondary>'
                + escapeClientText(panel.footerSecondary)
                + "</span></div>";
        }

        function renderArtifactsTableMarkup(artifacts) {
            return '<div class="artifact-table-wrap"><table class="artifact-table"><thead><tr><th>Name</th><th>Type</th><th>Size</th><th>Updated</th><th>Preview / Summary</th><th aria-label="Actions"></th></tr></thead><tbody>'
                + artifacts.map(renderArtifactRowMarkup).join("")
                + "</tbody></table></div>";
        }

        function renderArtifactRowMarkup(artifact) {
            const relativePath = escapeClientText(artifact.relativePath || "");
            return '<tr><td><div class="artifact-name"><span class="artifact-icon tone-'
                + escapeClientText(artifact.typeTone || "other")
                + '" aria-hidden="true"></span><span><strong>'
                + escapeClientText(artifact.name || "")
                + "</strong><small>"
                + escapeClientText(artifact.description || "")
                + '</small></span></div></td><td><span class="type-badge tone-'
                + escapeClientText(artifact.typeTone || "other")
                + '">'
                + escapeClientText(artifact.type || "")
                + "</td><td>"
                + escapeClientText(artifact.size || "")
                + "</td><td>"
                + escapeClientText(artifact.updated || "")
                + "</td><td>"
                + escapeClientText(artifact.summary || "")
                + '</td><td><div class="row-actions"><button class="icon-button text-button" type="button" data-command="openArtifact" data-relative-path="'
                + relativePath
                + '" aria-label="Preview '
                + escapeClientText(artifact.name || "artifact")
                + '">Preview</button><button class="icon-button text-button" type="button" data-command="revealArtifact" data-relative-path="'
                + relativePath
                + '" aria-label="Reveal '
                + escapeClientText(artifact.name || "artifact")
                + '">Show</button></div></td></tr>';
        }

        function escapeClientText(value) {
            return String(value)
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;");
        }
    </script>
</body>
</html>`;
}

function renderStage(stage: DashboardStage): string {
  const statusClass =
    stage.status === "pending" ? "" : ` ${escapeHtml(stage.status)}`;
  const detail = stage.detail ? escapeHtml(stage.detail) : "";
  return `<div class="step${statusClass}" data-stage-id="${escapeHtml(stage.id)}">
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
            <button class="mask-toggle" type="button" data-password-toggle="${fieldId}" aria-label="Show value" title="Show value">${renderIcon("eye")}</button>
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
    ? `<button class="primary-action full-width" type="button" data-command="runTune" disabled>${renderIcon("play")}<span>Run tune</span></button>
        <p class="status-footer">This will replay the selected endpoint with the detected harness.</p>`
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

function renderArtifactsPanel(panel: DashboardArtifactsPanelState): string {
  const subtitle = panel.selectedRunId
    ? "Artifacts for the selected tune run."
    : "Artifacts will appear here after a successful run.";

  return `<div data-artifacts-panel>
        <div class="panel-heading artifact-heading">
            <div>
                <h2>Tune results / artifacts</h2>
                <p>${escapeHtml(subtitle)}</p>
            </div>
            <div class="artifact-tools">
                <span data-artifact-count>${escapeHtml(panel.artifactCountLabel)}</span>
            </div>
        </div>
        ${
          panel.artifacts.length > 0
            ? renderArtifactsTable(panel.artifacts)
            : `<div class="empty-state compact-empty-state">
            <strong>${escapeHtml(panel.emptyTitle)}</strong>
            <p>${escapeHtml(panel.emptyDetail)}</p>
        </div>`
        }
        <div class="artifact-footer">
            <span data-artifact-footer-primary>${escapeHtml(panel.footerPrimary)}</span>
            <span data-artifact-footer-secondary>${escapeHtml(panel.footerSecondary)}</span>
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
                <button class="icon-button text-button" type="button" data-command="openArtifact" data-relative-path="${escapeHtml(artifact.relativePath)}" aria-label="Preview ${escapeHtml(artifact.name)}">Preview</button>
                <button class="icon-button text-button" type="button" data-command="revealArtifact" data-relative-path="${escapeHtml(artifact.relativePath)}" aria-label="Reveal ${escapeHtml(artifact.name)}">Show</button>
            </div>
        </td>
    </tr>`;
}

function renderArtifactIcon(tone: DashboardArtifact["typeTone"]): string {
  switch (tone) {
    case "markdown":
      return renderIcon("fileText", "artifact-type-icon");
    case "json":
      return renderIcon("code", "artifact-type-icon");
    case "sql":
      return renderIcon("database", "artifact-type-icon");
    case "html":
      return renderIcon("browser", "artifact-type-icon");
    default:
      return renderIcon("fileText", "artifact-type-icon");
  }
}

function renderRecentRunsPanel(
  state: {
    recentRuns: readonly DashboardRecentRun[];
    recentRunsEmptyTitle: string;
    recentRunsEmptyDetail: string;
  },
  selectedRunId: string,
): string {
  if (state.recentRuns.length === 0) {
    return `<div class="recent-runs-body" data-recent-runs-panel>
        <div class="empty-state">
            <span class="clock-icon" aria-hidden="true"></span>
            <strong>${escapeHtml(state.recentRunsEmptyTitle)}</strong>
            <p>${escapeHtml(state.recentRunsEmptyDetail)}</p>
        </div>
    </div>`;
  }

  return `<div class="recent-runs-body" data-recent-runs-panel>
        <div class="recent-run-list">
            ${state.recentRuns.map((run) => renderRecentRunItem(run, selectedRunId)).join("")}
        </div>
    </div>`;
}

function renderRecentRunItem(
  run: DashboardRecentRun,
  selectedRunId: string,
): string {
  const statusClass = run.status === "failed" ? "failed" : "completed";
  const selectedClass = run.id === selectedRunId ? " selected" : "";
  return `<button class="recent-run-item${selectedClass}" type="button" data-command="selectRecentRun" data-run-id="${escapeHtml(run.id)}" data-artifact-dir="${escapeHtml(run.artifactDir)}">
        <div class="recent-run-header">
            <strong>${escapeHtml(run.target)}</strong>
            <span class="recent-run-badge status-${statusClass}">${escapeHtml(run.statusLabel)}</span>
        </div>
        <span class="recent-run-meta">${escapeHtml(run.startedRelative)} · ${escapeHtml(run.artifactDir)}</span>
    </button>`;
}

function renderStatusDot(status: DashboardStatus): string {
  return `<span class="status-dot status-${status}" aria-hidden="true"></span>`;
}

type DashboardIcon =
  | "browser"
  | "check"
  | "code"
  | "database"
  | "edit"
  | "eye"
  | "fileText"
  | "menu"
  | "moreVertical"
  | "play"
  | "refresh";

function renderIcon(name: DashboardIcon, className = "button-icon"): string {
  return `<svg class="${className}" viewBox="0 0 24 24" aria-hidden="true" focusable="false">${renderIconBody(name)}</svg>`;
}

function renderIconBody(name: DashboardIcon): string {
  switch (name) {
    case "browser":
      return '<rect x="3" y="5" width="18" height="14" rx="2"></rect><path d="M3 9h18"></path><path d="M8 14l2 2 4-5"></path>';
    case "check":
      return '<path d="M20 6 9 17l-5-5"></path>';
    case "code":
      return '<path d="m8 18-6-6 6-6"></path><path d="m16 6 6 6-6 6"></path>';
    case "database":
      return '<ellipse cx="12" cy="5" rx="8" ry="3"></ellipse><path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5"></path><path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6"></path>';
    case "edit":
      return '<path d="M12 20h9"></path><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"></path>';
    case "eye":
      return '<path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle>';
    case "fileText":
      return '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z"></path><path d="M14 2v6h6"></path><path d="M8 13h8"></path><path d="M8 17h5"></path>';
    case "menu":
      return '<path d="M4 7h16"></path><path d="M4 12h16"></path><path d="M4 17h16"></path>';
    case "moreVertical":
      return '<circle cx="12" cy="5" r="1.7"></circle><circle cx="12" cy="12" r="1.7"></circle><circle cx="12" cy="19" r="1.7"></circle>';
    case "play":
      return '<path class="filled-icon" d="M8 5v14l11-7Z"></path>';
    case "refresh":
      return '<path d="M20 11a8.1 8.1 0 0 0-15.5-2M4 5v4h4"></path><path d="M4 13a8.1 8.1 0 0 0 15.5 2m.5 4v-4h-4"></path>';
    default:
      return "";
  }
}
