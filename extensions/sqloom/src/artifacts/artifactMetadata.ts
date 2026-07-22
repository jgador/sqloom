import type { DashboardArtifact } from "../sharedInterfaces/dashboard";

export type ArtifactTypeTone = DashboardArtifact["typeTone"];

export type ArtifactMetadata = {
  description: string;
  type: string;
  typeTone: ArtifactTypeTone;
  summary: string;
  sortOrder: number;
};

const artifactMetadataByRelativePath = new Map<string, ArtifactMetadata>([
  [
    "tune-summary.json",
    {
      description: "Tune workflow summary",
      type: "JSON",
      typeTone: "json",
      summary: "Run configuration, stage outcomes, and workflow metadata.",
      sortOrder: 10,
    },
  ],
  [
    "query-store-snapshot.json",
    {
      description: "Captured Query Store snapshot",
      type: "JSON",
      typeTone: "json",
      summary: "Query Store evidence collected during observe.",
      sortOrder: 20,
    },
  ],
  [
    "replay/sql-tuning-proposal.sql",
    {
      description: "Executable SQL tuning proposal",
      type: "SQL",
      typeTone: "sql",
      summary: "Suggested SQL changes produced by advise.",
      sortOrder: 30,
    },
  ],
  [
    "replay/sql-tuning-proposal.json",
    {
      description: "Structured SQL tuning proposal",
      type: "JSON",
      typeTone: "json",
      summary: "Machine-readable proposal details and rationale.",
      sortOrder: 40,
    },
  ],
  [
    "replay/tuning-advice.json",
    {
      description: "Tuning advice evidence pack",
      type: "JSON",
      typeTone: "json",
      summary: "Advice request, response, and supporting evidence.",
      sortOrder: 50,
    },
  ],
  [
    "replay/query-store-correlation.json",
    {
      description: "Replay-to-Query Store correlation report",
      type: "JSON",
      typeTone: "json",
      summary: "Statement and plan handle matches between replay and observe.",
      sortOrder: 60,
    },
  ],
  [
    "replay/replay-summary.json",
    {
      description: "Replay stage summary",
      type: "JSON",
      typeTone: "json",
      summary: "Replay execution results and captured SQL evidence.",
      sortOrder: 70,
    },
  ],
  [
    "replay/replay-plan.json",
    {
      description: "Replay execution plan",
      type: "JSON",
      typeTone: "json",
      summary: "Selected endpoints and replay plan metadata.",
      sortOrder: 80,
    },
  ],
  [
    "replay/replay-data-prep.json",
    {
      description: "Replay data preparation output",
      type: "JSON",
      typeTone: "json",
      summary: "Replay data agent preparation details.",
      sortOrder: 90,
    },
  ],
  [
    "replay/endpoints.json",
    {
      description: "Replay endpoint catalog",
      type: "JSON",
      typeTone: "json",
      summary: "Endpoint catalog captured for the replay run.",
      sortOrder: 100,
    },
  ],
  [
    "replay/sqlserver-schema.sql",
    {
      description: "Normalized SQL Server schema",
      type: "SQL",
      typeTone: "sql",
      summary: "Schema extracted for advice generation.",
      sortOrder: 110,
    },
  ],
  [
    "replay/sqlserver-schema-source.dacpac",
    {
      description: "SQL Server schema DACPAC",
      type: "DACPAC",
      typeTone: "other",
      summary: "DACPAC exported for schema extraction.",
      sortOrder: 120,
    },
  ],
]);

export function normalizeArtifactCatalogPath(value: string): string {
  return value.replace(/\\/g, "/").replace(/^\/+/, "").toLowerCase();
}

export function getArtifactMetadata(
  artifactRelativePath: string,
): ArtifactMetadata {
  const normalized = normalizeArtifactCatalogPath(artifactRelativePath);
  const known = artifactMetadataByRelativePath.get(normalized);
  if (known) {
    return known;
  }

  if (normalized.startsWith("replay/operations/") && normalized.endsWith(".json")) {
    return {
      description: "Replay operation evidence",
      type: "JSON",
      typeTone: "json",
      summary: "Captured SQL and execution details for one replayed operation.",
      sortOrder: 200,
    };
  }

  const fileName = normalized.split("/").at(-1) ?? normalized;
  const extension = fileName.includes(".")
    ? fileName.slice(fileName.lastIndexOf(".") + 1)
    : "";

  switch (extension) {
    case "sql":
      return {
        description: "SQL artifact",
        type: "SQL",
        typeTone: "sql",
        summary: "SQL output from the tune workflow.",
        sortOrder: 300,
      };
    case "json":
      return {
        description: "JSON artifact",
        type: "JSON",
        typeTone: "json",
        summary: "Structured output from the tune workflow.",
        sortOrder: 310,
      };
    case "html":
    case "htm":
      return {
        description: "HTML artifact",
        type: "HTML",
        typeTone: "html",
        summary: "HTML output from the tune workflow.",
        sortOrder: 320,
      };
    case "md":
    case "markdown":
      return {
        description: "Markdown artifact",
        type: "Markdown",
        typeTone: "markdown",
        summary: "Markdown output from the tune workflow.",
        sortOrder: 330,
      };
    default:
      return {
        description: "Tune workflow artifact",
        type: extension.length > 0 ? extension.toUpperCase() : "File",
        typeTone: "other",
        summary: "Generated output from the tune workflow.",
        sortOrder: 400,
      };
  }
}
