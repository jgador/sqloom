import * as vscode from "vscode";
import { outputChannelName } from "../constants/extensionConstants";

// One shared channel for CLI transcripts across services and commands.
let outputChannel: vscode.OutputChannel | undefined;

export function getSqloomOutputChannel(): vscode.OutputChannel {
  outputChannel ??= vscode.window.createOutputChannel(outputChannelName);
  return outputChannel;
}
