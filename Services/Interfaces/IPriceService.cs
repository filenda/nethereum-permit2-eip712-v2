using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services.Interfaces
{
    /// <summary>
    /// Service for retrieving token prices
    /// </summary>
    public interface IPriceService
    {
        /// <summary>
        /// Gets the USD value of a token amount
        /// </summary>
        /// <param name="tokenAddress">Token contract address</param>
        /// <param name="amount">Token amount</param>
        /// <returns>USD value</returns>
        Task<decimal> GetTokenValueInUsdAsync(string tokenAddress, decimal amount);
    }
}