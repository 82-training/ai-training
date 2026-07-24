# 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

- Codex（本次練習環境）
---

## 通用四問

### 1. 我的任務拆解

- 每一題先找「會被影響的那一層」。例如低庫存頁面是 Controller、Service、Repository、ViewModel、View 和測試都會碰到；重構則只限在 `OrderService` 和建立訂單的測試，避免愈改愈大。
- 碰到 bug 時，我不只用一句「它壞了」去問，而是先舉個例子，例如：把數字講清楚：21 筆資料配每頁 20 筆、Gold 商品 1000 元變成 810、庫存 10 下單 3 件後取消還是 7。這樣定位比較快，也比較知道修完到底有沒有對。
- 做新功能和重構前，我都先要求一份計畫：要改哪些檔、每層誰負責、怎麼測。計畫看過沒有超出範圍，才開始動手。

### 2. AI 幫上大忙的地方

- 最有幫助的是快速讀專案。先把 `ProductsController → ProductService → ProductRepository`、`OrderService` 和測試看過一次後，我很快就有一張地圖，知道查詢不能塞進 Controller、EF Core 要留在 Repository。
- 修 bug 時，它能把症狀接到實際程式碼和測試上。像分頁的 `Skip`、Gold 折扣重複套用、取消訂單漏回補庫存，都是先用測試鎖住現象，再做修改。
- 做低庫存頁面時，先出計畫真的有差。我可以先檢查它有沒有把近 30 天銷量放在 Repository、有沒有排除 Cancelled、會不會 N+1，以及 `threshold <= 0` 是不是會回表單錯誤，而不是等程式寫完才發現分層跑掉。
- 安全設定也不是只有寫文件而已：`git push --force` 被規則判成 `forbidden`，含 `TRUNCATE` 的 hook 回傳 exit code 2。這讓我知道護欄真的有在工作。

### 3. AI 誤導我的地方，與我如何發現

- 我最大的教訓是：測試全綠不等於所有東西都驗證完。低庫存頁面的 service 測試是通過的，但網站啟動時被本機 SQL Server 的加密連線擋住，所以 GET 表單、URL 和畫面訊息不能假裝已經在瀏覽器驗過。
- 一開始 hook 的模擬也踩過坑：指令看起來有跑，但 JSON 沒真的送到標準輸入，所以 `edit-log.txt` 沒出現。後來是我去看副作用、改用正確方式重測，才確認 hook 真正有作用。

### 4. 我會帶回日常工作的做法

- 以後我會固定先講三件事：要改什麼、不能碰什麼、怎樣才算驗證完成。尤其跨很多層的功能，先看計畫比直接叫工具開寫可靠很多。
- 修 bug 時我會保留可以重現的數字和步驟，先讓測試失敗，再修到它通過；做完也要補看相鄰情境，不只看那一個案例。
- 對危險操作，我會繼續用專案規則和 hook 擋住，而不是只靠 prompt 提醒。版本控制也維持先看差異、自己決定 stage、commit 和 push。

## 自我驗證（做到哪個階段答哪項）

### 第一階段 — Agentic Coding

練習 1

1. 我可以不看筆記說出三層責任：Web 放 Controller、Razor View 與 ViewModel；Core 放 Domain、Service、介面與共用結果型別；Infrastructure 放 DbContext、Repository、migration 和 seed data。
2. 我核對過設定與實際程式碼，發現「Service 一律回傳 `ServiceResult<T>`」不能照字面套用：`IProductService.GetAllAsync` 和 `GetActiveAsync` 是直接回傳清單；較精確的規則是「預期的業務失敗用 `ServiceResult<T>`，單純讀取可直接回傳資料」。我也以 `codex execpolicy check` 驗證 `git push --force` 的決策確實是 `forbidden`，並以含 `TRUNCATE` 的輸入驗證 SQL hook 回傳 exit code 2。
3. 我知道商業邏輯應放 Core Service；若要新增頁面，通常會調整 Controller action、Service 與介面、Repository 與介面、Web ViewModel、Razor View / 導覽列，以及 service 層測試。Controller 不應直接寫 EF Core 查詢。這個判斷也和目前 `ProductsController → ProductService → ProductRepository` 的實作相符。

練習 2

1. 我已用回歸測試重現三個 bug：21 筆訂單、每頁 20 筆時舊版第一頁只回傳 1 筆；Gold 1000 元商品舊版總額為 810；庫存 10 的商品下單 3 件後取消，舊版庫存維持 7。瀏覽器頁面的人工重現與修後實測尚未完成，這一項不能勾選為完成。
2. 我提供的脈絡包含具體數字與條件，而不是只貼客訴：分頁為 21 筆／每頁 20 筆、Gold 為 1000 × 1 且預期 900、庫存為 10 → 7 → 取消後應回 10。
3. 我已用測試驗證三個症狀的修復，但尚未回到瀏覽器逐項驗證；待網站可操作時，要確認新訂單位於第一頁、Gold 明細為原價 × 0.9、取消後商品頁庫存恢復。
4. 每個 bug 都補了可在修正前失敗的回歸測試；目前 `dotnet test OrderHub.sln --no-restore` 的實際結果為 32/32 通過。
5. Bug 1 與 Bug 2 已各有獨立 commit（`練習2 bug 1`、`練習2 bug 2`）；Bug 3 目前尚未 commit，所以這一項仍待完成。
6. 原本測試沒有抓到問題，是因為它們只測了局部行為：分頁測試只確認狀態篩選與總頁數，沒有檢查每頁內容；價格測試只測 `CalculateTotal`，沒有走過 `CreateOrderAsync` 到訂單明細的完整流程；取消測試只確認狀態改為 Cancelled，沒有檢查庫存是否回補。

練習 3

1. `LowStockViewModel.Threshold` 的預設值是 10，Controller 使用 GET model binding；`?threshold=3` 的路由與實際顯示結果原本安排以本機網站驗證，但網站啟動時因本機 SQL Server 的加密需求與目前環境不相容而中止，尚未把這項 UI smoke test 標為通過。
2. `Threshold` 有 `[Range(1, int.MaxValue)]`，Controller 在 `ModelState` 無效時直接回同一個 View；Service 也會拒絕 0 與負數。Service 的 0、-1 測試已通過；頁面的驗證訊息同樣受上述本機資料庫啟動問題影響，待環境可連線後再確認。
3. service 測試建立一筆 29 天內 Confirmed、10 天內 Cancelled、31 天前 Shipped 的訂單，查詢結果只計入 Confirmed 的 2 件；因此已驗證 Cancelled 與過期訂單被排除。
4. service 測試確認庫存很低但 `IsActive = false` 的商品不會出現在結果中。
5. 我審查過 diff：Controller 只處理 ModelState、呼叫 Service 與轉成 ViewModel；Service 不使用 EF Core；`ProductRepository` 使用單一 aggregate projection 計算銷量；Razor View 只綁 ViewModel。沒有新增 migration 或套件。
6. 新增四個 service 測試（門檻與排序、停售排除、近 30 天銷量、無效門檻）；實際 `dotnet test OrderHub.sln --no-restore` 為 37/37 通過，`dotnet build OrderHub.sln --no-restore` 為 0 warnings、0 errors。

練習 4

1. 重構後我執行 `dotnet test OrderHub.sln --no-restore`，結果為 38/38 通過；接著執行 `dotnet build OrderHub.sln --no-restore`，結果為 0 warnings、0 errors。
2. 這次改善的是 `CreateOrderAsync` 的可讀性：主方法現在只負責流程編排，明細的基本規則抽到 `ValidateOrderLines`，逐筆商品／庫存檢查和建立明細抽到 `ValidateAndAddOrderItemsAsync`。沒有改變的是客戶查詢、驗證順序、既有錯誤訊息、庫存扣減、價格 snapshot、儲存時機與 `ServiceResult<Order>` 的回傳方式；也沒有修改 Controller、Repository、資料庫 schema 或公開介面。
3. 我逐項檢查重構後的兩個私有方法：基本驗證仍先檢查空明細、數量、重複商品；逐筆處理仍會累積停售／不存在與庫存不足的錯誤，成功時才加入相同欄位的 `OrderItem`。另外補了「兩筆錯誤明細會一起回傳兩個錯誤」的測試，避免重構後意外改成只回第一個錯誤。


---

## 附錄：值得留下的對話片段

> 我：「先不要寫程式，先列出要改哪些檔、每一層放什麼、怎麼測。」

這句在做低庫存頁面時很好用。先把 Controller、Service、Repository、ViewModel、View 和測試列出來，我可以先抓出有沒有把 EF Core 放錯層、驗證有沒有漏掉、查詢會不會變成 N+1。

> 我：「21 筆、每頁 20 筆，第一頁只看到 1 筆；我要先用這個現象重現。」

這提醒我，報 bug 要講實際數字。後來 Gold 的 1000 變 810、庫存 10 取消後仍是 7，也都是同樣做法：先把現象講準，再談根因和修法。

> 我：「不要幫我 commit，所有東西讓我自己來 commit。」

這句是我的權限界線。程式完成不等於可以直接提交；我會先檢查差異、測試結果和 commit message，再自己決定要不要送出。

---
