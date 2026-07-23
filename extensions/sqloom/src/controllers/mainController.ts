import * as vscode from "vscode";
import {
  cmdInit,
  cmdOpenDashboard,
  cmdSelectCliPath,
  cmdTune,
} from "../constants/extensionConstants";
import { openDashboard, registerDashboardLauncher } from "../dashboard";
import { RecentRunsStore } from "../recentRuns/recentRunsStore";
import { CliService } from "../services/cliService";
import { CommandWorkflowService } from "../services/commandWorkflowService";
import { DashboardWorkflowService } from "../services/dashboardWorkflowService";
import { getSqloomOutputChannel } from "../services/outputChannelService";

/** Wires services, dashboard callbacks, and palette commands during activation. */
export default class MainController implements vscode.Disposable {
  private readonly outputChannel: vscode.OutputChannel;
  private readonly cliService: CliService;
  private readonly recentRunsStore: RecentRunsStore;
  private readonly dashboardWorkflowService: DashboardWorkflowService;
  private readonly commandWorkflowService: CommandWorkflowService;

  constructor(private readonly context: vscode.ExtensionContext) {
    this.outputChannel = getSqloomOutputChannel();
    this.cliService = new CliService(this.outputChannel);
    this.recentRunsStore = new RecentRunsStore(context.workspaceState);
    this.dashboardWorkflowService = new DashboardWorkflowService(
      this.cliService,
      this.recentRunsStore,
    );
    this.commandWorkflowService = new CommandWorkflowService(this.cliService);
  }

  activate(): void {
    const dashboardCallbacks = this.dashboardWorkflowService.createCallbacks();

    this.context.subscriptions.push(
      this.outputChannel,
      registerDashboardLauncher(this.context, dashboardCallbacks),
      vscode.commands.registerCommand(cmdOpenDashboard, () =>
        openDashboard(this.context, dashboardCallbacks),
      ),
      vscode.commands.registerCommand(cmdInit, () =>
        this.commandWorkflowService.runInit(),
      ),
      vscode.commands.registerCommand(cmdTune, () =>
        this.commandWorkflowService.runTune(),
      ),
      vscode.commands.registerCommand(cmdSelectCliPath, () =>
        this.commandWorkflowService.selectCliPath(),
      ),
    );
  }

  dispose(): void {
    // Disposables are owned by ExtensionContext.subscriptions.
  }
}
