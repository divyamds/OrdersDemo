namespace Orders.Api.Data;

public record Customer(int Id, string Name, string Email);
public record Product(int Id, string Name, decimal Price, int Stock, int ConcurrencyToken);
public record OrderItem(int ProductId, int Quantity, decimal UnitPrice);
public record Order(int Id, int CustomerId, string CustomerName, DateOnly Date, decimal Subtotal, decimal DiscountApplied, decimal Total, List<OrderItem> Items);
