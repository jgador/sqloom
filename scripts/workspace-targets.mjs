export const supportedActions = ["build", "watch", "test", "lint", "package", "publish:preview"];

export const workspaceTargets = [
    {
        target: "sqloom",
        aliases: ["extension", "vscode"],
        packageName: "sqloom",
        directory: "extensions/sqloom",
        scripts: supportedActions,
    },
];
