import * as vscode from "vscode";

export function getSqloomConfiguration(): vscode.WorkspaceConfiguration {
  return vscode.workspace.getConfiguration("sqloom");
}
