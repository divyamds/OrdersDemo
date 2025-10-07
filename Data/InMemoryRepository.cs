using System.Collections.Concurrent;

namespace Orders.Api.Data;

public class InMemoryRepository
{
    private readonly ConcurrentDictionary<int, Customer> _customers = new();
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private readonly ConcurrentDictionary<int, Order> _orders = new();
    private int _custId = 0, _prodId = 0, _orderId = 0;
    private readonly Random _rng = new();

    public InMemoryRepository()
    {
        AddCustomer(new Customer(0, "Alice", "alice@example.com"));
        AddCustomer(new Customer(0, "Bob", "bob@example.com"));
        AddProduct(new Product(0, "Pen", 10m, 100, 1));
        AddProduct(new Product(0, "Notebook", 50m, 50, 1));
        AddProduct(new Product(0, "Backpack", 900m, 10, 1));
        AddProduct(new Product(0, "Crayons", 10m, 20, 1));
        AddProduct(new Product(0, "WaterBottle", 150m, 50, 1));
        AddProduct(new Product(0, "TiffinBox", 500m, 10, 1));
    }

    private async Task SimulateLatency() => await Task.Delay(_rng.Next(5, 25));

    //  Customers 
    public async Task<Customer> AddCustomer(Customer c)
    {
        await SimulateLatency();
        var id = Interlocked.Increment(ref _custId);
        var nc = c with { Id = id };
        _customers[id] = nc;
        return nc;
    }

    public Task<IReadOnlyList<Customer>> ListCustomers() =>
        Task.FromResult<IReadOnlyList<Customer>>(_customers.Values.OrderBy(x => x.Id).ToList());

    public Task<Customer?> GetCustomer(int id) =>
        Task.FromResult(_customers.GetValueOrDefault(id));

    // --- Products ---
    public async Task<Product> AddProduct(Product p)
    {
        await SimulateLatency();
        var id = Interlocked.Increment(ref _prodId);
        var np = p with { Id = id, ConcurrencyToken = 1 };
        _products[id] = np;
        return np;
    }

    public Task<IReadOnlyList<Product>> ListProducts() =>
        Task.FromResult<IReadOnlyList<Product>>(_products.Values.OrderBy(x => x.Id).ToList());

    public Task<Product?> GetProduct(int id) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public async Task<bool> UpdateProduct(Product updated, int expectedToken)
    {
        await SimulateLatency();
        var ok = false;
        _products.AddOrUpdate(updated.Id,
            _ => updated,
            (_, existing) =>
            {
                if (existing.ConcurrencyToken != expectedToken) { ok = false; return existing; }
                ok = true;
                return updated with { ConcurrencyToken = existing.ConcurrencyToken + 1 };
            });
        return ok;
    }

    public bool DeleteProduct(int id) => _products.TryRemove(id, out _);

    // Orders 
    public async Task<Order> AddOrder(Order o)
    {
        await SimulateLatency();
        var id = Interlocked.Increment(ref _orderId);
        var no = o with { Id = id };
        _orders[id] = no;
        return no;
    }

    public Task<(IReadOnlyList<Order> Items, int Total)> QueryOrders(
        int? customerId, DateOnly? from, DateOnly? to, int page, int pageSize)
    {
        var q = _orders.Values.AsQueryable();
        if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId);
        if (from.HasValue) q = q.Where(o => o.Date >= from);
        if (to.HasValue) q = q.Where(o => o.Date <= to);
        var total = q.Count();
        var items = q.OrderByDescending(o => o.Date).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Order>)items, total));
    }
}
