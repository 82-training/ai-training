using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetLowStock_FiltersByStrictThreshold_AndSortsByStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 4, sku: "SKU-FOUR");
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-TWO");
        TestSetup.AddProduct(db, stock: 5, sku: "SKU-FIVE");

        var result = await service.GetLowStockAsync(5);

        Assert.True(result.Success);
        Assert.Equal(new[] { "SKU-TWO", "SKU-FOUR" }, result.Value!.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 1, sku: "SKU-ACTIVE");
        TestSetup.AddProduct(db, stock: 0, isActive: false, sku: "SKU-INACTIVE");

        var result = await service.GetLowStockAsync(5);

        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("SKU-ACTIVE", result.Value!.Single().Sku);
    }

    [Fact]
    public async Task GetLowStock_CountsRecentNonCancelledSalesOnly()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 1, sku: "SKU-LOW");
        var customer = TestSetup.AddCustomer(db);
        var now = DateTime.UtcNow;

        AddOrderItem(db, customer.Id, product.Id, 2, OrderStatus.Confirmed, now.AddDays(-29));
        AddOrderItem(db, customer.Id, product.Id, 3, OrderStatus.Cancelled, now.AddDays(-10));
        AddOrderItem(db, customer.Id, product.Id, 4, OrderStatus.Shipped, now.AddDays(-31));

        var result = await service.GetLowStockAsync(5);

        Assert.True(result.Success);
        Assert.Equal(2, result.Value!.Single().Last30DaysSoldQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLowStock_NonPositiveThreshold_Fails(int threshold)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var result = await service.GetLowStockAsync(threshold);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    private static void AddOrderItem(
        Infrastructure.Data.OrderHubDbContext db,
        int customerId,
        int productId,
        int quantity,
        OrderStatus status,
        DateTime createdAt)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            Items =
            {
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceSnapshot = 100m
                }
            }
        };

        db.Orders.Add(order);
        db.SaveChanges();
    }
}
