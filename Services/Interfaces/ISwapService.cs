using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services.Interfaces
{
    /// <summary>
    /// Service for executing token swaps
    /// </summary>
    public interface ISwapService
    {
        /// <summary>
        /// Swaps tokens using the appropriate DEX aggregator
        /// </summary>
        /// <param name="sellTokenAddress">Address of the token to sell</param>
        /// <param name="buyTokenAddress">Address of the token to buy</param>
        /// <param name="amount">Amount of token to sell</param>
        /// <returns>Transaction hash if successful</returns>
        Task<string> SwapTokensAsync(string sellTokenAddress, string buyTokenAddress, decimal amount);
    }
}