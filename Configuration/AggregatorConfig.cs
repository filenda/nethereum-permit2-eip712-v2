// Configuration/AggregatorConfig.cs
namespace BrlaUsdcSwap.Configuration
{
    public class AggregatorConfig
    {
        /// <summary>
        /// Threshold amount in USD for deciding between aggregators
        /// Under this amount: 1inch, Over this amount: 0x
        /// </summary>
        public decimal ThresholdAmountInUsd { get; set; } = 100.0m;

        /// <summary>
        /// 0x API base URL
        /// </summary>
        public string ZeroExApiBaseUrl { get; set; } = "https://polygon.api.0x.org/";

        /// <summary>
        /// 0x API key
        /// </summary>
        public string ZeroExApiKey { get; set; }

        /// <summary>
        /// 0x API version
        /// </summary>
        public string ZeroExApiVer { get; set; } = "v2";

        /// <summary>
        /// 1inch API base URL
        /// </summary>
        public string OneInchApiBaseUrl { get; set; } = "https://api.1inch.dev/swap";

        /// <summary>
        /// 1inch API key
        /// </summary>
        public string OneInchApiKey { get; set; }

        /// <summary>
        /// Polygon RPC URL
        /// </summary>
        public string PolygonRpcUrl { get; set; } = "https://polygon-rpc.com";

        /// <summary>
        /// Block explorer base URL
        /// </summary>
        public string BlockExplorerBaseUrl { get; set; } = "https://polygonscan.com/tx/";
    }
}

// Configuration/AppSettings.cs
namespace BrlaUsdcSwap.Configuration
{
    public class AppSettings
    {
        /// <summary>
        /// User's private key for signing transactions
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// BRLA token contract address on Polygon
        /// </summary>
        public string BrlaTokenAddress { get; set; } = "0xe6a537a407488807f0bbeb0038b79004f19dddfb";

        /// <summary>
        /// USDC token contract address on Polygon
        /// </summary>
        public string UsdcTokenAddress { get; set; } = "0x2791bca1f2de4661ed88a30c99a7a9449aa84174";

        /// <summary>
        /// Chain ID (137 for Polygon)
        /// </summary>
        public int ChainId { get; set; } = 137;

        /// <summary>
        /// User's wallet address
        /// </summary>
        public string WalletAddress { get; set; }

        /// <summary>
        /// BRLA token decimals
        /// </summary>
        public int BrlaDecimals { get; set; } = 18;

        /// <summary>
        /// USDC token decimals
        /// </summary>
        public int UsdcDecimals { get; set; } = 6;

        /// <summary>
        /// Aggregator configuration
        /// </summary>
        public AggregatorConfig Aggregator { get; set; } = new AggregatorConfig();
    }
}