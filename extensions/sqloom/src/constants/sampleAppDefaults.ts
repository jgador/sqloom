export const sampleAppHarnessPath =
  "tests/Sqloom/Sqloom.TestApp/default/Harness.cs";

export const sampleAppReadOnlyConnectionString =
  "Server=localhost;Database=AdventureWorksLT2025;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

export function defaultReadOnlyConnectionStringForHarness(
  harnessPath: string,
): string {
  const normalizedHarnessPath = harnessPath
    .trim()
    .replace(/^\.[\\/]/, "")
    .replaceAll("\\", "/");

  // Prefill only for the repo sample harness; never invent connection strings elsewhere.
  return normalizedHarnessPath === sampleAppHarnessPath
    ? sampleAppReadOnlyConnectionString
    : "";
}
