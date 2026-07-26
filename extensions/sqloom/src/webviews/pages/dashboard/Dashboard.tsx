import { useState, useEffect, useMemo, useRef, CSSProperties } from "react";
import { sqloomTokens } from "./theme";

interface SetupField {
  id: string;
  label: string;
  value: string;
  status: string;
  note?: string;
  kind?: string;
  options?: { value: string; label: string }[];
  placeholder?: string;
  required?: boolean;
  readonly?: boolean;
  secondaryValue?: string;
  actionLabel?: string;
}

interface SetupSummaryItem {
  id: string;
  label: string;
  value: string;
  detail?: string;
  status: string;
}

interface DashboardStateShape {
  title?: string;
  subtitle?: string;
  logoUri?: string;
  setupFields?: SetupField[];
  setupSummaryItems?: SetupSummaryItem[];
  readinessChecks?: unknown[];
  recentRuns?: RecentRun[];
  recentRunsTitle?: string;
  recentRunsAction?: string;
}

interface Artifact {
  name: string;
  relativePath: string;
  type?: string;
  typeTone?: string;
  stage?: string;
  size?: string;
  updated?: string;
  summary?: string;
  description?: string;
}

interface RecentRun {
  id: string;
  target?: string;
  status?: string;
  statusLabel?: string;
  artifactDir?: string;
  startedRelative?: string;
}

interface Endpoint {
  stableOperationKey: string;
}

interface ArtifactsPanelShape {
  selectedRunId?: string;
  workspaceFolderUri?: string;
  emptyTitle?: string;
  emptyDetail?: string;
  artifactCountLabel?: string;
  footerPrimary?: string;
  footerSecondary?: string;
  artifacts?: Artifact[];
}

// Declare globals injected by extension host
declare global {
  interface Window {
    acquireVsCodeApi: () => { postMessage: (message: unknown) => void };
    __INITIAL_STATE__: unknown;
    __INITIAL_ARTIFACTS__: unknown;
    __LOGO_URI__: string;
  }
}

const vscodeApi = window.acquireVsCodeApi();

// Icon component using SVGs from original design
const Icon = ({
  name,
  className = "button-icon",
}: {
  name: string;
  className?: string;
}) => {
  const getIconBody = (iconName: string) => {
    switch (iconName) {
      case "browser":
        return (
          <>
            <rect x="3" y="5" width="18" height="14" rx="2" />
            <path d="M3 9h18" />
            <path d="M8 14l2 2 4-5" />
          </>
        );
      case "check":
        return <path d="M20 6 9 17l-5-5" />;
      case "code":
        return (
          <>
            <path d="m8 18-6-6 6-6" />
            <path d="m16 6 6 6-6 6" />
          </>
        );
      case "database":
        return (
          <>
            <ellipse cx="12" cy="5" rx="8" ry="3" />
            <path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5" />
            <path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6" />
          </>
        );
      case "edit":
        return (
          <>
            <path d="M12 20h9" />
            <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
          </>
        );
      case "eye":
        return (
          <>
            <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" />
            <circle cx="12" cy="12" r="3" />
          </>
        );
      case "file":
        return (
          <>
            <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z" />
            <polyline points="14 2 14 8 20 8" />
          </>
        );
      case "fileText":
        return (
          <>
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" />
            <path d="M14 2v6h6" />
            <path d="M8 13h8" />
            <path d="M8 17h5" />
          </>
        );
      case "menu":
        return (
          <>
            <path d="M4 7h16" />
            <path d="M4 12h16" />
            <path d="M4 17h16" />
          </>
        );
      case "moreVertical":
        return (
          <>
            <circle cx="12" cy="5" r="1.7" />
            <circle cx="12" cy="12" r="1.7" />
            <circle cx="12" cy="19" r="1.7" />
          </>
        );
      case "play":
        return <path className="filled-icon" d="M8 5v14l11-7Z" />;
      case "refresh":
        return (
          <>
            <path d="M20 11a8.1 8.1 0 0 0-15.5-2M4 5v4h4" />
            <path d="M4 13a8.1 8.1 0 0 0 15.5 2m.5 4v-4h-4" />
          </>
        );
      default:
        return null;
    }
  };

  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      aria-hidden="true"
      focusable="false"
    >
      {getIconBody(name)}
    </svg>
  );
};

const StatusDot = ({ status }: { status: string }) => {
  return <span className={`status-dot status-${status}`} aria-hidden="true" />;
};

export const Dashboard = () => {
  const vscode = vscodeApi;
  const logoUri = window.__LOGO_URI__ || "";

  // Initial values from injected state
  const initialState = (window.__INITIAL_STATE__ || {}) as DashboardStateShape;
  const initialFields = initialState.setupFields || [];
  const getFieldVal = (id: string) =>
    initialFields.find((f) => f.id === id)?.value ?? "";

  const [mode, setMode] = useState<"run" | "edit">("run");

  // Form values
  const [formValues, setFormValues] = useState({
    workspace: getFieldVal("workspace"),
    cliPath: getFieldVal("cliPath"),
    harnessPath: getFieldVal("harnessPath"),
    target: getFieldVal("target"),
    openAiModel: getFieldVal("openAiModel"),
    openAiApiKey: getFieldVal("openAiApiKey"),
    readOnlyConnectionString: getFieldVal("readOnlyConnectionString"),
  });

  const [appliedSetup, setAppliedSetup] = useState({ ...formValues });

  // CLI state verification
  const [verifiedCliPath, setVerifiedCliPath] = useState(
    getFieldVal("cliPath"),
  );
  const [verifiedCliReady, setVerifiedCliReady] = useState(
    initialFields.find((f) => f.id === "cliPath")?.status === "ready",
  );
  const [verifiedCliDetail, setVerifiedCliDetail] = useState(
    initialFields.find((f) => f.id === "cliPath")?.note ?? "",
  );

  // Endpoint loading state
  const [endpoints, setEndpoints] = useState<Endpoint[]>([]);
  const [endpointCatalogState, setEndpointCatalogState] = useState<
    "idle" | "loading" | "loaded" | "empty" | "failed"
  >("idle");
  const [endpointCatalogMessage, setEndpointCatalogMessage] = useState("");
  const [loadedEndpointContext, setLoadedEndpointContext] = useState("");

  const activeEndpointRequestIdRef = useRef("");
  const endpointRequestSequenceRef = useRef(0);

  // Tune run status
  const activeTuneRunIdRef = useRef("");
  const [tuneRunInFlight, setTuneRunInFlight] = useState(false);

  // Tune stages statuses
  const defaultStageDetails: Record<string, string> = {
    replay: "Captures SQL evidence during replay",
    observe: "",
    correlate: "",
    advise: "",
  };
  const [stages, setStages] = useState([
    {
      id: "replay",
      number: 1,
      label: "Replay",
      status: "pending",
      detail: defaultStageDetails.replay,
    },
    {
      id: "observe",
      number: 2,
      label: "Observe",
      status: "pending",
      detail: "",
    },
    {
      id: "correlate",
      number: 3,
      label: "Correlate",
      status: "pending",
      detail: "",
    },
    { id: "advise", number: 4, label: "Advise", status: "pending", detail: "" },
  ]);

  // Recent runs and artifacts
  const [recentRuns, setRecentRuns] = useState<RecentRun[]>(
    initialState.recentRuns || [],
  );
  const [artifactsPanel, setArtifactsPanel] = useState<ArtifactsPanelShape>(
    (window.__INITIAL_ARTIFACTS__ || {}) as ArtifactsPanelShape,
  );
  const [selectedRunId, setSelectedRunId] = useState(
    ((window.__INITIAL_ARTIFACTS__ || {}) as ArtifactsPanelShape)
      .selectedRunId || "",
  );

  // Password visibility state
  const [passwordsVisible, setPasswordsVisible] = useState<
    Record<string, boolean>
  >({
    openAiApiKey: false,
    readOnlyConnectionString: false,
  });

  const getEndpointContext = (values: typeof formValues) => {
    return `${values.cliPath.trim()}\n${values.harnessPath.trim()}`;
  };

  const getCliStatus = (cliPath: string) => {
    const trimmed = cliPath.trim();
    if (trimmed.length === 0) {
      return {
        ready: false,
        detail: "Enter the Sqloom CLI executable or path.",
      };
    }

    if (trimmed === verifiedCliPath.trim()) {
      return {
        ready: verifiedCliReady,
        detail:
          verifiedCliDetail ||
          (verifiedCliReady
            ? "Sqloom CLI verified."
            : "Sqloom CLI unavailable."),
      };
    }

    return {
      ready: false,
      detail: "Refresh the dashboard to verify this CLI path.",
    };
  };

  // Setup Warning detail
  const getSetupWarningDetail = (
    values: typeof formValues,
    cliStatus: { ready: boolean; detail: string },
    cliReady: boolean,
  ) => {
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
    const endpointReady =
      endpointCatalogState === "loaded" &&
      loadedEndpointContext === getEndpointContext(values) &&
      values.target.trim().length > 0;

    if (!endpointReady) {
      const currentContext = getEndpointContext(values);
      if (
        loadedEndpointContext.length > 0 &&
        loadedEndpointContext !== currentContext
      ) {
        return "Apply setup or refresh endpoints for the current CLI and harness.";
      }
      if (endpointCatalogState === "loading") {
        return "Wait for endpoint discovery to finish.";
      }
      if (endpointCatalogState === "empty") {
        return "No replayable endpoints were discovered.";
      }
      if (endpointCatalogState === "failed") {
        return "Refresh endpoints after resolving the discovery error.";
      }
      return "Select a replay endpoint before running tune.";
    }

    if (values.openAiApiKey.trim().length === 0) {
      return "Enter an OpenAI API key before running tune.";
    }
    if (values.readOnlyConnectionString.trim().length === 0) {
      return "Enter a read-only SQL Server connection string before running tune.";
    }

    return "Review required values before running.";
  };

  // Validation
  const validation = useMemo(() => {
    const cliStatus = getCliStatus(formValues.cliPath);
    const cliPresent = formValues.cliPath.trim().length > 0;
    const cliReady = cliPresent && cliStatus.ready;
    const sqlConnectionReady =
      formValues.readOnlyConnectionString.trim().length > 0;

    const endpointReady =
      endpointCatalogState === "loaded" &&
      loadedEndpointContext === getEndpointContext(formValues) &&
      formValues.target.trim().length > 0;

    const requiredPresent =
      formValues.workspace.trim().length > 0 &&
      cliPresent &&
      formValues.openAiModel.trim().length > 0 &&
      formValues.harnessPath.trim().length > 0 &&
      endpointReady &&
      formValues.openAiApiKey.trim().length > 0 &&
      sqlConnectionReady;

    const harnessPresent = formValues.harnessPath.trim().length > 0;
    const ready = requiredPresent && harnessPresent && cliReady;
    const warningDetail = getSetupWarningDetail(
      formValues,
      cliStatus,
      cliReady,
    );

    return {
      ready,
      requiredPresent,
      harnessPresent,
      cliReady,
      cliStatus,
      sqlConnectionReady,
      endpointReady,
      warningDetail,
    };
  }, [
    formValues,
    endpointCatalogState,
    loadedEndpointContext,
    verifiedCliPath,
    verifiedCliReady,
    verifiedCliDetail,
  ]);

  const loadEndpoints = (values = formValues) => {
    if (
      values.cliPath.trim().length === 0 ||
      values.harnessPath.trim().length === 0
    ) {
      setEndpoints([]);
      setLoadedEndpointContext("");
      setEndpointCatalogState("failed");
      setEndpointCatalogMessage(
        values.cliPath.trim().length === 0
          ? "Enter the Sqloom CLI executable or path."
          : "Enter or detect a harness path before loading endpoints.",
      );
      return;
    }

    endpointRequestSequenceRef.current += 1;
    const requestId = "endpoint-" + endpointRequestSequenceRef.current;
    activeEndpointRequestIdRef.current = requestId;

    setEndpointCatalogState("loading");
    setEndpointCatalogMessage("Loading endpoints from Sqloom…");

    vscode.postMessage({
      command: "loadEndpoints",
      requestId,
      payload: {
        cliPath: values.cliPath,
        harnessPath: values.harnessPath,
      },
    });
  };

  // Mount logic
  useEffect(() => {
    loadEndpoints(formValues);

    const handleMessageEvent = (event: MessageEvent) => {
      const message = event.data;
      if (!message || typeof message !== "object") return;

      switch (message.command) {
        case "tuneRunStarted":
          if (message.runId) {
            activeTuneRunIdRef.current = message.runId;
            setTuneRunInFlight(true);
            setStages([
              {
                id: "replay",
                number: 1,
                label: "Replay",
                status: "active",
                detail: defaultStageDetails.replay,
              },
              {
                id: "observe",
                number: 2,
                label: "Observe",
                status: "pending",
                detail: "",
              },
              {
                id: "correlate",
                number: 3,
                label: "Correlate",
                status: "pending",
                detail: "",
              },
              {
                id: "advise",
                number: 4,
                label: "Advise",
                status: "pending",
                detail: "",
              },
            ]);
          }
          break;

        case "tuneProgress":
          if (message.runId === activeTuneRunIdRef.current) {
            setStages((prevStages) =>
              prevStages.map((stg) =>
                stg.id === message.stage
                  ? {
                      ...stg,
                      status: message.status,
                      detail: message.detail ?? stg.detail,
                    }
                  : stg,
              ),
            );
          }
          break;

        case "tuneRunFinished":
          if (message.runId === activeTuneRunIdRef.current) {
            setTuneRunInFlight(false);
            activeTuneRunIdRef.current = "";
            if (Array.isArray(message.recentRuns)) {
              setRecentRuns(message.recentRuns);
            }
            if (message.artifactsPanel) {
              setArtifactsPanel(message.artifactsPanel);
              if (message.artifactsPanel.selectedRunId) {
                setSelectedRunId(message.artifactsPanel.selectedRunId);
              }
            }
          }
          break;

        case "artifactsLoaded":
          if (message.runId && message.runId === selectedRunId) {
            setArtifactsPanel(message.panel);
          }
          break;

        case "recentRunsUpdated":
          if (Array.isArray(message.recentRuns)) {
            setRecentRuns(message.recentRuns);
          }
          break;

        case "endpointsLoaded":
          if (message.requestId === activeEndpointRequestIdRef.current) {
            const list: Endpoint[] = Array.isArray(message.endpoints)
              ? message.endpoints
              : [];
            setEndpoints(list);
            setLoadedEndpointContext(getEndpointContext(formValues));
            setVerifiedCliPath(formValues.cliPath.trim());
            setVerifiedCliReady(true);
            setVerifiedCliDetail("Sqloom CLI verified by endpoint discovery.");

            if (list.length === 0) {
              setEndpointCatalogState("empty");
              setEndpointCatalogMessage(
                "No replayable endpoints were discovered.",
              );
            } else {
              setEndpointCatalogState("loaded");
              setEndpointCatalogMessage(
                `${list.length} endpoint${list.length === 1 ? "" : "s"} loaded.`,
              );
            }

            // Check if current target is in new endpoints list, reset if not
            if (
              !list.some((ep) => ep.stableOperationKey === formValues.target)
            ) {
              setFormValues((prev) => ({ ...prev, target: "" }));
            }
          }
          break;

        case "endpointsFailed":
          if (message.requestId === activeEndpointRequestIdRef.current) {
            setLoadedEndpointContext("");
            setEndpoints([]);
            setEndpointCatalogState("failed");
            setEndpointCatalogMessage(
              message.message || "Unable to load Sqloom endpoints.",
            );
            setFormValues((prev) => ({ ...prev, target: "" }));
          }
          break;
      }
    };

    window.addEventListener("message", handleMessageEvent);
    return () => {
      window.removeEventListener("message", handleMessageEvent);
    };
  }, [formValues.cliPath, formValues.harnessPath, selectedRunId]);

  // Actions
  const handleRunTune = () => {
    vscode.postMessage({
      command: "runTune",
      payload: {
        cliPath: formValues.cliPath,
        harnessPath: formValues.harnessPath,
        target: formValues.target,
        modelProvider: "openai",
        openAiModel: formValues.openAiModel,
        openAiApiKey: formValues.openAiApiKey,
        readOnlyConnectionString: formValues.readOnlyConnectionString,
      },
    });
  };

  const handleEditSetup = () => {
    setMode("edit");
  };

  const handleCancelSetup = () => {
    setFormValues({ ...appliedSetup });
    setMode("run");
  };

  const handleApplySetup = () => {
    const contextChanged =
      getEndpointContext(formValues) !== getEndpointContext(appliedSetup);
    const currentCatalogLoaded =
      endpointCatalogState === "loaded" &&
      loadedEndpointContext === getEndpointContext(formValues);

    const nextValues = { ...formValues };
    if (contextChanged && !currentCatalogLoaded) {
      nextValues.target = "";
      setFormValues((prev) => ({ ...prev, target: "" }));
    }

    setAppliedSetup(nextValues);
    setMode("run");

    if (contextChanged && !currentCatalogLoaded) {
      loadEndpoints(nextValues);
    }
  };

  const handleDetectHarness = () => {
    const detectedHarnessPath =
      initialState.setupFields?.find((f) => f.id === "harnessPath")?.value ||
      "";
    setFormValues((prev) => ({ ...prev, harnessPath: detectedHarnessPath }));
  };

  const handleSelectRecentRun = (runId: string, artifactDir: string) => {
    setSelectedRunId(runId);
    vscode.postMessage({ command: "selectRecentRun", runId, artifactDir });
  };

  const handleOpenArtifact = (relativePath: string) => {
    vscode.postMessage({
      command: "openArtifact",
      relativePath,
      workspaceFolderUri: artifactsPanel.workspaceFolderUri || "",
    });
  };

  const handleRevealArtifact = (relativePath: string) => {
    vscode.postMessage({
      command: "revealArtifact",
      relativePath,
      workspaceFolderUri: artifactsPanel.workspaceFolderUri || "",
    });
  };

  const handleOpenRecentRunsFolder = () => {
    vscode.postMessage({ command: "openRecentRunsFolder" });
  };

  const togglePasswordVisibility = (field: string) => {
    setPasswordsVisible((prev) => ({ ...prev, [field]: !prev[field] }));
  };

  // Render variables
  const showSummarySubtitle = validation.ready
    ? "Everything looks good. You're ready to go."
    : "Review setup before running tune.";

  const workspaceName =
    initialState.setupFields?.find((f) => f.id === "workspace")?.value ??
    "No workspace";
  const workspacePath =
    initialState.setupFields?.find((f) => f.id === "workspace")
      ?.secondaryValue ?? "";

  const modelOptions =
    initialState.setupFields?.find((f) => f.id === "openAiModel")?.options ||
    [];

  const runStatusChecks = useMemo(() => {
    const setupStatus = validation.ready ? "ready" : "warning";
    const setupLabel = validation.ready
      ? "Run setup is valid"
      : "Run setup needs attention";
    const setupDetail = validation.ready
      ? "All required settings are configured."
      : validation.warningDetail;

    const harnessStatus = appliedSetup.harnessPath ? "ready" : "warning";
    const harnessLabel = appliedSetup.harnessPath
      ? "Harness detected"
      : "Harness not detected";
    const harnessDetail =
      appliedSetup.harnessPath || "Default harness path was not found.";

    const preflightStatus = validation.ready ? "ready" : "warning";
    const preflightLabel = validation.ready
      ? "Preflight checks passed"
      : "Preflight checks need review";
    const preflightDetail = validation.ready
      ? `${initialState.readinessChecks?.length || 7} of ${initialState.readinessChecks?.length || 7} checks passed`
      : validation.warningDetail;

    return [
      {
        id: "setup",
        status: setupStatus,
        label: setupLabel,
        detail: setupDetail,
      },
      {
        id: "harness",
        status: harnessStatus,
        label: harnessLabel,
        detail: harnessDetail,
      },
      {
        id: "preflight",
        status: preflightStatus,
        label: preflightLabel,
        detail: preflightDetail,
      },
    ];
  }, [validation, appliedSetup.harnessPath, initialState.readinessChecks]);

  const editStatusChecks = useMemo(() => {
    const reqStatus = validation.requiredPresent ? "ready" : "warning";
    const reqLabel = validation.requiredPresent
      ? "Required values present"
      : "Required values missing";
    const reqDetail = validation.requiredPresent
      ? "All required fields are filled."
      : validation.warningDetail;

    const harnessStatus = formValues.harnessPath ? "ready" : "warning";
    const harnessLabel = formValues.harnessPath
      ? "Harness detected"
      : "Harness not detected";
    const harnessDetail =
      formValues.harnessPath || "Default harness path was not found.";

    const readyStatus = validation.ready ? "ready" : "warning";
    const readyLabel = validation.ready
      ? "Ready to apply"
      : "Review before applying";
    const readyDetail = validation.ready
      ? "Setup is valid and ready to run."
      : validation.warningDetail;

    return [
      { id: "required", status: reqStatus, label: reqLabel, detail: reqDetail },
      {
        id: "harness",
        status: harnessStatus,
        label: harnessLabel,
        detail: harnessDetail,
      },
      {
        id: "ready",
        status: readyStatus,
        label: readyLabel,
        detail: readyDetail,
      },
    ];
  }, [validation, formValues.harnessPath]);

  const themeStyles = useMemo(() => {
    return {
      "--sqloom-border": sqloomTokens.border,
      "--sqloom-muted": sqloomTokens.muted,
      "--sqloom-control": sqloomTokens.control,
      "--sqloom-control-foreground": sqloomTokens.controlForeground,
      "--sqloom-focus": sqloomTokens.focus,
      "--sqloom-ready": sqloomTokens.ready,
      "--sqloom-warning": sqloomTokens.warning,
      "--sqloom-blue": sqloomTokens.blue,
      "--sqloom-blue-hover": sqloomTokens.blueHover,
      "--sqloom-foreground": sqloomTokens.foreground,
      "--sqloom-background": sqloomTokens.background,
      "--sqloom-font-family": sqloomTokens.fontFamily,
      "--sqloom-font-size": sqloomTokens.fontSize,
    } as CSSProperties;
  }, []);

  return (
    <main className="dashboard" data-mode={mode} style={themeStyles}>
      {/* Topbar */}
      <header className="topbar">
        <div className="brand">
          {logoUri && <img src={logoUri} alt="" />}
          <div className="brand-copy">
            <h1>{initialState.title || "Sqloom Tune"}</h1>
            <p>
              {initialState.subtitle ||
                "Tune SQL for performance with confidence."}
            </p>
          </div>
        </div>
        {mode === "run" && (
          <div className="topbar-actions mode-run">
            <button
              className="primary-action compact"
              type="button"
              data-command="runTune"
              disabled={!validation.ready || tuneRunInFlight}
              onClick={handleRunTune}
            >
              <Icon name="play" />
              <span>Run tune</span>
            </button>
          </div>
        )}
      </header>

      {/* Grid */}
      <div className="dashboard-grid" aria-label="Sqloom Tune dashboard">
        {/* Main Column */}
        <section className="main-column">
          {/* Mode Run Setup Panel */}
          {mode === "run" && (
            <section
              className="panel setup-panel mode-run"
              aria-label="Run setup"
            >
              <div className="panel-heading">
                <div>
                  <h2>Run setup</h2>
                  <p>Review and adjust your configuration before running.</p>
                </div>
                <button
                  className="secondary-action"
                  type="button"
                  onClick={handleEditSetup}
                >
                  <Icon name="edit" />
                  <span>Edit setup</span>
                </button>
              </div>
              <div className="setup-summary-grid">
                {/* Workspace summary item */}
                <div
                  className={`setup-summary-item status-${workspaceName !== "No workspace" ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>Workspace</span>
                    <StatusDot
                      status={
                        workspaceName !== "No workspace" ? "ready" : "warning"
                      }
                    />
                  </div>
                  <div className="summary-value">{workspaceName}</div>
                  <div className="summary-detail">{workspacePath}</div>
                </div>

                {/* CLI summary item */}
                <div
                  className={`setup-summary-item status-${validation.cliReady ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>CLI</span>
                    <StatusDot
                      status={validation.cliReady ? "ready" : "warning"}
                    />
                  </div>
                  <div className="summary-value">
                    {appliedSetup.cliPath || "sqloom"}
                  </div>
                  <div className="summary-detail">
                    {validation.cliStatus.detail}
                  </div>
                </div>

                {/* Model summary item */}
                <div
                  className={`setup-summary-item status-${appliedSetup.openAiModel ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>Model</span>
                    <StatusDot
                      status={appliedSetup.openAiModel ? "ready" : "warning"}
                    />
                  </div>
                  <div className="summary-value">
                    {appliedSetup.openAiModel || "Not selected"}
                  </div>
                  <div className="summary-detail">Model used for tuning.</div>
                </div>

                {/* Harness summary item */}
                <div
                  className={`setup-summary-item status-${appliedSetup.harnessPath ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>Harness</span>
                    <StatusDot
                      status={appliedSetup.harnessPath ? "ready" : "warning"}
                    />
                  </div>
                  <div className="summary-value">
                    {appliedSetup.harnessPath
                      ? "Default harness"
                      : "Not detected"}
                  </div>
                  <div className="summary-detail">
                    {appliedSetup.harnessPath ||
                      "Default harness path was not found."}
                  </div>
                </div>

                {/* API Key summary item */}
                <div
                  className={`setup-summary-item status-${appliedSetup.openAiApiKey ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>API key</span>
                    <StatusDot
                      status={appliedSetup.openAiApiKey ? "ready" : "warning"}
                    />
                  </div>
                  <div className="summary-value">
                    {appliedSetup.openAiApiKey ? "Configured" : "Required"}
                  </div>
                  <div className="summary-detail">
                    {appliedSetup.openAiApiKey
                      ? "Provided for this session."
                      : "Enter before running tune."}
                  </div>
                </div>

                {/* Connection String summary item */}
                <div
                  className={`setup-summary-item status-${validation.sqlConnectionReady ? "ready" : "warning"}`}
                >
                  <div className="summary-label-row">
                    <span>SQL connection</span>
                    <StatusDot
                      status={
                        validation.sqlConnectionReady ? "ready" : "warning"
                      }
                    />
                  </div>
                  <div className="summary-value">
                    {validation.sqlConnectionReady ? "Configured" : "Required"}
                  </div>
                  <div className="summary-detail">
                    {validation.sqlConnectionReady
                      ? "Provided for this session."
                      : "Enter before running tune."}
                  </div>
                </div>
              </div>

              <div className="detected-line">
                <StatusDot
                  status={appliedSetup.harnessPath ? "ready" : "neutral"}
                />
                <span>
                  Using detected harness:{" "}
                  <span>{appliedSetup.harnessPath || "Not detected"}</span>
                </span>
              </div>
              <div
                className={`endpoint-summary status-${validation.endpointReady ? "ready" : endpointCatalogState === "loading" ? "idle" : "warning"}`}
              >
                <StatusDot
                  status={
                    validation.endpointReady
                      ? "ready"
                      : endpointCatalogState === "loading"
                        ? "idle"
                        : "warning"
                  }
                />
                <span>
                  Replay endpoint:{" "}
                  <strong>
                    {validation.endpointReady
                      ? appliedSetup.target
                      : "Not selected"}
                  </strong>
                </span>
                <span className="endpoint-summary-detail">
                  {endpointCatalogMessage}
                </span>
              </div>
            </section>
          )}

          {/* Mode Edit Setup Panel */}
          {mode === "edit" && (
            <section
              className="panel setup-panel mode-edit"
              aria-label="Edit setup"
            >
              <div className="panel-heading">
                <div>
                  <h2>Edit setup</h2>
                  <p>Update required values before running.</p>
                </div>
                <button
                  className="secondary-action"
                  type="button"
                  onClick={() => loadEndpoints(formValues)}
                >
                  <Icon name="refresh" />
                  <span>Validate</span>
                </button>
              </div>
              <div className="edit-grid">
                <input type="hidden" value="openai" />

                {/* Workspace (Read Only) */}
                <div className="field">
                  <span className="field-label">
                    Workspace{" "}
                    <StatusDot
                      status={
                        workspaceName !== "No workspace" ? "ready" : "warning"
                      }
                    />
                  </span>
                  <span className="compound-control">
                    <input
                      className="control control-input"
                      type="text"
                      value={workspaceName}
                      readOnly
                    />
                    {workspacePath && (
                      <span className="inline-meta">{workspacePath}</span>
                    )}
                  </span>
                  <div className="field-note">
                    {workspaceName !== "No workspace"
                      ? "Workspace used as the Sqloom command directory."
                      : "Open a folder before running tune."}
                  </div>
                </div>

                {/* CLI Path */}
                <div className="field">
                  <span className="field-label">
                    CLI{" "}
                    <StatusDot
                      status={validation.cliReady ? "ready" : "warning"}
                    />
                  </span>
                  <span className="compound-control">
                    <input
                      className="control control-input"
                      type="text"
                      value={formValues.cliPath}
                      onChange={(e) =>
                        setFormValues((prev) => ({
                          ...prev,
                          cliPath: e.target.value,
                        }))
                      }
                      placeholder="sqloom"
                    />
                  </span>
                  <div className="field-note">
                    {validation.cliStatus.detail}
                  </div>
                </div>

                {/* Model Select */}
                <div className="field">
                  <span className="field-label">
                    Model{" "}
                    <StatusDot
                      status={formValues.openAiModel ? "ready" : "warning"}
                    />
                  </span>
                  <select
                    className="control control-select"
                    value={formValues.openAiModel}
                    onChange={(e) =>
                      setFormValues((prev) => ({
                        ...prev,
                        openAiModel: e.target.value,
                      }))
                    }
                  >
                    {modelOptions.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                  <div className="field-note">Model used for tuning.</div>
                </div>

                {/* Harness Path */}
                <div className="field">
                  <span className="field-label">
                    Harness{" "}
                    <StatusDot
                      status={formValues.harnessPath ? "ready" : "warning"}
                    />
                  </span>
                  <span className="compound-control">
                    <input
                      className="control control-input"
                      type="text"
                      value={formValues.harnessPath}
                      onChange={(e) =>
                        setFormValues((prev) => ({
                          ...prev,
                          harnessPath: e.target.value,
                        }))
                      }
                      placeholder="tests/Sqloom/Sqloom.TestApp/default/Harness.cs"
                    />
                    <button
                      className="field-action"
                      type="button"
                      onClick={handleDetectHarness}
                    >
                      Detect
                    </button>
                  </span>
                  <div className="field-note">
                    {formValues.harnessPath
                      ? "Auto-detected or custom harness path."
                      : "Default harness path was not found."}
                  </div>
                </div>

                {/* Endpoint Selection */}
                <div
                  className={`field status-${validation.endpointReady ? "ready" : endpointCatalogState === "loading" ? "idle" : "warning"}`}
                >
                  <span className="field-label">
                    Replay endpoint{" "}
                    <StatusDot
                      status={
                        validation.endpointReady
                          ? "ready"
                          : endpointCatalogState === "loading"
                            ? "idle"
                            : "warning"
                      }
                    />
                  </span>
                  <span className="compound-control">
                    <select
                      className="control control-select"
                      disabled={endpointCatalogState !== "loaded"}
                      value={formValues.target}
                      onChange={(e) =>
                        setFormValues((prev) => ({
                          ...prev,
                          target: e.target.value,
                        }))
                      }
                    >
                      <option value="">
                        {endpoints.length === 0
                          ? "No endpoints available"
                          : "Select an endpoint…"}
                      </option>
                      {endpoints.map((ep) => (
                        <option
                          key={ep.stableOperationKey}
                          value={ep.stableOperationKey}
                        >
                          {ep.stableOperationKey}
                        </option>
                      ))}
                    </select>
                    <button
                      className="field-action"
                      type="button"
                      onClick={() => loadEndpoints(formValues)}
                    >
                      Refresh
                    </button>
                  </span>
                  <div className="field-note">{endpointCatalogMessage}</div>
                </div>

                {/* OpenAI API Key */}
                <div className="field">
                  <span className="field-label">
                    API key{" "}
                    <StatusDot
                      status={formValues.openAiApiKey ? "ready" : "warning"}
                    />
                  </span>
                  <span className="password-control">
                    <input
                      className="control control-input"
                      type={passwordsVisible.openAiApiKey ? "text" : "password"}
                      value={formValues.openAiApiKey}
                      onChange={(e) =>
                        setFormValues((prev) => ({
                          ...prev,
                          openAiApiKey: e.target.value,
                        }))
                      }
                      placeholder="Enter API key for this run"
                      autoComplete="off"
                      spellCheck="false"
                    />
                    <button
                      className="mask-toggle"
                      type="button"
                      onClick={() => togglePasswordVisibility("openAiApiKey")}
                      aria-label={
                        passwordsVisible.openAiApiKey
                          ? "Hide value"
                          : "Show value"
                      }
                      title={
                        passwordsVisible.openAiApiKey
                          ? "Hide value"
                          : "Show value"
                      }
                    >
                      <Icon name="eye" />
                    </button>
                  </span>
                  <div className="field-note">
                    {getFieldVal("openAiApiKey")
                      ? "Masked by default. Edit to override."
                      : "Masked and passed only to the current dashboard run."}
                  </div>
                </div>

                {/* Connection String */}
                <div className="field">
                  <span className="field-label">
                    SQL connection{" "}
                    <StatusDot
                      status={
                        formValues.readOnlyConnectionString
                          ? "ready"
                          : "warning"
                      }
                    />
                  </span>
                  <span className="password-control">
                    <input
                      className="control control-input"
                      type={
                        passwordsVisible.readOnlyConnectionString
                          ? "text"
                          : "password"
                      }
                      value={formValues.readOnlyConnectionString}
                      onChange={(e) =>
                        setFormValues((prev) => ({
                          ...prev,
                          readOnlyConnectionString: e.target.value,
                        }))
                      }
                      placeholder="Read-only SQL Server connection string"
                      autoComplete="off"
                      spellCheck="false"
                    />
                    <button
                      className="mask-toggle"
                      type="button"
                      onClick={() =>
                        togglePasswordVisibility("readOnlyConnectionString")
                      }
                      aria-label={
                        passwordsVisible.readOnlyConnectionString
                          ? "Hide value"
                          : "Show value"
                      }
                      title={
                        passwordsVisible.readOnlyConnectionString
                          ? "Hide value"
                          : "Show value"
                      }
                    >
                      <Icon name="eye" />
                    </button>
                  </span>
                  <div className="field-note">
                    Masked and passed only to the current dashboard run.
                  </div>
                </div>
              </div>
              <div className="detected-line edit-detected">
                <StatusDot
                  status={formValues.harnessPath ? "ready" : "neutral"}
                />
                <span>
                  Detected harness:{" "}
                  <span>{formValues.harnessPath || "Not detected"}</span>
                </span>
              </div>

              {/* Edit actions */}
              <div className="edit-actions">
                <button
                  className="secondary-action"
                  type="button"
                  onClick={handleCancelSetup}
                >
                  Cancel
                </button>
                <button
                  className="primary-action compact"
                  type="button"
                  onClick={handleApplySetup}
                  disabled={!validation.ready}
                >
                  <Icon name="check" />
                  <span>Apply setup</span>
                </button>
              </div>
            </section>
          )}

          {/* Status Panels in Main Column */}
          {mode === "run" && (
            <section
              className="panel status-panel mode-run"
              aria-label={initialState.title || "Run tune"}
            >
              <h2>{initialState.title || "Run tune"}</h2>
              <p>{showSummarySubtitle}</p>
              <div className="status-list">
                {runStatusChecks.map((check) => (
                  <div
                    className={`status-check status-${check.status}`}
                    key={check.id}
                    data-status-check={check.id}
                  >
                    <StatusDot status={check.status} />
                    <div>
                      <strong>{check.label}</strong>
                      <span>{check.detail}</span>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          {mode === "edit" && (
            <section
              className="panel status-panel mode-edit"
              aria-label={initialState.title || "Edit setup status"}
            >
              <h2>{initialState.title || "Edit setup status"}</h2>
              <p>
                {validation.ready
                  ? "Everything looks good."
                  : "Required values need attention."}
              </p>
              <div className="status-list">
                {editStatusChecks.map((check) => (
                  <div
                    className={`status-check status-${check.status}`}
                    key={check.id}
                    data-status-check={check.id}
                  >
                    <StatusDot status={check.status} />
                    <div>
                      <strong>{check.label}</strong>
                      <span>{check.detail}</span>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Recent Runs Panel in Main Column */}
          <section
            className="panel recent-panel"
            aria-label={initialState.recentRunsTitle || "Recent runs"}
          >
            <div className="panel-heading compact-heading">
              <h2>{initialState.recentRunsTitle || "Recent runs"}</h2>
              <button
                className="link-button"
                type="button"
                onClick={handleOpenRecentRunsFolder}
              >
                {initialState.recentRunsAction || "View all"}
              </button>
            </div>

            <div className="recent-runs-body">
              {recentRuns && recentRuns.length > 0 ? (
                <div className="recent-run-list">
                  {recentRuns.map((run) => {
                    const status =
                      run.status === "failed" ? "failed" : "completed";
                    const isSelected = run.id === selectedRunId;
                    return (
                      <button
                        className={`recent-run-item${isSelected ? " selected" : ""}`}
                        type="button"
                        key={run.id}
                        onClick={() =>
                          handleSelectRecentRun(run.id, run.artifactDir || "")
                        }
                      >
                        <div className="recent-run-header">
                          <strong>{run.target || "Tune run"}</strong>
                          <span className={`recent-run-badge status-${status}`}>
                            {run.statusLabel ||
                              (run.status === "failed"
                                ? "Failed"
                                : "Completed")}
                          </span>
                        </div>
                        <span className="recent-run-meta">
                          {run.startedRelative || "Just now"} ·{" "}
                          {run.artifactDir}
                        </span>
                      </button>
                    );
                  })}
                </div>
              ) : (
                <div className="empty-state">
                  <span className="clock-icon" aria-hidden="true" />
                  <strong>No runs yet</strong>
                  <p>
                    Your recent tune runs will appear here. Run a tune to get
                    started.
                  </p>
                </div>
              )}
            </div>
          </section>
        </section>

        {/* Side Column */}
        <aside className="side-column" aria-label="Tune execution and results">
          {/* Stepper Progress in Side Column */}
          <nav className="stepper" aria-label="Tune workflow stages">
            {stages.map((stage) => {
              const statusClass =
                stage.status === "pending" ? "" : ` ${stage.status}`;
              return (
                <div
                  className={`step${statusClass}`}
                  key={stage.id}
                  data-stage-id={stage.id}
                >
                  <div className="step-index">{stage.number}</div>
                  <div className="step-label">{stage.label}</div>
                  <div className="step-detail">{stage.detail}</div>
                </div>
              );
            })}
          </nav>

          {/* Artifacts Panel in Side Column */}
          <section
            className="panel artifacts-panel"
            aria-label="Tune results and artifacts"
          >
            <div className="panel-heading artifact-heading">
              <div>
                <h2>Tune results / artifacts</h2>
                <p>
                  {artifactsPanel.selectedRunId
                    ? "Artifacts for the selected tune run."
                    : "Artifacts will appear here after a successful run."}
                </p>
              </div>
              <div className="artifact-tools">
                <span data-artifact-count>
                  {artifactsPanel.artifactCountLabel || "0 files"}
                </span>
              </div>
            </div>

            {artifactsPanel.artifacts && artifactsPanel.artifacts.length > 0 ? (
              <div className="artifact-table-wrap">
                <table className="artifact-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Size</th>
                      <th>Updated</th>
                      <th>Preview / Summary</th>
                      <th aria-label="Actions" />
                    </tr>
                  </thead>
                  <tbody>
                    {artifactsPanel.artifacts.map((art) => {
                      const typeTone = art.typeTone || "other";
                      const iconName =
                        art.typeTone === "markdown"
                          ? "fileText"
                          : art.typeTone === "json"
                            ? "code"
                            : art.typeTone === "sql"
                              ? "database"
                              : art.typeTone === "html"
                                ? "browser"
                                : "file";
                      return (
                        <tr
                          key={art.relativePath}
                          className={`stage-${art.stage || "general"}`}
                        >
                          <td>
                            <div className="artifact-name">
                              <span
                                className={`artifact-icon tone-${typeTone}`}
                                title={art.type || "File"}
                              >
                                <Icon
                                  name={iconName}
                                  className="artifact-type-icon"
                                />
                              </span>
                              <span>
                                <strong>{art.name}</strong>
                                <small>{art.description}</small>
                              </span>
                            </div>
                          </td>
                          <td>{art.size}</td>
                          <td>{art.updated}</td>
                          <td>{art.summary}</td>
                          <td>
                            <div className="row-actions">
                              <button
                                className="icon-button"
                                type="button"
                                title={`Preview ${art.name}`}
                                onClick={() =>
                                  handleOpenArtifact(art.relativePath)
                                }
                              >
                                <Icon name="eye" />
                              </button>
                              <button
                                className="icon-button"
                                type="button"
                                title={`Reveal ${art.name}`}
                                onClick={() =>
                                  handleRevealArtifact(art.relativePath)
                                }
                              >
                                <Icon name="browser" />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state compact-empty-state">
                <strong>{artifactsPanel.emptyTitle || "No artifacts"}</strong>
                <p>
                  {artifactsPanel.emptyDetail ||
                    "Run tune to generate artifacts."}
                </p>
              </div>
            )}

            <div className="artifact-footer">
              <span>{artifactsPanel.footerPrimary}</span>
              <span>{artifactsPanel.footerSecondary}</span>
            </div>
          </section>
        </aside>
      </div>
    </main>
  );
};
