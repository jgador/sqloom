const assert = require("node:assert/strict");
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
