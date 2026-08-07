# AccelerateDevGitHubCopilot

## 專案簡介

這是一個以 .NET 8 開發的圖書館借閱範例專案，採用分層架構，並提供一個互動式 Console 應用程式操作借閱流程。

目前 Repository 內已實作的執行介面只有 Console App，沒有 Web API、資料庫或前端網站。

使用者可以在 Console 中：

- 搜尋讀者
- 查看讀者與借閱明細
- 新增借閱
- 續借
- 歸還
- 續會員資格
- 重設執行期資料

## 前置需求

- Git
- .NET SDK 8.0

> 專案中的 Console、ApplicationCore、Infrastructure 與測試專案目前都以 `net8.0` 為目標框架。

## Clone 指令

```bash
git clone https://github.com/AndyHuang1223/AccelerateDevGitHubCopilot.git
cd AccelerateDevGitHubCopilot
```

## Restore 指令

```bash
dotnet restore AccelerateDevGitHubCopilot.sln
```

## Build 指令

```bash
dotnet build AccelerateDevGitHubCopilot.sln
```

## Run 指令

第一次執行或想把資料恢復成初始教學狀態時，請使用：

```bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
```

若只想延續前一次的執行期資料，請使用：

```bash
dotnet run --project src/Library.Console/Library.Console.csproj
```

啟動後會進入互動式 Console，並顯示目前使用的資料目錄。

## Test 指令

```bash
dotnet test tests/UnitTests/UnitTests.csproj
```

## Solution 結構

```text
AccelerateDevGitHubCopilot.sln
src/
  Library.ApplicationCore/
  Library.Console/
  Library.Infrastructure/
tests/
  UnitTests/
docs/
```

## 各 Project 的責任

### src/Library.ApplicationCore

- 放置核心實體、列舉、介面與業務規則
- `LoanService` 負責新增借閱、續借、歸還規則
- `PatronService` 負責會員續期規則

### src/Library.Infrastructure

- 提供 JSON 型態的資料存取實作
- `JsonPatronRepository` 處理讀者查詢與更新
- `JsonLoanRepository` 處理借閱查詢、可借館藏查詢與寫入
- `JsonData` 負責 seed 載入、執行期資料初始化與 JSON 儲存

### src/Library.Console

- 提供互動式主控台操作流程
- 組合 DI、設定檔與 Repository/Service
- 內含 seed JSON 檔案，作為執行期資料初始化來源

### tests/UnitTests

- 驗證 ApplicationCore 的借閱與會員規則
- 驗證 Infrastructure 的 JSON 初始化與借閱資料寫入行為

### docs

- 保存教學與設計說明文件
- 不屬於執行所需元件

## 目前支援的功能

- 依讀者姓名搜尋讀者
- 顯示讀者基本資料與所有借閱紀錄
- 從可借館藏清單建立新的借閱
- 對尚未歸還且未逾期的借閱進行續借
- 將借閱標記為已歸還
- 在規則允許時延長讀者會員資格
- 使用 `--reset-data` 重建執行期資料

## 新增借閱操作方式

1. 啟動 Console。
2. 輸入讀者姓名關鍵字搜尋讀者。
3. 以數字選取讀者。
4. 在讀者明細畫面輸入 `b` 進入借書流程。
5. 從畫面列出的可借館藏中輸入編號。
6. 系統會建立借閱，並回到該讀者的明細畫面。

## 借閱上限規則

- 每位讀者最多只能同時擁有 5 筆未歸還借閱
- 已歸還的借閱不計入上限
- 會員資格已過期的讀者不可新增借閱
- 新借閱建立成功後，預設到期日為借出當天後 14 天

## 館藏不可重複借出的規則

- 系統只會在借閱流程中列出目前可借的館藏
- 若某個 `BookItem` 已存在 `ReturnDate == null` 的借閱紀錄，該館藏會被視為不可借
- 建立借閱前，服務層會再次檢查該館藏是否仍在可借清單中，避免重複借出同一館藏

## 資料儲存方式

- `src/Library.Console/Json` 內的 JSON 檔是 seed 資料
- 程式啟動時會把 seed 複製到 Repository root 下的 `.library-data/` 作為執行期資料
- 執行中的借閱、歸還、續借與會員更新，會寫入 `.library-data/`，不會直接改寫 seed 檔
- `.library-data/` 已被 Git 忽略
- 使用 `--reset-data` 會刪除並重建 `.library-data/`

## 常見問題

### 為什麼我重新執行後資料和上一次不一樣？

因為 Console 會持續使用 `.library-data/` 中上次執行留下的資料。若要回到初始狀態，請重新執行：

```bash
dotnet run --project src/Library.Console/Library.Console.csproj -- --reset-data
```

### 為什麼 seed JSON 沒有被修改？

因為實際讀寫的是 `.library-data/` 中的執行期副本，`src/Library.Console/Json` 只作為初始化來源。

### 如果 `.library-data/` 缺少部分 JSON 檔案怎麼辦？

程式啟動時會停止，並提示重新使用 `--reset-data` 建立完整資料。

### 可以直接輸入館藏編號借書嗎？

不行。依目前實作，必須先搜尋並選取讀者，再從 Console 列出的可借館藏清單中用數字選取。

## 已知限制

- 目前只有 Console 操作介面，沒有 Web API、資料庫或前端
- 搜尋入口目前只有讀者姓名搜尋，沒有書籍搜尋或條碼掃描流程
- 借閱流程只能從讀者明細畫面進入，不能直接指定讀者 ID 或館藏 ID 進行借閱
- 資料儲存採本機 JSON 檔，未提供多人同時操作的同步或衝突處理機制
- `.library-data/` 的實際位置受執行時工作目錄影響；本文指令以 Repository root 為前提
- 是否已完整驗證 Windows 與 Linux 的互動式 Console 體驗：待確認