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

