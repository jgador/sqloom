export const dashboardStyles = `
        :root {
            color-scheme: light dark;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            min-width: 320px;
            min-height: 100vh;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
            font-family: var(--vscode-font-family);
            font-size: var(--vscode-font-size);
        }

        button,
        input,
        select {
            font: inherit;
        }

        .shell {
            min-height: 100vh;
            padding: 0;
        }

        .surface {
            width: 100%;
            min-height: 100vh;
            margin: 0;
            border: 0;
            border-radius: 0;
            background: var(--vscode-editor-background);
            box-shadow: none;
        }

        .top {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 24px;
            padding: 24px 28px 8px;
            align-items: start;
        }

        .brand {
            display: flex;
            gap: 18px;
            align-items: center;
            min-width: 0;
        }

        .brand img {
            width: 58px;
            height: 58px;
            border-radius: 8px;
            flex: 0 0 auto;
        }

        .title {
            margin: 0 0 4px;
            font-size: 24px;
            line-height: 1.2;
            font-weight: 700;
        }

        .subtitle,
        .caption,
        .muted {
            color: var(--vscode-descriptionForeground);
        }

        .primary-action {
            min-width: 160px;
            border: 0;
            border-radius: 4px;
            padding: 10px 18px;
            color: var(--vscode-button-foreground);
            background: var(--vscode-button-background);
            font-weight: 600;
            cursor: pointer;
        }

        .primary-action:hover {
            background: var(--vscode-button-hoverBackground);
        }

        .run-note {
            margin-top: 8px;
            text-align: center;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }

        .stepper {
            display: grid;
            grid-template-columns: repeat(5, minmax(92px, 1fr));
            gap: 0;
            padding: 20px 120px 16px;
        }

        .step {
            position: relative;
            display: grid;
            justify-items: center;
            text-align: center;
            min-width: 0;
        }

        .step:not(:last-child)::after {
            content: "";
            position: absolute;
            top: 16px;
            left: calc(50% + 28px);
            right: calc(-50% + 28px);
            height: 1px;
            background: var(--vscode-panel-border);
        }

        .step-index {
            display: grid;
            place-items: center;
            width: 34px;
            height: 34px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 999px;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
            font-weight: 700;
            z-index: 1;
        }

        .step.active .step-index {
            border-color: var(--vscode-focusBorder);
            color: var(--vscode-focusBorder);
        }

        .step-label {
            margin-top: 8px;
            font-weight: 600;
        }

        .step-detail {
            margin-top: 2px;
            min-height: 16px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }

        .summary-strip {
            display: grid;
            grid-template-columns: repeat(4, minmax(160px, 1fr)) auto;
            gap: 0;
            margin: 0 24px 14px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 8px;
            overflow: hidden;
        }

        .summary-item {
            display: grid;
            grid-template-columns: auto minmax(0, 1fr);
            gap: 12px;
            align-items: center;
            min-height: 58px;
            padding: 12px 18px;
            border-right: 1px solid var(--vscode-panel-border);
        }

        .summary-label,
        .row-title,
        .field-label {
            font-weight: 600;
        }

        .summary-detail {
            margin-top: 2px;
            font-size: 12px;
            color: var(--vscode-descriptionForeground);
        }

        .refresh {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            padding: 0 22px;
            border: 0;
            color: var(--vscode-textLink-foreground);
            background: transparent;
            cursor: pointer;
            font-weight: 600;
            white-space: nowrap;
        }

        .content {
            display: grid;
            grid-template-columns: minmax(280px, 360px) minmax(0, 1fr);
            gap: 16px;
            padding: 0 24px 20px;
        }

        .panel {
            border: 1px solid var(--vscode-panel-border);
            border-radius: 8px;
            background: var(--vscode-sideBar-background, var(--vscode-editor-background));
            min-width: 0;
        }

        .config {
            padding: 18px 20px;
        }

        .section-title {
            margin: 0 0 16px;
            font-size: 18px;
            line-height: 1.25;
        }

        .field {
            margin-bottom: 14px;
        }

        label.field {
            display: block;
        }

        .field-heading {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
            margin-bottom: 6px;
        }

        .field-label {
            margin-bottom: 6px;
            font-size: 12px;
        }

        .field-heading .field-label {
            margin-bottom: 0;
        }

        .field-status {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            color: var(--vscode-descriptionForeground);
            font-size: 11px;
            white-space: nowrap;
        }

        .field-status::before {
            content: "";
            width: 6px;
            height: 6px;
            border-radius: 999px;
            background: currentColor;
            opacity: 0.85;
        }

        .field-status.status-ready {
            color: #2f8f46;
        }

        .field-status.status-warning {
            color: var(--vscode-editorWarning-foreground, #b7791f);
        }

        .field-status.status-neutral,
        .field-status.status-idle {
            color: var(--vscode-descriptionForeground);
        }

        .field-note {
            margin-top: 6px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
            overflow-wrap: anywhere;
        }

        .control {
            display: flex;
            align-items: center;
            min-height: 32px;
            width: 100%;
            border: 1px solid var(--vscode-input-border, var(--vscode-panel-border));
            border-radius: 4px;
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            padding: 0 10px;
        }

        .control-value {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            min-width: 0;
            flex: 1 1 auto;
        }

        .control-icon {
            color: var(--vscode-descriptionForeground);
            margin-left: 8px;
            flex: 0 0 auto;
        }

        .control-input,
        .control-select {
            display: block;
            min-height: 32px;
            width: 100%;
            border: 1px solid var(--vscode-input-border, var(--vscode-panel-border));
            border-radius: 4px;
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            padding: 0 10px;
            outline: none;
        }

        .control-input:focus,
        .control-select:focus,
        .mask-toggle:focus {
            border-color: var(--vscode-focusBorder);
        }

        .password-control {
            position: relative;
        }

        .password-control .control-input {
            padding-right: 38px;
        }

        .mask-toggle {
            position: absolute;
            top: 1px;
            right: 1px;
            display: grid;
            place-items: center;
            width: 32px;
            height: 30px;
            border: 1px solid transparent;
            border-left-color: var(--vscode-input-border, var(--vscode-panel-border));
            border-radius: 0 4px 4px 0;
            background: transparent;
            color: var(--vscode-descriptionForeground);
            cursor: pointer;
        }

        .mask-toggle:hover {
            color: var(--vscode-input-foreground);
            background: var(--vscode-toolbar-hoverBackground, transparent);
        }

        .toggle-row {
            display: flex;
            align-items: center;
            gap: 8px;
            min-height: 32px;
        }

        .toggle {
            position: relative;
            width: 30px;
            height: 16px;
            border-radius: 999px;
            background: var(--vscode-inputOption-activeBorder, #9aa0a6);
            opacity: 0.8;
        }

        .toggle::after {
            content: "";
            position: absolute;
            width: 12px;
            height: 12px;
            top: 2px;
            left: 2px;
            border-radius: 999px;
            background: var(--vscode-editor-background);
            box-shadow: 0 1px 2px rgba(0, 0, 0, 0.24);
        }

        .main-panel {
            overflow: hidden;
        }

        .readiness {
            padding: 18px 22px 0;
        }

        .section-heading {
            display: flex;
            gap: 12px;
            align-items: baseline;
            margin-bottom: 10px;
        }

        .section-heading .section-title {
            margin: 0;
        }

        .check-row {
            display: grid;
            grid-template-columns: 30px minmax(150px, 1.1fr) minmax(120px, 0.8fr) minmax(180px, 1.4fr) auto;
            gap: 14px;
            align-items: center;
            min-height: 44px;
            border-top: 1px solid var(--vscode-panel-border);
        }

        .check-row:first-of-type {
            border-top: 0;
        }

        .status-dot {
            display: grid;
            place-items: center;
            width: 20px;
            height: 20px;
            border-radius: 999px;
            border: 1px solid currentColor;
            font-size: 12px;
            font-weight: 700;
        }

        .status-ready {
            color: #22a447;
        }

        .status-warning {
            color: var(--vscode-editorWarning-foreground, #b7791f);
        }

        .status-neutral,
        .status-idle {
            color: var(--vscode-descriptionForeground);
        }

        .badge {
            justify-self: start;
            border-radius: 4px;
            padding: 4px 10px;
            font-size: 12px;
            background: var(--vscode-badge-background);
            color: var(--vscode-badge-foreground);
        }

        .badge.status-ready {
            background: rgba(34, 164, 71, 0.12);
            color: #15803d;
        }

        .badge.status-warning {
            background: color-mix(
                in srgb,
                var(--vscode-editorWarning-foreground, #b7791f) 14%,
                transparent
            );
            color: var(--vscode-editorWarning-foreground, #b7791f);
        }

        .badge.status-neutral {
            background: var(--vscode-input-background);
            color: var(--vscode-descriptionForeground);
        }

        .row-action {
            border: 0;
            background: transparent;
            color: var(--vscode-textLink-foreground);
            cursor: pointer;
            font-weight: 600;
            white-space: nowrap;
        }

        .row-trailing {
            color: var(--vscode-editor-foreground);
            white-space: nowrap;
        }

        .check-detail {
            overflow-wrap: anywhere;
        }

        .results {
            margin-top: 8px;
            border-top: 1px solid var(--vscode-panel-border);
            padding: 16px 12px 10px;
        }

        .results .section-title {
            margin: 0;
        }

        .artifact {
            display: grid;
            grid-template-columns: 24px minmax(0, 1fr) 120px 28px;
            gap: 12px;
            align-items: center;
            min-height: 36px;
            margin-top: 8px;
            padding: 0 8px;
            border: 1px solid var(--vscode-panel-border);
            border-radius: 4px;
            background: var(--vscode-editor-background);
        }

        .file-icon {
            width: 14px;
            height: 18px;
            border: 1px solid var(--vscode-descriptionForeground);
            border-radius: 2px;
            position: relative;
        }

        .file-icon::after {
            content: "";
            position: absolute;
            right: -1px;
            top: -1px;
            width: 5px;
            height: 5px;
            border-left: 1px solid var(--vscode-descriptionForeground);
            border-bottom: 1px solid var(--vscode-descriptionForeground);
            background: var(--vscode-editor-background);
        }

        .skeleton {
            height: 5px;
            border-radius: 999px;
            background: var(--vscode-panel-border);
            opacity: 0.9;
        }

        .more {
            border: 0;
            background: transparent;
            color: var(--vscode-descriptionForeground);
            cursor: pointer;
            font-size: 18px;
        }

        @media (max-width: 1050px) {
            .top,
            .content,
            .summary-strip {
                grid-template-columns: 1fr;
            }

            .stepper {
                padding-left: 28px;
                padding-right: 28px;
                overflow-x: auto;
            }

            .summary-item {
                border-right: 0;
                border-bottom: 1px solid var(--vscode-panel-border);
            }

            .refresh {
                min-height: 44px;
            }

            .check-row {
                grid-template-columns: 30px minmax(120px, 1fr);
                gap: 8px 12px;
                padding: 10px 0;
            }

            .badge,
            .check-detail,
            .row-trailing,
            .row-action {
                grid-column: 2;
            }
        }

        @media (max-width: 620px) {
            .shell {
                padding: 0;
            }

            .surface {
                border-radius: 0;
            }

            .top {
                padding: 18px;
            }

            .brand {
                align-items: flex-start;
            }

            .brand img {
                width: 48px;
                height: 48px;
            }

            .stepper {
                grid-template-columns: repeat(5, 112px);
            }

            .content,
            .summary-strip {
                margin-left: 16px;
                margin-right: 16px;
                padding-left: 0;
                padding-right: 0;
            }

            .artifact {
                grid-template-columns: 24px minmax(0, 1fr) 28px;
            }

            .artifact .skeleton {
                display: none;
            }
        }
`;
