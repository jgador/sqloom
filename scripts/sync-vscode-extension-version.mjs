import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDirectory, "..");
const cliVersionPath = path.join(repoRoot, "Directory.Build.props");
const extensionPackagePath = path.join(repoRoot, "extensions", "sqloom", "package.json");
const packageLockPath = path.join(repoRoot, "package-lock.json");

const checkOnly = process.argv.includes("--check");

function formatJson(value) {
    return `${JSON.stringify(value, null, 2)}\n`;
}

async function readJson(filePath) {
    return JSON.parse(await readFile(filePath, "utf8"));
}

async function readCliVersion() {
    const props = await readFile(cliVersionPath, "utf8");
    const versionMatch = props.match(/<Version>([^<]+)<\/Version>/);

    if (!versionMatch || !versionMatch[1].trim()) {
        throw new Error(`${cliVersionPath} must define <Version> for Sqloom packages.`);
    }

    const version = versionMatch[1].trim();
    if (version.startsWith("v") || version.startsWith("V")) {
        throw new Error(
            "Directory.Build.props <Version> must not include a leading v for package versions.",
        );
    }

    return version;
}

function updateVersion(container, version, label) {
    if (!container || typeof container !== "object") {
        throw new Error(`${label} is missing from package-lock.json.`);
    }

    const currentVersion = container.version;
    if (currentVersion === version) {
        return false;
    }

    if (checkOnly) {
        throw new Error(`${label} version ${currentVersion ?? "<missing>"} does not match ${version}.`);
    }

    container.version = version;
    return true;
}

async function main() {
    const version = await readCliVersion();
    const extensionPackage = await readJson(extensionPackagePath);
    const packageLock = await readJson(packageLockPath);
    let changed = false;

    changed = updateVersion(extensionPackage, version, "extensions/sqloom/package.json") || changed;
    changed =
        updateVersion(packageLock.packages?.["extensions/sqloom"], version, "package-lock extensions/sqloom") ||
        changed;

    if (!changed) {
        console.log(`VS Code extension version is already in sync with CLI version ${version}.`);
        return;
    }

    await writeFile(extensionPackagePath, formatJson(extensionPackage), "utf8");
    await writeFile(packageLockPath, formatJson(packageLock), "utf8");
    console.log(`Synced VS Code extension version to CLI version ${version}.`);
}

main().catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exit(1);
});
