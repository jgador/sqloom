export const dashboardStyles = `
        :root {
            color-scheme: light dark;
            --sqloom-border: var(--vscode-panel-border, rgba(128, 128, 128, 0.35));
            --sqloom-muted: var(--vscode-descriptionForeground);
            --sqloom-card: var(--vscode-sideBar-background, var(--vscode-editor-background));
            --sqloom-card-soft: var(--vscode-editorWidget-background, var(--vscode-sideBar-background, var(--vscode-editor-background)));
            --sqloom-control: var(--vscode-input-background);
            --sqloom-control-foreground: var(--vscode-input-foreground);
            --sqloom-focus: var(--vscode-focusBorder, #0078d4);
            --sqloom-ready: #21a955;
            --sqloom-warning: var(--vscode-editorWarning-foreground, #b7791f);
            --sqloom-blue: var(--vscode-button-background, #0e70df);
            --sqloom-blue-hover: var(--vscode-button-hoverBackground, #1177eb);
        }

        body.vscode-dark {
            --sqloom-card: color-mix(in srgb, var(--vscode-editor-background) 88%, white 5%);
            --sqloom-card-soft: color-mix(in srgb, var(--vscode-editor-background) 82%, white 7%);
            --sqloom-row: color-mix(in srgb, var(--vscode-editor-background) 93%, white 4%);
        }

        body.vscode-light,
        body.vscode-high-contrast-light {
            --sqloom-card: color-mix(in srgb, var(--vscode-editor-background) 94%, black 3%);
            --sqloom-card-soft: color-mix(in srgb, var(--vscode-editor-background) 98%, black 2%);
            --sqloom-row: color-mix(in srgb, var(--vscode-editor-background) 98%, black 2%);
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

        button {
            user-select: none;
        }

        .dashboard {
            min-height: 100vh;
            padding: 18px 22px 22px;
            background: var(--vscode-editor-background);
        }

        .topbar {
            position: sticky;
            top: 0;
            z-index: 100;
            background: var(--vscode-editor-background);
            border-bottom: 1px solid var(--sqloom-border);
            padding: 14px 22px;
            margin: -18px -22px 18px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .brand {
            display: flex;
            gap: 14px;
            align-items: center;
            min-width: 0;
        }

        .brand img {
            width: 44px;
            height: 44px;
            border-radius: 7px;
            flex: 0 0 auto;
        }

        .brand-copy {
            min-width: 0;
        }

        .brand h1,
        .panel h2 {
            margin: 0;
            color: var(--vscode-editor-foreground);
            font-weight: 700;
        }

        .brand h1 {
            font-size: 22px;
            line-height: 1.15;
        }

        .brand p,
        .panel p,
        .field-note,
        .summary-detail,
        .artifact-tools,
        .artifact-footer,
        .status-check [data-check-detail],
        .empty-state p,
        .inline-meta,
        .status-footer,
        .artifact-name small {
            color: var(--sqloom-muted);
        }

        .brand p,
        .panel p,
        .status-footer,
        .empty-state p {
            margin: 4px 0 0;
        }

        .primary-action,
        .secondary-action,
        .field-action,
        .icon-button,
        .link-button {
            border-radius: 5px;
            cursor: pointer;
        }

        .primary-action {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            min-height: 38px;
            border: 1px solid transparent;
            padding: 8px 18px;
            color: var(--vscode-button-foreground);
            background: var(--sqloom-blue);
            font-weight: 600;
        }

        .primary-action:hover {
            background: var(--sqloom-blue-hover);
        }

        .primary-action:disabled,
        .primary-action:disabled:hover {
            cursor: not-allowed;
            opacity: 0.55;
            background: var(--sqloom-blue);
        }

        .primary-action.compact {
            min-height: 32px;
            padding: 6px 14px;
        }

        .primary-action.full-width {
            width: 100%;
            margin-top: 22px;
        }

        .secondary-action,
        .field-action,
        .icon-button {
            min-height: 32px;
            border: 1px solid var(--sqloom-border);
            color: var(--vscode-editor-foreground);
            background: transparent;
        }

        .secondary-action {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            padding: 6px 14px;
            white-space: nowrap;
        }

        .button-icon {
            display: block;
            width: 16px;
            height: 16px;
            flex: 0 0 16px;
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        .button-icon .filled-icon {
            fill: currentColor;
            stroke: none;
        }

        .secondary-action:hover,
        .field-action:hover,
        .icon-button:hover {
            background: var(--vscode-toolbar-hoverBackground, color-mix(in srgb, var(--vscode-editor-foreground) 8%, transparent));
        }

        .field-action {
            padding: 0 14px;
            border-radius: 0 5px 5px 0;
        }

        .link-button {
            border: 0;
            padding: 0;
            color: var(--vscode-textLink-foreground);
            background: transparent;
        }

        .stepper {
            display: grid;
            grid-template-columns: repeat(4, minmax(80px, 1fr));
            gap: 0;
            padding: 16px 20px;
            border: 1px solid var(--sqloom-border);
            border-radius: 6px;
            background: var(--sqloom-card);
        }

        .step {
            position: relative;
            display: grid;
            justify-items: center;
            min-width: 0;
            text-align: center;
        }

        .step:not(:last-child)::after {
            content: "";
            position: absolute;
            top: 16px;
            left: calc(50% + 34px);
            right: calc(-50% + 34px);
            height: 1px;
            background: var(--sqloom-border);
        }

        .step.active:not(:last-child)::before {
            content: "";
            position: absolute;
            top: 16px;
            left: calc(50% + 34px);
            right: calc(-50% + 34px);
            height: 2px;
            background: var(--sqloom-focus);
            z-index: 1;
        }

        .step-index {
            position: relative;
            z-index: 2;
            display: grid;
            place-items: center;
            width: 34px;
            height: 34px;
            border: 1px solid var(--sqloom-border);
            border-radius: 999px;
            background: var(--vscode-editor-background);
            color: var(--vscode-editor-foreground);
            font-weight: 700;
        }

        .step.active .step-index {
            border-color: var(--sqloom-focus);
            color: var(--sqloom-focus);
            box-shadow: 0 0 0 3px color-mix(in srgb, var(--sqloom-focus) 18%, transparent);
        }

        .step.completed .step-index {
            border-color: var(--sqloom-ready);
            background: color-mix(in srgb, var(--sqloom-ready) 12%, var(--vscode-editor-background));
            color: var(--sqloom-ready);
        }

        .step.failed .step-index {
            border-color: var(--sqloom-warning);
            background: color-mix(in srgb, var(--sqloom-warning) 12%, var(--vscode-editor-background));
            color: var(--sqloom-warning);
        }

        .step.completed:not(:last-child)::before {
            content: "";
            position: absolute;
            top: 16px;
            left: calc(50% + 34px);
            right: calc(-50% + 34px);
            height: 2px;
            background: var(--sqloom-ready);
            z-index: 1;
        }

        .step-label {
            margin-top: 8px;
            font-weight: 600;
        }

        .step-detail {
            min-height: 16px;
            margin-top: 2px;
            color: var(--sqloom-muted);
            font-size: 12px;
        }

        .dashboard-grid {
            display: grid;
            grid-template-columns: minmax(320px, 380px) minmax(0, 1fr);
            gap: 20px;
            align-items: start;
        }

        .main-column,
        .side-column {
            display: grid;
            gap: 16px;
            align-content: start;
            min-width: 0;
        }

        .panel {
            min-width: 0;
            border: 1px solid var(--sqloom-border);
            border-radius: 6px;
            background: var(--sqloom-card);
            box-shadow: none;
        }

        .setup-panel,
        .artifacts-panel,
        .status-panel,
        .recent-panel {
            padding: 18px 20px;
        }

        .dashboard[data-mode="run"] .mode-edit,
        .dashboard[data-mode="edit"] .mode-run {
            display: none;
        }

        .panel-heading {
            display: flex;
            justify-content: space-between;
            gap: 16px;
            align-items: flex-start;
            margin-bottom: 14px;
        }

        .panel-heading h2,
        .status-panel h2,
        .recent-panel h2 {
            font-size: 18px;
            line-height: 1.25;
        }

        .compact-heading {
            align-items: center;
            margin-bottom: 0;
        }

        .setup-summary-grid {
            display: grid;
            grid-template-columns: 1fr;
            margin: 12px 0;
        }

        .setup-summary-item {
            display: grid;
            gap: 5px;
            padding: 10px 0;
            border-bottom: 1px solid color-mix(in srgb, var(--sqloom-border) 35%, transparent);
        }

        .setup-summary-item:last-child {
            border-bottom: 0;
        }

        .summary-label-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 8px;
            color: var(--vscode-editor-foreground);
            font-size: 12px;
            font-weight: 600;
        }

        .summary-value {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            min-width: 0;
        }

        .summary-detail {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            min-width: 0;
            font-size: 12px;
        }

        .detected-line {
            display: flex;
            gap: 10px;
            align-items: center;
            min-height: 32px;
            margin-top: 12px;
            color: var(--sqloom-muted);
            overflow-wrap: anywhere;
        }

        .detected-line .status-dot {
            flex: 0 0 auto;
        }

        .endpoint-summary {
            display: flex;
            align-items: center;
            gap: 10px;
            min-height: 34px;
            margin-top: 8px;
            border-top: 1px solid var(--sqloom-border);
            padding-top: 8px;
            overflow-wrap: anywhere;
        }

        .endpoint-summary .status-dot {
            flex: 0 0 auto;
        }

        .endpoint-summary-detail {
            margin-left: auto;
            color: var(--sqloom-muted);
            font-size: 12px;
            text-align: right;
        }

        .edit-grid {
            display: grid;
            grid-template-columns: 1fr;
            gap: 16px;
        }

        .field {
            display: grid;
            gap: 6px;
            min-width: 0;
        }

        .field-label {
            display: flex;
            align-items: center;
            gap: 7px;
            color: var(--vscode-editor-foreground);
            font-size: 12px;
            font-weight: 600;
        }

        .field-label .status-dot {
            width: 14px;
            height: 14px;
            font-size: 9px;
        }

        .control {
            min-height: 32px;
            width: 100%;
            border: 1px solid var(--vscode-input-border, var(--sqloom-border));
            border-radius: 5px;
            background: var(--sqloom-control);
            color: var(--sqloom-control-foreground);
            outline: none;
        }

        .control-input,
        .control-select {
            padding: 0 10px;
        }

        .control-input:focus,
        .control-select:focus,
        .mask-toggle:focus,
        .secondary-action:focus,
        .field-action:focus,
        .icon-button:focus,
        .primary-action:focus {
            border-color: var(--sqloom-focus);
            outline: 1px solid var(--sqloom-focus);
            outline-offset: 1px;
        }

        .control-input[readonly] {
            color: var(--sqloom-muted);
        }

        .compound-control {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            align-items: center;
        }

        .compound-control .control-input {
            border-radius: 5px 0 0 5px;
        }

        .compound-control .control-select {
            border-radius: 5px 0 0 5px;
        }

        .control-select:disabled {
            cursor: not-allowed;
            opacity: 0.7;
        }

        .inline-meta {
            display: inline-flex;
            align-items: center;
            min-height: 32px;
            max-width: 170px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            border-top: 1px solid var(--vscode-input-border, var(--sqloom-border));
            border-bottom: 1px solid var(--vscode-input-border, var(--sqloom-border));
            background: var(--sqloom-control);
            padding: 0 10px;
            font-size: 12px;
        }

        .compound-control .inline-meta:last-child {
            border-right: 1px solid var(--vscode-input-border, var(--sqloom-border));
            border-radius: 0 5px 5px 0;
        }

        .password-control {
            position: relative;
            display: block;
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
            border-left-color: var(--vscode-input-border, var(--sqloom-border));
            border-radius: 0 5px 5px 0;
            background: transparent;
            color: var(--sqloom-muted);
            cursor: pointer;
        }

        .mask-toggle .button-icon {
            width: 15px;
            height: 15px;
            flex-basis: 15px;
        }

        .mask-toggle:hover {
            color: var(--vscode-input-foreground);
            background: var(--vscode-toolbar-hoverBackground, transparent);
        }

        .field-note {
            min-height: 16px;
            overflow-wrap: anywhere;
            font-size: 12px;
        }

        .edit-actions {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin-top: 12px;
        }

        .artifact-heading {
            align-items: center;
        }

        .artifact-tools {
            display: flex;
            align-items: center;
            gap: 12px;
            white-space: nowrap;
        }

        .icon-button {
            display: inline-grid;
            place-items: center;
            width: 32px;
            height: 32px;
            padding: 0;
            color: var(--sqloom-muted);
        }

        .icon-button .button-icon {
            width: 16px;
            height: 16px;
            flex-basis: 16px;
        }

        .artifact-table-wrap {
            overflow-x: auto;
        }

        .artifact-table {
            width: 100%;
            border-collapse: collapse;
        }

        .artifact-table th,
        .artifact-table td {
            border-top: 1px solid var(--sqloom-border);
            padding: 10px 12px;
            text-align: left;
            vertical-align: middle;
        }

        .artifact-table th {
            color: var(--sqloom-muted);
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
        }

        .artifact-table tbody tr {
            background: transparent;
        }

        .artifact-table tbody tr:hover {
            background: var(--sqloom-row);
        }

        .artifact-name {
            display: grid;
            grid-template-columns: 34px minmax(0, 1fr);
            gap: 12px;
            align-items: center;
            min-width: 0;
        }

        .artifact-name strong,
        .artifact-name small {
            display: block;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            min-width: 0;
        }

        .artifact-icon {
            display: grid;
            place-items: center;
            width: 34px;
            height: 34px;
            border-radius: 5px;
        }

        .artifact-type-icon {
            display: block;
            width: 18px;
            height: 18px;
            fill: none;
            stroke: currentColor;
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        .tone-markdown {
            color: #c084fc;
            background: rgba(126, 58, 242, 0.18);
        }

        .tone-json {
            color: #3794ff;
            background: rgba(0, 122, 204, 0.16);
        }

        .tone-sql {
            color: #28c7d8;
            background: rgba(8, 145, 178, 0.16);
        }

        .tone-html {
            color: var(--sqloom-muted);
            background: color-mix(in srgb, var(--vscode-editor-foreground) 10%, transparent);
        }

        .type-badge {
            display: inline-flex;
            align-items: center;
            min-height: 22px;
            border-radius: 4px;
            padding: 2px 8px;
            font-size: 12px;
            font-weight: 600;
        }

        .row-actions {
            display: flex;
            justify-content: flex-end;
            gap: 8px;
        }

        .artifact-footer {
            display: flex;
            justify-content: space-between;
            gap: 16px;
            border-top: 1px solid var(--sqloom-border);
            padding-top: 12px;
            margin-top: 4px;
            overflow-wrap: anywhere;
        }

        .status-panel {
            min-height: 248px;
        }

        .status-panel p {
            color: var(--sqloom-muted);
        }

        .status-list {
            display: grid;
            gap: 18px;
            margin-top: 22px;
        }

        .status-check {
            display: grid;
            grid-template-columns: 22px minmax(0, 1fr);
            gap: 12px;
            align-items: center;
        }

        .status-check strong,
        .status-check [data-check-detail] {
            display: block;
            overflow-wrap: anywhere;
        }

        .status-check strong {
            margin-bottom: 3px;
            font-weight: 600;
        }

        .status-footer {
            text-align: center;
            font-size: 12px;
        }

        .recent-panel {
            min-height: 318px;
            display: grid;
            grid-template-rows: auto minmax(0, 1fr);
        }

        .empty-state {
            display: grid;
            place-items: center;
            align-content: center;
            gap: 8px;
            min-height: 230px;
            text-align: center;
        }

        .empty-state strong {
            font-size: 16px;
        }

        .compact-empty-state {
            min-height: 180px;
            margin-top: 8px;
        }

        .text-button {
            width: auto;
            min-width: 58px;
            padding: 0 8px;
            font-size: 11px;
            font-weight: 600;
        }

        .clock-icon {
            position: relative;
            width: 42px;
            height: 42px;
            border: 1px solid var(--sqloom-border);
            border-radius: 999px;
        }

        .clock-icon::before,
        .clock-icon::after {
            content: "";
            position: absolute;
            left: 20px;
            top: 10px;
            width: 1px;
            height: 12px;
            background: var(--sqloom-border);
            transform-origin: bottom;
        }

        .clock-icon::after {
            top: 20px;
            height: 10px;
            transform: rotate(120deg);
        }

        .recent-runs-body {
            min-height: 230px;
        }

        .recent-run-list {
            display: grid;
            gap: 10px;
            padding-top: 14px;
        }

        .recent-run-item {
            display: grid;
            gap: 6px;
            width: 100%;
            padding: 12px 14px;
            border: 1px solid var(--sqloom-border);
            border-radius: 6px;
            background: var(--sqloom-card-soft);
            color: inherit;
            text-align: left;
            cursor: pointer;
        }

        .recent-run-item:hover {
            border-color: var(--vscode-focusBorder);
            background: var(--vscode-list-hoverBackground);
        }

        .recent-run-item.selected {
            border-color: var(--vscode-focusBorder);
            background: color-mix(in srgb, var(--vscode-focusBorder) 12%, var(--sqloom-card-soft));
            box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--vscode-focusBorder) 35%, transparent);
        }

        .recent-run-header {
            display: flex;
            align-items: flex-start;
            justify-content: space-between;
            gap: 10px;
        }

        .recent-run-header strong {
            overflow-wrap: anywhere;
            font-weight: 600;
        }

        .recent-run-badge {
            flex: 0 0 auto;
            padding: 2px 8px;
            border-radius: 999px;
            font-size: 11px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.02em;
        }

        .recent-run-badge.status-completed {
            color: var(--vscode-testing-iconPassed);
            background: color-mix(in srgb, var(--vscode-testing-iconPassed) 16%, transparent);
        }

        .recent-run-badge.status-failed {
            color: var(--vscode-testing-iconFailed);
            background: color-mix(in srgb, var(--vscode-testing-iconFailed) 16%, transparent);
        }

        .recent-run-meta {
            color: var(--sqloom-muted);
            font-size: 12px;
            overflow-wrap: anywhere;
        }

        .status-dot {
            position: relative;
            display: grid;
            place-items: center;
            width: 18px;
            height: 18px;
            border: 1px solid currentColor;
            border-radius: 999px;
            flex: 0 0 auto;
        }

        .status-dot::before,
        .status-dot::after {
            content: "";
            position: absolute;
            display: block;
        }

        .status-ready.status-dot::before {
            left: 50%;
            top: 50%;
            width: 45%;
            height: 24%;
            border-left: 2px solid currentColor;
            border-bottom: 2px solid currentColor;
            transform: translate(-50%, -58%) rotate(-45deg);
        }

        .status-warning.status-dot::before {
            top: 22%;
            left: calc(50% - 1px);
            width: 2px;
            height: 44%;
            border-radius: 999px;
            background: currentColor;
        }

        .status-warning.status-dot::after {
            bottom: 16%;
            left: calc(50% - 1px);
            width: 2px;
            height: 2px;
            border-radius: 999px;
            background: currentColor;
        }

        .status-neutral.status-dot::before {
            width: 4px;
            height: 4px;
            border-radius: 999px;
            background: currentColor;
            opacity: 0.75;
        }

        .status-idle.status-dot::before {
            width: 6px;
            height: 6px;
            border: 1px solid currentColor;
            border-radius: 999px;
        }

        .status-ready {
            color: var(--sqloom-ready);
        }

        .status-warning {
            color: var(--sqloom-warning);
        }

        .status-neutral,
        .status-idle {
            color: var(--sqloom-muted);
        }

        @media (max-width: 1050px) {
            .dashboard-grid {
                grid-template-columns: 1fr;
            }

            .topbar {
                flex-direction: column;
                align-items: flex-start;
                gap: 12px;
            }

            .stepper {
                grid-template-columns: repeat(4, 116px);
                overflow-x: auto;
                padding-left: 4px;
                padding-right: 4px;
            }
        }

        @media (max-width: 720px) {
            .dashboard {
                padding: 14px;
            }

            .brand {
                align-items: flex-start;
            }

            .brand img {
                width: 38px;
                height: 38px;
            }

            .brand h1 {
                font-size: 20px;
            }

            .panel-heading,
            .artifact-footer,
            .edit-actions {
                flex-direction: column;
                align-items: stretch;
            }

            .setup-summary-grid,
            .edit-grid {
                grid-template-columns: 1fr;
            }

            .setup-summary-item,
            .setup-summary-item:nth-child(3n) {
                border-right: 0;
            }

            .setup-summary-item:nth-child(n + 2) {
                border-top: 1px solid var(--sqloom-border);
            }

            .compound-control {
                grid-template-columns: minmax(0, 1fr);
            }

            .compound-control .control-input,
            .compound-control .inline-meta,
            .field-action {
                border-radius: 5px;
            }

            .inline-meta {
                max-width: none;
                border: 1px solid var(--vscode-input-border, var(--sqloom-border));
                border-top: 0;
            }

            .field-action {
                margin-top: 6px;
            }
        }
`;
