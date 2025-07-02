// Models/Shared/QuoteRequest.cs
using System.Collections.Generic;

namespace BrlaUsdcSwap.Models.Shared
{
    public class QuoteRequest
    {
        /// <summary>
        /// Address of the token to sell
        /// </summary>
        public string SellTokenAddress { get; set; }

        /// <summary>
        /// Address of the token to buy
        /// </summary>
        public string BuyTokenAddress { get; set; }

        /// <summary>
        /// Amount of sell token to swap (in human-readable format)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// User's wallet address
        /// </summary>
        public string WalletAddress { get; set; }

        /// <summary>
        /// Chain ID (137 for Polygon)
        /// </summary>
        public int ChainId { get; set; }

        /// <summary>
        /// Additional parameters specific to an aggregator implementation
        /// </summary>
        public Dictionary<string, string> AdditionalParams { get; set; } = new Dictionary<string, string>();
    }
}

// Models/Shared/QuoteResponse.cs
namespace BrlaUsdcSwap.Models.Shared
{
    public class QuoteResponse
    {
        /// <summary>
        /// Address of the token being sold
        /// </summary>
        public string SellTokenAddress { get; set; }

        /// <summary>
        /// Address of the token being purchased
        /// </summary>
        public string BuyTokenAddress { get; set; }

        /// <summary>
        /// Amount of sell token in wei/smallest unit
        /// </summary>
        public string SellAmount { get; set; }

        /// <summary>
        /// Amount of buy token in wei/smallest unit
        /// </summary>
        public string BuyAmount { get; set; }

        /// <summary>
        /// Estimated gas for the swap
        /// </summary>
        public string EstimatedGas { get; set; }

        /// <summary>
        /// Gas price in wei
        /// </summary>
        public string GasPrice { get; set; }

        /// <summary>
        /// Whether token approval is required
        /// </summary>
        public bool RequiresApproval { get; set; }

        /// <summary>
        /// Protocol fee (if any) in sell token units
        /// </summary>
        public string ProtocolFee { get; set; }

        /// <summary>
        /// Liquidity sources used for the quote
        /// </summary>
        public string[] Sources { get; set; }

        /// <summary>
        /// For 0x Permit2 signature data or similar
        /// </summary>
        public object PermitData { get; set; }

        /// <summary>
        /// Raw transaction data from the aggregator
        /// </summary>
        public object RawTransaction { get; set; }

        /// <summary>
        /// Original response object from the aggregator
        /// </summary>
        public object OriginalResponse { get; set; }
    }
}

// Models/Shared/SwapRequest.cs
namespace BrlaUsdcSwap.Models.Shared
{
    public class SwapRequest
    {
        /// <summary>
        /// Address of the token to sell
        /// </summary>
        public string SellTokenAddress { get; set; }

        /// <summary>
        /// Address of the token to buy
        /// </summary>
        public string BuyTokenAddress { get; set; }

        /// <summary>
        /// Amount of sell token to swap (in human-readable format)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// User's wallet address
        /// </summary>
        public string WalletAddress { get; set; }

        /// <summary>
        /// Chain ID (137 for Polygon)
        /// </summary>
        public int ChainId { get; set; }

        /// <summary>
        /// User's private key for signing transactions
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Maximum allowed slippage percentage (default 1%)
        /// </summary>
        public decimal SlippagePercentage { get; set; } = 1.0m;

        /// <summary>
        /// Optional quote data from a previous quote request
        /// </summary>
        public QuoteResponse QuoteData { get; set; }
    }
}

// Models/Shared/SwapResponse.cs
namespace BrlaUsdcSwap.Models.Shared
{
    public class SwapResponse
    {
        /// <summary>
        /// Whether the swap was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Transaction hash of the swap
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// Error message if the swap failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Amount of tokens sold
        /// </summary>
        public decimal SellAmount { get; set; }

        /// <summary>
        /// Amount of tokens received
        /// </summary>
        public decimal BuyAmount { get; set; }

        /// <summary>
        /// Gas used for the transaction
        /// </summary>
        public decimal GasUsed { get; set; }

        /// <summary>
        /// URL to view the transaction on a block explorer
        /// </summary>
        public string BlockExplorerUrl { get; set; }
    }
}

// Models/Shared/ApprovalRequest.cs
namespace BrlaUsdcSwap.Models.Shared
{
    public class ApprovalRequest
    {
        /// <summary>
        /// Address of the token to approve
        /// </summary>
        public string TokenAddress { get; set; }

        /// <summary>
        /// Address of the token owner
        /// </summary>
        public string OwnerAddress { get; set; }

        /// <summary>
        /// Address of the spender (router contract)
        /// </summary>
        public string SpenderAddress { get; set; }

        /// <summary>
        /// Amount to approve (in human-readable format)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Chain ID (137 for Polygon)
        /// </summary>
        public int ChainId { get; set; }

        /// <summary>
        /// Private key for signing the approval transaction
        /// </summary>
        public string PrivateKey { get; set; }
    }
}

// Models/Shared/ApprovalResponse.cs
namespace BrlaUsdcSwap.Models.Shared
{
    public class ApprovalResponse
    {
        /// <summary>
        /// Whether the approval was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Transaction hash of the approval transaction
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// Approval transaction data
        /// </summary>
        public string ApprovalData { get; set; }

        /// <summary>
        /// Error message if the approval failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Raw transaction object
        /// </summary>
        public object RawTransaction { get; set; }
    }
}