export type DashboardOption = {
  label: string;
  value: string;
};

export const modelProviderOptions: readonly DashboardOption[] = [
  { label: "OpenAI", value: "openai" },
];

export const openAiModelOptions: readonly DashboardOption[] = [
  { label: "gpt-5.4", value: "gpt-5.4" },
  { label: "gpt-5.4 mini", value: "gpt-5.4-mini" },
  { label: "gpt-5.5", value: "gpt-5.5" },
  { label: "gpt-5.6 Sol", value: "gpt-5.6-sol" },
  { label: "gpt-5.6 Terra", value: "gpt-5.6-terra" },
  { label: "gpt-5.6 Luna", value: "gpt-5.6-luna" },
];

export const defaultOpenAiModel = "gpt-5.4-mini";

export function isKnownModelProvider(value: string): boolean {
  return modelProviderOptions.some((option) => option.value === value);
}

export function isKnownOpenAiModel(value: string): boolean {
  return openAiModelOptions.some((option) => option.value === value);
}
