import { spawn } from "node:child_process";
import * as path from "node:path";
import * as vscode from "vscode";

const outputChannel = vscode.window.createOutputChannel("Sqloom");
const knownRunFiles = [
    "tune-summary.json",
    "query-store-snapshot.json",
    "replay/replay-data-prep.json",
    "replay/query-store-correlation.json",
    "replay/tuning-advice.json",
    "replay/sql-tuning-proposal.json",
    "replay/sql-tuning-proposal.sql",
];

type RefreshableProvider = {
    refresh(): void;
};

export function activate(context: vscode.ExtensionContext) {
    const cliService = new CliService();
    const runsProvider = new RunsProvider();
    const adviceProvider = new AdviceProvider();
    const workflowProvider = new WorkflowProvider();

    context.subscriptions.push(
        outputChannel,
        vscode.window.registerTreeDataProvider("sqloom.workflows", workflowProvider),
        vscode.window.registerTreeDataProvider("sqloom.runs", runsProvider),
        vscode.window.registerTreeDataProvider("sqloom.advice", adviceProvider),
        vscode.commands.registerCommand("sqloom.init", () =>
            runInit(cliService, [runsProvider, adviceProvider]),
        ),
        vscode.commands.registerCommand("sqloom.tune", () =>
            runTune(cliService, [runsProvider, adviceProvider]),
        ),
        vscode.commands.registerCommand("sqloom.refreshRuns", () => {
            runsProvider.refresh();
            adviceProvider.refresh();
        }),
        vscode.commands.registerCommand("sqloom.openArtifactRoot", openArtifactRoot),
        vscode.commands.registerCommand("sqloom.openSqlProposal", openSqlProposal),
        vscode.commands.registerCommand("sqloom.openFile", openUriCommand),
        vscode.commands.registerCommand("sqloom.revealFile", revealUriCommand),
        vscode.commands.registerCommand("sqloom.selectCliPath", selectCliPath),
    );
}

export function deactivate() {
    outputChannel.dispose();
}

class CliService {
    async run(args: string[], workspaceFolder: vscode.WorkspaceFolder): Promise<boolean> {
        const cliPath = getConfiguration().get<string>("cli.path", "sqloom").trim() || "sqloom";
        const redactedCommand = [cliPath, ...redactArgs(args)].join(" ");

        outputChannel.show(true);
        outputChannel.appendLine(`> ${redactedCommand}`);
        outputChannel.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
        outputChannel.appendLine("");

        const exitCode = await vscode.window.withProgress(
            {
                location: vscode.ProgressLocation.Notification,
                title: `Sqloom ${args[0]}`,
            },
            () =>
                new Promise<number>((resolve) => {
                    const child = spawn(cliPath, args, {
                        cwd: workspaceFolder.uri.fsPath,
                        env: process.env,
                        shell: false,
                    });

                    child.stdout.on("data", (chunk: Buffer) => {
                        outputChannel.append(chunk.toString());
                    });

                    child.stderr.on("data", (chunk: Buffer) => {
                        outputChannel.append(chunk.toString());
                    });

                    child.on("error", (error) => {
                        outputChannel.appendLine("");
                        outputChannel.appendLine(`Failed to start Sqloom: ${error.message}`);
                        resolve(-1);
                    });

                    child.on("close", (code) => {
                        outputChannel.appendLine("");
                        outputChannel.appendLine(`Sqloom exited with code ${code ?? -1}.`);
                        resolve(code ?? -1);
                    });
                }),
        );

        if (exitCode === 0) {
            vscode.window.showInformationMessage("Sqloom command completed.");
            return true;
        }

        vscode.window.showErrorMessage("Sqloom command failed. See the Sqloom output channel.");
        return false;
    }
}

class WorkflowProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
    getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
        return element;
    }

    getChildren(): vscode.ProviderResult<vscode.TreeItem[]> {
        return [
            createActionItem(
                "Run Tune Workflow",
                "Run replay, observe, correlate, and advise through the Sqloom CLI.",
                "play",
                "sqloom.tune",
            ),
            createActionItem(
                "Initialize Agent Skill",
                "Scaffold the Sqloom agent skill in this repository.",
                "repo-create",
                "sqloom.init",
            ),
            createActionItem(
                "Refresh Artifacts",
                "Refresh Sqloom run and advice views.",
                "refresh",
                "sqloom.refreshRuns",
            ),
            createActionItem(
                "Open Artifact Root",
                "Open the configured Sqloom artifact root.",
                "folder-opened",
                "sqloom.openArtifactRoot",
            ),
            createActionItem(
                "Select CLI Path",
                "Configure the Sqloom CLI executable path for this workspace.",
                "terminal",
                "sqloom.selectCliPath",
            ),
        ];
    }
}

class RunsProvider implements vscode.TreeDataProvider<TreeNode> {
    private readonly onDidChangeTreeDataEmitter = new vscode.EventEmitter<TreeNode | undefined>();
    readonly onDidChangeTreeData = this.onDidChangeTreeDataEmitter.event;

    refresh(): void {
        this.onDidChangeTreeDataEmitter.fire(undefined);
    }

    getTreeItem(element: TreeNode): vscode.TreeItem {
        return element;
    }

    async getChildren(element?: TreeNode): Promise<TreeNode[]> {
        const workspaceFolder = getPrimaryWorkspaceFolder();
        if (!workspaceFolder) {
            return [createMessageItem("Open a workspace to inspect Sqloom artifacts.")];
        }

        const artifactRoot = getArtifactRoot(workspaceFolder);
        if (!element) {
            const runUris = await collectRunDirectories(artifactRoot);
            await vscode.commands.executeCommand("setContext", "sqloomRunsEmpty", runUris.length === 0);

            if (runUris.length === 0) {
                return [createMessageItem("No Sqloom runs found.")];
            }

            return runUris.map((uri) => createRunItem(uri, artifactRoot));
        }

        if (element instanceof RunNode) {
            return collectKnownRunFileItems(element.uri);
        }

        return [];
    }
}

class AdviceProvider implements vscode.TreeDataProvider<TreeNode> {
    private readonly onDidChangeTreeDataEmitter = new vscode.EventEmitter<TreeNode | undefined>();
    readonly onDidChangeTreeData = this.onDidChangeTreeDataEmitter.event;

    refresh(): void {
        this.onDidChangeTreeDataEmitter.fire(undefined);
    }

    getTreeItem(element: TreeNode): vscode.TreeItem {
        return element;
    }

    async getChildren(): Promise<TreeNode[]> {
        const workspaceFolder = getPrimaryWorkspaceFolder();
        if (!workspaceFolder) {
            return [createMessageItem("Open a workspace to inspect Sqloom advice.")];
        }

        const artifactRoot = getArtifactRoot(workspaceFolder);
        const files = await collectFilesByName(artifactRoot, [
            "sql-tuning-proposal.sql",
            "tuning-advice.json",
        ]);
        await vscode.commands.executeCommand("setContext", "sqloomAdviceEmpty", files.length === 0);

        if (files.length === 0) {
            return [createMessageItem("No Sqloom advice files found.")];
        }

        return files.map((uri) => {
            const contextValue =
                path.basename(uri.fsPath).toLowerCase() === "sql-tuning-proposal.sql"
                    ? "sqloomProposal"
                    : "sqloom.adviceFile";
            return createFileItem(uri, artifactRoot, contextValue);
        });
    }
}

type TreeNode = RunNode | FileNode | MessageNode;

class RunNode extends vscode.TreeItem {
    constructor(
        readonly uri: vscode.Uri,
        artifactRoot: vscode.Uri,
    ) {
        super(relativeLabel(artifactRoot, uri), vscode.TreeItemCollapsibleState.Collapsed);
        this.description = "run";
        this.tooltip = uri.fsPath;
        this.contextValue = "sqloom.run";
        this.iconPath = new vscode.ThemeIcon("history");
        this.resourceUri = uri;
        this.command = {
            command: "sqloom.revealFile",
            title: "Reveal Run",
            arguments: [uri],
        };
    }
}

class FileNode extends vscode.TreeItem {
    constructor(
        readonly uri: vscode.Uri,
        label: string,
        contextValue: string,
    ) {
        super(label, vscode.TreeItemCollapsibleState.None);
        this.tooltip = uri.fsPath;
        this.contextValue = contextValue;
        this.resourceUri = uri;
        this.iconPath = new vscode.ThemeIcon(
            path.extname(uri.fsPath).toLowerCase() === ".sql" ? "file-code" : "json",
        );
        this.command = {
            command: "sqloom.openFile",
            title: "Open File",
            arguments: [uri],
        };
    }
}

class MessageNode extends vscode.TreeItem {
    constructor(message: string) {
        super(message, vscode.TreeItemCollapsibleState.None);
        this.iconPath = new vscode.ThemeIcon("info");
    }
}

async function runInit(
    cliService: CliService,
    refreshableProviders: readonly RefreshableProvider[],
): Promise<void> {
    const workspaceFolder = await pickWorkspaceFolder();
    if (!workspaceFolder) {
        return;
    }

    const agent = await vscode.window.showQuickPick(["codex", "claude", "copilot", "all"], {
        title: "Sqloom Agent Target",
        placeHolder: "Select the agent skill target to scaffold.",
    });
    if (!agent) {
        return;
    }

    const overwrite = await vscode.window.showQuickPick(["No", "Yes"], {
        title: "Overwrite changed scaffolded files?",
        placeHolder: "Choose whether to pass --overwrite.",
    });
    if (!overwrite) {
        return;
    }

    const args = ["init", "--agent", agent];
    if (overwrite === "Yes") {
        args.push("--overwrite");
    }

    const succeeded = await cliService.run(args, workspaceFolder);
    if (succeeded) {
        refreshableProviders.forEach((provider) => provider.refresh());
    }
}

async function runTune(
    cliService: CliService,
    refreshableProviders: readonly RefreshableProvider[],
): Promise<void> {
    const workspaceFolder = await pickWorkspaceFolder();
    if (!workspaceFolder) {
        return;
    }

    const harnessPath = await vscode.window.showInputBox({
        title: "Sqloom Harness Path",
        prompt: "C# harness file, harness project, assembly, solution, solution filter, or directory.",
        value: await defaultHarnessPath(workspaceFolder),
        ignoreFocusOut: true,
    });
    if (!harnessPath) {
        return;
    }

    const target = await vscode.window.showInputBox({
        title: "Replay Target",
        prompt: "Optional exact operation, for example GET /api/products/by-category.",
        ignoreFocusOut: true,
    });
    if (target === undefined) {
        return;
    }

    const readOnlyConnectionString = await vscode.window.showInputBox({
        title: "Read-only SQL Server Connection String",
        prompt: "Optional. Used for Query Store reads and schema export when the harness does not provide one.",
        password: true,
        ignoreFocusOut: true,
    });
    if (readOnlyConnectionString === undefined) {
        return;
    }

    const openAiApiKey =
        process.env.OPENAI_API_KEY ??
        (await vscode.window.showInputBox({
            title: "OpenAI API Key",
            prompt: "Required by the current sqloom tune CLI surface. The extension does not store this value.",
            password: true,
            ignoreFocusOut: true,
        }));
    if (!openAiApiKey) {
        vscode.window.showWarningMessage("Sqloom tune requires an OpenAI API key.");
        return;
    }

    const configuration = getConfiguration();
    const openAiModel = configuration.get<string>("openai.model", "gpt-5.4-mini");
    const replayDataAgent = configuration.get<string>("replayDataAgent", "required");

    const args = [
        "tune",
        harnessPath,
        "--model-provider",
        "openai",
        "--openai-api-key",
        openAiApiKey,
        "--openai-model",
        openAiModel,
        "--replay-data-agent",
        replayDataAgent,
    ];

    if (target.trim().length > 0) {
        args.push("--target", target.trim());
    }

    if (readOnlyConnectionString.trim().length > 0) {
        args.push("--read-only-connection-string", readOnlyConnectionString.trim());
    }

    const succeeded = await cliService.run(args, workspaceFolder);
    if (succeeded) {
        refreshableProviders.forEach((provider) => provider.refresh());
    }
}

async function selectCliPath(): Promise<void> {
    const configuration = getConfiguration();
    const current = configuration.get<string>("cli.path", "sqloom");
    const next = await vscode.window.showInputBox({
        title: "Sqloom CLI Path",
        prompt: "Use sqloom when the CLI is available on PATH, or enter an absolute executable path.",
        value: current,
        ignoreFocusOut: true,
    });

    if (!next) {
        return;
    }

    await configuration.update("cli.path", next, vscode.ConfigurationTarget.Workspace);
}

async function openArtifactRoot(): Promise<void> {
    const workspaceFolder = getPrimaryWorkspaceFolder();
    if (!workspaceFolder) {
        vscode.window.showWarningMessage("Open a workspace to inspect Sqloom artifacts.");
        return;
    }

    const artifactRoot = getArtifactRoot(workspaceFolder);
    await revealUriCommand(artifactRoot);
}

async function openSqlProposal(target?: vscode.Uri | FileNode): Promise<void> {
    const explicitUri = uriFromCommandTarget(target);
    if (explicitUri) {
        await openUriCommand(explicitUri);
        return;
    }

    const workspaceFolder = getPrimaryWorkspaceFolder();
    if (!workspaceFolder) {
        vscode.window.showWarningMessage("Open a workspace to inspect Sqloom advice.");
        return;
    }

    const proposals = await collectFilesByName(getArtifactRoot(workspaceFolder), [
        "sql-tuning-proposal.sql",
    ]);
    if (proposals.length === 0) {
        vscode.window.showWarningMessage("No Sqloom SQL proposal files were found.");
        return;
    }

    await openUriCommand(proposals[0]);
}

async function openUriCommand(target?: vscode.Uri | FileNode): Promise<void> {
    const uri = uriFromCommandTarget(target);
    if (!uri) {
        return;
    }

    const document = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(document);
}

async function revealUriCommand(target?: vscode.Uri | RunNode | FileNode): Promise<void> {
    const uri = uriFromCommandTarget(target);
    if (!uri) {
        return;
    }

    await vscode.commands.executeCommand("revealFileInOS", uri);
}

function uriFromCommandTarget(target?: vscode.Uri | RunNode | FileNode): vscode.Uri | undefined {
    if (!target) {
        return undefined;
    }

    if (target instanceof vscode.Uri) {
        return target;
    }

    return target.uri;
}

async function pickWorkspaceFolder(): Promise<vscode.WorkspaceFolder | undefined> {
    const folders = vscode.workspace.workspaceFolders;

    if (!folders || folders.length === 0) {
        vscode.window.showWarningMessage("Open a workspace before running Sqloom.");
        return undefined;
    }

    if (folders.length === 1) {
        return folders[0];
    }

    const selected = await vscode.window.showQuickPick(
        folders.map((folder) => ({
            label: folder.name,
            description: folder.uri.fsPath,
            folder,
        })),
        {
            title: "Sqloom Workspace",
            placeHolder: "Select the workspace folder for this Sqloom command.",
        },
    );

    return selected?.folder;
}

function getPrimaryWorkspaceFolder(): vscode.WorkspaceFolder | undefined {
    return vscode.workspace.workspaceFolders?.[0];
}

function getConfiguration(): vscode.WorkspaceConfiguration {
    return vscode.workspace.getConfiguration("sqloom");
}

function getArtifactRoot(workspaceFolder: vscode.WorkspaceFolder): vscode.Uri {
    const configuredRoot =
        getConfiguration().get<string>("artifactsRoot", "artifacts/sqloom").trim() ||
        "artifacts/sqloom";

    if (path.isAbsolute(configuredRoot)) {
        return vscode.Uri.file(configuredRoot);
    }

    return vscode.Uri.joinPath(workspaceFolder.uri, ...splitPath(configuredRoot));
}

async function defaultHarnessPath(workspaceFolder: vscode.WorkspaceFolder): Promise<string> {
    const sampleHarness = "tests/Sqloom/Sqloom.TestApp/default/Harness.cs";
    const sampleHarnessUri = vscode.Uri.joinPath(workspaceFolder.uri, ...splitPath(sampleHarness));

    try {
        await vscode.workspace.fs.stat(sampleHarnessUri);
        return sampleHarness;
    } catch {
        return "";
    }
}

async function collectRunDirectories(artifactRoot: vscode.Uri): Promise<vscode.Uri[]> {
    const collected = new Map<string, vscode.Uri>();
    await visitDirectories(artifactRoot, 0, 6, async (directory) => {
        const children = await readDirectorySafe(directory);
        const fileNames = new Set(
            children
                .filter(([, type]) => type === vscode.FileType.File)
                .map(([name]) => name.toLowerCase()),
        );

        if (
            fileNames.has("tune-summary.json") ||
            fileNames.has("replay-data-prep.json") ||
            fileNames.has("query-store-correlation.json") ||
            fileNames.has("tuning-advice.json") ||
            fileNames.has("sql-tuning-proposal.sql")
        ) {
            collected.set(directory.toString(), directory);
        }
    });

    return sortUrisByModifiedTime([...collected.values()]);
}

async function collectKnownRunFileItems(runUri: vscode.Uri): Promise<TreeNode[]> {
    const items: TreeNode[] = [];

    for (const relativeFile of knownRunFiles) {
        const uri = vscode.Uri.joinPath(runUri, ...splitPath(relativeFile));
        if (await fileExists(uri)) {
            const contextValue =
                path.basename(uri.fsPath).toLowerCase() === "sql-tuning-proposal.sql"
                    ? "sqloomProposal"
                    : "sqloom.runFile";
            items.push(createFileItem(uri, runUri, contextValue));
        }
    }

    if (items.length === 0) {
        return [createMessageItem("No known Sqloom artifact files found in this run.")];
    }

    return items;
}

async function collectFilesByName(artifactRoot: vscode.Uri, fileNames: readonly string[]) {
    const wanted = new Set(fileNames.map((name) => name.toLowerCase()));
    const collected: vscode.Uri[] = [];

    await visitDirectories(artifactRoot, 0, 7, async (directory) => {
        const children = await readDirectorySafe(directory);
        for (const [name, type] of children) {
            if (type === vscode.FileType.File && wanted.has(name.toLowerCase())) {
                collected.push(vscode.Uri.joinPath(directory, name));
            }
        }
    });

    return sortUrisByModifiedTime(collected);
}

async function visitDirectories(
    directory: vscode.Uri,
    depth: number,
    maxDepth: number,
    visitor: (directory: vscode.Uri) => Promise<void>,
): Promise<void> {
    if (depth > maxDepth) {
        return;
    }

    const children = await readDirectorySafe(directory);
    if (children.length === 0 && depth === 0) {
        return;
    }

    await visitor(directory);

    for (const [name, type] of children) {
        if (type === vscode.FileType.Directory) {
            await visitDirectories(vscode.Uri.joinPath(directory, name), depth + 1, maxDepth, visitor);
        }
    }
}

async function sortUrisByModifiedTime(uris: vscode.Uri[]): Promise<vscode.Uri[]> {
    const pairs = await Promise.all(
        uris.map(async (uri) => {
            try {
                const stat = await vscode.workspace.fs.stat(uri);
                return { uri, mtime: stat.mtime };
            } catch {
                return { uri, mtime: 0 };
            }
        }),
    );

    return pairs.sort((left, right) => right.mtime - left.mtime).map((pair) => pair.uri);
}

async function readDirectorySafe(uri: vscode.Uri): Promise<[string, vscode.FileType][]> {
    try {
        return await vscode.workspace.fs.readDirectory(uri);
    } catch {
        return [];
    }
}

async function fileExists(uri: vscode.Uri): Promise<boolean> {
    try {
        const stat = await vscode.workspace.fs.stat(uri);
        return stat.type === vscode.FileType.File;
    } catch {
        return false;
    }
}

function createActionItem(
    label: string,
    tooltip: string,
    icon: string,
    command: string,
): vscode.TreeItem {
    const item = new vscode.TreeItem(label, vscode.TreeItemCollapsibleState.None);
    item.tooltip = tooltip;
    item.iconPath = new vscode.ThemeIcon(icon);
    item.command = {
        command,
        title: label,
    };

    return item;
}

function createRunItem(uri: vscode.Uri, artifactRoot: vscode.Uri): RunNode {
    return new RunNode(uri, artifactRoot);
}

function createFileItem(uri: vscode.Uri, root: vscode.Uri, contextValue: string): FileNode {
    return new FileNode(uri, relativeLabel(root, uri), contextValue);
}

function createMessageItem(message: string): MessageNode {
    return new MessageNode(message);
}

function relativeLabel(root: vscode.Uri, target: vscode.Uri): string {
    const relative = path.relative(root.fsPath, target.fsPath);
    return relative.length === 0 ? path.basename(target.fsPath) : relative.replace(/\\/g, "/");
}

function splitPath(value: string): string[] {
    return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}

function redactArgs(args: readonly string[]): string[] {
    const sensitiveFlags = new Set(["--openai-api-key", "--read-only-connection-string"]);
    const redacted: string[] = [];
    let redactNext = false;

    for (const arg of args) {
        if (redactNext) {
            redacted.push("<redacted>");
            redactNext = false;
            continue;
        }

        redacted.push(quoteArg(arg));
        if (sensitiveFlags.has(arg)) {
            redactNext = true;
        }
    }

    return redacted;
}

function quoteArg(arg: string): string {
    if (/^[A-Za-z0-9._:/\\-]+$/.test(arg)) {
        return arg;
    }

    return JSON.stringify(arg);
}
