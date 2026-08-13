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
