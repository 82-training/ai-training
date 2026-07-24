namespace OrderHub.Core.Services;

public class LowStockProduct
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public int Last30DaysSoldQuantity { get; init; }
}
