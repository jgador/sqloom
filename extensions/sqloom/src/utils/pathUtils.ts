export function splitPath(value: string): string[] {
  return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}
