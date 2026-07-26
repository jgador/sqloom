import React from "react";
import ReactDOM from "react-dom/client";
import { Dashboard } from "./Dashboard";
import "./dashboard.css";

// Declare globals injected by extension host
declare global {
  interface Window {
    acquireVsCodeApi: () => { postMessage: (message: unknown) => void };
    __INITIAL_STATE__: unknown;
    __INITIAL_ARTIFACTS__: unknown;
    __LOGO_URI__: string;
  }
}

const rootElement = document.getElementById("root");
if (rootElement) {
  ReactDOM.createRoot(rootElement).render(
    <React.StrictMode>
      <Dashboard />
    </React.StrictMode>,
  );
}
