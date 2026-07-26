import esbuild from "esbuild";
import { spawn } from "node:child_process";
import process from "node:process";

const args = process.argv.slice(2);
const isWatch = args.includes("--watch");

const baseConfig = {
  bundle: true,
  minify: !isWatch,
  sourcemap: isWatch,
  logLevel: "info",
};

// Extension config (Node target)
const extensionConfig = {
  ...baseConfig,
  entryPoints: ["src/extension.ts"],
  outfile: "dist/extension.js",
  external: ["vscode"],
  format: "cjs",
  platform: "node",
  target: "node16",
};

// Webviews config (Browser target)
const webviewsConfig = {
  ...baseConfig,
  tsconfig: "tsconfig.webviews.json",
  entryPoints: {
    dashboard: "src/webviews/pages/dashboard/index.tsx",
  },
  outdir: "dist/views",
  format: "esm",
  platform: "browser",
  target: "es2022",
  loader: {
    ".css": "css",
    ".svg": "file",
    ".png": "file",
  },
};

async function main() {
  if (isWatch) {
    // Spawn tsc -watch in background for compiling individual files for tests
    const tsc = spawn(
      "npx",
      [
        "tsc",
        "-watch",
        "-p",
        "tsconfig.extension.json",
        "--preserveWatchOutput",
      ],
      {
        stdio: "inherit",
        shell: true,
      },
    );

    tsc.on("close", (code) => {
      if (code !== 0) {
        console.error(`tsc watch process exited with code ${code}`);
      }
    });

    const extCtx = await esbuild.context(extensionConfig);
    const webviewsCtx = await esbuild.context(webviewsConfig);
    await extCtx.watch();
    await webviewsCtx.watch();
    console.log("Watching for changes...");
  } else {
    await esbuild.build(extensionConfig);
    await esbuild.build(webviewsConfig);
    console.log("esbuild bundle complete.");
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
