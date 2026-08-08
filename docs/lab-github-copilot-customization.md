# Lab：GitHub Copilot 客製化開發流程

## 使用 Instruction、Skill、MCP 與 Custom Agent 延伸既有 .NET 專案

> 本 Lab 接續第一堂的 Create Loan 完成版，使用 lab/copilot-create-loan 的程式狀態。學員應從乾淨的 starter checkout 開始，在自己的工作分支完成練習。

本文件是學生操作手冊。請依照 Part 0 到 Part 10 的順序執行；每一 Part 都包含前置條件、操作步驟、Prompt 或檔案範本、預期結果與檢查點。若 Copilot 的輸出和範本不同，以責任、工具邊界、驗證證據為判斷重點，不必逐字複製模型輸出。

---

## Lab 目標

完成本 Lab 後，你將能夠：

1. 說明 Repository Instructions、Agent Skills、MCP 與 Custom Agents 的責任邊界。
2. 把適用於所有工作的 .NET Repository 規則寫成 file-based instruction。
3. 把跨檔案、可重複的開發流程封裝成 Agent Skill。
4. 以 Microsoft Learn MCP 查詢官方 .NET 文件，並保留來源證據。
5. 建立限制工具權限的唯讀 Library Code Reviewer Custom Agent。
6. 使用完全相同的需求，比較沒有客製化與有客製化時的修改品質。
7. 在不擴大範圍的前提下，完成「逾期且未歸還則禁止新增借閱」規則與測試。

## 課堂主線

~~~text
Instruction   管「永遠適用的規則」
      ↓
Skill         管「可重複的任務流程」
      ↓
MCP           提供「Repository 之外的工具與知識」
      ↓
Custom Agent  定義「角色、工具邊界與輸出格式」
      ↓
整合實作與唯讀 Review
~~~

本 Lab 只使用 VS Code Agent customization，不加入 Prompt Files、Hooks、自製 MCP Server、資料庫或新的 NuGet package。VS Code 的 file-based instruction 使用 .instructions.md 檔案；本 Lab 以 applyTo: "**" 示範整個 Repository 都自動套用的規則。若沒有 applyTo，檔案不會自動套用，仍可能透過語意匹配被載入。[VS Code custom instructions 官方說明](https://code.visualstudio.com/docs/agent-customization/custom-instructions)

## 前置需求

- Git
- .NET SDK 8
- Visual Studio Code（建議使用支援 Agent Skills、Custom Agents 與 MCP 的近期版本）
- GitHub Copilot Chat 與 Agent mode
- 可連線至 learn.microsoft.com 的網路

先在終端機確認版本：

~~~bash
git --version
dotnet --version
code --version
~~~

MCP 使用 Microsoft Learn 的公開 HTTP server，不需要 API key；第一次啟動時仍必須確認 server trust。組織政策可能停用 MCP，若看不到工具，請記錄限制並請管理員確認 chat.mcp.access。

---

## Part 0：取得 starter 並建立工作分支（0:00–0:10）

### 前置條件

- 已安裝 Git、.NET SDK、VS Code 與 GitHub Copilot。
- 你知道這個 GitHub repository 的 clone URL。
- 若已經有本機 checkout，從「確認 Repository root」開始即可。

### 操作步驟

第一次使用本機時：

~~~bash
git clone <repository-url>
cd AccelerateDevGitHubCopilot
code .
~~~

若你已在 Repository 內，先確認位置與遠端：

~~~bash
git rev-parse --show-toplevel
git remote -v
git fetch origin lab/copilot-customization-starter
~~~

從指定的 starter 建立學員工作分支：

~~~bash
git switch -c lab/copilot-customization origin/lab/copilot-customization-starter
git branch --show-current
git status --short
~~~

預期目前分支是 lab/copilot-customization，git status --short 沒有輸出。

若課堂尚未發布 lab/copilot-customization-starter，可暫時從第一堂完成版建立同名工作分支：

~~~bash
git fetch origin lab/copilot-create-loan
git switch -c lab/copilot-customization origin/lab/copilot-create-loan
~~~

### 預期結果與檢查點

- [ ] VS Code 開啟的是 Repository root，而不是 src 或 tests 子目錄。
- [ ] git branch --show-current 顯示 lab/copilot-customization。
- [ ] 工作樹乾淨，尚未建立任何 customization 檔案。

若 branch 已存在，先使用 git status --short 確認沒有未保存的課堂內容，再執行 git switch lab/copilot-customization；不要覆蓋其他人的修改。

---

## Part 1：確認 starter baseline（0:10–0:20）

### 前置條件

Part 0 的工作樹乾淨，而且目前在 lab/copilot-customization。

### 操作步驟

在 Repository root 執行：

~~~bash
dotnet restore
dotnet build
dotnet test
git diff --check
~~~

預期結果：

- restore 成功。
- build 顯示 0 Error(s)；既有 warnings 可以保留。
- test 顯示 37 passed。
- git diff --check 沒有輸出。

接著用固定資料驗證上一堂的 Console 流程：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

在 Console 依序輸入：

1. 在 Enter a string to search for patrons by name: 輸入 Patron Six。
2. 搜尋結果輸出 1) Patron Six 時輸入 1。
3. 在讀者詳細資料的 Input Options 輸入 b。
4. 在 Available Book Items 清單選擇顯示 [14]、[19] 或 [20] 的那一列；輸入的是清單列號，不是 BookItem id。
5. 預期看到成功描述，回到 Patron Details，且借閱清單已重新載入。
6. 輸入 q 結束程式。

完成後再次 reset，避免這筆借閱影響後續固定資料：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

### 預期結果與檢查點

- [ ] baseline 為 37 tests passed。
- [ ] build 沒有 error。
- [ ] Console 可以建立一筆正常借閱。
- [ ] reset 後 runtime data 回到固定 seed 狀態。

若 baseline 不是上述結果，先不要進入 Agent 實驗；記錄完整 command output，確認 branch、SDK 版本、restore 狀態與 runtime data，再請講師協助。

---

## Part 2：Baseline 實驗——沒有客製化（0:20–0:30）

### 前置條件

先確認以下檔案尚未建立：

- .github/instructions/repository-development-guidelines.instructions.md
- .github/skills/dotnet-feature-development/SKILL.md
- .github/agents/library-code-reviewer.agent.md
- .vscode/mcp.json

開一個新的 VS Code Chat，使用一般 Agent mode。不要先建立 Instruction、Skill、MCP 或 Custom Agent。

### 操作步驟與固定 Prompt

將下列 Prompt 完整貼上；Part 7 會再次使用完全相同的文字：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

在 Agent 開始修改前，觀察它是否先探索檔案與提出 plan。完成或停止後，不要立即接受結果，先記錄：

| 項目 | 觀察結果 |
|---|---|
| 商業規則放在哪一層？ | |
| 是否修改 Console 來計算規則？ | |
| 是否新增不必要的抽象、套件或 public interface？ | |
| 是否有 active overdue、returned overdue、current active 測試？ | |
| 拒絕時是否驗證沒有查詢館藏或 AddLoan？ | |
| 是否真的執行 build、test、diff check？ | |
| Agent 宣稱完成的內容，哪些有命令輸出佐證？ | |

### 復原步驟

優先使用 VS Code Chat 的 Undo；若 Agent 提供 checkpoint，使用 Restore checkpoint 回到 Prompt 前的狀態。復原後執行：

~~~bash
git status --short
git diff --stat
~~~

若出現未追蹤檔案：

1. 先用 git status --short 列出檔案。
2. 只確認本次實驗產生的檔案，不能刪除個人檔案。
3. 可先執行 git clean -n -- <path> 預覽，再只移除已確認的實驗檔案；不要對整個 Repository 使用無範圍的 clean。
4. 再次確認 git status --short 與 git diff --stat 都沒有輸出。

### 預期結果與檢查點

- [ ] 已完成 Before observation table。
- [ ] Chat Undo 或 Restore checkpoint 已完成。
- [ ] 工作樹回到乾淨狀態。
- [ ] 尚未保留任何功能答案，Part 3–6 才開始建立 customization。

---

## Part 3：建立 Repository Instructions（0:30–0:50）

### 前置條件

- Part 2 工作樹乾淨。
- 已開啟 VS Code Chat，能使用 /create-instruction 或手動建立檔案。

### 操作步驟

建立目錄：

~~~bash
mkdir -p .github/instructions
~~~

在 Chat 執行 /create-instruction，提供下列 Prompt 產生初稿：

~~~text
請為目前這個 .NET 8 C# Library Management Console repository 建立
repository-wide file-based instruction。規則要涵蓋 ApplicationCore、
Infrastructure、Console 的責任、最小修改、既有 xUnit/NSubstitute 測試慣例、
不修改 seed data、dotnet build/test/diff-check 驗證，以及不得捏造命令結果。
請輸出適合儲存為 .github/instructions/repository-development-guidelines.instructions.md
的 Markdown，並包含正確 YAML frontmatter。
~~~

將產生的檔案保存為：

.github/instructions/repository-development-guidelines.instructions.md

再把檔案修正為以下 canonical 版本：

~~~markdown
---
name: repository-development-guidelines
description: Repository-wide C# and .NET development conventions
applyTo: "**"
---

# Repository Development Guidelines

This is a .NET 8 C# library management console application.

## Architecture

- Business rules belong in Library.ApplicationCore.
- Library.Console handles input, output, and flow coordination only.
- Library.Infrastructure handles JSON persistence and repository implementation.
- Do not calculate business rules in the Console or Infrastructure layers.

## Development

- Follow existing entities, services, interfaces, enums, and repository patterns.
- Prefer the smallest focused change.
- Do not add a NuGet package unless the requirement clearly needs it.
- Do not change public interfaces without a concrete requirement.
- Do not change seed data or unrelated behavior.
- Preserve nullable reference type correctness.

## Testing and verification

- Follow the existing xUnit and NSubstitute test style.
- Cover success, rejection, boundary, and no-side-effect paths.
- Before reporting completion, run dotnet build, dotnet test, and git diff --check.
- Never claim a command succeeded unless its output was actually observed.
~~~

### 驗證 instruction 是否載入

1. 儲存檔案後重新開啟 Chat，或執行 VS Code 的 customization diagnostics。
2. 使用會讀取 C# 檔案、但不應修改程式的 Prompt：

   ~~~text
   請閱讀目前的 LoanService.cs 與相關測試，摘要這個 repository 的分層責任。
   只分析，不修改檔案；請列出你實際參考的檔案。
   ~~~

3. 在 response 的 References／引用資訊中確認 .github/instructions/repository-development-guidelines.instructions.md。
4. 若看不到 References，使用 diagnostics 檢查 syntax、路徑與 workspace root，再重新開啟 Chat。

applyTo: "**" 是本 Lab 選定的 Repository-wide 示範。若日後只想套用 C# 檔案，可以改為 **/*.cs,**/*.csproj；本次不要改變範圍，以便所有學員比較相同結果。

### 預期結果與檢查點

- [ ] 檔案位於 .github/instructions/，副檔名是 .instructions.md。
- [ ] frontmatter 含 name、description、applyTo: "**"。
- [ ] 內容沒有單一逾期需求的完整解法。
- [ ] References 或 diagnostics 能證明 instruction 已載入。

---

## Part 4：建立 Agent Skill 與 Trigger 實驗（0:50–1:20）

### 前置條件

- Part 3 的 instruction 已儲存且可被發現。
- 目前工作樹可以包含 instruction，但不要開始實作逾期功能。

### 建立 Skill

建立目錄：

~~~bash
mkdir -p .github/skills/dotnet-feature-development
~~~

在 Chat 執行 /create-skill，使用下列 Prompt 產生初稿：

~~~text
請為目前的 .NET Library Management repository 建立名為
dotnet-feature-development 的 Agent Skill。它要處理跨 ApplicationCore、
Infrastructure、Console、Service、Repository 與 UnitTests 的功能修改，
流程必須是探索現況、確認需求、分層計畫、最小實作、成功/拒絕/邊界/
no-side-effect 測試，以及 build/test/diff-check 驗證。請把架構與測試細節
拆成同一目錄的 architecture.md 與 testing.md，並在 SKILL.md 使用相對連結。
~~~

將初稿放在 .github/skills/dotnet-feature-development/SKILL.md，並修正成以下 frontmatter 與核心流程：

~~~markdown
---
name: dotnet-feature-development
description: >
  Implement or modify features in this C# .NET library management
  application while following its ApplicationCore, Infrastructure,
  Console, repository, testing, and verification conventions. Use when
  adding or changing services, business rules, repositories, Console
  flows, or related tests.
argument-hint: "[feature or change to implement]"
---

# .NET Feature Development

Use this workflow for changes that cross the Library ApplicationCore, Infrastructure, Console, or UnitTests projects.

## Workflow

1. Read the relevant code, tests, and documentation before proposing changes. Use [architecture guidance](./architecture.md) to identify the correct layer.
2. Convert the request into explicit acceptance criteria. Identify decisions that are not established by the repository or request; do not silently invent them.
3. Produce a small implementation plan with the affected files before editing.
4. Put business rules in ApplicationCore, persistence mechanics in Infrastructure, and input/output or flow coordination in Console.
5. Reuse existing entities, services, interfaces, enums, repository patterns, and test factories whenever possible.
6. Add tests for success, rejection, boundary, and no-side-effect paths. Follow [testing guidance](./testing.md).
7. Run dotnet build, dotnet test, and git diff --check before reporting completion. Report only commands and results that were actually observed.
8. If code and requirements conflict, stop and show the evidence, options, risks, and decision needed.

## Scope guardrails

- Prefer the smallest focused change.
- Do not add NuGet packages, database infrastructure, Web API layers, or unrelated refactors.
- Do not change seed data or public interfaces without a concrete requirement.
- Do not claim tests or commands passed without their output.
~~~

建立 architecture.md：

~~~~markdown
# Library architecture reference

## Project responsibilities

| Project | Responsibility |
|---|---|
| Library.ApplicationCore | Entities, enums, service and repository contracts, and business rules |
| Library.Infrastructure | JSON loading, relationship population, repository implementation, and persistence |
| Library.Console | Console input/output, state transitions, dependency injection, and flow coordination |
| UnitTests | ApplicationCore and Infrastructure tests using xUnit and NSubstitute |

## Dependency direction

~~~text
Library.Console ───────> Library.Infrastructure ───────> Library.ApplicationCore
       └────────────────────────────────────────────────> Library.ApplicationCore

UnitTests ──────────────> Library.Infrastructure
UnitTests ──────────────> Library.ApplicationCore
~~~

Business decisions belong in ApplicationCore. Infrastructure may answer data queries and save data, but it must not decide whether a patron is eligible to borrow. Console should display service results and coordinate the interaction, not duplicate domain rules.
~~~~

建立 testing.md：

~~~~markdown
# Testing reference

- Test framework: xUnit 2.5.3.
- Mocking library: NSubstitute 5.1.0.
- Use Arrange, Act, Assert and the existing test naming style.
- Reuse PatronFactory and LoanFactory before adding new helpers.
- Verify successful calls with Received(1) and rejected calls with DidNotReceive().
- For rejected operations, verify both the result status and that input collections/entities remain unchanged.
- Cover normal success, rejection, boundary values, persistence failures, and no-side-effect paths.

Run these commands from the repository root:

~~~bash
dotnet build
dotnet test
git diff --check
~~~

Do not treat a generated test summary or an agent statement as proof unless the command was actually executed in the current working tree.
~~~~

### 預期結果與 trigger 實驗

1. 儲存三個檔案，執行 /skills 或 customization diagnostics。
2. 確認 dotnet-feature-development 被發現，description 能涵蓋 Service、Repository、Console 與測試修改。
3. 使用新的 Chat session 完成以下三個實驗。前兩個只要求分析，不得修改程式：

   **實驗 A：一般 Repository 問題**

   ~~~text
   請解釋 Loan entity 與 LoanService 的責任邊界。
   只閱讀與分析，不要修改任何檔案，也不要執行完整 feature workflow。
   ~~~

   預期：instruction 可以影響回答，但不會啟動完整的探索→計畫→實作→測試流程。

   **實驗 B：功能需求，自動觸發觀察**

   ~~~text
   請分析「在建立借閱時加入逾期且未歸還的禁止規則」需要修改哪些層與測試。
   只提出 acceptance criteria 與檔案計畫，不要修改檔案。
   ~~~

   預期：觀察 description 是否自動載入 Skill，以及回答是否提到兩個 resource。

   **實驗 C：明確觸發**

   在 Chat 輸入 /dotnet-feature-development，再貼上實驗 B 的需求。預期 Skill 明確被載入。

4. 將三次結果記錄在觀察表：是否載入、是否列 plan、是否修改檔案、是否引用 architecture.md／testing.md。

若自動觸發不穩定，保留實驗 B 的結果，使用 slash command 完成實驗 C；不要為了觸發 Skill 提前修改功能。

### 預期結果與檢查點

- [ ] SKILL.md 的 name 與資料夾名稱相同。
- [ ] SKILL.md 實際包含 ./architecture.md 與 ./testing.md 兩個相對 Markdown links。
- [ ] /skills 或 diagnostics 能發現 Skill。
- [ ] 實驗 A、B 沒有修改程式；實驗 C 能明確載入 Skill。

---

## 休息（1:20–1:30）

---

## Part 5：設定 Microsoft Learn MCP（1:30–1:50）

### 前置條件

- 已完成 Part 3–4，並重新開啟 Chat。
- VS Code 版本支援 MCP；網路可連線到 learn.microsoft.com。

### 建立設定

建立 .vscode 目錄：

~~~bash
mkdir -p .vscode
~~~

建立 .vscode/mcp.json，內容必須是：

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

不要加入 API key、token 或其他秘密；這是公開 Microsoft Learn MCP endpoint。設定檔是本 Lab 唯一要追蹤的 .vscode 檔案。

### 啟動與觀察

1. 在 VS Code Command Palette 執行 MCP: List Servers。
2. 找到 microsoftLearn，執行啟動，第一次出現提示時確認 trust。
3. 開啟 server output，記錄啟動成功或錯誤訊息。
4. 在 Chat 的 Configure Tools 中確認 Microsoft Learn tools 可見。
5. 執行下列查詢：

   ~~~text
   請使用 Microsoft Learn MCP 查詢 .NET 8 中 TimeProvider 的官方文件，
   並說明它如何讓依賴目前時間的商業邏輯更容易測試。
   請列出文件標題與連結，不要只依賴模型記憶。
   ~~~

### 記錄與判斷

在觀察表記錄：

| 項目 | 結果 |
|---|---|
| 是否真的呼叫 Microsoft Learn MCP？ | |
| 官方來源標題與連結 | |
| server output 是否有錯誤？ | |
| 組織 policy 是否限制 MCP？ | |
| TimeProvider 是否是本次必要修改？ | |

MCP 提供外部證據，不代表所有建議都必須納入本次修改。本 Lab 將 TimeProvider 視為非阻擋的後續改善，既有規格仍使用 DateTime.Now，避免擴大範圍。[Microsoft Learn MCP 使用說明](https://learn.microsoft.com/en-us/training/support/mcp-get-started)

若 MCP 無法使用：

1. 先保留 MCP: List Servers 與 server output 的錯誤證據。
2. 可以閱讀 Microsoft Learn 官方頁面完成 TimeProvider 概念討論。
3. 在觀察表與回顧中明確標記「未完成 MCP 實驗」；不可宣稱已呼叫 MCP，也不可捏造工具輸出。

### 預期結果與檢查點

- [ ] .vscode/mcp.json 只有 microsoftLearn HTTP server。
- [ ] 已執行 List Servers、trust、server output 與 Configure Tools。
- [ ] 已記錄官方來源與實際 MCP/環境結果。
- [ ] MCP 失敗時有保留錯誤，沒有假裝成功。

---

## Part 6：建立唯讀 Library Code Reviewer（1:50–2:10）

### 前置條件

- Part 5 已完成，或已記錄 MCP 無法使用的明確原因。
- 目前可以看到 .github/skills/dotnet-feature-development/SKILL.md。

### 建立 Custom Agent

在 Chat 執行 /create-agent，提供：

~~~text
請建立一個名為 Library Code Reviewer 的唯讀 Custom Agent，檢查目前 .NET Library
Management repository 的需求、ApplicationCore/Infrastructure/Console 分層、
測試、資料副作用與 build/test 證據。工具只能使用 read、search 與 microsoftLearn/*，
不得使用 edit、execute 或任何寫入工具。輸出要包含 severity、檔案證據、違規規則、
建議、是否阻擋交付、驗證狀態與 Microsoft Learn 來源。
~~~

將檔案保存為 .github/agents/library-code-reviewer.agent.md，並固定使用下列 frontmatter 與 body：

~~~markdown
---
name: Library Code Reviewer
description: >
  Review changes to the .NET library management application for
  architecture, business-rule placement, tests, side effects, and
  verification evidence. Use Microsoft Learn for narrow .NET claims.
tools:
  - read
  - search
  - microsoftLearn/*
---

# Library Code Review

You are a read-only reviewer for this repository. Do not edit, create, delete, or format files. Do not run commands. Use read and search to inspect the working tree and the available terminal output.

Review the requested change against the repository instruction, the user acceptance criteria, and the linked feature workflow at ../skills/dotnet-feature-development/SKILL.md.

## Required checks

1. Confirm that each requirement has an implementation and a test.
2. Confirm business rules are in Library.ApplicationCore.
3. Confirm Console code only handles interaction and flow coordination.
4. Confirm Infrastructure does not decide borrowing eligibility.
5. Check success, rejection, boundary, and no-side-effect tests.
6. Check for unrelated files, public API changes, new packages, seed-data edits, and partial updates.
7. Check the latest terminal output for dotnet build, dotnet test, and git diff --check. If output is unavailable, explicitly mark verification as UNVERIFIED; never infer success.
8. Use microsoftLearn/* only for narrow .NET or framework claims. Include the official source title and link when it is used.

## Report format

Return:

1. A short verdict: approve, approve with follow-up, or request changes.
2. A findings table with severity, file evidence, violated rule or acceptance criterion, recommendation, and whether it blocks delivery.
3. Verification evidence and anything that remains UNVERIFIED.
4. A separate follow-up section for improvements outside this Lab. TimeProvider may be mentioned as a testing improvement, but it is not a blocker for the existing DateTime.Now design unless the requirement explicitly asks for it.
~~~

### 驗證 Agent 是唯讀

1. 儲存檔案後重新開啟 VS Code，從 Agent picker 選取 Library Code Reviewer。
2. 先執行以下命令並記錄兩份結果：

   ~~~bash
   git status --short
   git diff --stat
   ~~~

3. 輸入 review Prompt：

   ~~~text
   請 review 目前工作樹的變更。不要修改任何檔案。
   請依 Repository 規則、逾期借閱需求、測試完整性、資料副作用與實際驗證證據輸出報告。
   ~~~

4. Review 完成後再次執行相同兩個 Git 命令。
5. 比較前後輸出；若不一致，先找出是哪一個檔案被修改，再復原已確認屬於 Reviewer 的變更。

Reviewer 看不到測試輸出時必須標記 UNVERIFIED，不能由工作樹或模型敘述推測測試成功。

### 預期結果與檢查點

- [ ] Agent 出現在 Agent picker。
- [ ] tools 只有 read、search、microsoftLearn/*。
- [ ] body 明確禁止 edit、execute 與其他寫入工具。
- [ ] review 報告包含固定欄位與 UNVERIFIED 規則。
- [ ] Reviewer 前後 git status --short 與 git diff --stat 一致。

---

## Part 7：整合挑戰——逾期禁止新增借閱（2:10–2:35）

### 前置條件與工作順序

1. 開一個新的 Chat session，確認 response References 會載入 Repository Instruction。
2. 使用一般 Agent 進行實作，不要一開始就用唯讀 Reviewer。
3. 讓 dotnet-feature-development 自動載入，或先輸入 /dotnet-feature-development 明確載入。
4. MCP 僅用來查證框架層建議；不要因為 TimeProvider 擴大本次修改。

### 固定 Prompt

以下文字必須與 Part 2 完全相同：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

要求 Agent 依序完成：

1. 先探索 LoanService、LoanCreationStatus、相關 repository、Console 與 CreateLoan tests。
2. 列出 acceptance criteria、分層 plan 與預計檔案，等待你確認後再 edit。
3. 實作最小修改，完成測試。
4. 執行 dotnet build、dotnet test、git diff --check，在回覆中只報告看得到的輸出。
5. 檢查 git diff，確認沒有 seed data、NuGet、public interface 或 Console 商業規則的非必要變更。

### 功能規格（驗收基準）

- ILoanService.CreateLoan(int patronId, int bookItemId) 與 repository contracts 維持不變。
- 在 LoanCreationStatus 新增 PatronHasOverdueLoan 及一致、清楚的 description。
- CreateLoan 判斷順序固定為：Patron 不存在 → 會員過期 → 已達五本上限 → 逾期未歸還 → 館藏存在／可借 → 建立 Loan。
- 逾期定義為 ReturnDate == null && DueDate < DateTime.Now。
- 已歸還的逾期紀錄與尚未逾期的 active loan 都不阻擋借閱。
- 拒絕時不得查詢 GetBookItem、GetAvailableBookItems，不得呼叫 AddLoan，不得修改 Patron 的 Loans。
- Console 不新增判斷邏輯；沿用 LoanCreationStatus 與 EnumHelper 顯示原因。

### 必要測試與 Review 順序

測試至少涵蓋：

- active overdue loan 回傳 PatronHasOverdueLoan。
- returned overdue loan 可以建立借閱。
- 未逾期 active loan 可以建立借閱。
- 拒絕時不呼叫 GetBookItem、GetAvailableBookItems、AddLoan，且集合狀態不變。
- 會員過期與五本上限維持既有優先順序。

完成一般 Agent 的 plan、edit 與測試後，先保存 terminal output，再切換到 Library Code Reviewer，輸入：

~~~text
請 review 這次逾期借閱規則的變更。只讀取與搜尋，不要修改檔案。
請對照 Repository Instruction、dotnet-feature-development Skill、需求順序、
測試/no-side-effect、build/test/diff-check 實際證據輸出報告；查證框架建議時附 Microsoft Learn 來源。
~~~

### 預期結果與檢查點

- [ ] 先由一般 Agent 實作，再由 Reviewer 唯讀檢查。
- [ ] Skill 的探索、plan、最小修改、測試與驗證流程可觀察。
- [ ] 功能與測試符合固定規格。
- [ ] Reviewer 沒有修改工作樹，報告中的未驗證項目已明確標記。

---

## Part 8：手動驗證與 runtime JSON（2:35–2:50）

### 前置條件

Part 7 的程式測試已完成，且你知道 dotnet run 輸出中的 runtime data 路徑。每個案例開始前都要 reset，避免前一案例的新增 Loan 影響下一案例。

### 共用操作流程

每一個案例都執行：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

在 Console：

1. 搜尋指定的 Patron name。
2. 在搜尋結果選取該 Patron（單一結果通常輸入 1）。
3. 輸入 b。
4. 從 Available Book Items 選取顯示 [14]、[19] 或 [20] 的列號。
5. 記錄 Console 描述、Loan 清單前後差異與 runtime Loans.json 的差異。
6. 輸入 q 結束，下一個案例重新執行 reset。

若要確認資料沒有部分更新，使用程式啟動時列出的 runtime data path 開啟 Loans.json，比較 reset 前後的筆數與相關 Patron/Loan 記錄；不要把 .library-data 加入 Git。

### 四個案例

| 案例 | 操作 | 預期結果與資料檢查 |
|---|---|---|
| Patron Eight | 搜尋 Patron Eight，選 1，輸入 b，選一個可借 BookItem | 拒絕並顯示逾期原因；Loan 數量不變；不新增或部分更新 runtime JSON |
| Patron Nine | 搜尋 Patron Nine，選 1，輸入 b，選一個可借 BookItem | 已歸還的逾期紀錄不阻擋；成功建立借閱；Loan 數量增加一筆 |
| Patron Three | 搜尋 Patron Three，選 1，輸入 b，選一個可借 BookItem | 仍因五本 active loan 上限拒絕；不因逾期規則改變既有優先順序 |
| Patron Seven | 搜尋 Patron Seven，選 1，輸入 b，選一個可借 BookItem | 仍因會員過期拒絕；不查詢或新增後續借閱資料 |

### 預期結果與檢查點

- [ ] 四案例都在開始前使用 --reset-data。
- [ ] Patron Eight 拒絕且 Loan 數量、集合狀態不變。
- [ ] Patron Nine 可以正常新增 Loan。
- [ ] Patron Three 與 Patron Seven 的既有結果沒有回歸。
- [ ] runtime JSON 沒有非預期新增、重複或部分更新。

---

## Part 9：Before／After 回顧與選型（2:50–3:00）

完成下表，Before 使用 Part 2 的觀察，After 使用 Part 7–8 的實際證據：

| 評估面向 | 沒有 customization | 有 customization |
|---|---|---|
| 規則是否在 ApplicationCore | | |
| 是否遵循既有 Repository／Service pattern | | |
| 測試是否涵蓋拒絕與 no-side-effect | | |
| 是否有 build/test 實際證據 | | |
| 是否能用 Skill 重複流程 | | |
| Reviewer 是否有清楚角色與工具邊界 | | |
| MCP 來源是否可追溯 | | |
| 修改範圍是否能被 diff 追蹤 | | |

### 選型速查

| 需求 | 適合機制 |
|---|---|
| 所有工作都要遵守的架構與測試規則 | Instruction |
| 特定任務的多步驟流程與輔助資源 | Skill |
| 專門角色、輸出格式與工具權限 | Custom Agent |
| Repository 之外的文件、API、資料或動作 | MCP |

### 預期結果與檢查點

- [ ] Before／After 每一格都有觀察或命令證據，而不是只寫感想。
- [ ] 能用自己的話解釋四種 customization 的責任邊界。
- [ ] 能說明 TimeProvider 為何是後續改善，而不是本次阻擋條件。

---

## Part 10：最終驗證、Commit 與 Push（課後交付）

### 最終驗證

從 Repository root 執行：

~~~bash
dotnet build
dotnet test
git diff --check
git status --short
git diff --stat
~~~

預期：build 為 0 errors；既有 37 tests 未回歸，加入本次三個測試後 solution 應全部通過；diff check 沒有 whitespace error。git status --short 可以列出本次教材與程式變更，但不得列出 .library-data、秘密或個人設定。

### Commit

只加入本次課堂允許的檔案：

~~~bash
git add .github docs src tests .vscode/mcp.json
git status --short
git commit -m "feat: add overdue borrowing rule and Copilot customizations"
~~~

Commit 前再次確認沒有 staged 的 .library-data、API key、token、個人 VS Code 設定或不相關檔案。教師私有資料不應加入 staging。

### 可選 Push

若教師要求上傳工作分支，執行：

~~~bash
git push -u origin lab/copilot-customization
~~~

若沒有 push 權限，保留 commit hash 與完整錯誤輸出，交給教師，不要改推送 solution branch。

### 完成條件

- [ ] baseline 37 tests 未回歸，solution tests 全部通過。
- [ ] dotnet build、git diff --check 成功且結果有實際輸出。
- [ ] Instruction、Skill、MCP 與 Custom Agent 都能被發現或驗證。
- [ ] Reviewer 前後工作樹不變，無法看到的測試都標記 UNVERIFIED。
- [ ] 四個手動案例與 runtime JSON 檢查完成。
- [ ] Before／After 表、MCP 來源與 review 報告完成。
- [ ] Commit 不包含 runtime data 或 private answer key。

---

## Troubleshooting

### 看不到 Instruction

- 確認檔案是 .github/instructions/repository-development-guidelines.instructions.md，副檔名不是 .md.txt。
- 確認 YAML frontmatter 的 name、description 與 applyTo: "**" 格式正確。
- 重新開啟 Chat workspace，或執行 customization diagnostics。
- 確認使用的是 Chat／Agent request，而不是 inline completion。
- 在 diagnostics 與 response References 中查看實際載入狀態。

### 看不到 Skill

- 確認檔名是 SKILL.md，且位於 .github/skills/dotnet-feature-development/。
- 確認 name 只有小寫字母、數字與連字號，且與資料夾名稱相同。
- 確認 SKILL.md 的 ./architecture.md 與 ./testing.md links 指向同一資料夾。
- 執行 /skills 或 customization diagnostics；先用 /dotnet-feature-development 明確觸發。

### MCP 無法啟動

- 執行 MCP: List Servers，查看 microsoftLearn server output。
- 確認 .vscode/mcp.json 的 URL 是 https://learn.microsoft.com/api/mcp，且沒有秘密欄位。
- 確認可連線至 https://learn.microsoft.com。
- 重新執行 MCP trust/reset trust，再重新啟動 server。
- 若組織政策停用 MCP，記錄為環境限制；可閱讀官方文件完成概念討論，但必須標記「未完成 MCP 實驗」。

### Reviewer 修改了檔案

- 確認 Agent frontmatter 沒有 edit、execute、terminal 或其他寫入工具。
- 執行 git status --short 與 git diff --stat 找出變更。
- 復原前先確認變更只包含該 Reviewer 產生的檔案；不要覆蓋學員尚未保存的實作。

### Tests 或 Console 資料不一致

- 先確認 dotnet test 的完整輸出與目前 branch。
- 每個手動案例前執行 dotnet run ... -- --reset-data。
- 使用程式輸出的 runtime data path 檢查 Loans.json，不要直接修改 seed JSON。
- 如果 baseline 不是 37 tests，停止課堂流程並請講師協助環境問題。
