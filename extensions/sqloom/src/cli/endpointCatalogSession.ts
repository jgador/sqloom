import { ReplayEndpoint } from "../sharedInterfaces/dashboard";

/** Tracks the latest endpoint catalog so tune can only run against a loaded selection. */
export class EndpointCatalogSession {
  private generation = 0;
  private context: string | undefined;
  private endpointKeys = new Set<string>();

  beginLoad(): number {
    this.generation += 1;
    this.context = undefined;
    this.endpointKeys.clear();
    return this.generation;
  }

  completeLoad(
    generation: number,
    context: string,
    endpoints: readonly ReplayEndpoint[],
  ): boolean {
    if (!this.isCurrent(generation)) {
      return false;
    }

    this.context = context;
    this.endpointKeys = new Set(
      endpoints.map((endpoint) => endpoint.stableOperationKey),
    );
    return true;
  }

  isCurrent(generation: number): boolean {
    return generation === this.generation;
  }

  canRun(context: string, target: string): boolean {
    return (
      target.length > 0 &&
      context === this.context &&
      this.endpointKeys.has(target)
    );
  }

  clear(): void {
    this.generation += 1;
    this.context = undefined;
    this.endpointKeys.clear();
  }
}
