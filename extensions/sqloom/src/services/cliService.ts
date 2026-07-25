import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import * as vscode from "vscode";
import {
  formatLaunchFailureMessage,
  quoteArg,
  redactArgs,
} from "../utils/cliArgs";
import { getSqloomConfiguration } from "./sqloomConfigurationService";

/** Spawns the public sqloom CLI and mirrors stdout/stderr into the Sqloom output channel. */
export class CliService {
  constructor(private readonly outputChannel: vscode.OutputChannel) {}

  async run(
    args: string[],
    workspaceFolder: vscode.WorkspaceFolder,
    cliPathOverride?: string,
  ): Promise<boolean> {
    const result = await this.runWithExitCode(args, workspaceFolder, {
      cliPathOverride,
    });
    return result.exitCode === 0;
  }

  async runWithExitCode(
    args: string[],
    workspaceFolder: vscode.WorkspaceFolder,
    options?: {
      cliPathOverride?: string;
      quietCompletionToasts?: boolean;
    },
  ): Promise<{ exitCode: number; launchFailureMessage?: string }> {
    const cliPath =
      options?.cliPathOverride?.trim() ||
      getSqloomConfiguration().get<string>("cli.path", "sqloom").trim() ||
      "sqloom";
    const redactedCommand = [cliPath, ...redactArgs(args)].join(" ");

    this.outputChannel.show(true);
    this.outputChannel.appendLine(`> ${redactedCommand}`);
    this.outputChannel.appendLine(`cwd: ${workspaceFolder.uri.fsPath}`);
    this.outputChannel.appendLine("");

    let launchFailureMessage: string | undefined;
    const exitCode = await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: `Sqloom ${args[0]}`,
      },
      () =>
        new Promise<number>((resolve) => {
          let failedToStart = false;
          let child: ChildProcessWithoutNullStreams;
          try {
            child = spawn(cliPath, args, {
              cwd: workspaceFolder.uri.fsPath,
              env: process.env,
              shell: false,
            });
          } catch (error) {
            launchFailureMessage = formatLaunchFailureMessage(cliPath, error);
            this.outputChannel.appendLine(launchFailureMessage);
            resolve(-1);
            return;
          }

          child.stdout.on("data", (chunk: Buffer) => {
            this.outputChannel.append(chunk.toString());
          });

          child.stderr.on("data", (chunk: Buffer) => {
            this.outputChannel.append(chunk.toString());
          });

          child.on("error", (error) => {
            failedToStart = true;
            launchFailureMessage = formatLaunchFailureMessage(cliPath, error);
            this.outputChannel.appendLine("");
            this.outputChannel.appendLine(launchFailureMessage);
            resolve(-1);
          });

          child.on("close", (code) => {
            if (failedToStart) {
              return;
            }

            this.outputChannel.appendLine("");
            this.outputChannel.appendLine(
              `Sqloom exited with code ${code ?? -1}.`,
            );
            resolve(code ?? -1);
          });
        }),
    );

    // Dashboard tune reports completion itself after progress and artifacts settle.
    if (!options?.quietCompletionToasts) {
      if (exitCode === 0) {
        vscode.window.showInformationMessage("Sqloom command completed.");
      } else {
        vscode.window.showErrorMessage(
          launchFailureMessage ??
            "Sqloom command failed. See the Sqloom output channel.",
        );
      }
    }

    return { exitCode, launchFailureMessage };
  }

  appendCommandLine(cliPath: string, command: readonly string[]): void {
    this.outputChannel.appendLine(
      `> ${[cliPath, ...command].map(quoteArg).join(" ")}`,
    );
  }

  appendLine(line: string): void {
    this.outputChannel.appendLine(line);
  }

  append(text: string): void {
    this.outputChannel.append(text);
  }
}
