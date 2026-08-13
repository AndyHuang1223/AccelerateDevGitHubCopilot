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
