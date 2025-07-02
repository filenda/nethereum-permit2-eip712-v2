using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Services.Implementations;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BrlaUsdcSwap
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("   BRLA/USDC DEX Aggregator Swap Application  ");
            Console.WriteLine("==============================================");

            try
            {
                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                // Setup DI
                var serviceProvider = new ServiceCollection()
                    .Configure<AppSettings>(configuration.GetSection("AppSettings"))
                    .AddSingleton<IConfiguration>(configuration)
                    // Add memory cache for price data
                    .AddMemoryCache()
                    // Add HTTP clients
                    .AddHttpClient()
                    .AddHttpClient("ZeroEx", client =>
                    {
                        var zeroExConfig = configuration.GetSection("AppSettings:Aggregator");
                        client.BaseAddress = new Uri(zeroExConfig["ZeroExApiBaseUrl"]);
                        client.DefaultRequestHeaders.Add("0x-api-key", zeroExConfig["ZeroExApiKey"]);
                        client.DefaultRequestHeaders.Add("0x-version", zeroExConfig["ZeroExApiVer"]);
                    })
                    .Services
                    .AddHttpClient("OneInch", client =>
                    {
                        var oneInchConfig = configuration.GetSection("AppSettings:Aggregator");
                        if (!string.IsNullOrEmpty(oneInchConfig["OneInchApiKey"]))
                        {
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {oneInchConfig["OneInchApiKey"]}");
                        }
                    })
                    .Services
                    .AddHttpClient("PriceAPI")
                    .Services
                    // Register services
                    .AddTransient<ZeroExAggregator>()
                    .AddTransient<OneInchAggregator>()
                    .AddTransient<AggregatorFactory>()
                    .AddTransient<IPriceService, PriceService>()
                    .AddTransient<ISwapService, SwapService>()
                    .BuildServiceProvider();

                // Get services
                var swapService = serviceProvider.GetRequiredService<ISwapService>();
                var appSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;

                // Display menu
                Console.WriteLine("Token Swap Options:");
                Console.WriteLine("1. Swap BRLA to USDC");
                Console.WriteLine("2. Swap USDC to BRLA");

                // Get swap direction
                int swapDirection = 1; // Default to BRLA → USDC

                if (args.Length > 1 && int.TryParse(args[1], out int argDirection) && (argDirection == 1 || argDirection == 2))
                {
                    swapDirection = argDirection;
                }
                else
                {
                    Console.Write("Enter your choice (1 or 2): ");
                    if (!int.TryParse(Console.ReadLine(), out swapDirection) || (swapDirection != 1 && swapDirection != 2))
                    {
                        Console.WriteLine("Invalid choice. Using default: 1 (BRLA to USDC)");
                        swapDirection = 1;
                    }
                }

                // Determine token addresses based on swap direction
                string sellTokenAddress = swapDirection == 1 ? appSettings.BrlaTokenAddress : appSettings.UsdcTokenAddress;
                string buyTokenAddress = swapDirection == 1 ? appSettings.UsdcTokenAddress : appSettings.BrlaTokenAddress;
                string sellTokenName = swapDirection == 1 ? "BRLA" : "USDC";
                string buyTokenName = swapDirection == 1 ? "USDC" : "BRLA";

                // Parse amount from command line or ask user
                decimal amountToSwap;
                if (args.Length > 0 && decimal.TryParse(args[0], out amountToSwap))
                {
                    // Use amount from command line
                }
                else
                {
                    Console.Write($"Enter amount of {sellTokenName} to swap: ");
                    if (!decimal.TryParse(Console.ReadLine(), out amountToSwap))
                    {
                        Console.WriteLine("Invalid amount. Please enter a valid number.");
                        return;
                    }
                }

                // Execute the swap
                var result = await swapService.SwapTokensAsync(sellTokenAddress, buyTokenAddress, amountToSwap);

                // Transaction result is displayed by the SwapService
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============================================");
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }

                // Additional error details
                Console.WriteLine("\nTroubleshooting tips:");
                Console.WriteLine("1. Check your token balance");
                Console.WriteLine("2. Ensure you have enough MATIC for gas fees");
                Console.WriteLine("3. Try with a smaller amount");
                Console.WriteLine("4. Verify your API keys are valid");
                Console.WriteLine("5. Check your network connection");
                Console.WriteLine("==============================================");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}