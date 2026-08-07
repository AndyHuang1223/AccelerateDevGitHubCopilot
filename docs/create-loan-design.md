# Create Loan Design

## 1. 目的

本文件記錄 Console 圖書館管理系統新增借閱功能的已確認設計。

本文件只記錄目前已確認且已採用的設計。若後續有新需求或延伸功能，應另行補充，不在本文自行假設。

## 2. 範圍

本次功能要達成以下目標：

- 使用者先選擇一位讀者。
- 使用者再選擇一筆可借閱的館藏。
- 系統建立一筆新的 Loan 紀錄。
- 每位讀者最多只能同時持有 5 本未歸還書籍。
- 已借出的館藏不可重複借出。
- Console 需要清楚顯示成功或失敗訊息。

## 3. 已確認的架構原則

- 商業規則位於 ApplicationCore。
- Console 不自行計算未歸還借閱數量。
- Infrastructure 不決定借閱是否允許，只提供資料查詢與持久化。
- 沿用既有 Service、Repository、Entity 與 enum description 結果回傳模式。
- 不新增第三方套件。
- 不修改與需求無關的程式碼。
- 不假設不存在的 API 或資料庫。
- 不可將續借或 UpdateLoan 當成新增借閱。

## 4. 固定設計決策

以下設計已固定，後續實作與測試都依此為準：

- LoanDate = DateTime.Now
- DueDate = LoanDate.AddDays(14)
- ReturnDate = null
- JsonLoanRepository.AddLoan 依目前最大 Loan ID + 1 產生新 ID
- BookItem 可借狀態由是否存在相同 BookItemId 且 ReturnDate == null 的 Loan 推導
- 不新增或更新 BookItem 狀態欄位
- 所有驗證完成後只執行一次 AddLoan 與一次 SaveLoans
- 驗證失敗時不得修改資料
- 新增借閱也要檢查讀者會員是否有效
- 即使沒有可借館藏，Console 仍顯示借閱選項；進入後顯示提示並回到讀者詳情

## 5. Repository 目前已存在的能力

### 5.1 Patron repository 已存在能力

目前 IPatronRepository 與 JsonPatronRepository 已提供：

- 依 PatronId 取得單一讀者
- 依姓名搜尋讀者
- 更新讀者資料

目前的讀者明細已會帶出該讀者的 Loans，因此 ApplicationCore 可以直接依讀者資料判斷未歸還借閱數量。

### 5.2 Loan repository 原本已存在能力

在本次功能加入前，ILoanRepository 與 JsonLoanRepository 已提供：

- 依 LoanId 取得單一 Loan
- 更新既有 Loan

這些能力原本已支援還書與續借流程，但不足以支援新增借閱。

### 5.3 JsonData 已存在能力

JsonData 已提供：

- 載入 Authors、Books、BookItems、Patrons、Loans
- 保存 Loans
- 保存 Patrons
- 重新組合 Patron、Loan、BookItem 的關聯資料

本次新增借閱沿用這套載入與保存模式，不另外引入新的資料存取機制。

## 6. 本次需要新增的能力

### 6.1 ApplicationCore 需要新增的能力

ApplicationCore 需要新增一條正式的新增借閱流程，並由 LoanService 負責全部借閱規則。

已新增或需要具備的能力如下：

- CreateLoan service 入口
- LoanCreationStatus 結果列舉
- 驗證讀者是否存在
- 驗證讀者會員是否過期
- 計算讀者未歸還借閱數量
- 驗證是否已達 5 本上限
- 驗證指定館藏是否存在
- 驗證指定館藏是否已被未歸還借閱占用
- 驗證成功後建立新的 Loan

### 6.2 Repository 需要新增的能力

Repository 需要新增最小必要能力來支援借閱流程，但不承擔商業規則判斷。

已新增或需要具備的能力如下：

- AddLoan：新增一筆 Loan
- GetBookItem：依 BookItemId 取得單一館藏
- GetAvailableBookItems：取得目前可借閱的館藏清單

其中 GetAvailableBookItems 的資料來源是 BookItems 與 active loans 的比對結果，但「是否允許借閱」的最終決策仍由 LoanService 負責。

### 6.3 Console 需要新增的能力

Console 需要新增借閱的互動入口與狀態流程。

已新增或需要具備的能力如下：

- 在讀者詳情畫面提供 BorrowBook 選項
- 新增 BorrowBook state
- 顯示可借閱館藏清單
- 讓使用者選擇一筆館藏
- 呼叫 LoanService.CreateLoan
- 顯示成功或失敗訊息
- 成功後重新載入讀者明細
- 沒有可借館藏時提示並返回讀者詳情

## 7. 借閱流程設計

借閱流程如下：

1. 使用者搜尋並選擇一位讀者。
2. 系統進入讀者詳情畫面。
3. 使用者選擇 BorrowBook。
4. Console 透過 repository 取得可借閱館藏清單。
5. 若沒有可借館藏，顯示提示並返回讀者詳情。
6. 若有可借館藏，使用者選擇其中一筆館藏。
7. Console 呼叫 LoanService.CreateLoan(patronId, bookItemId)。
8. LoanService 驗證讀者是否存在。
9. LoanService 驗證會員是否有效。
10. LoanService 計算 active loans，判斷是否已達 5 本上限。
11. LoanService 驗證 BookItem 是否存在。
12. LoanService 驗證該 BookItem 是否出現在目前可借館藏中。
13. 全部驗證通過後建立新的 Loan。
14. Repository 只執行一次 AddLoan 與一次 SaveLoans。
15. Console 顯示結果訊息，成功後重新載入讀者明細。

## 8. 借閱規則與結果狀態

新增借閱沿用既有 enum description 模式，使用 LoanCreationStatus 表示結果。

目前已確認的狀態如下：

- Success
- PatronNotFound
- MembershipExpired
- BookItemNotFound
- BookItemUnavailable
- LoanLimitReached
- Error

這些狀態同時作為 ApplicationCore 回傳結果與 Console 顯示訊息來源。

## 9. 資料一致性要求

- 失敗時不得建立新的 Loan
- 失敗時不得留下部分更新
- 新增借閱不得修改 BookItem JSON 結構
- 只在所有驗證通過後才執行資料寫入
- 新增資料寫入流程只有一次 AddLoan 與一次 SaveLoans

本次設計可降低部分更新風險，但不處理跨程序同時寫入 JSON 的併發問題。

## 10. 本次不處理的功能

以下功能明確不在本次範圍內：

- 新增 BookItem 借出狀態欄位
- 修改 Book 或 BookItem 的資料模型來儲存可借狀態
- 將新增借閱改寫成續借或 UpdateLoan
- 加入館藏搜尋、分頁、排序等進階借閱清單功能
- 重構既有還書流程
- 重構既有續借流程
- 引入資料庫或其他外部儲存系統
- 處理跨程序 JSON 併發寫入
- 修改與需求無關的畫面或流程

## 11. 驗證要求

本次功能需要滿足以下驗證方向：

- 讀者有 0 到 4 本未歸還借閱時可以繼續借閱
- 第 5 本借閱可以成功
- 已有 5 本未歸還借閱時，第 6 本必須被拒絕
- 已歸還借閱不計入上限
- 已借出的館藏不可重複借出
- 拒絕時不得建立新的 Loan
- 拒絕時不得留下部分更新
- Console 需要顯示清楚的成功或失敗訊息
- 原有測試必須繼續通過

## 12. 尚待確認事項

目前無尚待確認事項。

## 13. 實際實作結果

### 實際實作結果

目前已可直接從程式碼與測試確認，新增借閱功能已依本文件範圍落地，實際結果如下：

- `LoanService.CreateLoan(int patronId, int bookItemId)` 已實作，並作為新增借閱的唯一業務規則入口
- `LoanCreationStatus` 已實作，包含 `Success`、`PatronNotFound`、`MembershipExpired`、`BookItemNotFound`、`BookItemUnavailable`、`LoanLimitReached`、`Error`
- `ILoanRepository` / `JsonLoanRepository` 已新增 `AddLoan`、`GetBookItem`、`GetAvailableBookItems`
- `JsonLoanRepository.AddLoan()` 會以目前最大 `Loan.Id + 1` 指派新 ID，寫回 runtime `Loans.json`，再重新載入資料
- `JsonLoanRepository.GetAvailableBookItems()` 會以 `ReturnDate == null` 的借閱資料推導哪些館藏不可借
- Console 已新增 `BorrowBook` 狀態與 `b` 操作入口
- 使用者可從 `PatronDetails` 進入借閱流程、查看可借館藏清單、選取館藏，並建立借閱
- 若目前沒有可借館藏，Console 會顯示提示訊息並回到讀者詳情
- 借閱成功或失敗後，Console 都會顯示狀態訊息；成功後會重新載入讀者明細
- 新增借閱的單元測試與 Infrastructure 測試已補上，且目前整體測試可通過

## 14. 規劃與實作對照

### 與原始 Plan 的差異

本次實作與原始 Plan 大致一致，沒有明顯偏離核心設計的地方，但有幾點在落地後可以更精確地描述：

- 原始 Plan 以「需要新增的能力」描述目標；目前這些能力都已正式存在於程式碼中，不再只是規劃
- 原始 Plan 說明 Repository 只提供最小必要能力；實際實作也維持這個方向，沒有額外加入搜尋、排序、分頁或館藏狀態欄位
- 原始 Plan 提到「所有驗證完成後只執行一次 AddLoan 與一次 SaveLoans」；實際程式碼中 `LoanService` 只呼叫一次 `AddLoan`，而 `SaveLoans` 由 `JsonLoanRepository.AddLoan()` 內部負責執行一次
- 原始 Plan 沒有明寫新增借閱後是否立即重新載入整份 JSON；實際上 repository 在寫入後會呼叫 `LoadData()` 重新載入 runtime data，讓後續讀取拿到更新後的關聯資料
- 原始 Plan 強調 Console 要顯示清楚訊息；實際上沿用既有 `EnumHelper.GetDescription(...)` 模式，而不是新增另一套訊息格式

整體來說，這次實作屬於依照原始 Plan 收斂完成，而非中途改規格。

## 15. 收斂後結論

### 最終設計決策

根據目前實作結果，本功能最終採用以下設計決策：

- 新增借閱的商業規則全部集中在 `LoanService.CreateLoan(...)`
- Console 只負責引導使用者流程與顯示結果，不負責借閱規則判斷
- Repository 只提供查詢與持久化能力，不負責判斷是否可借
- 館藏可借狀態不落地為獨立欄位，而是由 active loans 動態推導
- 新借閱建立時固定使用 `LoanDate = DateTime.Now`、`DueDate = LoanDate.AddDays(14)`、`ReturnDate = null`
- 新借閱 ID 由 `JsonLoanRepository.AddLoan()` 以最大 ID 遞增產生
- 借閱資料只寫入 runtime JSON，不直接改寫 seed data
- 拒絕借閱時不建立資料、不更新館藏欄位，也不改變讀者既有借閱狀態

## 16. 風險與限制

### 已知限制

- 目前只有 Console 互動流程，沒有 Web API、資料庫或前端介面
- 借閱流程必須從讀者明細進入，不能直接用讀者 ID 與館藏 ID 發起借閱
- 可借館藏清單沒有搜尋、排序、分頁或篩選機制
- 館藏是否可借完全由未歸還借閱推導，沒有獨立的館藏借出狀態欄位
- JSON 寫入流程未處理跨程序並行寫入、檔案鎖定或衝突解決
- runtime data 的實際位置仍依賴執行時工作目錄解析
- 借閱規則目前只涵蓋會員有效性、借閱上限與館藏是否已借出，未包含預約、罰款、損壞、遺失或館藏條件限制

## 17. 後續建議

### 後續可改善事項

- 為 `BorrowBook` 流程補上更直接的整合測試或互動流程測試，降低只靠 service/repository 測試的落差
- 在可借館藏清單中加入基本搜尋或排序，避免館藏數量增加後操作性下降
- 將 runtime data 路徑解析方式收斂為更明確的 Repository root 或設定值，降低工作目錄差異造成的混淆
- 若未來需要多人操作，應改用可處理併發的持久化機制，而不是直接共享 JSON 檔
- 若借閱規則會持續擴充，可把「可借判斷」拆成更明確的規則物件或 policy，讓 `LoanService` 更容易維護
- 若使用情境擴大，可補上書籍搜尋、館藏搜尋與更完整的借閱操作導引
