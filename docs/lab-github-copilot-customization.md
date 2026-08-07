# Lab：GitHub Copilot 客製化開發流程

## 使用 Instruction、Skill、MCP 與 Custom Agent 延伸既有 .NET 專案

> 本 Lab 接續第一堂的 Create Loan 完成版，使用 lab/copilot-create-loan 的程式狀態。
> 學員應從乾淨的 starter checkout 開始，並在自己的工作分支完成練習。

---

## Lab 目標

完成本 Lab 後，你將能夠：

1. 說明 Repository Instructions、Agent Skills、MCP 與 Custom Agents 的責任邊界。
2. 把適用於所有工作的 .NET Repository 規則寫成 always-on instruction。
3. 把跨檔案、可重複的開發流程封裝成 Agent Skill。
4. 以 Microsoft Learn MCP 查詢最新官方 .NET 文件，並保留來源證據。
5. 建立限制工具權限的唯讀 Code Reviewer Custom Agent。
6. 使用相同需求比較沒有客製化與有客製化時的修改品質。
7. 在不擴大範圍的前提下，完成逾期借閱規則與測試。

---

## 課堂主線

~~~text
Instructions  管「永遠適用的規則」
      ↓
Skill         管「可重複的任務流程」
      ↓
MCP           提供「Repository 之外的工具與知識」
      ↓
Custom Agent  定義「角色、工具邊界與輸出格式」
      ↓
整合實作與唯讀 Review
~~~

本 Lab 只使用 VS Code Agent customization，不加入 Prompt Files、Hooks、自製 MCP Server、資料庫或新的 NuGet package。

---

## 前置需求

- Git
- .NET SDK 8
- Visual Studio Code（建議使用支援 Agent Skills、Custom Agents 與 MCP 的近期版本）
- GitHub Copilot Chat 與 Agent mode
- 可連線至 learn.microsoft.com 的網路

確認版本：

~~~bash
git --version
dotnet --version
code --version
~~~

MCP 使用 Microsoft Learn 的公開唯讀端點，不需要 API key；第一次啟動時仍必須確認你信任此 MCP server。組織政策可能停用 MCP，若看不到工具，請先請管理員確認 chat.mcp.access。

---

## Part 0：取得完成版並建立工作分支

若你從 Repository root 開始：

~~~bash
git fetch origin lab/copilot-customization-starter
git switch -c lab/copilot-customization origin/lab/copilot-customization-starter
~~~

確認工作樹乾淨：

~~~bash
git status --short
~~~

若課堂尚未發布 lab/copilot-customization-starter，可直接從完成版開始：

~~~bash
git fetch origin lab/copilot-create-loan
git switch -c lab/copilot-customization origin/lab/copilot-create-loan
~~~

---

## Part 1：確認 starter baseline

執行：

~~~bash
dotnet restore
dotnet build
dotnet test
git diff --check
~~~

Starter 應已經包含第一堂的 Create Loan：

- ILoanService.CreateLoan(int patronId, int bookItemId)
- 會員過期檢查
- 五本 active loan 上限
- 可借館藏判斷
- JSON AddLoan
- Console 建立借閱流程
- 既有測試全部通過（目前參考 baseline 為 37 tests）

請先執行：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

確認 Console 可以建立一筆正常借閱，然後重新 reset data，讓後續實驗使用固定資料。

---

## Part 2：Baseline 實驗——沒有客製化

先確認以下檔案尚不存在，或暫時不要建立：

- .github/copilot-instructions.md
- .github/skills/dotnet-feature-development/SKILL.md
- .github/agents/library-code-reviewer.agent.md
- .vscode/mcp.json

在 Agent mode 使用以下完全固定的需求：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

觀察並記錄：

| 項目 | 觀察結果 |
|---|---|
| 商業規則放在哪一層？ | |
| 是否修改了 Console 來計算規則？ | |
| 是否新增不必要的抽象或套件？ | |
| 是否補上拒絕、已歸還與未逾期測試？ | |
| 是否真的執行 build/test？ | |
| Agent 宣稱完成的內容，哪些有命令輸出佐證？ | |

完成觀察後，使用 VS Code Chat 的 Undo／Restore checkpoint 復原 Agent 修改，確認：

~~~bash
git status --short
~~~

只應顯示乾淨工作樹。若使用 Git 復原，請先確認沒有要保留的個人檔案，再只復原本次實驗產生的檔案。

---

## Part 3：建立 Repository Instructions

建立 .github/copilot-instructions.md。內容應是短小、獨立、適用於大多數工作的規則，不要放單一需求的完整流程。

至少包含：

~~~markdown
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

在 Chat 中詢問一個與程式無關的 Repository 問題，再查看 response References，確認 .github/copilot-instructions.md 已被載入。注意：Instructions 影響 Chat／Agent request，不代表 inline completion 一定會讀取它。

---

## Part 4：建立 Agent Skill

建立目錄：

~~~text
.github/skills/dotnet-feature-development/
├── SKILL.md
├── architecture.md
└── testing.md
~~~

SKILL.md 的 YAML frontmatter 必須包含：

~~~yaml
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
~~~

Skill body 固定要求 Agent：

1. 先讀取相關程式碼、測試與文件，不能直接猜架構。
2. 把需求拆成 acceptance criteria，指出仍不確定的決策。
3. 先列出分層修改計畫與預計檔案，再開始修改。
4. 讓 ApplicationCore 負責商業規則，Infrastructure 負責資料操作，Console 負責互動。
5. 為成功、拒絕、邊界與「拒絕後狀態不變」建立測試。
6. 執行 dotnet build、dotnet test、git diff --check，只報告實際觀察到的結果。
7. 若規格與程式碼衝突，停止並列出證據，不自行改變需求。

architecture.md 應描述四個 Project 的責任與依賴方向；testing.md 應描述 xUnit／NSubstitute、Arrange／Act／Assert、Factory、Received／DidNotReceive 與本專案的測試命令。SKILL.md 必須以相對 Markdown link 引用這兩個檔案。

執行三個 trigger 實驗：

1. 「請解釋 Loan entity」：應使用 Instructions，但不需要整個 Skill workflow。
2. 「在建立借閱時加入逾期規則」：觀察 Skill 是否因 description 自動載入。
3. 使用 /dotnet-feature-development 明確載入 Skill，再輸入同一需求。

若自動觸發不穩定，使用 slash command 做課堂示範，並把兩種結果記錄在觀察表中。

---

## Part 5：設定 Microsoft Learn MCP

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

在 VS Code 執行：

1. MCP: List Servers
2. 啟動 microsoftLearn 並確認 trust
3. 查看 server output
4. 在 Chat 的 Configure Tools 確認可見工具

詢問：

~~~text
請使用 Microsoft Learn MCP 查詢 .NET 8 中 TimeProvider 的官方文件，
並說明它如何讓依賴目前時間的商業邏輯更容易測試。
請列出文件標題與連結，不要只依賴模型記憶。
~~~

記錄：

- MCP 是否真的被呼叫？
- 回答是否包含 Microsoft Learn 來源？
- 若 server 不能啟動，錯誤輸出是什麼？
- 這個建議是否屬於本次最小需求，還是後續改善？

MCP 提供外部工具與知識，不會取代開發者對範圍、相容性與風險的判斷。

---

## Part 6：建立唯讀 Library Code Reviewer

建立 .github/agents/library-code-reviewer.agent.md：

~~~yaml
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
~~~

Agent body 必須要求：

- 只讀取與搜尋，不修改檔案。
- 檢查需求是否完整、商業規則是否在 ApplicationCore、Console 是否只協調流程。
- 檢查成功、拒絕、邊界與 no-side-effect 測試。
- 檢查是否真的有 build/test 輸出；看不到輸出就標記「未驗證」。
- 對每個問題輸出 severity、檔案證據、違反的規則、建議與是否阻擋交付。
- 只有在需要查證 .NET API 或框架行為時使用 Microsoft Learn MCP，並附官方來源。
- 將 TimeProvider 列為可考慮的後續改善，不因本 Lab 的既有 DateTime.Now 設計直接擴大修改範圍。

切換到此 Agent，輸入：

~~~text
請 review 目前工作樹的變更。不要修改任何檔案。
請依 Repository 規則、逾期借閱需求、測試完整性、資料副作用與實際驗證證據輸出報告。
~~~

執行前後比較：

~~~bash
git status --short
git diff --stat
~~~

兩次結果必須一致。

---

## Part 7：整合挑戰——逾期禁止新增借閱

再次使用與 Part 2 完全相同的 Prompt：

~~~text
在建立借閱時，如果讀者有逾期且尚未歸還的借閱，必須拒絕新的借閱。
請補上必要的程式碼、測試與驗證，並遵循現有專案架構。
~~~

固定設計：

- ILoanService.CreateLoan(int patronId, int bookItemId) 不變。
- 在 LoanCreationStatus 新增 PatronHasOverdueLoan 及清楚的 description。
- 判斷順序為：Patron 不存在 → 會員過期 → 五本上限 → 逾期未歸還 → 館藏存在／可借 → 新增 Loan。
- 逾期定義為 ReturnDate == null && DueDate < DateTime.Now。
- 已歸還的逾期紀錄不阻擋。
- 拒絕路徑不得查詢後續館藏、呼叫 AddLoan 或修改 Patron Loans。
- Console 不自行判斷；沿用既有 enum description 顯示結果。

必要測試：

- active overdue loan 回傳 PatronHasOverdueLoan。
- returned overdue loan 可以借閱。
- 未逾期 active loan 可以借閱。
- 拒絕時不呼叫 GetBookItem、GetAvailableBookItems、AddLoan。
- 會員過期與五本上限既有優先順序不回歸。

完成後依序執行：

~~~bash
dotnet build
dotnet test
git diff --check
~~~

再使用 Library Code Reviewer 產生唯讀報告。

---

## Part 8：手動驗證

重新建立固定資料：

~~~bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
~~~

至少驗證：

| 情境 | 預期 |
|---|---|
| Patron Eight，有逾期未歸還 Loan | 拒絕，顯示逾期原因，Loan 數量不變 |
| 只有已歸還逾期紀錄的 Patron | 可以建立借閱 |
| Patron Three，已有 5 筆 active Loan | 仍回傳原本的上限錯誤 |
| Patron Seven，會員已過期 | 仍回傳會員過期錯誤 |

每次手動測試後可重新執行 --reset-data，避免 runtime JSON 污染下一個案例。

---

## Part 9：Before／After 回顧

完成下表：

| 評估面向 | 沒有 customization | 有 customization |
|---|---|---|
| 規則是否在 ApplicationCore | | |
| 是否遵循既有 Repository／Service pattern | | |
| 測試是否涵蓋拒絕與 no-side-effect | | |
| 是否有 build/test 實際證據 | | |
| 是否能用 Skill 重複流程 | | |
| Reviewer 是否有清楚角色與工具邊界 | | |
| MCP 來源是否可追溯 | | |

### 選型速查

| 需求 | 適合機制 |
|---|---|
| 所有工作都要遵守的架構與測試規則 | Instruction |
| 特定任務的多步驟流程與輔助資源 | Skill |
| 專門角色、輸出格式與工具權限 | Custom Agent |
| Repository 之外的文件、API、資料或動作 | MCP |

---

## Troubleshooting

### 看不到 Skill

- 確認檔名是 SKILL.md。
- 確認 name 只有小寫字母、數字與連字號，且與目錄名稱相同。
- 執行 /skills 或 Chat customization diagnostics。
- 先用 /dotnet-feature-development 明確觸發。

### Instruction 沒出現在 References

- 確認檔案位於 .github/copilot-instructions.md。
- 重新開啟 Chat workspace。
- 確認使用的是 Chat／Agent request，而不是 inline completion。

### MCP 無法啟動

- 執行 MCP: List Servers 查看 output。
- 確認可連線至 https://learn.microsoft.com。
- 重新執行 MCP: Reset Trust 後重新確認。
- 若組織政策停用 MCP，請記錄為環境限制，不要把本機 MCP 改成自製 server。

### Reviewer 修改了檔案

- 確認 Agent frontmatter 沒有 edit 或可寫入工具。
- 執行 git diff 找出變更。
- 復原前先確認只包含該 Reviewer 產生的修改。

---

## 完成條件

- dotnet build 成功。
- dotnet test 全部通過，且沒有刪除既有測試。
- git diff --check 成功。
- 逾期、已歸還、未逾期、上限與會員過期案例符合規格。
- Instruction、Skill、MCP 與 Custom Agent 都能在 VS Code 中被發現或驗證。
- Reviewer 前後工作樹沒有被修改。
- Before／After 觀察表與 Review 報告已完成。

