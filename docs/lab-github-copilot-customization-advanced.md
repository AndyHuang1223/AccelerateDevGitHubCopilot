# 進階 Lab：從 Team SOP 到可治理的外部能力

## 定位與前置條件

本 Lab 接續 [lab-github-copilot-customization.md](lab-github-copilot-customization.md)。學員應已完成核心 Lab 的 AGENTS.md、三個 ask-only Prompt Files、`dotnet-feature-development` Skill，以及 Planner → Implementer → Reviewer handoff。

本 Lab 約需 2.5–3 小時，採「先觀察、再建立、最後驗證」的節奏。它會建立可重複的 quality workflows、唯讀 Internal Docs MCP server，並讓 Planner 針對 `Library.Api` 產生架構計畫；本 Lab **不新增 `Library.Api` project、不新增 endpoint，也不修改既有 Library production code**。

官方格式以目前 VS Code 文件為準：

- [Agent Skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)
- [Prompt Files](https://code.visualstudio.com/docs/agent-customization/prompt-files)
- [Custom Agents and handoffs](https://code.visualstudio.com/docs/agent-customization/custom-agents)
- [MCP servers in VS Code](https://code.visualstudio.com/docs/agent-customization/mcp-servers)
- [Microsoft Learn MCP](https://learn.microsoft.com/en-us/training/support/mcp)

## 進階主線

~~~text
Conventional Commit
       ↓
PR Readiness
       ↓
Code Review
       ↓
Internal Docs MCP
       ↓
Planner + Repository + Microsoft Learn + Company Docs
       ↓
Library.Api architecture plan
~~~

Skill 解決「這類工作應該怎麼做」；MCP 解決「Agent 還能存取哪些外部資料或工具」。三個 Skill 都是可重複流程，但只有 `pr-readiness` 可以執行驗證命令；它也只能輸出 READY／NOT READY，不得自動 commit、push、merge 或部署。

## Part A：三階 Agent Skills

### A1. Conventional Commit

建立 Workspace skill：

~~~text
.github/skills/conventional-commit/
├── SKILL.md
├── references/commit-conventions.md
├── examples/commit-examples.md
└── scripts/
    ├── collect-staged-changes.sh
    └── collect-staged-changes.ps1
~~~

`SKILL.md` frontmatter 至少包含：

~~~yaml
---
name: conventional-commit
description: Suggest Conventional Commit messages from staged changes without committing.
argument-hint: "[optional focus]"
user-invocable: true
disable-model-invocation: true
---
~~~

流程必須是：讀取 staged status 與 diff → 判斷 type／scope → 偵測 mixed concerns → 套用 `references/commit-conventions.md` → 輸出一個或多個建議。Script 只收集客觀 facts；Skill 不可執行 `git commit`，也不可修改 staged files。

POSIX script：

~~~bash
#!/usr/bin/env bash
set -euo pipefail
git status --short
git diff --cached --name-status
git diff --cached
~~~

PowerShell script 必須提供等價輸出：

~~~powershell
$ErrorActionPreference = "Stop"
git status --short
git diff --cached --name-status
git diff --cached
~~~

驗收：在沒有 staged changes、單一 feature、以及 feature + tests + docs 三種狀態下，Skill 都只產生建議並明確標記資料不足，不建立 commit。

### A2. PR Readiness

建立：

~~~text
.github/skills/pr-readiness/
├── SKILL.md
├── references/definition-of-done.md
├── references/pr-checklist.md
└── scripts/
    ├── verify.sh
    └── verify.ps1
~~~

Skill 要依序收集變更、執行 build、test、diff-check，再讀取兩個 references，檢查：

- 新行為是否有測試。
- 是否有 regression、boundary 與 no-side-effect 覆蓋。
- 是否修改 public API、NuGet、seed data、configuration 或 migration。
- 是否有文件與 breaking-change 說明。

輸出固定包含 `Risk`、每項 evidence、`READY` 或 `NOT READY`、blocking findings 與仍然 `UNVERIFIED` 的項目。只要命令失敗或必要證據缺少，就不能輸出 READY。

### A3. Code Review

建立：

~~~text
.github/skills/code-review/
├── SKILL.md
├── references/review-checklist.md
├── references/architecture-rules.md
├── references/security-checklist.md
└── scripts/
    ├── collect-review-context.sh
    └── collect-review-context.ps1
~~~

流程為 collect diff → understand intent → risk classification → correctness → architecture → security → testing → maintainability → performance → breaking changes。每個 finding 必須包含 severity、file／line evidence、rule、recommendation 與 blocking status。沒有實際命令輸出時標記 `UNVERIFIED`；Skill 不得自行修改程式或假造測試結果。

三個 Skill 都要使用相對連結，例如 `./references/pr-checklist.md`，且 `name` 必須與父資料夾一致。`disable-model-invocation: true` 可讓課堂以 slash command 明確驗收，避免和核心 Skill 意外同時載入。

## Part B：建立 Internal Docs MCP

### B1. 教學資料

新增下列只讀公司文件：

~~~text
training/internal-docs/
├── architecture.md
├── api-guidelines.md
└── incident-runbook.md
~~~

文件內容不可包含 secrets。至少要描述：ApplicationCore／Infrastructure／Console 責任、`/api/{resource}` 命名、ProblemDetails、`X-Correlation-Id`、public API rate limiting、controller 不直接存取 repository，以及 incident escalation 流程。

### B2. Server contract

`tools/InternalDocsMcp` 是 .NET 8 console project，使用官方 `ModelContextProtocol` 2.0.0 package 與 stdio transport。它只允許讀取啟動參數指定的 docs root，並拒絕：

- 空白或過長 query。
- 非 `.md` 檔案。
- 不在 allow-list 內的檔案。
- `..`、絕對路徑或 symlink escape。

Expose exactly three read-only tools：

| Tool | Input | Output |
|---|---|---|
| `list_documents` | none | sorted Markdown names |
| `search_docs` | `query` | bounded matches with document and line number |
| `get_document` | `name` | complete Markdown content |

搜尋大小寫不敏感，結果上限固定為 20 筆；找不到內容回傳空結果，不將 exception 或本機絕對路徑洩漏給 client。Tool 不得建立、刪除或寫入文件。

### B3. VS Code configuration

`.vscode/mcp.json` 同時註冊 Microsoft Learn 與本機 server：

~~~json
{
  "servers": {
    "microsoftLearn": {
      "type": "http",
      "url": "https://learn.microsoft.com/api/mcp"
    },
    "internalDocs": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/tools/InternalDocsMcp/InternalDocsMcp.csproj",
        "--",
        "${workspaceFolder}/training/internal-docs"
      ]
    }
  }
}
~~~

執行 MCP: List Servers，確認 trust、server output 與 Configure Tools。先呼叫 `list_documents`，再測試一個 `search_docs` 與 `get_document`。若 stdio server 啟動失敗，保留完整 output，並將 MCP 部分標成 `UNVERIFIED`；不可把模型記憶當作 tool call 證據。

## Part C：API Planning Capstone

新增 `.github/prompts/create-api.prompt.md`，使用 `agent: Library Planner`，並要求：

~~~text
目前 repository 是 .NET 8 Library Console application，沒有 Web API。
請只產生 Library.Api 的架構與實作計畫，不建立 project、不修改 source/test。

先讀取 repository、AGENTS.md 與 Internal Docs MCP；再使用 Microsoft Learn MCP 查證
目前 ASP.NET Core Controllers、ProblemDetails、OpenAPI、health checks 與 rate limiting
的官方建議。輸出每個來源的標題與連結，並區分 repository fact、company standard、
official guidance 與 inference。

計畫必須涵蓋：
- Library.Api 作為 transport layer，重用 ApplicationCore／Infrastructure，Controller 不直接做商業判斷。
- GET /health、GET /api/book-items/{id}、POST /api/loans。
- DTO、DI、ProblemDetails、OpenAPI、health checks、rate limiting 與 tests。
- 預計新增／修改檔案、依賴方向、風險、未決事項與驗收條件。
~~~

以 `Library Planner` 執行後，人工檢查它沒有建立 `src/Library.Api`，且計畫同時引用 repository 架構、Internal Docs 與 Microsoft Learn 證據。這個 Capstone 的目的，是展示「Repository Context + Company Context + Current Official Guidance → Decision-ready Plan」，不是把課堂變成 Web API implementation lab。

## 進階驗收

~~~bash
dotnet build
dotnet test
git diff --check
dotnet run --project tools/InternalDocsMcp/InternalDocsMcp.csproj -- training/internal-docs
~~~

另以 `rg` 檢查：

~~~bash
rg -n 'name: (conventional-commit|pr-readiness|code-review)|disable-model-invocation|\.\/references|\.\/scripts' .github/skills
rg -n 'list_documents|search_docs|get_document|microsoftLearn|internalDocs' .vscode tools docs
~~~

完成條件：三個 Skill 均可被發現且不會自動 commit；MCP tools 只讀取 allow-listed Markdown；Planner 只產生 API plan；沒有 `Library.Api` project、endpoint、secret 或 production code 變更。

