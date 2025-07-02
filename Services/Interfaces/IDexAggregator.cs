using BrlaUsdcSwap.Models.Shared;
using System.Threading.Tasks;

namespace BrlaUsdcSwap.Services.Interfaces
{
    /// <summary>
    /// Interface for DEX aggregator services that provides abstraction
    /// over different aggregator implementations (0x, 1inch, etc.)
    /// </summary>
    public interface IDexAggregator
    {
        /// <summary>
        /// Gets the name of the aggregator implementation
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a swap quote from the aggregator
        /// </summary>
        Task<QuoteResponse> GetQuoteAsync(QuoteRequest request);

        /// <summary>
        /// Executes a token swap transaction
        /// </summary>
        Task<SwapResponse> ExecuteSwapAsync(SwapRequest request);

        /// <summary>
        /// Gets approval transaction data for token spending
        /// </summary>
        Task<ApprovalResponse> GetApprovalDataAsync(ApprovalRequest request);

        /// <summary>
        /// Checks if approval is needed for the given token and amount
        /// </summary>
        Task<bool> NeedsApprovalAsync(ApprovalRequest request);

        /// <summary>
        /// Gets the contract address that requires approval for token spending
        /// </summary>
        string GetSpenderAddress(int chainId);
    }
}