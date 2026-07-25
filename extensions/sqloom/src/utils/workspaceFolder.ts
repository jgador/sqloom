import * as vscode from "vscode";

export function getPrimaryWorkspaceFolder():
  vscode.WorkspaceFolder | undefined {
  return vscode.workspace.workspaceFolders?.[0];
}

export function findWorkspaceFolderByUri(
  workspaceFolders: readonly vscode.WorkspaceFolder[],
  workspaceFolderUri: string,
): vscode.WorkspaceFolder | undefined {
  const normalizedUri = workspaceFolderUri.trim();
  if (normalizedUri.length === 0) {
    return undefined;
  }

  for (const folder of workspaceFolders) {
    if (
      folder.uri.toString() === normalizedUri ||
      folder.uri.fsPath === normalizedUri
    ) {
      return folder;
    }
  }

  return undefined;
}

export function resolveWorkspaceFolder(
  workspaceFolderUri?: string,
): vscode.WorkspaceFolder | undefined {
  const match = findWorkspaceFolderByUri(
    vscode.workspace.workspaceFolders ?? [],
    workspaceFolderUri ?? "",
  );
  if (match) {
    return match;
  }

  // Recent runs from older extension builds may not have stored a workspace URI.
  return getPrimaryWorkspaceFolder();
}
