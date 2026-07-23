import * as vscode from "vscode";
import MainController from "./controllers/mainController";

// Extension entry point; activation wiring lives in MainController.
let mainController: MainController | undefined;

export function activate(context: vscode.ExtensionContext): void {
  mainController = new MainController(context);
  context.subscriptions.push(mainController);
  mainController.activate();
}

export function deactivate(): void {
  mainController = undefined;
}
