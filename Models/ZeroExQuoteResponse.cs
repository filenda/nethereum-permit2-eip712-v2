using Newtonsoft.Json;
using System.Collections.Generic;

namespace BrlaUsdcSwap.Models
{
    /// <summary>
    /// Response model for the 0x Protocol quote endpoint
    /// </summary>
    public class ZeroExQuoteResponse
    {
        [JsonProperty("chainId")]
        public int ChainId { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("guaranteedPrice")]
        public string GuaranteedPrice { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("gasPrice")]
        public string GasPrice { get; set; }

        [JsonProperty("protocolFee")]
        public string ProtocolFee { get; set; }

        [JsonProperty("minimumProtocolFee")]
        public string MinimumProtocolFee { get; set; }

        [JsonProperty("buyTokenAddress")]
        public string BuyTokenAddress { get; set; }

        [JsonProperty("sellTokenAddress")]
        public string SellTokenAddress { get; set; }

        [JsonProperty("buyAmount")]
        public string BuyAmount { get; set; }

        [JsonProperty("sellAmount")]
        public string SellAmount { get; set; }

        [JsonProperty("estimatedGas")]
        public string EstimatedGas { get; set; }

        [JsonProperty("sources")]
        public List<ZeroExSource> Sources { get; set; }

        [JsonProperty("allowanceTarget")]
        public string AllowanceTarget { get; set; }

        [JsonProperty("route")]
        public ZeroExRoute Route { get; set; }

        [JsonProperty("transaction")]
        public ZeroExTransaction Transaction { get; set; }

        [JsonProperty("fees")]
        public ZeroExFees Fees { get; set; }

        [JsonProperty("permit2")]
        public ZeroExPermit2 Permit2 { get; set; }

        [JsonProperty("issues")]
        public ZeroExIssues Issues { get; set; }
    }

    public class ZeroExSource
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("proportion")]
        public string Proportion { get; set; }
    }

    public class ZeroExRoute
    {
        [JsonProperty("fills")]
        public List<ZeroExFill> Fills { get; set; }
    }

    public class ZeroExFill
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("input")]
        public string Input { get; set; }

        [JsonProperty("output")]
        public string Output { get; set; }

        [JsonProperty("inputAmount")]
        public string InputAmount { get; set; }

        [JsonProperty("outputAmount")]
        public string OutputAmount { get; set; }
    }

    public class ZeroExTransaction
    {
        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("gas")]
        public string Gas { get; set; }

        [JsonProperty("gasPrice")]
        public string GasPrice { get; set; }
    }

    public class ZeroExFees
    {
        [JsonProperty("zeroExFee")]
        public ZeroExFee ZeroExFee { get; set; }
    }

    public class ZeroExFee
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }
    }

    public class ZeroExPermit2
    {
        [JsonProperty("eip712")]
        public object Eip712 { get; set; }

        [JsonProperty("permitData")]
        public ZeroExPermitData PermitData { get; set; }
    }

    public class ZeroExPermitData
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("spender")]
        public string Spender { get; set; }

        [JsonProperty("deadline")]
        public string Deadline { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }
    }

    public class ZeroExIssues
    {
        [JsonProperty("allowance")]
        public ZeroExAllowanceIssue Allowance { get; set; }
    }

    public class ZeroExAllowanceIssue
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("allowance")]
        public string Allowance { get; set; }

        [JsonProperty("required")]
        public string Required { get; set; }
    }
}