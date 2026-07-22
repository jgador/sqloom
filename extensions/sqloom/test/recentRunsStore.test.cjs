const assert = require("node:assert/strict");
const test = require("node:test");

const {
  formatLastTuneRunSummary,
  formatRelativeTime,
  toDashboardRecentRun,
} = require("../dist/recentRuns/formatRecentRun.js");
const { RecentRunsStore } = require("../dist/recentRuns/recentRunsStore.js");

function createMemoryMemento() {
  const values = new Map();

  return {
    get(key, defaultValue) {
      return values.has(key) ? values.get(key) : defaultValue;
    },
    async update(key, value) {
      if (value === undefined) {
        values.delete(key);
        return;
      }

      values.set(key, value);
    },
  };
}

test("formats relative times for recent runs", () => {
  const now = Date.parse("2026-07-22T06:00:00.000Z");
  assert.equal(
    formatRelativeTime("2026-07-22T05:59:40.000Z", now),
    "Just now",
  );
  assert.equal(
    formatRelativeTime("2026-07-22T05:30:00.000Z", now),
    "30m ago",
  );
  assert.equal(
    formatRelativeTime("2026-07-21T06:00:00.000Z", now),
    "1d ago",
  );
});

test("records dashboard tune runs in workspace order", async () => {
  const store = new RecentRunsStore(createMemoryMemento());
  await store.record({
    id: "run-1",
    startedAtUtc: "2026-07-22T05:00:00.000Z",
    finishedAtUtc: "2026-07-22T05:01:00.000Z",
    success: true,
    target: "GET /api/products",
    artifactDir: "artifacts/sqloom/tune/dashboard-run-1",
    workspaceFolderUri: "file:///c:/repo/project-a",
    openAiModel: "gpt-5.4-mini",
  });
  await store.record({
    id: "run-2",
    startedAtUtc: "2026-07-22T06:00:00.000Z",
    finishedAtUtc: "2026-07-22T06:02:00.000Z",
    success: false,
    target: "POST /api/orders",
    artifactDir: "artifacts/sqloom/tune/dashboard-run-2",
    workspaceFolderUri: "file:///c:/repo/project-b",
    openAiModel: "gpt-5.4-mini",
  });

  const runs = store.list();
  assert.equal(runs.length, 2);
  assert.equal(runs[0]?.id, "run-2");
  assert.equal(runs[1]?.id, "run-1");
  assert.equal(runs[0]?.workspaceFolderUri, "file:///c:/repo/project-b");
});

test("maps stored runs to dashboard recent run rows", () => {
  const dashboardRun = toDashboardRecentRun(
    {
      id: "run-1",
      startedAtUtc: "2026-07-22T05:00:00.000Z",
      finishedAtUtc: "2026-07-22T05:01:00.000Z",
      success: true,
      target: "GET /api/products",
      artifactDir: "artifacts/sqloom/tune/dashboard-run-1",
      workspaceFolderUri: "file:///c:/repo/project-a",
      openAiModel: "gpt-5.4-mini",
      source: "dashboard",
    },
    Date.parse("2026-07-22T05:02:00.000Z"),
  );

  assert.equal(dashboardRun.status, "completed");
  assert.equal(dashboardRun.statusLabel, "Completed");
  assert.equal(dashboardRun.startedRelative, "1m ago");
});

test("summarizes the latest tune run for dashboard state", () => {
  assert.deepEqual(formatLastTuneRunSummary([]), {
    detail: "Never run",
    status: "idle",
  });

  assert.deepEqual(
    formatLastTuneRunSummary(
      [
        {
          id: "run-1",
          startedAtUtc: "2026-07-22T05:00:00.000Z",
          finishedAtUtc: "2026-07-22T05:01:00.000Z",
          success: false,
          target: "GET /api/products",
          artifactDir: "artifacts/sqloom/tune/dashboard-run-1",
          workspaceFolderUri: "",
          openAiModel: "gpt-5.4-mini",
          source: "dashboard",
        },
      ],
      Date.parse("2026-07-22T05:01:30.000Z"),
    ),
    {
      detail: "Just now · failed · GET /api/products",
      status: "warning",
    },
  );
});

test("preserves workspace folder URI for legacy records without the field", async () => {
  const memento = createMemoryMemento();
  await memento.update("sqloom.dashboard.recentTuneRuns", [
    {
      id: "legacy-run",
      startedAtUtc: "2026-07-22T05:00:00.000Z",
      finishedAtUtc: "2026-07-22T05:01:00.000Z",
      success: true,
      target: "GET /api/products",
      artifactDir: "artifacts/sqloom/tune/legacy-run",
      openAiModel: "gpt-5.4-mini",
      source: "dashboard",
    },
  ]);

  const store = new RecentRunsStore(memento);
  assert.equal(store.list()[0]?.workspaceFolderUri, "");
});
