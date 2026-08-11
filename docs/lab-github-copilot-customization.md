# Lab：GitHub Copilot 客製化 AI 驅動開發

## 使用 VS Code Agent Customizations 延伸既有 .NET 專案

本 Lab 接續 Library Management Repository 的 Create Loan 完成版。你會用同一個 repository，逐步建立可分享的規則、流程、角色、模型、外部工具與安全護欄，最後完成「逾期且未歸還則禁止新增借閱」功能。

本文件是學生操作手冊。每個 Part 都包含前置條件、操作步驟、Prompt／檔案範本、預期結果與檢查點。模型輸出可以不同，但必須以責任邊界、工具權限與實際驗證證據為準。

官方總覽：[Create and manage agent customizations](https://code.visualstudio.com/docs/agent-customization/overview)

## Lab 目標

完成後，你將能夠：

1. 使用 Chat: Open Customizations 管理 User／Workspace customization。
2. 用 /init 建立通用 AGENTS.md，讓 Agent 回答與實作遵循 repository 慣例。
3. 用 Prompt File、Agent Skill 與 Custom Agent 封裝可重複任務。
4. 根據任務選擇 Language Model、thinking effort 與 tool-calling 能力。
5. 使用 Microsoft Learn MCP 取得 repository 之外的官方證據。
6. 使用安全的 Hook 觀察 agent lifecycle，理解 deterministic guardrail。
7. 讀懂 Plugin manifest，理解如何打包並分享 Skills、Agents、Hooks 與 MCP。
8. 用相同需求比較沒有 customization 與完整 customization 的差異。

## 課堂主線

~~~text
Create and Manage  管理位置、scope、發現與診斷
        ↓
AGENTS.md          定義所有工作都適用的通則
        ↓
Prompt File        將明確、手動的任務變成 slash command
        ↓
Agent Skill        封裝可重複的多步驟開發流程
        ↓
Custom Agent       限定角色、工具與輸出格式
        ↓
Language Model     依任務選擇模型與 thinking effort
        ↓
MCP                取得 Repository 之外的工具與知識
        ↓
Hooks              在 agent lifecycle 執行 deterministic guardrail
        ↓
Plugin             將多種 customization 打包、安裝與分享
        ↓
整合實作與 Review
~~~

Instructions 會自動套用；Prompt Files 由使用者明確呼叫；Skills 依相關性自動載入或用 slash command 明確載入；Custom Agent 定義角色與工具；Language Model 決定推理能力；MCP 提供外部能力；Hooks 執行可預期的命令；Plugin 是發佈與安裝的包裝層。參考 [VS Code customization concepts](https://code.visualstudio.com/docs/agents/concepts/customization)。

本 Lab 的主線 instruction 是根目錄 AGENTS.md；.github/copilot-instructions.md 只在比較段落中提到，不需要建立兩份 Always-on instruction。Prompt File、Skill、Agent、Hook 與 MCP 在學員操作後才建立；starter 本身不預先放入功能答案。

## 前置需求

- Git
- .NET SDK 8
- Visual Studio Code 與 GitHub Copilot Chat
- 支援 Agent mode、Agent Customizations、Skills、Custom Agents 與 MCP 的近期 VS Code
- 可連線至 learn.microsoft.com 的網路

在終端機確認版本：

~~~bash
git --version
dotnet --version
code --version
~~~

Hooks 與 Plugins 目前是 Preview；模型清單、MCP policy、Hooks policy 與 Plugins policy 可能依帳號、組織和 VS Code 版本不同。環境限制必須保留錯誤證據，不得假裝成功。

---

## Part 0：Create and Manage 與取得 starter（0:00–0:10）

### 前置條件

- 已取得 repository clone URL。
- 已安裝 VS Code 與 GitHub Copilot。
- 目前沒有未保存的課堂修改。

### 操作步驟

第一次使用：

~~~bash
git clone <repository-url>
cd AccelerateDevGitHubCopilot
code .
~~~

已有 checkout 時：

~~~bash
git rev-parse --show-toplevel
git remote -v
git fetch origin lab/copilot-customization-starter
~~~

從 starter 建立學員工作分支：

~~~bash
git switch -c lab/copilot-customization origin/lab/copilot-customization-starter
git branch --show-current
git status --short
~~~

在 VS Code：

1. 確認開啟 repository root，不是 src 或 tests 子目錄。
2. 開啟 Chat，選取 GitHub Copilot Agent harness。
3. 執行 Chat: Open Customizations。
4. 查看 Instructions、Skills、Agents、Prompts、Hooks 與 Plugins 頁籤。
5. 在 New 選單比較 User 與 Workspace scope；本 Lab 的檔案都選 Workspace。
6. 執行 Developer: Open Agent Debug Panel，確認能看到目前 workspace 的 discovery log。

### 預期結果與檢查點

- [ ] 目前分支是 lab/copilot-customization。
- [ ] git status --short 沒有輸出。
- [ ] Customizations editor 能開啟，且已確認 Agent harness 與 Workspace scope。
- [ ] 尚未建立 AGENTS.md、.github/prompts、.github/skills、.github/agents、.github/hooks 或 .vscode/mcp.json。

---

## Part 1：Baseline（0:10–0:20）

### 前置條件

Part 0 工作樹乾淨，且尚未建立任何 customization。

### 操作步驟

從 repository root 執行：

~~~bash
dotnet restore
dotnet build
dotnet test
git diff --check
~~~

預期結果：restore 成功、build 0 errors、既有測試 37 passed、diff check 沒有輸出。

再確認 Console baseline：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

依序輸入：

1. 搜尋 Patron Six。
2. 搜尋結果只有 Patron Six 時輸入 1。
3. 在讀者詳細資料輸入 b。
4. 在 Available Book Items 選擇顯示 [14]、[19] 或 [20] 的列；輸入清單列號，不是 ID。
5. 確認成功訊息與重新載入的借閱清單。
6. 輸入 q 結束。
7. 再執行一次 --reset-data，避免 baseline 借閱影響後續實驗。

### 預期結果與檢查點

- [ ] baseline 為 37 tests passed、build 0 errors。
- [ ] Console 可以建立正常借閱。
- [ ] reset 後 runtime data 回到 seed 狀態。

若 baseline 不符合預期，先記錄完整 command output，不要進入 Agent 實驗。

---

## Part 2：Before 實驗——沒有 customization（0:20–0:30）

### 前置條件

確認以下路徑尚不存在：

- AGENTS.md
- .github/prompts/overdue-loan-review.prompt.md
- .github/skills/dotnet-feature-development/SKILL.md
- .github/agents/library-code-reviewer.agent.md
- .github/hooks/library-lab-session.json
- .vscode/mcp.json

### 操作步驟與固定 Prompt

開啟新的 Chat，選一般 Agent，貼上以下 Prompt。Part 10 會使用完全相同文字：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

Agent 修改前，記錄是否先探索、列 plan、辨識測試與檢查副作用：

| 觀察面向 | Before 結果 |
|---|---|
| 商業規則放在哪一層 | |
| 是否修改 Console 判斷規則 | |
| 是否新增套件、抽象或 public interface | |
| 是否有 active overdue、returned overdue、current active 測試 | |
| 是否驗證拒絕時沒有館藏查詢或 AddLoan | |
| 是否真的執行 build、test、diff check | |
| 哪些宣稱有命令輸出佐證 | |

### 復原步驟

優先使用 Chat Undo 或 Restore checkpoint。若有未追蹤檔案：

~~~bash
git status --short
git diff --stat
git clean -n -- <confirmed-experiment-path>
~~~

只移除已確認由本次實驗產生的檔案，不要對整個 repository 執行無範圍的 clean。復原後再次確認 git status --short 與 git diff --stat 沒有輸出。

### 預期結果與檢查點

- [ ] Before observation table 完成。
- [ ] Chat Undo／Restore checkpoint 完成。
- [ ] 工作樹乾淨，沒有保留功能答案。

---

## Part 3：Instructions——用 /init 建立 AGENTS.md（0:30–0:45）

### 前置條件

- Part 2 已復原且工作樹乾淨。
- VS Code 開啟 repository root。
- 使用 GitHub Copilot Agent harness。

### 操作步驟

在 Chat 輸入：

~~~text
/init
~~~

讓 Copilot 探索 repository 並產生 workspace guidance。若目前版本將檔案命名或儲存到其他支援位置，請保留有用內容並整理成 repository root 的 AGENTS.md；本 Lab 的 canonical 檔案就是 AGENTS.md。

將內容整理成以下結構：

~~~markdown
# Library Management Repository Guidelines

## How to answer questions

- Read relevant source files, tests, and docs before answering; never guess the repository structure.
- Cite actual file paths, types, methods, tests, or observed command output as evidence.
- Separate observed facts, reasonable inferences, and unverified information.
- If the user asks only for explanation or analysis, do not modify files.
- For a plan, list acceptance criteria, affected areas, expected files, risks, and unresolved decisions first.
- Never invent commands, tests, tool calls, or documentation sources.

## Architecture and development

- Business rules belong in Library.ApplicationCore.
- Library.Infrastructure handles JSON persistence and repositories.
- Library.Console handles input, output, and flow coordination only.
- Follow existing entities, services, interfaces, enums, repositories, and factories.
- Prefer the smallest focused change; do not add packages or change public interfaces without a concrete requirement.
- Do not change seed data or unrelated behavior.

## Testing and verification

- Follow existing xUnit and NSubstitute conventions.
- Cover success, rejection, boundary, and no-side-effect paths.
- Before reporting completion, run and observe dotnet build, dotnet test, and git diff --check.
- Never claim a command succeeded unless its output was actually observed.
~~~

### 驗證與觀察

1. 執行 Chat: Open Customizations，確認 Instructions 頁籤能看到 Workspace AGENTS.md。
2. 執行 Chat: Configure Instructions，確認檔案可被發現。
3. 執行 Chat Diagnostics 或 Developer: Open Agent Debug Panel，確認沒有 discovery error。
4. 貼上通用驗證 Prompt：

~~~text
請分析目前 LoanService 的責任、相關測試與資料流。
請列出你實際讀取的檔案，區分觀察事實與推論。
只分析，不要修改任何檔案。
~~~

5. 檢查回答是否引用實際檔案並區分事實／推論，且 git status --short 沒有程式修改。

.github/copilot-instructions.md 是另一種 workspace-wide instruction 位置；本 Lab 不同時建立兩份，以免學員無法判斷規則來源。References 的呈現依 Chat UI 版本而異，不是唯一驗收證據。

### 預期結果與檢查點

- [ ] 根目錄存在 AGENTS.md。
- [ ] Configure Instructions 與 Diagnostics 能發現且沒有錯誤。
- [ ] 通用 Prompt 只分析，不修改檔案。
- [ ] 回答包含檔案證據、事實／推論區分與未確認資訊標記。

---

## Part 4：Prompt File——把固定分析任務變成 slash command（0:45–0:55）

### 前置條件

AGENTS.md 已可被發現，且尚未開始逾期功能實作。

### 操作步驟

建立 Workspace Prompt File：

1. 執行 Chat: Open Customizations。
2. 開啟 Prompts 頁籤。
3. 選擇 New Prompt → Workspace。
4. 或執行 Chat: New Prompt File／create-prompt。
5. 儲存為 .github/prompts/overdue-loan-review.prompt.md。

內容：

~~~markdown
---
name: overdue-loan-review
description: Analyze the overdue borrowing requirement without editing files
argument-hint: "[optional context]"
agent: ask
---

Analyze the request to prevent a patron with an overdue, unreturned loan from creating a new loan.

Read the relevant LoanService, entities, repositories, enum, Console flow, tests, and documentation.
Return:

1. Acceptance criteria.
2. Observed facts with file and symbol evidence.
3. Reasonable inferences.
4. Unverified information.
5. A minimal file-level implementation plan.
6. Tests for success, rejection, boundary, and no side effects.

Do not edit, create, delete, format, or execute commands. Do not claim tests passed.
~~~

### 驗證與觀察

1. 在 Chat 輸入 /overdue-loan-review。
2. 觀察它是否使用 ask，只回傳分析與計畫。
3. 執行 Chat: Run Prompt，選同一個 Prompt File。
4. 在 Prompt File editor 按 play，選目前 Chat 或新 Chat 執行。
5. 確認它是手動呼叫，不會因一般問題自動執行。

Prompt Files 是可手動呼叫的 Markdown slash command；它和自動套用的 Instructions、可相關性載入的 Skill 不同。Agent Host 不使用傳統 Prompt Files 時，應改用 Skill；本 Lab 使用 VS Code local Chat 驗證。官方格式：[Prompt Files](https://code.visualstudio.com/docs/agent-customization/prompt-files)。

### 預期結果與檢查點

- [ ] Prompt File 出現在 Workspace Prompts 清單。
- [ ] /overdue-loan-review 能明確執行。
- [ ] 回答只有分析、證據與 plan，沒有修改檔案。
- [ ] Prompt File 不包含 API key、秘密或功能答案。

---

## Part 5：Agent Skill——封裝 .NET 功能開發流程（0:55–1:15）

### 前置條件

AGENTS.md 與 Prompt File 已完成，尚未開始逾期功能實作。

### 設定與建立

確認 VS Code settings：

~~~json
{
  "chat.useAgentSkills": true,
  "chat.agentSkillsLocations": {
    ".github/skills": true
  }
}
~~~

使用 Chat: Open Customizations → Skills → New Skill → Workspace，或 /create-skill，建立：

~~~text
.github/skills/dotnet-feature-development/
├── SKILL.md
├── architecture.md
└── testing.md
~~~

SKILL.md frontmatter：

~~~yaml
---
name: dotnet-feature-development
description: >
  Implement or modify features in this C# .NET library management
  application across ApplicationCore, Infrastructure, Console,
  repositories, services, and tests while following repository conventions.
argument-hint: "[feature or change to implement]"
user-invocable: true
disable-model-invocation: false
---
~~~

核心流程必須包含：

~~~markdown
# .NET Feature Development

1. Explore source, tests, docs, and existing patterns.
2. State acceptance criteria and unresolved decisions.
3. Create a minimal file-level plan before editing.
4. Put business rules in ApplicationCore, persistence in Infrastructure, and interaction in Console.
5. Reuse existing services, repositories, enums, entities, and factories.
6. Add success, rejection, boundary, and no-side-effect tests.
7. Read [architecture guidance](./architecture.md) and [testing guidance](./testing.md).
8. Run and observe dotnet build, dotnet test, and git diff --check.
9. Stop when requirements conflict; do not invent a design or claim unobserved results.
~~~

architecture.md 必須描述：

- Library.ApplicationCore：entities、enums、contracts、business rules。
- Library.Infrastructure：JSON loading、repository implementation、persistence。
- Library.Console：input/output、flow coordination、dependency injection。
- UnitTests：xUnit、NSubstitute、Factory 與 no-side-effect tests。
- Console → Infrastructure → ApplicationCore 的依賴方向。

testing.md 必須描述：

- xUnit 2.5.3、NSubstitute 5.1.0。
- Arrange／Act／Assert 與既有命名風格。
- PatronFactory、LoanFactory、Received(1)、DidNotReceive()。
- success、rejection、boundary、persistence failure、no-side-effect 測試。
- dotnet build、dotnet test、git diff --check。

### Trigger 實驗

執行 /skills 開啟 Configure Skills；這個命令本身不代表 Skill 已在目前 request 執行。確認 dotnet-feature-development 出現在 Workspace scope。

依序執行三個實驗：

**A：一般 Repository 問題（只分析）**

~~~text
請解釋 Loan entity 與 LoanService 的責任邊界。
只閱讀與分析，不要修改任何檔案，也不要執行完整 feature workflow。
~~~

**B：功能需求的自動載入觀察（只分析）**

~~~text
請分析「在建立借閱時加入逾期且未歸還的禁止規則」需要修改哪些層與測試。
只提出 acceptance criteria 與檔案計畫，不要修改檔案。
~~~

**C：明確觸發**

在 Chat 輸入 /dotnet-feature-development，再貼上 B 的需求。這是穩定驗收方式。

自動語意載入只作觀察，不列為必然通過條件。記錄三次實驗是否載入 Skill、是否引用兩個 resource、是否列 plan、是否修改檔案。

官方說明：[Agent Skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)。

### 預期結果與檢查點

- [ ] Skill 出現在 Workspace Skills 清單。
- [ ] chat.useAgentSkills 與 .github/skills location 已設定。
- [ ] SKILL.md 的 name 與資料夾相同。
- [ ] 內容包含 ./architecture.md 與 ./testing.md。
- [ ] A、B 沒有修改程式；C 能明確載入完整 workflow。
- [ ] Diagnostics 沒有 Skill discovery error。

---

## Part 6：Custom Agent——建立唯讀 Library Code Reviewer（1:25–1:40）

### 前置條件

Instructions、Prompt File、Skill 已被發現；MCP 可以稍後加入，Reviewer 必須能在 MCP 不可用時標記未驗證。

### 建立步驟

使用 Chat: Open Customizations → Agents → New Agent → Workspace，或 /create-agent，建立：

~~~text
.github/agents/library-code-reviewer.agent.md
~~~

固定 frontmatter：

~~~yaml
---
name: Library Code Reviewer
description: >
  Read-only review of the .NET library management application for
  requirements, architecture, tests, side effects, and verification evidence.
tools:
  - read
  - search
  - microsoftLearn/*
---
~~~

Reviewer body 必須要求：

- 檢查需求與 acceptance criteria。
- 檢查 ApplicationCore／Infrastructure／Console 分層。
- 檢查 success、rejection、boundary、no-side-effect 測試。
- 檢查 unrelated files、public API、NuGet、seed data 與部分更新。
- 只能讀取與搜尋，不得 edit、create、delete、format 或 execute。
- 沒有實際 build/test/diff 輸出時必須標記 UNVERIFIED。
- 使用 MCP 時列出官方標題與連結。

### 驗證步驟

1. 從 Agent picker 選取 Library Code Reviewer。
2. 執行：

~~~bash
git status --short
git diff --stat
~~~

3. 輸入：

~~~text
請 review 目前工作樹的變更。只讀取與搜尋，不要修改任何檔案。
請檢查需求、分層、測試、no-side-effect 與實際驗證證據；
如果看不到命令輸出，請標記 UNVERIFIED。
~~~

4. Review 完成後再次執行相同 Git 命令。
5. 比較前後輸出；若不一致，停止交付並找出變更來源。

### 預期結果與檢查點

- [ ] Reviewer 出現在 Agent picker。
- [ ] tools 只有 read、search、microsoftLearn/*。
- [ ] 沒有 edit、execute 或其他寫入工具。
- [ ] 報告包含 severity、檔案證據、違反規則、建議與驗證狀態。
- [ ] Reviewer 前後 git status --short 與 git diff --stat 一致。

---

## Part 7：Language Model——選擇適合的模型（1:40–1:50）

### 前置條件

Chat model picker 可見，不需要 BYOK，也不要在 repository 儲存 API key。

### 操作步驟

1. 開啟 Chat model picker，記錄目前可用模型。
2. 選擇 Auto，執行 Part 3 的 repository analysis Prompt。
3. 選擇一個可用的快速模型，執行相同 Prompt。
4. 選擇一個支援 thinking effort 的推理模型，執行相同 Prompt。
5. 執行 Chat: Manage Language Models，查看 capabilities、context size、billing 與 visibility。
6. 若模型支援，調整 thinking effort；Agent 實作模型必須支援 tool calling。
7. 記錄實際帳號可用的模型名稱，不把固定模型名稱寫進教材答案。

| 實驗 | 模型／effort | 是否支援 tools | 延遲 | 品質 | AI credit／policy 備註 |
|---|---|---|---|---|---|
| Auto | | | | | |
| 快速模型 | | | | | |
| 推理模型 | | | | | |

模型選擇是 runtime decision，不是 Instructions、Skill 或 Agent 的替代品。官方說明：[AI language models in VS Code](https://code.visualstudio.com/docs/agent-customization/language-models)。

### 預期結果與檢查點

- [ ] 已用相同 Prompt 比較至少兩種可用模型或 Auto。
- [ ] 已記錄 tool-calling、thinking effort、延遲與品質觀察。
- [ ] 沒有設定或提交秘密。
- [ ] 模型或組織 policy 限制已被明確記錄。

---

## Part 8：Microsoft Learn MCP——取得外部官方證據（1:50–2:05）

### 前置條件

VS Code 支援 MCP，且網路可連線到 learn.microsoft.com。

### 建立設定

~~~bash
mkdir -p .vscode
~~~

建立 .vscode/mcp.json：

~~~json
{
  "servers": {
    "microsoftLearn": {
      "type": "http",
      "url": "https://learn.microsoft.com/api/mcp"
    }
  }
}
~~~

不要加入 API key 或 token；.vscode/mcp.json 是唯一要追蹤的 .vscode 檔案。

### 啟動與驗證

1. 執行 MCP: List Servers。
2. 啟動 microsoftLearn，第一次出現 trust 時確認來源。
3. 查看 server output。
4. 在 Chat 的 Configure Tools 確認 Microsoft Learn tools。
5. 執行：

~~~text
請使用 Microsoft Learn MCP 查詢 .NET 8 中 TimeProvider 的官方文件，
並說明它如何讓依賴目前時間的商業邏輯更容易測試。
請列出官方文件標題與連結，不要只依賴模型記憶。
~~~

記錄是否真的呼叫 MCP、來源標題／連結、server error、organization policy，以及 TimeProvider 是否必要。MCP 提供外部證據，但不代表所有建議都要納入本次修改；本 Lab 將 TimeProvider 列為後續改善。[Microsoft Learn MCP guide](https://learn.microsoft.com/en-us/training/support/mcp-get-started)

若 MCP 失敗：保留 List Servers 與 server output 錯誤，可以閱讀官方文件完成概念討論，但必須標記「未完成 MCP 實驗」，不可捏造 tool call。

### 預期結果與檢查點

- [ ] microsoftLearn server 可被列出，或保留明確失敗證據。
- [ ] 已確認 trust、server output、Configure Tools。
- [ ] 查詢結果包含可追溯的官方來源。
- [ ] 沒有秘密欄位。

---

## Part 9：Hooks 與 Plugins（2:05–2:20）

### A. Hooks：安全的 lifecycle 觀察

Agent Hooks 目前是 Preview。使用 Chat: Configure Hooks、/create-hook 或 Customizations editor 建立 Workspace hook：

~~~text
建立一個只在 SessionStart 顯示訊息的 Library Lab hook。
它只能回傳 JSON systemMessage，不得編輯、刪除、格式化、migration 或寫入任何資料。
請將檔案儲存到 .github/hooks/library-lab-session.json。
~~~

非破壞性範本；macOS／Linux 與 Windows 命令依平台選用：

~~~json
{
  "hooks": {
    "SessionStart": [
      {
        "type": "command",
        "command": "printf '{\"systemMessage\":\"Library Lab hook loaded\"}'",
        "windows": "powershell -NoProfile -Command \"Write-Output '{\\\"systemMessage\\\":\\\"Library Lab hook loaded\\\"}'\"",
        "timeout": 10
      }
    ]
  }
}
~~~

重新開啟 Chat，查看 Agent Debug Logs 與 Output 的 GitHub Copilot Chat Hooks channel。記錄 hook input／output、exit code、policy 限制與是否執行。不要把 Hook 改成 formatter、migration、刪檔或資料寫入命令。官方說明：[Agent Hooks](https://code.visualstudio.com/docs/agent-customization/hooks)。

### B. Plugins：只檢視 manifest，不安裝外部來源

在 Customizations editor 的 Plugins 頁籤查看 marketplace、啟用／停用與 trust 流程。不安裝外部 Plugin；先檢查 chat.plugins.enabled 是否受 organization policy 控制。

教材提供 docs/examples/library-ai-workflow-plugin/plugin.json 作為不會自動載入的示範 manifest：

~~~json
{
  "name": "library-ai-workflow",
  "description": "Example bundle for the Library Management AI workflow",
  "version": "0.1.0",
  "author": { "name": "Build School" },
  "skills": "skills/",
  "agents": "agents/",
  "hooks": "hooks.json",
  "mcpServers": ".mcp.json"
}
~~~

這是放在 docs/examples 的教學 manifest，不會被 VS Code 當作 workspace plugin 自動載入。Plugin 可以打包 slash commands、Skills、Agents、Hooks 與 MCP；安裝前必須檢查 publisher、hooks、MCP commands 與權限。官方說明：[Agent Plugins](https://code.visualstudio.com/docs/agent-customization/agent-plugins)。

### 預期結果與檢查點

- [ ] Hook 只顯示非破壞性訊息，且能在 logs 中觀察。
- [ ] Hook 不包含 destructive command。
- [ ] 已知道 Hook 的 Preview、JSON、exit code 與 policy 限制。
- [ ] Plugin manifest 是有效 JSON，且沒有被安裝或自動載入。
- [ ] 能說明 Plugin 與 Skill／Agent／MCP／Hook 的包裝關係。

---

## Part 10：整合實作——逾期禁止新增借閱（2:20–2:50）

### 前置條件與工作順序

開啟新的 Chat session，依序確認：

1. AGENTS.md 已被 Instructions diagnostics 發現。
2. /overdue-loan-review 可分析但不修改。
3. dotnet-feature-development 已明確或自動載入。
4. 使用一般 Agent 實作，不要一開始選唯讀 Reviewer。
5. MCP 只用來查證框架層建議；TimeProvider 是非阻擋 follow-up。

### 固定 Prompt

貼上與 Part 2 完全相同的文字：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

要求 Agent 依序執行：

1. 讀取 LoanService、LoanCreationStatus、repository、Console flow、CreateLoan tests 與 docs。
2. 列出 acceptance criteria、影響範圍、分層 plan 與預計檔案，確認後才 edit。
3. 做最小修改。
4. 新增 success、rejection、boundary、no-side-effect tests。
5. 執行並觀察 dotnet build、dotnet test、git diff --check。
6. 檢查 diff，不得修改 seed data、NuGet、public interface 或 Console 商業規則。
7. 保存 terminal output，再切換 Library Code Reviewer 唯讀 review。

### 功能規格

- ILoanService.CreateLoan(int patronId, int bookItemId) 與 repository contracts 不變。
- LoanCreationStatus 新增 PatronHasOverdueLoan 與清楚描述。
- 判斷順序：Patron 不存在 → 會員過期 → 五本上限 → 逾期未歸還 → BookItem 存在／可借 → 建立 Loan。
- 逾期定義：ReturnDate == null && DueDate < DateTime.Now。
- 已歸還逾期紀錄與未逾期 active loan 不阻擋。
- 拒絕時不得查詢 GetBookItem、GetAvailableBookItems，不得呼叫 AddLoan，不得修改 Patron Loans。
- Console 只使用 LoanCreationStatus 與 EnumHelper 顯示結果。

### 測試與 Review

至少涵蓋：

- active overdue 回傳 PatronHasOverdueLoan。
- returned overdue 可以建立借閱。
- current active 可以建立借閱。
- 拒絕時 no-side-effect 與 repository calls 驗證。
- 會員過期與五本上限的既有優先順序不回歸。

切換 Reviewer 後輸入：

~~~text
請 review 這次逾期借閱規則的變更。只讀取與搜尋，不要修改檔案。
請對照 AGENTS.md、dotnet-feature-development Skill、需求順序、
測試/no-side-effect、build/test/diff-check 實際證據輸出報告；
查證框架建議時附 Microsoft Learn 來源。
~~~

### 預期結果與檢查點

- [ ] 一般 Agent 完成 plan、edit、test；Reviewer 只讀取與搜尋。
- [ ] 新增 PatronHasOverdueLoan 且規則位於 ApplicationCore。
- [ ] 測試涵蓋四種必要路徑與 no-side-effect。
- [ ] Reviewer 看不到的命令輸出都標記 UNVERIFIED。
- [ ] Reviewer 前後工作樹不變。

---

## Part 11：手動驗證、回顧與交付（2:50–3:00）

### 手動驗證

每個案例開始前都 reset：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

在 Console 搜尋指定 Patron、選取 1、輸入 b、選擇顯示 [14]、[19] 或 [20] 的列，記錄訊息、Loan 數量與 runtime JSON。每個案例結束輸入 q，下一個案例重新 reset。

| Patron | 預期結果 |
|---|---|
| Patron Eight | 因 active overdue 拒絕；Loan 數量與集合不變，不新增或部分更新 JSON。 |
| Patron Nine | 只有 returned overdue，可以正常新增一筆 Loan。 |
| Patron Three | 仍因五本 active loan 上限拒絕。 |
| Patron Seven | 仍因會員過期拒絕。 |

不要把 .library-data 加入 Git；檢查 runtime JSON 沒有非預期新增、重複或部分更新。

### Before／After 評估

| 面向 | Before | After |
|---|---|---|
| 分層與規則遵循 | | |
| 修改範圍 | | |
| 測試完整性 | | |
| build/test/diff 實際證據 | | |
| 可重複流程 | | |
| Reviewer 可追溯性 | | |
| MCP 官方來源 | | |
| Model／Hook／Plugin 限制 | | |

### Decision matrix

| 需求 | 適合機制 |
|---|---|
| 所有 Chat request 都適用的通則 | AGENTS.md / Instruction |
| 手動重複呼叫的一段任務描述 | Prompt File |
| 多步驟能力、資源與 scripts | Agent Skill |
| 專門角色、工具權限與輸出格式 | Custom Agent |
| 回答品質、速度、thinking effort | Language Model |
| Repository 之外的文件、API、資料或動作 | MCP |
| Agent lifecycle 的 deterministic command | Hook |
| 將多種 customization 打包分享 | Plugin |

### 最終驗證與交付

~~~bash
dotnet build
dotnet test
git diff --check
git status --short
git diff --stat
~~~

預期：build 0 errors、starter 的 37 tests 未回歸、solution tests 全部通過、diff check 沒有輸出。確認沒有 .library-data、API key、token、個人 VS Code 設定或 .lab-reference。

Commit 範例：

~~~bash
git add .github AGENTS.md docs src tests .vscode/mcp.json
git status --short
git commit -m "feat: add overdue borrowing rule and Copilot customizations"
~~~

若教師要求推送：

~~~bash
git push -u origin lab/copilot-customization
~~~

### 完成條件

- [ ] 37 baseline tests 未回歸，solution tests 全部通過。
- [ ] 九種 customization 都能被發現、執行或完成概念驗收。
- [ ] Preview／policy 限制有實際錯誤或未完成紀錄。
- [ ] Reviewer 前後工作樹不變。
- [ ] 四個手動案例、runtime JSON、Before／After 表與 review 報告完成。
- [ ] Commit 不包含 .library-data 或 private answer key。

---

## Troubleshooting

### Customizations editor 或檔案看不到

- 確認開啟 repository root，且 Chat input 選的是正確 Agent harness。
- 確認建立時選 Workspace，而不是 User scope。
- 執行 Chat: Open Customizations、Chat: Configure Instructions 與 Diagnostics。
- 執行 Developer: Open Agent Debug Panel 查看 discovery error。
- 若是在 monorepo 子目錄開啟，確認是否需要 chat.useCustomizationsInParentRepositories。

### AGENTS.md 沒有套用

- 確認檔案位於 repository root，名稱大小寫正確。
- 重新開啟 Chat 或執行 Developer: Reload Window。
- 確認使用的是 Chat／Agent request，不是 inline completion；inline suggestions 不會套用 custom instructions。
- 不要同時建立另一份 .github/copilot-instructions.md 造成來源混淆。

### Prompt File 或 Skill 沒有出現

- Prompt File 必須是 .github/prompts/*.prompt.md。
- Skill 必須是 .github/skills/<name>/SKILL.md，frontmatter name 必須與資料夾一致。
- 確認 chat.useAgentSkills 為 true、chat.agentSkillsLocations 包含 .github/skills。
- /skills 只是 Configure Skills；用 /dotnet-feature-development 驗證實際觸發。
- Prompt File 是手動 slash command；自動語意觸發不是必要驗收。

### Language Model 不可用

- Restricted Mode、Copilot plan 或 organization policy 可能只顯示 Auto。
- Agent model 必須支援 tool calling。
- 不要為了課堂擅自加入 API key；記錄 policy 限制即可。

### MCP 無法啟動

- 執行 MCP: List Servers，查看 microsoftLearn server output。
- 確認 URL 是 https://learn.microsoft.com/api/mcp，沒有秘密欄位。
- 重新確認 trust，並檢查 organization MCP policy。
- 可以閱讀官方文件完成概念，但必須標記「未完成 MCP 實驗」。

### Hook 無法執行

- Hook 必須位於 .github/hooks/*.json。
- 查看 GitHub Copilot Chat Hooks Output channel 與 Agent Debug Logs。
- 確認 command 會輸出合法 JSON，且沒有 destructive side effect。
- Hooks 為 Preview；組織停用時保留錯誤證據。

### Plugin 不出現

- 本 Lab 的 docs/examples/.../plugin.json 只是 manifest 範例，不會自動安裝。
- 外部 Plugin 可能需要 chat.plugins.enabled 與 marketplace trust。
- 不要為了課堂安裝未知來源；檢查 publisher、hooks、MCP commands 與權限。

### Reviewer 修改了檔案

- 確認 Agent tools 沒有 edit、execute、terminal 或其他寫入工具。
- 比較 review 前後的 git status --short 與 git diff --stat。
- 復原前先確認變更只來自 Reviewer，不要覆蓋學員未保存的功能修改。

### Tests 或 Console 資料不一致

- 每個手動案例前執行 --reset-data。
- 以 runtime path 檢查 Loans JSON，不要修改 seed JSON。
- 如果 baseline 不是 37 tests，停止流程並先排除 branch、SDK、restore 與資料問題。
