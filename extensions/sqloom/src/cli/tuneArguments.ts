export type DashboardTuneArgumentsInput = {
  harnessPath: string;
  target: string;
  modelProvider: string;
  openAiApiKey: string;
  openAiModel: string;
  replayDataAgent: string;
  readOnlyConnectionString: string;
  artifactDir: string;
};

export function buildDashboardTuneArguments(
  input: DashboardTuneArgumentsInput,
): string[] {
  const target = input.target.trim();
  if (target.length === 0) {
    throw new Error("A replay endpoint target is required.");
  }

  return [
    "tune",
    input.harnessPath,
    "--target",
    target,
    "--model-provider",
    input.modelProvider,
    "--openai-api-key",
    input.openAiApiKey,
    "--openai-model",
    input.openAiModel,
    "--replay-data-agent",
    input.replayDataAgent,
    "--read-only-connection-string",
    input.readOnlyConnectionString,
    "--artifact-dir",
    input.artifactDir,
  ];
}
