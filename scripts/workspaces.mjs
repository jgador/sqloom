import { spawn, spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";
import process from "node:process";
import { supportedActions, workspaceTargets } from "./workspace-targets.mjs";

const npmInvocation = resolveNpmInvocation();
const minSupportedNodeMajor = 24;

function resolveNpmInvocation() {
  const candidates = [
    path.join(
      path.dirname(process.execPath),
      "node_modules",
      "npm",
      "bin",
      "npm-cli.js",
    ),
    path.join(
      path.dirname(path.dirname(process.execPath)),
      "lib",
      "node_modules",
      "npm",
      "bin",
      "npm-cli.js",
    ),
  ];

  const npmCliPath = candidates.find((candidate) => existsSync(candidate));

  if (npmCliPath) {
    return {
      command: process.execPath,
      baseArgs: [npmCliPath],
    };
  }

  return {
    command: "npm",
    baseArgs: [],
  };
}

function ensureSupportedNodeVersion() {
  const currentNodeMajor = Number.parseInt(
    process.versions.node.split(".")[0],
    10,
  );

  if (
    !Number.isFinite(currentNodeMajor) ||
    currentNodeMajor < minSupportedNodeMajor
  ) {
    throw new Error(
      `Node.js ${minSupportedNodeMajor}+ is required. Current version: ${process.version}.`,
    );
  }
}

function parseTargetValue(flag, value) {
  if (!value || value.trim().length === 0 || value.startsWith("-")) {
    throw new Error(
      `Missing value for ${flag}. Usage: npm run <action> -- --target <name>[,<name>]`,
    );
  }

  return value;
}

function parseArgs(argv) {
  const [action, ...rest] = argv;
  const options = {
    action,
    forwardedArgs: [],
    requireTarget: false,
    targetValue: undefined,
  };

  for (let i = 0; i < rest.length; i++) {
    const arg = rest[i];

    if (arg === "--target" || arg === "-t") {
      options.targetValue = parseTargetValue(arg, rest[i + 1]);
      i++;
      continue;
    }

    if (arg.startsWith("--target=")) {
      options.targetValue = parseTargetValue(
        "--target",
        arg.slice("--target=".length),
      );
      continue;
    }

    if (arg === "--require-target") {
      options.requireTarget = true;
      continue;
    }

    options.forwardedArgs.push(arg);
  }

  return options;
}

function printUsage() {
  console.log(`Usage:
  npm run build [-- --target <name>[,<name>]]
  npm run watch [-- --target <name>[,<name>]]
  npm run test [-- --target <name>[,<name>]]
  npm run lint [-- --target <name>[,<name>]]
  npm run package [-- --target <name>[,<name>]]
  npm run publish:preview
  npm run list:targets
`);
}

function resolveTargets(action, targetValue) {
  const availableTargets = workspaceTargets.filter((target) =>
    target.scripts.includes(action),
  );

  if (!targetValue) {
    return availableTargets;
  }

  const requested = targetValue
    .split(",")
    .map((name) => name.trim())
    .filter(Boolean);

  return requested.map((name) => {
    const target = workspaceTargets.find(
      (candidate) =>
        candidate.target === name || candidate.aliases.includes(name),
    );

    if (!target) {
      throw new Error(
        `Unknown target "${name}". Run "npm run list:targets" to see supported targets.`,
      );
    }

    if (!target.scripts.includes(action)) {
      throw new Error(
        `Target "${target.target}" does not support "${action}".`,
      );
    }

    return target;
  });
}

function runWorkspaceScript(target, action, forwardedArgs = []) {
  const npmArgs = [...npmInvocation.baseArgs, "run", action, "--if-present"];

  if (forwardedArgs.length > 0) {
    npmArgs.push("--", ...forwardedArgs);
  }

  console.log(`\n> ${target.target} (${target.packageName}) :: ${action}`);
  const result = spawnSync(npmInvocation.command, npmArgs, {
    cwd: path.join(process.cwd(), target.directory),
    stdio: "inherit",
  });

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

function watchTargets(targets, forwardedArgs = []) {
  console.log(
    `Watching targets: ${targets.map((target) => target.target).join(", ")}`,
  );

  const children = targets.map((target) => {
    const npmArgs = [...npmInvocation.baseArgs, "run", "watch", "--if-present"];

    if (forwardedArgs.length > 0) {
      npmArgs.push("--", ...forwardedArgs);
    }

    console.log(`\n> ${target.target} (${target.packageName}) :: watch`);
    return spawn(npmInvocation.command, npmArgs, {
      cwd: path.join(process.cwd(), target.directory),
      stdio: "inherit",
    });
  });

  let closing = false;
  const closeChildren = (signal = "SIGTERM") => {
    if (closing) {
      return;
    }

    closing = true;
    for (const child of children) {
      if (!child.killed) {
        child.kill(signal);
      }
    }
  };

  process.on("SIGINT", () => {
    closeChildren("SIGINT");
  });

  process.on("SIGTERM", () => {
    closeChildren("SIGTERM");
  });

  children.forEach((child) => {
    child.on("exit", (code) => {
      if (!closing && code && code !== 0) {
        closeChildren("SIGTERM");
        process.exit(code);
      }
    });
  });
}

function listTargets() {
  console.log("Available targets:\n");

  for (const target of workspaceTargets) {
    console.log(
      `- ${target.target}: package=${target.packageName}; dir=${target.directory}; scripts=${target.scripts.join(", ")}; aliases=${target.aliases.join(", ")}`,
    );
  }
}

function main() {
  ensureSupportedNodeVersion();

  const { action, forwardedArgs, requireTarget, targetValue } = parseArgs(
    process.argv.slice(2),
  );

  if (!action || action === "help" || action === "--help" || action === "-h") {
    printUsage();
    return;
  }

  if (action === "list") {
    listTargets();
    return;
  }

  if (!supportedActions.includes(action)) {
    throw new Error(
      `Unsupported action "${action}". Supported actions: ${supportedActions.join(", ")}`,
    );
  }

  if (requireTarget && !targetValue) {
    throw new Error(
      `The "${action}" command requires --target. Run "npm run list:targets" to see options.`,
    );
  }

  const targets = resolveTargets(action, targetValue);

  if (action === "watch") {
    watchTargets(targets, forwardedArgs);
    return;
  }

  for (const target of targets) {
    runWorkspaceScript(target, action, forwardedArgs);
  }
}

try {
  main();
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}
