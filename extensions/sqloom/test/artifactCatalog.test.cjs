const assert = require("node:assert/strict");
const test = require("node:test");

const {
  formatFileSize,
  mapScannedFilesToArtifacts,
} = require("../dist/artifacts/artifactCatalogCore.js");
const { getArtifactMetadata } = require("../dist/artifacts/artifactMetadata.js");

test("maps known tune artifacts with stable ordering", () => {
  const artifacts = mapScannedFilesToArtifacts(
    [
      {
        artifactRelativePath: "replay/replay-summary.json",
        workspaceRelativePath:
          "artifacts/sqloom/tune/dashboard-test/replay/replay-summary.json",
        sizeBytes: 2048,
        modifiedAtUtc: "2026-07-22T05:01:00.000Z",
      },
      {
        artifactRelativePath: "tune-summary.json",
        workspaceRelativePath:
          "artifacts/sqloom/tune/dashboard-test/tune-summary.json",
        sizeBytes: 1024,
        modifiedAtUtc: "2026-07-22T05:02:00.000Z",
      },
      {
        artifactRelativePath: "replay/sql-tuning-proposal.sql",
        workspaceRelativePath:
          "artifacts/sqloom/tune/dashboard-test/replay/sql-tuning-proposal.sql",
        sizeBytes: 512,
        modifiedAtUtc: "2026-07-22T05:03:00.000Z",
      },
    ],
    Date.parse("2026-07-22T05:04:00.000Z"),
  );

  assert.equal(artifacts.length, 3);
  assert.equal(artifacts[0]?.name, "tune-summary.json");
  assert.equal(artifacts[1]?.name, "sql-tuning-proposal.sql");
  assert.equal(artifacts[2]?.name, "replay-summary.json");
  assert.equal(artifacts[0]?.relativePath, "artifacts/sqloom/tune/dashboard-test/tune-summary.json");
  assert.equal(artifacts[0]?.size, "1 KB");
  assert.equal(artifacts[0]?.updated, "2m ago");
});

test("describes replay operation artifacts", () => {
  const metadata = getArtifactMetadata(
    "replay/operations/01-GET-api-products.json",
  );

  assert.equal(metadata.type, "JSON");
  assert.match(metadata.summary, /operation/i);
});

test("formats artifact file sizes", () => {
  assert.equal(formatFileSize(900), "900 B");
  assert.equal(formatFileSize(2048), "2 KB");
  assert.equal(formatFileSize(5 * 1024 * 1024), "5.0 MB");
});
