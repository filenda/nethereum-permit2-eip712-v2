using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Models.Shared;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services.Implementations
{
    public class SwapService : ISwapService
    {
        private readonly AggregatorFactory _aggregatorFactory;
        private readonly IPriceService _priceService;
        private readonly AppSettings _appSettings;
        
        public SwapService(
            AggregatorFactory aggregatorFactory,
            IPriceService priceService,
            IOptions<AppSettings> appSettings)
        {
            _aggregatorFactory = aggregatorFactory;
            _priceService = priceService;
            _appSettings = appSettings.Value;
        }

        public async Task<string> SwapTokensAsync(string sellTokenAddress, string buyTokenAddress, decimal amount)
        {
            // Get wallet address from private key
            string walletAddress = _appSettings.GetWalletAddress();
            
            // Get token names for better logging
            string sellTokenName = sellTokenAddress == _appSettings.BrlaTokenAddress ? "BRLA" : "USDC";
            string buyTokenName = buyTokenAddress == _appSettings.BrlaTokenAddress ? "BRLA" : "USDC";

            Console.WriteLine("----------------------------------------------");
            Console.WriteLine($"Starting swap: {amount} {sellTokenName} to {buyTokenName}");
            Console.WriteLine($"Using wallet address: {walletAddress}");
            Console.WriteLine("----------------------------------------------");

            try
            {
                // Determine USD value of the swap
                decimal amountInUsd = await _priceService.GetTokenValueInUsdAsync(sellTokenAddress, amount);
                Console.WriteLine($"Estimated value: ${amountInUsd:F2} USD");

                // Select the appropriate aggregator based on the USD threshold
                var aggregator = _aggregatorFactory.CreateAggregator(amountInUsd);
                
                // Create quote request
                var quoteRequest = new QuoteRequest
                {
                    SellTokenAddress = sellTokenAddress,
                    BuyTokenAddress = buyTokenAddress,
                    Amount = amount,
                    WalletAddress = walletAddress,
                    ChainId = _appSettings.ChainId
                };

                // Get quote
                var quote = await aggregator.GetQuoteAsync(quoteRequest);
                
                // Display quote information
                if (quote.Sources?.Length > 0)
                {
                    Console.WriteLine($"Quote received from sources: {string.Join(", ", quote.Sources)}");
                }
                
                Console.WriteLine($"Expected output: {decimal.Parse(quote.BuyAmount) / (decimal)Math.Pow(10, buyTokenName == "BRLA" ? 18 : 6)} {buyTokenName}");
                Console.WriteLine($"Estimated gas: {quote.EstimatedGas}");

                // Create swap request
                var swapRequest = new SwapRequest
                {
                    SellTokenAddress = sellTokenAddress,
                    BuyTokenAddress = buyTokenAddress,
                    Amount = amount,
                    WalletAddress = walletAddress,
                    ChainId = _appSettings.ChainId,
                    PrivateKey = _appSettings.PrivateKey,
                    SlippagePercentage = 1.0m, // 1% slippage
                    QuoteData = quote
                };
                
                // Execute the swap (handles approval if needed)
                Console.WriteLine("Executing swap transaction...");
                var swapResponse = await aggregator.ExecuteSwapAsync(swapRequest);
                
                if (swapResponse.Success)
                {
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine("Swap completed successfully!");
                    Console.WriteLine($"Transaction hash: {swapResponse.TransactionHash}");
                    Console.WriteLine($"Gas used: {swapResponse.GasUsed}");
                    Console.WriteLine($"You received: {swapResponse.BuyAmount} {buyTokenName}");
                    Console.WriteLine($"Check transaction: {swapResponse.BlockExplorerUrl}");
                    Console.WriteLine("----------------------------------------------");
                    return swapResponse.TransactionHash;
                }
                else
                {
                    throw new Exception($"Swap failed: {swapResponse.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine($"Swap failed with error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                Console.WriteLine("----------------------------------------------");
                throw;
            }
        }
    }
}