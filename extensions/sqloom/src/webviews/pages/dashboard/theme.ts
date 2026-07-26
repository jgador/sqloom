export interface SqloomTheme {
  border: string;
  muted: string;
  control: string;
  controlForeground: string;
  focus: string;
  ready: string;
  warning: string;
  blue: string;
  blueHover: string;
  foreground: string;
  background: string;
  fontFamily: string;
  fontSize: string;
}

/**
 * Maps Sqloom UI design tokens to the corresponding VS Code theme variables.
 * This ensures the webview UI dynamically inherits theme styles.
 */
export const sqloomTokens: SqloomTheme = {
  border: "var(--vscode-panel-border, rgba(128, 128, 128, 0.35))",
  muted: "var(--vscode-descriptionForeground)",
  control: "var(--vscode-input-background)",
  controlForeground: "var(--vscode-input-foreground)",
  focus: "var(--vscode-focusBorder, #0078d4)",
  ready: "#21a955",
  warning: "var(--vscode-editorWarning-foreground, #b7791f)",
  blue: "var(--vscode-button-background, #0e70df)",
  blueHover: "var(--vscode-button-hoverBackground, #1177eb)",
  foreground: "var(--vscode-editor-foreground)",
  background: "var(--vscode-editor-background)",
  fontFamily: "var(--vscode-font-family)",
  fontSize: "var(--vscode-font-size)",
};
