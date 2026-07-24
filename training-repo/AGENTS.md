# OrderHub — 專案記憶

## 專案簡介

OrderHub 是公司內部訂單管理系統：可管理客戶與商品、建立、查詢及取消訂單。
這是單一 ASP.NET Core MVC 網站與單一 SQL Server 資料庫；維持既有簡單分層，不引入不必要的架構。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views）
- EF Core 8.0.11 + SQL Server
- 測試：xUnit 2.5.3、EF Core InMemory

## 結構與慣例

- `src/OrderHub.Web`：Controller、Razor View、ViewModel、DI 設定。
- `src/OrderHub.Core`：Domain、Service、Repository 介面、`ServiceResult<T>` 與 `PagedResult<T>`。
- `src/OrderHub.Infrastructure`：`DbContext`、EF Core repository、migrations 與 seed data。
- `tests/OrderHub.Tests`：xUnit 單元測試；沿用 `TestSetup` 的 InMemory context 和 factory helpers。
- Controller 負責 HTTP、`ModelState`、轉換 ViewModel 與 redirect；商業規則放在 Core service。
- 只有 Infrastructure repository 可直接使用 `OrderHubDbContext` / EF Core；Web 與 Core service 不可直接碰它。
- 預期內的業務失敗回傳 `ServiceResult<T>`；Controller 將 `Errors` 加到 ModelState 或 TempData，不用例外處理使用者輸入。
- Razor View 只綁 Web ViewModel，不直接綁 Domain entity。
- 使用者輸入用 DataAnnotations 與 ModelState 驗證，無效輸入不可造成 HTTP 500。
- 金額一律使用 `decimal`。折扣率與訂單總額計算維持在 `OrderService` 的 `GetDiscountRate`、`CalculateSubtotal`、`CalculateTotal`。
- 先閱讀並沿用 `ProductsController` / `ProductService` / `ProductRepository` 的命名與非同步寫法。

## 常用指令

- `dotnet build OrderHub.sln`
- `dotnet test OrderHub.sln`
- `dotnet run --project src/OrderHub.Web`

## 重要檔案與護欄

- `src/OrderHub.Infrastructure/Migrations/**` 是 migration 歷史；不手動修改。
- `src/OrderHub.Web/appsettings.json` 與 `appsettings.Development.json` 含環境設定；修改前先取得使用者同意。
- 不讀取或寫入 `*.pfx`、`appsettings.Production.json`、user-secrets 或其他機密檔。
- 未經使用者同意，不新增 NuGet 套件、不做與目前任務無關的重構、不改變既有資料庫資料。
- 修 bug 時採最小變更，補能在修復前失敗的回歸測試；一個修復一個獨立 commit。
