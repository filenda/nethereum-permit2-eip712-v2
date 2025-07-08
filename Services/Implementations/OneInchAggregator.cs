using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Models;
using BrlaUsdcSwap.Models.Shared;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Web;

namespace BrlaUsdcSwap.Services.Implementations
{
    /// <summary>
    /// Implementation of IDexAggregator for the 1inch Protocol
    /// </summary>
    public class OneInchAggregator : IDexAggregator
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public string Name => "1inch Protocol";

        public OneInchAggregator(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            _httpClient = httpClientFactory.CreateClient("OneInch");
            _appSettings = appSettings.Value;

            // Don't add the Authorization header here since it's already added in Program.cs
            // Just set the base address if needed
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_appSettings.Aggregator.OneInchApiBaseUrl);
            }
        }

        public async Task<QuoteResponse> GetQuoteAsync(QuoteRequest request)
        {
            Console.WriteLine($"Getting quote from {Name}...");
            
            // Determine correct decimals based on which token is being sold
            int decimals = request.SellTokenAddress == _appSettings.BrlaTokenAddress
                ? _appSettings.BrlaDecimals
                : _appSettings.UsdcDecimals;

            // Convert decimal to integer with appropriate decimals
            BigInteger sellAmountInWei = ConvertToTokenDecimals(request.Amount, decimals);

            // Build query parameters
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["fromTokenAddress"] = request.SellTokenAddress;
            queryParams["toTokenAddress"] = request.BuyTokenAddress;
            queryParams["amount"] = sellAmountInWei.ToString();
            
            // Add any additional parameters
            foreach (var param in request.AdditionalParams)
            {
                queryParams[param.Key] = param.Value;
            }

            // Build the URL for 1inch (v6.0 API)
            string baseUrl = $"{_appSettings.Aggregator.OneInchApiBaseUrl}/v6.0/{request.ChainId}";
            
            //TODO: Remove this delay when going to prod
            await Task.Delay(1000); // Add delay to respect 1inch API rate limit
            
            // Make the request
            Console.WriteLine($"Requesting quote from: {baseUrl}/quote?{queryParams}");
            var response = await _httpClient.GetAsync($"{baseUrl}/quote?{queryParams}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"1inch API error: {response.StatusCode} - {errorContent}");
            }

            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw response: {content}");
            var oneInchQuote = JsonConvert.DeserializeObject<OneInchQuoteResponse>(content);

            // Extract sources - might be null if protocol information is not included
            var sources = new List<string>();
            if (oneInchQuote.Protocols != null)
            {
                foreach (var path in oneInchQuote.Protocols)
                {
                    foreach (var segment in path)
                    {
                        foreach (var protocol in segment)
                        {
                            if (!sources.Contains(protocol.Name))
                            {
                                sources.Add(protocol.Name);
                            }
                        }
                    }
                }
            }
            else
            {
                // Default source if protocols info not available
                sources.Add("1inch");
            }

            // Get gas price
            var web3 = new Web3(_appSettings.Aggregator.PolygonRpcUrl);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();

            // Use appropriate amount values based on available response fields
            string buyAmount = oneInchQuote.DstAmount ?? oneInchQuote.ToAmount ?? "0";
            string sellAmount = oneInchQuote.SrcAmount ?? oneInchQuote.FromAmount ?? sellAmountInWei.ToString();
            
            // Estimate gas if not provided
            long estimatedGas = oneInchQuote.EstimatedGas > 0 ? 
                oneInchQuote.EstimatedGas : 
                250000; // Default value if not provided

            // Convert to standard QuoteResponse
            var standardResponse = new QuoteResponse
            {
                SellTokenAddress = request.SellTokenAddress,
                BuyTokenAddress = request.BuyTokenAddress,
                SellAmount = sellAmount,
                BuyAmount = buyAmount,
                EstimatedGas = estimatedGas.ToString(),
                GasPrice = gasPrice.Value.ToString(),
                RequiresApproval = true, // We'll check this separately
                ProtocolFee = "0", // 1inch doesn't show protocol fees in quote
                Sources = sources.ToArray(),
                RawTransaction = null, // 1inch doesn't provide tx data in quote
                OriginalResponse = oneInchQuote
            };

            Console.WriteLine($"1inch quote received: {standardResponse.BuyAmount} (buy) for {standardResponse.SellAmount} (sell)");
            
            return standardResponse;
        }

        public async Task<SwapResponse> ExecuteSwapAsync(SwapRequest request)
        {
            Console.WriteLine($"Executing swap with {Name}...");
            
            // First, check if approval is needed
            var spenderAddress = GetSpenderAddress(request.ChainId);
            var approvalRequest = new ApprovalRequest
            {
                TokenAddress = request.SellTokenAddress,
                OwnerAddress = request.WalletAddress,
                SpenderAddress = spenderAddress,
                Amount = request.Amount,
                ChainId = request.ChainId,
                PrivateKey = request.PrivateKey
            };
            
            if (await NeedsApprovalAsync(approvalRequest))
            {
                Console.WriteLine("Token approval needed. Sending approval transaction...");
                
                try
                {
                    // Get approval data
                    var approvalResponse = await GetApprovalDataAsync(approvalRequest);
                    
                    // Create web3 instance for sending transactions
                    var approvalWeb3 = new Web3(new Nethereum.Web3.Accounts.Account(request.PrivateKey), _appSettings.Aggregator.PolygonRpcUrl);
                    
                    // Send approval transaction
                    var approvalTxInput = (TransactionInput)approvalResponse.RawTransaction;
                    
                    // Log details before sending
                    Console.WriteLine($"Sending approval transaction: To={approvalTxInput.To}, From={approvalTxInput.From}");
                    Console.WriteLine($"Gas: {approvalTxInput.Gas.Value}, GasPrice: {approvalTxInput.GasPrice.Value}");
                    
                    var approvalTxHash = await approvalWeb3.Eth.TransactionManager.SendTransactionAsync(approvalTxInput);
                    
                    Console.WriteLine($"Approval transaction sent: {approvalTxHash}. Waiting for confirmation...");
                    
                    // Wait for approval to be mined
                    var approvalReceipt = await WaitForTransactionReceipt(approvalWeb3, approvalTxHash);
                    if (approvalReceipt.Status.Value != 1)
                    {
                        return new SwapResponse
                        {
                            Success = false,
                            ErrorMessage = "Approval transaction failed",
                            TransactionHash = approvalTxHash
                        };
                    }
                    
                    Console.WriteLine("Approval confirmed successfully. Proceeding with swap...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during approval: {ex.Message}");
                    
                    // Sleep briefly to allow for potential blockchain state changes
                    await Task.Delay(2000);
                    
                    // Check again if approval is needed (might have been approved elsewhere)
                    if (await NeedsApprovalAsync(approvalRequest))
                    {
                        throw new Exception($"Approval failed and still needed: {ex.Message}", ex);
                    }
                    else
                    {
                        Console.WriteLine("Approval appears to be in place despite error. Proceeding with swap...");
                    }
                }
            }
            
            // Now, get the swap transaction
            // Convert to token decimals
            int decimals = request.SellTokenAddress == _appSettings.BrlaTokenAddress
                ? _appSettings.BrlaDecimals
                : _appSettings.UsdcDecimals;
            BigInteger sellAmountInWei = ConvertToTokenDecimals(request.Amount, decimals);
            
            // Build query parameters for swap
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["fromTokenAddress"] = request.SellTokenAddress;
            queryParams["toTokenAddress"] = request.BuyTokenAddress;
            queryParams["amount"] = sellAmountInWei.ToString();
            queryParams["fromAddress"] = request.WalletAddress;
            queryParams["slippage"] = request.SlippagePercentage.ToString();
            
            // Build the URL for 1inch
            string baseUrl = $"{_appSettings.Aggregator.OneInchApiBaseUrl}/v6.0/{request.ChainId}";
            
            Console.WriteLine("Requesting swap transaction data from 1inch...");
            
            //TODO: Remove this delay when going to prod
            await Task.Delay(1000); // Add delay to respect 1inch API rate limit
            
            // Make the request
            var response = await _httpClient.GetAsync($"{baseUrl}/swap?{queryParams}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"1inch swap API error: {errorContent}");
            }
            
            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw swap response: {content}");
            var swapResponse = JsonConvert.DeserializeObject<OneInchSwapResponse>(content);
            
            // Execute the transaction
            var swapWeb3 = new Web3(new Nethereum.Web3.Accounts.Account(request.PrivateKey), _appSettings.Aggregator.PolygonRpcUrl);
            
            Console.WriteLine("Sending swap transaction...");
            
            // Get current gas price with a premium (1.1x)
            var gasPrice = await swapWeb3.Eth.GasPrice.SendRequestAsync();
            var gasPriceWithPremium = new HexBigInteger(gasPrice.Value * 11 / 10);
            
            // Create transaction input
            var swapTxInput = new TransactionInput
            {
                From = request.WalletAddress,
                To = swapResponse.Tx.To,
                Data = swapResponse.Tx.Data,
                Value = new HexBigInteger(BigInteger.Parse(swapResponse.Tx.Value ?? "0")),
                Gas = new HexBigInteger(swapResponse.Tx.Gas * 13 / 10), // Adding 30% buffer
                GasPrice = gasPriceWithPremium
            };
            
            // Log details before sending
            Console.WriteLine($"Swap transaction details: To={swapTxInput.To}, From={swapTxInput.From}");
            Console.WriteLine($"Gas: {swapTxInput.Gas.Value}, GasPrice: {swapTxInput.GasPrice.Value}");
            
            // Send the transaction
            var txHash = await swapWeb3.Eth.TransactionManager.SendTransactionAsync(swapTxInput);
            
            Console.WriteLine($"Transaction sent: {txHash}. Waiting for confirmation...");
            
            // Wait for receipt and verify success
            var receipt = await WaitForTransactionReceipt(swapWeb3, txHash);
            
            // Calculate the expected buy amount in human-readable format
            var buyTokenDecimals = request.BuyTokenAddress == _appSettings.BrlaTokenAddress 
                ? _appSettings.BrlaDecimals 
                : _appSettings.UsdcDecimals;
            
            var buyAmount = BigInteger.Parse(swapResponse.ToAmount ?? "0");
            var humanReadableBuyAmount = (decimal)buyAmount / (decimal)Math.Pow(10, buyTokenDecimals);
            
            return new SwapResponse
            {
                Success = receipt.Status.Value == 1,
                TransactionHash = txHash,
                ErrorMessage = receipt.Status.Value != 1 ? "Transaction failed on-chain" : null,
                GasUsed = (decimal)receipt.GasUsed.Value,
                SellAmount = request.Amount,
                BuyAmount = humanReadableBuyAmount,
                BlockExplorerUrl = $"{_appSettings.Aggregator.BlockExplorerBaseUrl}{txHash}"
            };
        }

        public async Task<ApprovalResponse> GetApprovalDataAsync(ApprovalRequest request)
        {
            Console.WriteLine($"Getting approval data for {Name}...");
            
            // Build query parameters for approval
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["tokenAddress"] = request.TokenAddress;
            
            // For 1inch, we need to use a large number for infinity approval
            // Use max uint256 value for unlimited approval
            BigInteger maxApproval = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
            queryParams["amount"] = maxApproval.ToString();
            
            // Build the URL for 1inch
            string baseUrl = $"{_appSettings.Aggregator.OneInchApiBaseUrl}/v6.0/{request.ChainId}";
            
            //TODO: Remove this delay when going to prod
            await Task.Delay(1000); // Add delay to respect 1inch API rate limit
            
            // Make the request
            Console.WriteLine($"Requesting approval transaction from: {baseUrl}/approve/transaction?{queryParams}");
            var response = await _httpClient.GetAsync($"{baseUrl}/approve/transaction?{queryParams}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"1inch approval API error: {response.StatusCode} - {errorContent}");
            }
            
            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw approval response: {content}");
            var approveResponse = JsonConvert.DeserializeObject<OneInchApproveResponse>(content);
            
            // Create web3 instance for gas estimation
            var web3 = new Web3(_appSettings.Aggregator.PolygonRpcUrl);
            
            // Get current gas price with a small premium (1.1x)
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            var gasPriceWithPremium = new HexBigInteger(gasPrice.Value * 11 / 10);
            
            //TODO: Use dynamic estimations by using eth_estimateGas RPC method instead of fixed values
            // Use a fixed gas limit for approvals which should be safe
            var gasLimit = new HexBigInteger(100000);
            
            // Create transaction input
            var txInput = new TransactionInput
            {
                From = request.OwnerAddress,
                To = approveResponse.To,
                Data = approveResponse.Data,
                Value = new HexBigInteger(0),
                Gas = gasLimit,
                GasPrice = gasPriceWithPremium
            };
            
            Console.WriteLine($"Approval transaction prepared: To={txInput.To}, Gas={txInput.Gas.Value}, GasPrice={txInput.GasPrice.Value}");
            
            return new ApprovalResponse
            {
                Success = true,
                ApprovalData = approveResponse.Data,
                RawTransaction = txInput
            };
        }

        public async Task<bool> NeedsApprovalAsync(ApprovalRequest request)
        {
            Console.WriteLine($"Checking if approval is needed for {Name}...");
            
            // Build query parameters for allowance check
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["tokenAddress"] = request.TokenAddress;
            queryParams["walletAddress"] = request.OwnerAddress;
            
            // Build the URL for 1inch
            string baseUrl = $"{_appSettings.Aggregator.OneInchApiBaseUrl}/v6.0/{request.ChainId}";
            
            //TODO: Remove this delay when going to prod
            await Task.Delay(1000); // Add delay to respect 1inch API rate limit
            
            // Make the request
            var response = await _httpClient.GetAsync($"{baseUrl}/approve/allowance?{queryParams}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"1inch allowance API error: {errorContent}");
            }
            
            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            var allowanceResponse = JsonConvert.DeserializeObject<OneInchAllowanceResponse>(content);
            
            // Get the required amount in token decimals
            var requiredAmount = ConvertToTokenDecimals(request.Amount, GetTokenDecimals(request.TokenAddress));
            
            // Compare allowance with required amount
            var currentAllowance = BigInteger.Parse(allowanceResponse.Allowance);
            bool needsApproval = currentAllowance < requiredAmount;
            
            Console.WriteLine($"Current allowance: {currentAllowance}, Required: {requiredAmount}, Needs approval: {needsApproval}");
            
            return needsApproval;
        }

        public string GetSpenderAddress(int chainId)
        {
            // Return the 1inch router address for the specified chain
            if (chainId == 137) // Polygon
            {
                return "0x1111111254fb6c44bac0bed2854e76f90643097d"; // 1inch Router v6 on Polygon
            }
            
            throw new ArgumentException($"Spender address not configured for chain ID: {chainId}");
        }

        #region Private Helper Methods
        
        private BigInteger ConvertToTokenDecimals(decimal amount, int decimals)
        {
            if (decimals == 18)
            {
                return Web3.Convert.ToWei(amount);
            }
            else if (decimals == 6)
            {
                return new BigInteger(amount * 1_000_000m);
            }
            else
            {
                return (BigInteger)(amount * (decimal)Math.Pow(10, decimals));
            }
        }
        
        private async Task<TransactionReceipt> WaitForTransactionReceipt(Web3 web3, string transactionHash)
        {
            // Wait for the transaction to be mined
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            int attempts = 0;
            while (receipt == null && attempts < 30) // Limit to 30 attempts (2.5 minutes)
            {
                await Task.Delay(5000); // Check every 5 seconds
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                attempts++;
                Console.WriteLine($"Waiting for receipt... Attempt {attempts}/30");
            }
            
            if (receipt == null)
            {
                throw new Exception("Transaction is taking too long to be mined.");
            }
            
            return receipt;
        }
        
        private int GetTokenDecimals(string tokenAddress)
        {
            // Return the appropriate decimals for the token
            if (tokenAddress.Equals(_appSettings.BrlaTokenAddress, StringComparison.OrdinalIgnoreCase))
            {
                return _appSettings.BrlaDecimals;
            }
            else if (tokenAddress.Equals(_appSettings.UsdcTokenAddress, StringComparison.OrdinalIgnoreCase))
            {
                return _appSettings.UsdcDecimals;
            }
            
            throw new ArgumentException($"Decimals not found for token address: {tokenAddress}");
        }
        
        #endregion
    }
    
    #region 1inch API Response Models
    
    public class OneInchQuoteResponse
    {
        [JsonProperty("dstAmount")]
        public string DstAmount { get; set; }
        
        [JsonProperty("srcAmount")]
        public string SrcAmount { get; set; }
        
        [JsonProperty("toAmount")]
        public string ToAmount { get; set; }

        [JsonProperty("fromAmount")]
        public string FromAmount { get; set; }

        [JsonProperty("protocols")]
        public List<List<List<OneInchProtocolRoute>>> Protocols { get; set; }

        [JsonProperty("estimatedGas")]
        public long EstimatedGas { get; set; }
    }

    public class OneInchProtocolRoute
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("part")]
        public long Part { get; set; }

        [JsonProperty("fromTokenAddress")]
        public string FromTokenAddress { get; set; }

        [JsonProperty("toTokenAddress")]
        public string ToTokenAddress { get; set; }
    }

    public class OneInchSwapResponse
    {
        [JsonProperty("toAmount")]
        public string ToAmount { get; set; }

        [JsonProperty("fromAmount")]
        public string FromAmount { get; set; }

        [JsonProperty("tx")]
        public OneInchTransaction Tx { get; set; }
    }

    public class OneInchTransaction
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("gasPrice")]
        public string GasPrice { get; set; }

        [JsonProperty("gas")]
        public long Gas { get; set; }
    }

    public class OneInchApproveResponse
    {
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("gasPrice")]
        public string GasPrice { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("gas")]
        public long Gas { get; set; }
    }

    public class OneInchAllowanceResponse
    {
        [JsonProperty("allowance")]
        public string Allowance { get; set; }
    }
    
    #endregion
}