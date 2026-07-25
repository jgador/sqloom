import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";

const defaultCliPath = "sqloom";
const defaultProbeTimeoutMs = 4000;

// Collapse concurrent availability probes for the same configured CLI path.
const inFlightChecks = new Map<string, Promise<CliAvailabilityStatus>>();

export type CliAvailabilityStatus = {
  cliPath: string;
  ready: boolean;
  detail: string;
  version?: string;
};

export async function checkCliAvailability(
  cliPath: string,
  timeoutMs = defaultProbeTimeoutMs,
): Promise<CliAvailabilityStatus> {
  const effectiveCliPath = cliPath.trim() || defaultCliPath;
  const cacheKey = `${effectiveCliPath}\0${timeoutMs}`;
  const inFlightCheck = inFlightChecks.get(cacheKey);

  if (inFlightCheck) {
    return inFlightCheck;
  }

  const check = new Promise<CliAvailabilityStatus>((resolve) => {
    let settled = false;
    const stdout: Buffer[] = [];
    const stderr: Buffer[] = [];

    let child: ChildProcessWithoutNullStreams;
    try {
      child = spawn(effectiveCliPath, ["--version"], {
        env: process.env,
        shell: false,
        windowsHide: true,
      });
    } catch (error) {
      resolve({
        cliPath: effectiveCliPath,
        ready: false,
        detail: launchFailureDetail(error),
      });
      return;
    }

    const timeout = setTimeout(() => {
      child.kill();
      complete({
        cliPath: effectiveCliPath,
        ready: false,
        detail:
          "Timed out checking the Sqloom CLI. Install the CLI or update sqloom.cli.path.",
      });
    }, timeoutMs);

    const complete = (status: CliAvailabilityStatus): void => {
      if (settled) {
        return;
      }

      settled = true;
      clearTimeout(timeout);
      resolve(status);
    };

    child.stdout.on("data", (chunk: Buffer) => {
      stdout.push(chunk);
    });

    child.stderr.on("data", (chunk: Buffer) => {
      stderr.push(chunk);
    });

    child.on("error", (error) => {
      complete({
        cliPath: effectiveCliPath,
        ready: false,
        detail: launchFailureDetail(error),
      });
    });

    child.on("close", (code) => {
      if (code === 0) {
        const version = firstOutputLine(stdout);
        if (!isSqloomVersionOutput(version)) {
          complete({
            cliPath: effectiveCliPath,
            ready: false,
            detail:
              "The configured executable did not return a Sqloom version banner.",
          });
          return;
        }

        complete({
          cliPath: effectiveCliPath,
          ready: true,
          detail: `Detected ${version}.`,
          version,
        });
        return;
      }

      complete({
        cliPath: effectiveCliPath,
        ready: false,
        detail: failedProbeDetail(code, stdout, stderr),
      });
    });
  });

  inFlightChecks.set(cacheKey, check);
  check.finally(() => inFlightChecks.delete(cacheKey));

  return check;
}

function firstOutputLine(chunks: readonly Buffer[]): string | undefined {
  const output = Buffer.concat(chunks).toString().trim();
  const firstLine = output.split(/\r?\n/, 1)[0]?.trim();
  return firstLine && firstLine.length > 0 ? firstLine : undefined;
}

function isSqloomVersionOutput(value: string | undefined): value is string {
  return value !== undefined && /^sqloom\s+\S+/i.test(value);
}

function failedProbeDetail(
  code: number | null,
  stdout: readonly Buffer[],
  stderr: readonly Buffer[],
): string {
  const output = firstOutputLine(stderr) ?? firstOutputLine(stdout);
  const exitCode = code ?? -1;
  const suffix = output ? ` ${output}` : "";
  return `Sqloom CLI check failed with exit code ${exitCode}.${suffix}`;
}

function launchFailureDetail(error: unknown): string {
  if (isNodeError(error) && error.code === "ENOENT") {
    return "Sqloom CLI was not found. Install the sqloom .NET tool or update sqloom.cli.path.";
  }

  const message =
    error instanceof Error && error.message.length > 0
      ? error.message
      : "Could not start the Sqloom CLI.";

  return `${message} Install the CLI or update sqloom.cli.path.`;
}

function isNodeError(error: unknown): error is NodeJS.ErrnoException {
  return error instanceof Error && "code" in error;
}
