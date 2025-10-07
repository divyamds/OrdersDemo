using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Orders.Api.Data;
using Orders.Api.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly InMemoryRepository _repo;
    private readonly IDiscountClient _discountClient;
    private readonly IMemoryCache _cache;

    public OrdersController(InMemoryRepository repo, IDiscountClient discountClient, IMemoryCache cache)
    {
        _repo = repo;
        _discountClient = discountClient;
        _cache = cache;
    }

    // GET /api/orders?customerId=2&from=2025-09-01&to=2025-09-30&page=1&pageSize=10
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Query(
        [FromQuery] int? customerId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        string cacheKey = $"orders-{customerId}-{from}-{to}-{page}-{pageSize}";
        if (_cache.TryGetValue(cacheKey, out (IReadOnlyList<Order> Items, int Total) cached))
        {
            var etag = $"W/\"{cached.GetHashCode()}\"";
            if (Request.Headers.IfNoneMatch == etag)
                return StatusCode(304);

            Response.Headers.ETag = etag;

            var cachedWithNames = await AddCustomerNames(cached.Items);
            return Ok(new { Items = cachedWithNames, cached.Total });
        }

        var result = await _repo.QueryOrders(customerId, from, to, page, pageSize);
        Response.Headers.ETag = $"W/\"{result.GetHashCode()}\"";

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));

        var ordersWithNames = await AddCustomerNames(result.Items);
        return Ok(new { Items = ordersWithNames, result.Total });
    }

    // POST /api/orders
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        // Validate customer exists
        var customer = await _repo.GetCustomer(dto.CustomerId);
        if (customer == null) return BadRequest($"Customer {dto.CustomerId} not found");

        decimal subtotal = 0m;
        var items = new List<OrderItem>();

        foreach (var i in dto.Items)
        {
            var product = await _repo.GetProduct(i.ProductId);
            if (product == null) return BadRequest($"Product {i.ProductId} not found");
            if (product.Stock < i.Quantity) return BadRequest($"Insufficient stock for product {product.Name}");

            subtotal += product.Price * i.Quantity;
            items.Add(new OrderItem(i.ProductId, i.Quantity, product.Price));

            // Decrement stock
            await _repo.UpdateProduct(product with { Stock = product.Stock - i.Quantity }, product.ConcurrencyToken);
        }

        decimal discountPercent = 0m;
        if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
            discountPercent = await _discountClient.GetDiscountAsync(dto.DiscountCode, CancellationToken.None);

        var discountApplied = subtotal * discountPercent / 100m;
        var total = subtotal - discountApplied;

        var order = new Order(
            Id: 0,
            CustomerId: dto.CustomerId,
             CustomerName: customer.Name,
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            Subtotal: subtotal,
            DiscountApplied: discountApplied,
            Total: total,
            Items: items
        );

        var added = await _repo.AddOrder(order);

        var result = new
        {
            added.Id,
            added.CustomerId,
            CustomerName = customer.Name, 
            added.Date,
            added.Subtotal,
            added.DiscountApplied,
            added.Total,
            added.Items
        };

        return CreatedAtAction(nameof(Query), new { id = added.Id }, result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = (await _repo.QueryOrders(null, null, null, 1, int.MaxValue))
                        .Items.FirstOrDefault(o => o.Id == id);

        if (order == null) return NotFound();

        var customer = await _repo.GetCustomer(order.CustomerId);

        var result = new
        {
            order.Id,
            order.CustomerId,
            CustomerName = customer?.Name ?? "Unknown",
            order.Date,
            order.Subtotal,
            order.DiscountApplied,
            order.Total,
            order.Items
        };

        return Ok(result);
    }

    // Helper method to add customer names
    private async Task<List<object>> AddCustomerNames(IReadOnlyList<Order> orders)
    {
        var list = new List<object>();
        foreach (var o in orders)
        {
            var cust = await _repo.GetCustomer(o.CustomerId);
            list.Add(new
            {
                o.Id,
                o.CustomerId,
                CustomerName = cust?.Name ?? "Unknown",
                o.Date,
                o.Subtotal,
                o.DiscountApplied,
                o.Total,
                o.Items
            });
        }
        return list;
    }

    public record CreateOrderDto(int CustomerId, string? DiscountCode, List<ItemDto> Items);
    public record ItemDto(int ProductId, int Quantity);
}
