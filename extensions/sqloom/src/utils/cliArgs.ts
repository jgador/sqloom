export function quoteArg(arg: string): string {
  if (/^[A-Za-z0-9._:/\\-]+$/.test(arg)) {
    return arg;
  }

  return JSON.stringify(arg);
}

/** Redacts secret flag values before they are written to the output channel. */
export function redactArgs(args: readonly string[]): string[] {
  const sensitiveFlags = new Set([
    "--openai-api-key",
    "--read-only-connection-string",
  ]);
  const redacted: string[] = [];
  let redactNext = false;

  for (const arg of args) {
    if (redactNext) {
      redacted.push("<redacted>");
      redactNext = false;
      continue;
    }

    redacted.push(quoteArg(arg));
    if (sensitiveFlags.has(arg)) {
      redactNext = true;
    }
  }

  return redacted;
}

export function formatLaunchFailureMessage(
  cliPath: string,
  error: unknown,
): string {
  if (isNodeError(error) && error.code === "ENOENT") {
    return `Sqloom CLI was not found at ${quoteArg(cliPath)}. Install the sqloom .NET tool or update sqloom.cli.path.`;
  }

  const message =
    error instanceof Error && error.message.length > 0
      ? error.message
      : "Could not start the Sqloom CLI.";

  return `Failed to start Sqloom: ${message}`;
}

function isNodeError(error: unknown): error is NodeJS.ErrnoException {
  return error instanceof Error && "code" in error;
}
