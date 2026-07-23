import * as vscode from "vscode";
import {
  getPrimaryWorkspaceFolder,
  resolveWorkspaceFolder,
} from "../utils/workspaceFolder";
import { splitPath } from "../utils/pathUtils";

export async function openArtifactFile(
  relativePath: string,
  workspaceFolderUri?: string,
): Promise<void> {
  const workspaceFolder = resolveWorkspaceFolder(workspaceFolderUri);
  const normalizedPath = relativePath.trim();
  if (!workspaceFolder || normalizedPath.length === 0) {
    return;
  }

  const artifactUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(normalizedPath),
  );

  try {
    await vscode.workspace.fs.stat(artifactUri);
  } catch {
    void vscode.window.showWarningMessage(
      `Sqloom artifact was not found: ${normalizedPath}`,
    );
    return;
  }

  if (normalizedPath.toLowerCase().endsWith(".dacpac")) {
    // DACPAC files are binary; reveal instead of opening a text editor tab.
    await vscode.commands.executeCommand("revealInExplorer", artifactUri);
    return;
  }

  await vscode.window.showTextDocument(artifactUri, { preview: true });
}

export async function revealArtifactFile(
  relativePath: string,
  workspaceFolderUri?: string,
): Promise<void> {
  const workspaceFolder = resolveWorkspaceFolder(workspaceFolderUri);
  const normalizedPath = relativePath.trim();
  if (!workspaceFolder || normalizedPath.length === 0) {
    return;
  }

  const artifactUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(normalizedPath),
  );

  try {
    await vscode.workspace.fs.stat(artifactUri);
  } catch {
    void vscode.window.showWarningMessage(
      `Sqloom artifact was not found: ${normalizedPath}`,
    );
    return;
  }

  await vscode.commands.executeCommand("revealInExplorer", artifactUri);
}

export async function revealArtifactDirectory(
  artifactDir: string,
): Promise<void> {
  const workspaceFolder = getPrimaryWorkspaceFolder();
  const normalizedArtifactDir = artifactDir.trim();
  if (!workspaceFolder || normalizedArtifactDir.length === 0) {
    return;
  }

  const artifactUri = vscode.Uri.joinPath(
    workspaceFolder.uri,
    ...splitPath(normalizedArtifactDir),
  );

  try {
    await vscode.workspace.fs.stat(artifactUri);
  } catch {
    void vscode.window.showWarningMessage(
      `Sqloom artifact directory was not found: ${normalizedArtifactDir}`,
    );
    return;
  }

  await vscode.commands.executeCommand("revealInExplorer", artifactUri);
}
