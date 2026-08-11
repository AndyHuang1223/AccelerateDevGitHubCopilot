# Lab：GitHub Copilot 客製化 AI 驅動開發

## 使用 VS Code Agent Customizations 延伸既有 .NET 專案

本 Lab 接續 Library Management Repository 的 Create Loan 完成版。你會用同一個 repository，逐步建立可分享的規則、流程、角色與外部工具，最後完成「逾期且未歸還則禁止新增借閱」功能。

本文件是學生操作手冊。每個 Part 都包含前置條件、操作步驟、Prompt／檔案範本、預期結果與檢查點。模型輸出可以不同，但必須以責任邊界、工具權限與實際驗證證據為準。

官方總覽：[Create and manage agent customizations](https://code.visualstudio.com/docs/agent-customization/overview)

## Lab 目標

完成後，你將能夠：

1. 使用 Chat: Open Customizations 管理 User／Workspace customization。
2. 用 /init 建立通用 AGENTS.md，讓 Agent 回答與實作遵循 repository 慣例。
3. 建立可套用到不同需求的 Feature Plan、Test Gap 與 Review Prompt File。
4. 用 Agent Skill、Custom Agent 與選修 MCP 延伸多步驟流程、角色權限與外部證據。
5. 用相同需求比較沒有 customization 與完整 customization 的差異。

## 課堂主線

~~~text
Create and Manage  管理位置、scope、發現與診斷
        ↓
AGENTS.md          定義所有工作都適用的通則
        ↓
Prompt Files       將可重複的單一任務變成 slash command
        ↓
Agent Skill        封裝包含資源的多步驟開發流程
        ↓
Custom Agent       限定角色、工具與輸出格式
        ↓
MCP                取得 Repository 之外的工具與知識
        ↓
整合實作與 Review
~~~

Instructions 會自動套用；Prompt Files 由使用者明確呼叫；Skills 依相關性自動載入或用 slash command 明確載入；Custom Agent 定義角色與工具；MCP 提供外部能力。Language Model、Hooks 與 Plugins 本次不列入實作範圍。參考 [VS Code customization concepts](https://code.visualstudio.com/docs/agents/concepts/customization)。

本 Lab 的主線 instruction 是根目錄 AGENTS.md；.github/copilot-instructions.md 只在比較段落中提到，不需要建立兩份 Always-on instruction。三個 Prompt File、Skill、Agent 與 MCP 都在學員操作後才建立；starter 本身不預先放入功能答案。

本 Lab 的 Prompt File 不是逾期借閱的答案，而是可以接收不同需求的 reusable workflow。逾期借閱只作為第一次實作案例；學生也要用相同 Prompt 驗證會員續期或歸還流程等第二個需求。

## 提示詞使用方式

- 以 `/init`、`/create-prompt`、`/create-skill`、`/create-agent` 開頭的內容是 Chat 指令；請直接在 Chat 輸入。
- `~~~text` 區塊是可以直接複製貼上的完整提示詞。若指令先開啟建立精靈，請在精靈詢問角色、內容或儲存位置時貼上對應提示詞。
- Prompt File、Skill 與 Custom Agent 產生的內容是初稿；建立完成後，仍要按照文件中的 canonical frontmatter、檔案路徑、工具限制與流程逐項檢查。
- 除非提示詞明確要求修改，分析與驗證提示詞都必須要求 Copilot 只讀取、不修改，且不得宣稱未實際觀察到的命令或工具結果。

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

本 Lab 不實作 Language Model、Hooks 與 Plugins；若想延伸，請另開短實驗。MCP policy 可能依帳號、組織和 VS Code 版本不同；環境限制必須保留錯誤證據，不得假裝成功。

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
4. 查看 Instructions、Skills、Agents 與 Prompts 頁籤。
5. 在 New 選單比較 User 與 Workspace scope；本 Lab 的檔案都選 Workspace。
6. 執行 Developer: Open Agent Debug Panel，確認能看到目前 workspace 的 discovery log。

### 預期結果與檢查點

- [ ] 目前分支是 lab/copilot-customization。
- [ ] git status --short 沒有輸出。
- [ ] Customizations editor 能開啟，且已確認 Agent harness 與 Workspace scope。
- [ ] 尚未建立 AGENTS.md、.github/prompts、.github/skills、.github/agents 或 .vscode/mcp.json。

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
- .github/prompts/feature-plan.prompt.md
- .github/prompts/test-gap.prompt.md
- .github/prompts/review-change.prompt.md
- .github/skills/dotnet-feature-development/SKILL.md
- .github/agents/library-code-reviewer.agent.md
- .vscode/mcp.json

### 操作步驟與固定 Prompt

開啟新的 Chat，選一般 Agent，貼上以下 Prompt。Part 8 會使用完全相同文字：

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

如果 `/init` 沒有產生檔案，或產生的檔案需要整理，請在同一個 Chat 貼上以下提示詞：

~~~text
請探索目前這個 .NET 8 C# Library Management repository 的 source、tests、docs、solution 與 project 結構，
建立或整理 repository root 的 AGENTS.md。

AGENTS.md 必須要求 Agent：
- 先讀取相關 source、tests 與 docs，再回答問題；不可猜測 repository 結構。
- 使用實際檔案路徑、型別、方法、測試或命令輸出作為證據。
- 清楚區分觀察事實、合理推論與尚未驗證資訊。
- 使用者只要求分析或說明時，不得修改檔案。
- 提出 plan 時，先列 acceptance criteria、影響範圍、預計檔案、風險與未決定事項。
- 不得捏造命令、測試、工具呼叫或文件來源。
- 將 business rules 放在 Library.ApplicationCore，JSON persistence 放在 Library.Infrastructure，
  input/output 與 flow coordination 放在 Library.Console。
- 沿用現有 entities、services、interfaces、enums、repositories 與 factories，優先採用最小修改。
- 不得在沒有具體需求時新增套件、修改 public interface、seed data 或無關功能。
- 測試要涵蓋 success、rejection、boundary 與 no-side-effect paths。
- 回報完成前，實際執行並觀察 dotnet build、dotnet test 與 git diff --check。
- 沒有實際命令輸出時，不得宣稱驗證成功。

只建立或整理 AGENTS.md，不要修改任何 source、test、seed data 或其他 documentation。
完成後請列出實際讀取的檔案與建立的檔案，並標記任何未驗證資訊。
~~~

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

## Part 4：Prompt Files——把可重複任務變成 slash command（0:45–1:10）

### 前置條件

AGENTS.md 已可被發現，且尚未開始逾期功能實作。

### 操作步驟

建立三個 Workspace Prompt File。它們要描述可重複的任務，不要把逾期借閱的功能答案寫死在 Prompt 裡：

1. 執行 Chat: Open Customizations。
2. 開啟 Prompts 頁籤。
3. 選擇 New Prompt → Workspace。
4. 或執行 Chat: New Prompt File／create-prompt。
5. 分別儲存為：
   - `.github/prompts/feature-plan.prompt.md`
   - `.github/prompts/test-gap.prompt.md`
   - `.github/prompts/review-change.prompt.md`

如果使用 `/create-prompt` 產生初稿，請分別指定上述路徑與用途，或在新的 Chat 貼上以下提示詞一次建立三個檔案：

~~~text
請為目前這個 .NET 8 C# Library Management repository 建立三個 Workspace Prompt Files：

- .github/prompts/feature-plan.prompt.md
- .github/prompts/test-gap.prompt.md
- .github/prompts/review-change.prompt.md

它們必須是可套用到不同功能需求的 reusable workflow，不得把 overdue borrowing 的功能答案寫死在 Prompt 裡。

feature-plan 的用途是：針對使用者提供的 feature request，建立 evidence-based implementation plan。
test-gap 的用途是：將使用者提供的 feature request 對應到既有測試與缺少的測試案例。
review-change 的用途是：唯讀檢查目前變更的需求、分層、測試、side effects 與驗證證據。

三個 Prompt File 都必須使用 ask agent，且只讀取與搜尋，不得 edit、create、delete、format 或 execute。
不要修改任何 source、test、seed data 或其他 documentation。
~~~

### `feature-plan.prompt.md`

~~~markdown
---
name: feature-plan
description: Create an evidence-based implementation plan for a feature request
argument-hint: "[feature request]"
agent: ask
---

請針對以下功能需求建立實作計畫：

${input:featureRequest}

請先讀取相關的 source、tests、docs、interfaces 與現有 patterns。

輸出：

1. Acceptance criteria
2. 目前已存在的功能與檔案證據
3. 需求與現況之間的缺口
4. 影響的 project、檔案與 symbol
5. ApplicationCore、Infrastructure、Console 的責任分配
6. Success、rejection、boundary、no-side-effect 測試案例
7. 風險與尚未確認事項

只分析，不要修改檔案、建立檔案、執行命令或宣稱測試成功。
~~~

### `test-gap.prompt.md`

~~~markdown
---
name: test-gap
description: Map a feature request to existing and missing tests
argument-hint: "[feature request]"
agent: ask
---

功能需求：

${input:featureRequest}

請讀取相關 production code、既有測試、Factory 與文件，建立測試缺口分析。

請輸出 Markdown table：

| Acceptance criterion | Existing test | Missing scenario | Suggested test location |
|---|---|---|---|

至少檢查：

- success path
- rejection path
- boundary condition
- returned／inactive data
- no-side-effect behavior
- repository interaction
- regression of existing rules

只分析，不要修改檔案或執行命令。
對無法由 repository 證實的內容標記為 UNVERIFIED。
~~~

### `review-change.prompt.md`

~~~markdown
---
name: review-change
description: Perform a read-only review of current changes
argument-hint: "[review focus]"
agent: ask
---

請唯讀 Review 目前的變更。

Review focus：

${input:reviewFocus}

請檢查：

- 需求與 acceptance criteria
- 分層責任
- 商業規則位置
- 測試完整性
- no-side-effect 與 partial update
- public API、NuGet、seed data 與 unrelated changes
- build、test、diff-check 的實際證據

輸出：

1. Verdict
2. Findings table：severity、file evidence、rule violated、recommendation、blocking status
3. 已驗證項目
4. UNVERIFIED 項目
5. Non-blocking follow-up

只能讀取與搜尋，不得修改檔案。
~~~

### 驗證與觀察

1. 在 Chat 輸入 `/feature-plan`，提供逾期借閱需求。
2. 再以同一個 Prompt 提供會員續期或歸還流程需求，確認 Prompt 沒有綁定單一功能。
3. 執行 `/test-gap`，確認輸出包含 success、rejection、boundary 與 no-side-effect。
4. 執行 Chat: Run Prompt，或在 Prompt File editor 按 play，選目前 Chat 或新 Chat 執行。
5. 確認三個 Prompt 都是手動呼叫，不會因一般問題自動執行，也沒有修改工作樹。

Prompt Files 是可手動呼叫的 Markdown slash command；它和自動套用的 Instructions、可相關性載入的 Skill 不同。Agent Host 不使用傳統 Prompt Files 時，應改用 Skill；本 Lab 使用 VS Code local Chat 驗證。官方格式：[Prompt Files](https://code.visualstudio.com/docs/agent-customization/prompt-files)。

### 預期結果與檢查點

- [ ] 三個 Prompt File 出現在 Workspace Prompts 清單。
- [ ] `/feature-plan` 至少能套用到逾期借閱與另一個不同需求。
- [ ] `/test-gap` 能列出既有測試、缺少情境與建議測試位置。
- [ ] 三個 Prompt 的回答只有分析、證據與 plan，沒有修改檔案。
- [ ] Prompt Files 不包含 API key、秘密或單一功能的完整答案。

---

## Part 5：Agent Skill——封裝含資源的 .NET 功能開發流程（1:10–1:30）

### 前置條件

AGENTS.md 與三個 Prompt File 已完成，尚未開始逾期功能實作。

三個 Prompt File 已經涵蓋單一、手動重複的分析任務。本 Part 只在需要跨多個步驟、並且要引用 `architecture.md` 與 `testing.md` 等資源時建立 Skill；不要只把較長的 Prompt 改名成 Skill。

### 設定與建立

預設的 Workspace Skill 位置是 `.github/skills`。如果目前版本沒有自動發現 Skill，再檢查 `chat.agentSkillsLocations` 是否包含該目錄；不要把版本特定的設定視為本 Part 的唯一驗收條件。

~~~json
{
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

### 使用 /create-skill 產生初稿

在新的 Chat 輸入 `/create-skill`。如果 slash command 先開啟產生流程，請在下一則訊息貼上以下提示詞：

~~~text
請為目前這個 .NET 8 C# Library Management Console repository
建立一個 Workspace Agent Skill。

Skill 名稱必須是：
dotnet-feature-development

請將 Skill 儲存到：
.github/skills/dotnet-feature-development/

這個 Skill 用於新增或修改：
- Library.ApplicationCore 的 business rules、entities、services、enums
- Library.Infrastructure 的 repositories 與 JSON persistence
- Library.Console 的 input/output 與 flow coordination
- UnitTests 中的 xUnit、NSubstitute 與 Factory-based tests

請建立以下檔案：
- SKILL.md
- architecture.md
- testing.md

SKILL.md 的 frontmatter 必須包含：
- name: dotnet-feature-development
- 能匹配 .NET Library feature、Service、Repository、Console 與 tests 的 description
- argument-hint: "[feature or change to implement]"
- user-invocable: true
- disable-model-invocation: true

Skill 流程必須是：
1. 先閱讀 source、tests、docs 與現有 patterns。
2. 列出 acceptance criteria、影響範圍與尚未確認的決策。
3. 在修改前提出最小 file-level implementation plan。
4. 將 business rules 放在 ApplicationCore。
5. 將 persistence 放在 Infrastructure。
6. 將互動與流程控制放在 Console。
7. 沿用既有 entities、services、interfaces、enums、repositories 與 factories。
8. 補上 success、rejection、boundary 與 no-side-effect tests。
9. 執行並觀察 dotnet build、dotnet test、git diff --check。
10. 沒有實際命令輸出時，不得宣稱驗證成功。

SKILL.md 必須用相對 Markdown links 參考：
- ./architecture.md
- ./testing.md

architecture.md 請描述各 project 的責任與 Console → Infrastructure → ApplicationCore 依賴方向。
testing.md 請描述 xUnit、NSubstitute、PatronFactory、LoanFactory、Received(1)、DidNotReceive() 與測試命令。

請加入限制：
- 優先採用最小修改。
- 不新增 NuGet package、資料庫、Web API 或無關重構。
- 不任意修改 public interface、seed data 或既有行為。
- 如果需求與現有程式碼衝突，先列出證據與需要決定的選項。
- 這次只建立 Skill 檔案，不要實作 overdue borrowing 功能。
~~~

產生流程詢問儲存位置時，選擇 `Workspace`；詢問名稱時使用 `dotnet-feature-development`。`/create-skill` 是 AI 輔助產生初稿，完成後仍要依下方 canonical frontmatter、流程與相對連結人工檢查，不要直接把模型輸出視為驗收結果。

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
disable-model-invocation: true
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

在 Chat 輸入 `/dotnet-feature-development`，再貼上以下完整提示詞。這是穩定驗收方式。

~~~text
請分析「在建立借閱時加入逾期且未歸還的禁止規則」需要修改哪些層與測試。

請明確載入並遵循 dotnet-feature-development Skill，讀取相關 source、tests、docs 與 Skill resources。
只提出 acceptance criteria、影響範圍與最小 file-level implementation plan，不要修改任何檔案。
請列出：
1. ApplicationCore、Infrastructure、Console 各層的責任。
2. 需要檢查的 LoanService、LoanCreationStatus、repository、Console flow 與測試檔案。
3. success、rejection、boundary 與 no-side-effect 測試案例。
4. 目前無法從 repository 證實的資訊，並標記為 UNVERIFIED。

不要執行完整 feature workflow，不要宣稱 build、test 或 diff check 已成功。
~~~

本 Lab 將 Skill 設為明確呼叫，避免它與通用 Prompt File 重複載入。若課堂要觀察自動語意載入，可另以 `disable-model-invocation: false` 做延伸實驗，但不列為必要驗收條件。記錄是否引用兩個 resource、是否列 plan、是否修改檔案。

官方說明：[Agent Skills](https://code.visualstudio.com/docs/agent-customization/agent-skills)。

### 預期結果與檢查點

- [ ] Skill 出現在 Workspace Skills 清單。
- [ ] `.github/skills` 被發現；若需額外設定，已記錄實際版本與設定名稱。
- [ ] SKILL.md 的 name 與資料夾相同。
- [ ] 內容包含 ./architecture.md 與 ./testing.md。
- [ ] A、B 沒有修改程式；C 能明確載入完整 workflow 與兩個 resource。
- [ ] Diagnostics 沒有 Skill discovery error。

---

## Part 6：Custom Agent——建立唯讀 Library Code Reviewer（1:30–1:45）

### 前置條件

Instructions、三個 Prompt File 與 Skill 已被發現；MCP 可以稍後加入，Reviewer 必須能在 MCP 不可用時標記未驗證。

### 建立步驟

使用 Chat: Open Customizations → Agents → New Agent → Workspace，或 /create-agent，建立：

~~~text
.github/agents/library-code-reviewer.agent.md
~~~

### 使用 /create-agent 產生初稿

這個流程要在 **Agent mode** 的新 Chat session 執行。`/create-agent` 會先詢問
Agent 的角色與工具，再產生 `.agent.md` 初稿；如果目前版本沒有顯示這個 slash
command，改用 `Chat: New Custom Agent`，並把下列提示詞貼到建立畫面或下一則訊息。
官方流程參考：[Custom agents](https://code.visualstudio.com/docs/agent-customization/custom-agents)。

1. 在 Chat 的 mode picker 選 **Agent**。
2. 輸入 `/create-agent`。
3. 選擇 Workspace scope，名稱填入 `Library Code Reviewer`。
4. 當 Copilot 詢問角色、工具與輸出格式時，貼上以下完整提示詞：

~~~text
請為目前這個 .NET 8 C# Library Management Console repository
建立一個 Workspace Custom Agent，名稱是 Library Code Reviewer。

請將檔案儲存到：
.github/agents/library-code-reviewer.agent.md

這個 Agent 是唯讀 reviewer，只能使用：
- read
- search
- microsoftLearn/*

不得使用：
- edit
- create
- delete
- format
- execute
- terminal 或其他寫入工具

請檢查：
1. 使用者需求與 acceptance criteria
2. ApplicationCore、Infrastructure、Console 分層
3. success、rejection、boundary、no-side-effect tests
4. unrelated files、public API、NuGet、seed data 與 partial updates
5. 實際 dotnet build、dotnet test、git diff --check 證據

看不到命令輸出時必須標記 UNVERIFIED，不得推測成功。
使用 Microsoft Learn 時，輸出官方文件標題與連結。

Reviewer 必須輸出：
- 簡短 verdict
- severity、file evidence、violated rule/criterion、recommendation、blocking status 的 findings table
- verification evidence and UNVERIFIED items
- non-blocking follow-up

請建立 .agent.md frontmatter 與 Markdown body。
這次只建立 Custom Agent 檔案，不要修改任何 source、test 或 documentation。
~~~

5. 完成建立後，開啟 `.github/agents/library-code-reviewer.agent.md`，確認產生的
   frontmatter 與下方 canonical 版本一致；Copilot 產生的文字若有變異，請手動修正。

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

## Part 7：選修 Microsoft Learn MCP——取得外部官方證據（1:45–2:00）

### 前置條件

VS Code 支援 MCP，且網路可連線到 learn.microsoft.com。本 Part 是選修短實驗，不是逾期借閱功能實作或 Prompt File 驗收的必要條件。

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

## Part 8：整合實作——逾期禁止新增借閱（2:00–2:40）

### 前置條件與工作順序

開啟新的 Chat session，依序確認：

1. AGENTS.md 已被 Instructions diagnostics 發現。
2. `/feature-plan` 與 `/test-gap` 可分析但不修改。
3. dotnet-feature-development 已明確載入；若未建立 Skill，使用一般 Agent 依照同一個 workflow 執行。
4. 使用一般 Agent 實作，不要一開始選唯讀 Reviewer。
5. MCP 若已完成，只用來查證框架層建議；TimeProvider 是非阻擋 follow-up。

### Baseline Prompt

以下提示詞只用於 Before／After 比較；Part 2 與本 Part 的 baseline 必須使用完全相同的需求描述：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

### Feature input 與 reusable implementation template

先執行 `/feature-plan`，輸入本次案例需求；確認 plan 後，再執行 `/test-gap` 檢查測試缺口。這兩個 Prompt 必須先完成唯讀分析，不能直接取代 plan confirmation。

接著在一般 Agent Chat 使用以下 reusable implementation template。若不是從 Prompt File 執行，請將 `${input:featureRequest}` 與 `${input:acceptedPlan}` 替換成實際內容：

~~~text
請在目前 .NET 8 C# Library Management repository 中完成以下功能需求：

${input:featureRequest}

以下是已確認的 acceptance criteria 與 implementation plan：

${input:acceptedPlan}

請依以下順序工作：
1. 先讀取相關 source、tests、docs、interfaces 與現有 patterns。
2. 如果目前工作樹或需求與 plan 衝突，先列出實際證據與選項，不要自行發明設計。
3. 在修改前確認最小 file-level plan；確認後才 edit。
4. 將 business rules 放在 Library.ApplicationCore，persistence 放在 Library.Infrastructure，input/output 與 flow coordination 放在 Library.Console。
5. 沿用現有 entities、services、interfaces、enums、repositories 與 factories。
6. 補上 success、rejection、boundary 與 no-side-effect tests。
7. 執行並實際觀察 dotnet build、dotnet test 與 git diff --check。
8. 檢查 diff，確認沒有修改 seed data、NuGet、public interface、Console 商業規則或無關功能。

完成後回報修改檔案、測試案例、實際命令輸出與仍然 UNVERIFIED 的項目。
沒有實際命令輸出時，不得宣稱 build、test 或 diff check 成功。
~~~

### 本次案例的驗收條件

以下內容是本次逾期借閱案例的需求輸入，不是 reusable Prompt File 的固定答案：

- `ILoanService.CreateLoan(int patronId, int bookItemId)` 與既有 repository contracts 維持不變。
- 讀者有逾期且尚未歸還的借閱時，新的借閱必須被拒絕，並顯示清楚一致的結果訊息。
- 判斷順序維持：Patron 不存在 → 會員過期 → 五本未歸還借閱上限 → 逾期且未歸還 → BookItem 存在／可借 → 建立 Loan。
- 逾期定義為 `ReturnDate == null && DueDate < DateTime.Now`。
- 已歸還的逾期借閱與未逾期的 active loan 不得被逾期規則阻擋。
- 逾期規則拒絕時，不得查詢 `GetBookItem` 或 `GetAvailableBookItems`，不得呼叫 `AddLoan`，也不得修改 Patron Loans。
- Console 只能使用既有結果狀態與 `EnumHelper` 顯示結果，不得自行計算商業規則。

### 操作順序

1. 以 `/feature-plan` 分析逾期借閱需求。
2. 以 `/test-gap` 對照既有 `CreateLoan` tests、Factory 與上述驗收條件。
3. 確認 plan 後，使用一般 Agent 執行 reusable implementation template；若 Part 5 已完成，先明確載入 `dotnet-feature-development` Skill。
4. 新增或補強 active overdue、returned overdue、current active、no-side-effect 與既有優先順序測試。
5. 執行並保存 `dotnet build`、`dotnet test` 與 `git diff --check` 的實際輸出。
6. 使用 `/review-change` 輸入「逾期借閱規則、分層、測試與驗證證據」，再切換 `Library Code Reviewer` 做唯讀 Review。

### 預期結果與檢查點

- [ ] 一般 Agent 完成 plan、edit、test；Reviewer 只讀取與搜尋。
- [ ] 新增 PatronHasOverdueLoan 且規則位於 ApplicationCore。
- [ ] 測試涵蓋四種必要路徑與 no-side-effect。
- [ ] Reviewer 看不到的命令輸出都標記 UNVERIFIED。
- [ ] Reviewer 前後工作樹不變。

---

## Part 9：手動驗證、回顧與交付（2:40–3:00）

### 前置條件

Part 8 的程式實作、測試與唯讀 Review 已完成；你知道 runtime data path，且每個手動案例都可以重新 reset。

### 操作步驟

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

### Decision matrix

| 需求 | 適合機制 |
|---|---|
| 所有 Chat request 都適用的通則 | AGENTS.md / Instruction |
| 手動重複呼叫的一段任務描述 | Prompt File |
| 多步驟能力、資源與 scripts | Agent Skill |
| 專門角色、工具權限與輸出格式 | Custom Agent |
| Repository 之外的文件、API、資料或動作 | MCP |

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
- [ ] AGENTS.md、三個 Prompt File、Skill 與 Custom Agent 都能被發現並完成必要驗收；MCP 若未完成，保留實際錯誤證據。
- [ ] 同一個 `/feature-plan` 至少套用到逾期借閱與另一個不同功能需求。
- [ ] `/test-gap` 能找出既有測試、缺少情境與建議測試位置。
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
- 確認 `.github/skills` 位於預設 Workspace Skill 位置；若使用額外位置，再檢查 `chat.agentSkillsLocations`。
- `/skills` 只是 Configure Skills；用 `/dotnet-feature-development` 驗證實際觸發。
- Prompt Files 應分別以 `/feature-plan`、`/test-gap` 與 `/review-change` 驗證。
- Prompt File 是手動 slash command；自動語意觸發不是必要驗收。

### MCP 無法啟動

- 執行 MCP: List Servers，查看 microsoftLearn server output。
- 確認 URL 是 https://learn.microsoft.com/api/mcp，沒有秘密欄位。
- 重新確認 trust，並檢查 organization MCP policy。
- 可以閱讀官方文件完成概念，但必須標記「未完成 MCP 實驗」。

### Reviewer 修改了檔案

- 確認 Agent tools 沒有 edit、execute、terminal 或其他寫入工具。
- 比較 review 前後的 git status --short 與 git diff --stat。
- 復原前先確認變更只來自 Reviewer，不要覆蓋學員未保存的功能修改。

### Tests 或 Console 資料不一致

- 每個手動案例前執行 --reset-data。
- 以 runtime path 檢查 Loans JSON，不要修改 seed JSON。
- 如果 baseline 不是 37 tests，停止流程並先排除 branch、SDK、restore 與資料問題。
