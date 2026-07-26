const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");

const { serializeForScript } = require("../dist/utils/webview.js");

test("escapes script-breaking sequences in embedded JSON", () => {
  const serialized = serializeForScript({
    target: "GET </script><script>alert(1)</script>",
    artifactDir: "artifacts/sqloom/tune/</script>",
  });

  assert.equal(serialized.includes("</script>"), false);
  assert.equal(serialized.includes("\\u003c"), true);
  assert.deepEqual(JSON.parse(serialized), {
    target: "GET </script><script>alert(1)</script>",
    artifactDir: "artifacts/sqloom/tune/</script>",
  });
});

test("dashboard bundle uses the automatic JSX runtime", () => {
  const bundle = fs.readFileSync(
    path.join(__dirname, "../dist/views/dashboard.js"),
    "utf8",
  );

  assert.doesNotMatch(bundle, /\bReact\.createElement\(/);
});

test("dashboard bundle acquires the VS Code API once", () => {
  const bundle = fs.readFileSync(
    path.join(__dirname, "../dist/views/dashboard.js"),
    "utf8",
  );
  const acquisitions = bundle.match(/acquireVsCodeApi\(\)/g) ?? [];

  assert.equal(acquisitions.length, 1);
});
