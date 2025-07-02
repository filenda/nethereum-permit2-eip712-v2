using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services.Implementations
{
    /// <summary>
    /// Service for retrieving token prices from external APIs
    /// </summary>
    public class PriceService : IPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;
        private readonly IMemoryCache _cache;

        public PriceService(
            IHttpClientFactory httpClientFactory,
            IOptions<AppSettings> appSettings,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("PriceAPI");
            _appSettings = appSettings.Value;
            _cache = cache;
        }

        public async Task<decimal> GetTokenValueInUsdAsync(string tokenAddress, decimal amount)
        {
            try
            {
                // For simplicity, if the token is USDC, we can use its value directly
                if (tokenAddress.Equals(_appSettings.UsdcTokenAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return amount; // USDC is 1:1 with USD
                }

                // For BRLA, fetch the current price
                if (tokenAddress.Equals(_appSettings.BrlaTokenAddress, StringComparison.OrdinalIgnoreCase))
                {
                    // Check cache first (5 min expiration)
                    string cacheKey = $"price_{tokenAddress}_usd";
                    if (!_cache.TryGetValue(cacheKey, out decimal tokenPrice))
                    {
                        tokenPrice = await FetchBrlaPrice();

                        // Cache the price for 5 minutes
                        _cache.Set(cacheKey, tokenPrice, TimeSpan.FromMinutes(5));
                    }

                    return amount * tokenPrice;
                }

                // For other tokens, throw an exception
                throw new ArgumentException($"Token price not available for address: {tokenAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating USD value: {ex.Message}");
                // If all else fails, return a value that will use the more secure option (0x)
                return _appSettings.Aggregator.ThresholdAmountInUsd + 1m;
            }
        }

        private async Task<decimal> FetchBrlaPrice()
        {
            try
            {
                // Try primary price source (CoinGecko)
                var response = await _httpClient.GetAsync("https://api.coingecko.com/api/v3/simple/price?ids=brazilian-real&vs_currencies=usd");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var priceData = JsonConvert.DeserializeObject<dynamic>(content);
                    return priceData["brazilian-real"]["usd"];
                }

                // Try secondary source if first fails
                response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exchangeData = JsonConvert.DeserializeObject<dynamic>(content);
                    // BRL is Brazilian Real (national currency)
                    // Assuming a conversion factor between BRL and BRLA token
                    decimal brlUsdRate = 1 / (decimal)exchangeData.rates.BRL;
                    return brlUsdRate * 0.95m; // Assume BRLA trades at a 5% discount to BRL
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching BRLA price: {ex.Message}");
            }

            // Fallback to a conservative estimate for BRLA
            return 0.20m; // 1 BRLA = 0.2 USD (conservative estimate)
        }
    }
}