using System.Net.Http.Json;

namespace Orders.Api.Services;

public interface IDiscountClient
{
    Task<decimal> GetDiscountAsync(string code, CancellationToken ct);
}

public class DiscountClient : IDiscountClient
{
    private readonly HttpClient _http;

    public DiscountClient(HttpClient http) => _http = http;

    public async Task<decimal> GetDiscountAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0m;

        try
        {
         
            var discounts = await _http.GetFromJsonAsync<List<DiscountDto>>("discounts", ct);

           
            var match = discounts?.FirstOrDefault(d =>
                d.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

            return match?.Percent ?? 0m;
        }
        catch
        {
           
            return 0m;
        }
    }

    private record DiscountDto(string Code, decimal Percent);
}
