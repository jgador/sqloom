import * as vscode from "vscode";
import { sampleAppHarnessPath } from "../constants/sampleAppDefaults";
import { splitPath } from "../utils/pathUtils";

export async function pickWorkspaceFolder(): Promise<
  vscode.WorkspaceFolder | undefined
> {
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

export async function defaultHarnessPath(
  workspaceFolder: vscode.WorkspaceFolder,
): Promise<string> {
  // Only auto-detect the checked-in sample harness; other repos must enter a path explicitly.
  const sampleHarnessUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(sampleAppHarnessPath),
  );

  try {
    await vscode.workspace.fs.stat(sampleHarnessUri);
    return sampleAppHarnessPath;
  } catch {
    return "";
  }
}
