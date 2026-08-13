---
name: create-api
description: Produce an evidence-based Library.Api architecture plan without editing the repository
argument-hint: "[API requirement]"
agent: Library Planner
tools:
  - read
  - search
  - microsoftLearn/*
---

目前 repository 是 .NET 8 Library Console application，沒有 Web API。
請只產生 Library.Api 的架構與實作計畫，不建立 project、不修改 source/test。

需求：
${input:apiRequirement:Expose the existing Library domain capabilities through an ASP.NET Core Controllers API.}

先讀取 repository、AGENTS.md 與 Internal Docs MCP；再使用 Microsoft Learn MCP 查證目前 ASP.NET Core Controllers、ProblemDetails、OpenAPI、health checks 與 rate limiting 的官方建議。輸出每個來源的標題與連結，並區分 repository fact、company standard、official guidance 與 inference。

計畫必須涵蓋：

- Library.Api 作為 transport layer，重用 ApplicationCore／Infrastructure，Controller 不直接做商業判斷。
- GET /health、GET /api/book-items/{id}、POST /api/loans。
- DTO、DI、ProblemDetails、OpenAPI、health checks、rate limiting 與 tests。
- 預計新增／修改檔案、依賴方向、風險、未決事項與驗收條件。

只分析，不建立 `src/Library.Api`，不執行命令，不宣稱未觀察到的結果。

