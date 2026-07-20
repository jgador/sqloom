import { spawn } from "node:child_process";
import { mkdtemp, readFile, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { ReplayEndpoint } from "../sharedInterfaces/dashboard";

export type EndpointCatalogInput = {
  cliPath: string;
  harnessPath: string;
  cwd: string;
};

export type EndpointProcessResult = {
  exitCode: number;
  stdout: string;
  stderr: string;
  launchError?: string;
};

export type EndpointProcessRunner = (
  executable: string,
  args: readonly string[],
  cwd: string,
) => Promise<EndpointProcessResult>;

export type EndpointCatalogLoadResult =
  | {
      status: "loaded";
      endpoints: ReplayEndpoint[];
      command: readonly string[];
      stdout: string;
      stderr: string;
    }
  | {
      status: "failed";
      message: string;
      command: readonly string[];
      stdout: string;
      stderr: string;
    };

export async function loadEndpointCatalog(
  input: EndpointCatalogInput,
  runProcess: EndpointProcessRunner = runEndpointProcess,
): Promise<EndpointCatalogLoadResult> {
  let temporaryDirectory: string;
  try {
    temporaryDirectory = await mkdtemp(
      join(tmpdir(), "sqloom-vscode-endpoints-"),
    );
  } catch (error) {
    return {
      status: "failed",
      message: formatEndpointCatalogError(error),
      command: ["endpoints", input.harnessPath],
      stdout: "",
      stderr: "",
    };
  }
  const outputPath = join(temporaryDirectory, "endpoints.json");
  const args = [
    "endpoints",
    input.harnessPath,
    "--json-output-file",
    outputPath,
  ] as const;
  let processResult: EndpointProcessResult = {
    exitCode: -1,
    stdout: "",
    stderr: "",
  };

  try {
    processResult = await runProcess(input.cliPath, args, input.cwd);
    if (processResult.exitCode !== 0) {
      return {
        status: "failed",
        message:
          processResult.launchError ??
          `Sqloom endpoint discovery exited with code ${processResult.exitCode}.`,
        command: args,
        stdout: processResult.stdout,
        stderr: processResult.stderr,
      };
    }

    const json = await readFile(outputPath, "utf8");
    const endpoints = parseEndpointCatalog(json);
    return {
      status: "loaded",
      endpoints,
      command: args,
      stdout: processResult.stdout,
      stderr: processResult.stderr,
    };
  } catch (error) {
    return {
      status: "failed",
      message: formatEndpointCatalogError(error),
      command: args,
      stdout: processResult.stdout,
      stderr: processResult.stderr,
    };
  } finally {
    await rm(temporaryDirectory, { recursive: true, force: true }).catch(
      () => undefined,
    );
  }
}

export function parseEndpointCatalog(json: string): ReplayEndpoint[] {
  const value: unknown = JSON.parse(json);
  if (!Array.isArray(value)) {
    throw new Error("Sqloom endpoint output must be a JSON array.");
  }

  return value.map((item, index) => parseEndpoint(item, index));
}

function parseEndpoint(value: unknown, index: number): ReplayEndpoint {
  if (!isRecord(value)) {
    throw new Error(`Sqloom endpoint at index ${index} must be an object.`);
  }

  const stableOperationKey = requiredString(value, "stableOperationKey", index);
  const httpMethod = requiredString(value, "httpMethod", index);
  const route = requiredString(value, "route", index);
  const controllerType = optionalString(value, "controllerType", index);
  const methodName = optionalString(value, "methodName", index);

  return {
    stableOperationKey,
    httpMethod,
    route,
    ...(controllerType === undefined ? {} : { controllerType }),
    ...(methodName === undefined ? {} : { methodName }),
  };
}

function requiredString(
  value: Record<string, unknown>,
  propertyName: string,
  index: number,
): string {
  const propertyValue = value[propertyName];
  if (typeof propertyValue !== "string" || propertyValue.trim().length === 0) {
    throw new Error(
      `Sqloom endpoint at index ${index} has an invalid ${propertyName}.`,
    );
  }

  return propertyValue;
}

function optionalString(
  value: Record<string, unknown>,
  propertyName: string,
  index: number,
): string | undefined {
  const propertyValue = value[propertyName];
  if (propertyValue === undefined || propertyValue === null) {
    return undefined;
  }

  if (typeof propertyValue !== "string") {
    throw new Error(
      `Sqloom endpoint at index ${index} has an invalid ${propertyName}.`,
    );
  }

  return propertyValue;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function formatEndpointCatalogError(error: unknown): string {
  if (error instanceof Error) {
    return `Unable to load Sqloom endpoints: ${error.message}`;
  }

  return "Unable to load Sqloom endpoints.";
}

async function runEndpointProcess(
  executable: string,
  args: readonly string[],
  cwd: string,
): Promise<EndpointProcessResult> {
  return new Promise((resolve) => {
    let settled = false;
    let stdout = "";
    let stderr = "";

    const finish = (result: EndpointProcessResult) => {
      if (settled) {
        return;
      }

      settled = true;
      resolve(result);
    };

    try {
      const child = spawn(executable, args, {
        cwd,
        env: process.env,
        shell: false,
        windowsHide: true,
      });

      child.stdout.on("data", (chunk: Buffer) => {
        stdout += chunk.toString();
      });
      child.stderr.on("data", (chunk: Buffer) => {
        stderr += chunk.toString();
      });
      child.on("error", (error) => {
        finish({
          exitCode: -1,
          stdout,
          stderr,
          launchError: `Unable to start Sqloom: ${error.message}`,
        });
      });
      child.on("close", (code) => {
        finish({ exitCode: code ?? -1, stdout, stderr });
      });
    } catch (error) {
      finish({
        exitCode: -1,
        stdout,
        stderr,
        launchError: formatEndpointCatalogError(error),
      });
    }
  });
}
