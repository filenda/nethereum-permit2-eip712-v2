using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Models;
using BrlaUsdcSwap.Models.Shared;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Web;

namespace BrlaUsdcSwap.Services.Implementations
{
    /// <summary>
    /// Implementation of IDexAggregator for the 0x Protocol
    /// </summary>
    public class ZeroExAggregator : IDexAggregator
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public string Name => "0x Protocol";

        public ZeroExAggregator(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        {
            // _httpClient = httpClientFactory.CreateClient("ZeroEx");
            _httpClient = httpClientFactory.CreateClient();
            _appSettings = appSettings.Value;

            // Set base address and default headers
            _httpClient.BaseAddress = new Uri(_appSettings.Aggregator.ZeroExApiBaseUrl);

            if (!string.IsNullOrEmpty(_appSettings.Aggregator.ZeroExApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("0x-api-key", _appSettings.Aggregator.ZeroExApiKey);
                _httpClient.DefaultRequestHeaders.Add("0x-version", _appSettings.Aggregator.ZeroExApiVer);
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
            queryParams["chainId"] = request.ChainId.ToString();
            queryParams["buyToken"] = request.BuyTokenAddress;
            queryParams["sellToken"] = request.SellTokenAddress;
            queryParams["sellAmount"] = sellAmountInWei.ToString();
            queryParams["taker"] = request.WalletAddress;

            // Add any additional parameters
            foreach (var param in request.AdditionalParams)
            {
                queryParams[param.Key] = param.Value;
            }

            // Make the request
            var response = await _httpClient.GetAsync($"swap/permit2/quote?{queryParams}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"0x API error: {response.StatusCode} - {errorContent}");
            }

            // Parse the response
            var content = await response.Content.ReadAsStringAsync();
            var zeroExQuote = JsonConvert.DeserializeObject<ZeroExQuoteResponse>(content);

            // Convert to standard QuoteResponse
            var standardResponse = new QuoteResponse
            {
                SellTokenAddress = request.SellTokenAddress,
                BuyTokenAddress = request.BuyTokenAddress,
                SellAmount = zeroExQuote.SellAmount,
                BuyAmount = zeroExQuote.BuyAmount,
                EstimatedGas = zeroExQuote.Transaction?.Gas,
                GasPrice = zeroExQuote.Transaction?.GasPrice,
                RequiresApproval = zeroExQuote.Issues?.Allowance != null && zeroExQuote.Permit2 == null,
                ProtocolFee = zeroExQuote.Fees?.ZeroExFee?.Amount,
                Sources = zeroExQuote.Route?.Fills?.Select(f => f.Source).ToArray(),
                PermitData = zeroExQuote.Permit2,
                RawTransaction = zeroExQuote.Transaction,
                OriginalResponse = zeroExQuote
            };

            Console.WriteLine($"0x quote received: {standardResponse.BuyAmount} (buy) for {standardResponse.SellAmount} (sell)");

            return standardResponse;
        }

        public async Task<SwapResponse> ExecuteSwapAsync(SwapRequest request)
        {
            Console.WriteLine($"Executing swap with {Name}...");

            // Get the original quote response
            var quoteData = (ZeroExQuoteResponse)request.QuoteData.OriginalResponse;

            // Initialize web3 with the user's private key
            var web3 = new Web3(new Nethereum.Web3.Accounts.Account(request.PrivateKey), _appSettings.Aggregator.PolygonRpcUrl);

            string transactionHash;

            // Check if Permit2 is present and requires signing
            string permit2Signature = null;
            if (quoteData.Permit2?.Eip712 != null)
            {
                Console.WriteLine("Quote contains Permit2 data, generating signature...");
                permit2Signature = GeneratePermit2Signature(quoteData.Permit2.Eip712, request.PrivateKey);
                Console.WriteLine($"Permit2 signature generated: {permit2Signature}");
            }

            // Check and handle approval if needed (same logic as SwapService)
            // Only do regular approval if Permit2 isn't being used
            if (quoteData.Issues?.Allowance != null)
            {
                var sellAmountWei = new BigInteger(decimal.Parse(quoteData.SellAmount));
                
                // Use spender from allowance issue, fallback to Permit2 contract if null
                string spenderAddress = !string.IsNullOrEmpty(quoteData.Issues.Allowance.Spender) 
                    ? quoteData.Issues.Allowance.Spender 
                    : GetSpenderAddress(request.ChainId);
                
                Console.WriteLine($"Using spender address: {spenderAddress}");
                await ApproveTokenSpendingAsync(request.SellTokenAddress, spenderAddress, sellAmountWei, request.PrivateKey);
            }

            // Execute the swap transaction
            if (permit2Signature != null)
            {
                // Execute swap with Permit2 signature
                transactionHash = await ExecuteSwapWithPermit2Signature(web3, quoteData, permit2Signature);
            }
            else
            {
                // Execute standard swap without Permit2
                transactionHash = await ExecuteStandardSwap(web3, quoteData);
            }

            // Wait for receipt and verify success
            Console.WriteLine($"Transaction sent: {transactionHash}. Waiting for confirmation...");
            var receipt = await WaitForTransactionReceipt(web3, transactionHash);

            // Calculate the expected buy amount in human-readable format
            var buyTokenDecimals = request.BuyTokenAddress == _appSettings.BrlaTokenAddress
                ? _appSettings.BrlaDecimals
                : _appSettings.UsdcDecimals;

            var buyAmount = BigInteger.Parse(quoteData.BuyAmount);
            var humanReadableBuyAmount = (decimal)buyAmount / (decimal)Math.Pow(10, buyTokenDecimals);

            return new SwapResponse
            {
                Success = receipt.Status.Value == 1,
                TransactionHash = transactionHash,
                ErrorMessage = receipt.Status.Value != 1 ? "Transaction failed on-chain" : null,
                GasUsed = (decimal)receipt.GasUsed.Value,
                SellAmount = request.Amount,
                BuyAmount = humanReadableBuyAmount,
                BlockExplorerUrl = $"{_appSettings.Aggregator.BlockExplorerBaseUrl}{transactionHash}"
            };
        }

        public async Task<ApprovalResponse> GetApprovalDataAsync(ApprovalRequest request)
        {
            Console.WriteLine($"Getting approval data for {Name}...");

            // If we're using Permit2, the approval needs to be for the Permit2 contract
            // For 0x v2, we need to approve the Permit2 contract as the spender
            var permitSpender = GetSpenderAddress(request.ChainId);

            // Create web3 instance
            var web3 = new Web3(new Nethereum.Web3.Accounts.Account(request.PrivateKey), _appSettings.Aggregator.PolygonRpcUrl);

            // Get token ABI
            var tokenAbi = GetTokenAbi(request.TokenAddress);
            var contract = web3.Eth.GetContract(tokenAbi, request.TokenAddress);
            var approveFunction = contract.GetFunction("approve");

            // Get current gas price
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();

            // Convert amount to token decimals
            var tokenDecimals = GetTokenDecimals(request.TokenAddress);
            var approvalAmount = ConvertToTokenDecimals(request.Amount, tokenDecimals);

            // Estimate gas for approval
            var estimatedGas = await approveFunction.EstimateGasAsync(
                request.OwnerAddress,
                null,
                null,
                permitSpender,
                approvalAmount);

            // Add 30% buffer to gas estimate
            var gasLimit = new HexBigInteger(estimatedGas.Value * 13 / 10);

            // Create transaction data
            var txInput = new TransactionInput
            {
                From = request.OwnerAddress,
                To = request.TokenAddress,
                Data = approveFunction.GetData(permitSpender, approvalAmount),
                Gas = gasLimit,
                GasPrice = gasPrice
            };

            return new ApprovalResponse
            {
                Success = true,
                ApprovalData = txInput.Data,
                RawTransaction = txInput
            };
        }

        public async Task<bool> NeedsApprovalAsync(ApprovalRequest request)
        {
            Console.WriteLine($"Checking if approval is needed for {Name}...");

            // Get the permit2 contract address
            var permitSpender = GetSpenderAddress(request.ChainId);

            // Create web3 instance
            var web3 = new Web3(_appSettings.Aggregator.PolygonRpcUrl);

            // Get token contract
            var tokenAbi = GetTokenAbi(request.TokenAddress);
            var contract = web3.Eth.GetContract(tokenAbi, request.TokenAddress);

            // Check allowance
            var allowanceFunction = contract.GetFunction("allowance");
            var allowance = await allowanceFunction.CallAsync<BigInteger>(
                request.OwnerAddress,
                permitSpender);

            // Convert amount to token decimals
            var tokenDecimals = GetTokenDecimals(request.TokenAddress);
            var requiredAmount = ConvertToTokenDecimals(request.Amount, tokenDecimals);

            // Check if allowance is sufficient
            bool needsApproval = allowance < requiredAmount;
            Console.WriteLine($"Current allowance: {allowance}, Required: {requiredAmount}, Needs approval: {needsApproval}");

            return needsApproval;
        }

        public string GetSpenderAddress(int chainId)
        {
            // For 0x v2, this is the Permit2 contract address
            return "0x000000000022D473030F116dDEE9F6B43aC78BA3";
        }

        public async Task ApproveTokenSpendingAsync(ApprovalRequest request)
        {
            Console.WriteLine($"Approving token spending for {Name}...");

            // Get the permit2 contract address
            var permitSpender = GetSpenderAddress(request.ChainId);

            // Create web3 instance
            var web3 = new Web3(new Nethereum.Web3.Accounts.Account(request.PrivateKey), _appSettings.Aggregator.PolygonRpcUrl);

            // Get token contract
            var tokenAbi = GetTokenAbi(request.TokenAddress);
            var contract = web3.Eth.GetContract(tokenAbi, request.TokenAddress);

            // Convert amount to token decimals
            var tokenDecimals = GetTokenDecimals(request.TokenAddress);
            var approvalAmount = ConvertToTokenDecimals(request.Amount, tokenDecimals);

            // Get approve function
            var approveFunction = contract.GetFunction("approve");

            try
            {
                // Get current gas price
                var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
                Console.WriteLine($"Current gas price: {gasPrice.Value} wei");

                // Estimate gas for approval
                var estimatedGas = await approveFunction.EstimateGasAsync(
                    request.OwnerAddress,
                    null,
                    null,
                    permitSpender,
                    approvalAmount);

                // Add 30% buffer to the estimated gas
                var gasLimit = new HexBigInteger(estimatedGas.Value * 13 / 10);

                Console.WriteLine($"Estimated gas for approval: {estimatedGas.Value}");
                Console.WriteLine($"Gas limit with buffer: {gasLimit.Value}");

                // Send approval transaction
                var approveTxHash = await approveFunction.SendTransactionAsync(
                    request.OwnerAddress,
                    gasLimit,
                    gasPrice,
                    null,
                    permitSpender,
                    approvalAmount);

                Console.WriteLine($"Approval transaction sent: {approveTxHash}");
                
                // Wait for approval transaction to be mined
                var receipt = await WaitForTransactionReceipt(web3, approveTxHash);
                
                if (receipt.Status.Value != 1)
                {
                    throw new Exception("Approval transaction failed on-chain");
                }
                
                Console.WriteLine("Token approval successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during token approval: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw new Exception("Token approval failed", ex);
            }
        }

        private async Task ApproveTokenSpendingAsync(string tokenAddress, string spenderAddress, BigInteger amount, string privateKey)
        {
            Console.WriteLine("Checking token allowance...");

            // Create web3 instance
            var web3 = new Web3(new Nethereum.Web3.Accounts.Account(privateKey), _appSettings.Aggregator.PolygonRpcUrl);

            // Get token ABI
            var tokenAbi = GetTokenAbi(tokenAddress);
            var contract = web3.Eth.GetContract(tokenAbi, tokenAddress);

            // Get allowance function
            var allowanceFunction = contract.GetFunction("allowance");
            var allowance = await allowanceFunction.CallAsync<BigInteger>(
                web3.TransactionManager.Account.Address,
                spenderAddress);

            Console.WriteLine($"Current allowance: {allowance}");

            // If allowance is less than the amount we want to sell, approve
            if (allowance < amount)
            {
                Console.WriteLine("Approving token spending...");

                // Get approve function
                var approveFunction = contract.GetFunction("approve");
                string approveTxHash;

                try
                {
                    // Get current gas price
                    var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
                    Console.WriteLine($"Current gas price: {gasPrice.Value} wei");

                    try
                    {
                        // Try to estimate gas first (for better accuracy)
                        var estimatedGas = await approveFunction.EstimateGasAsync(
                            web3.TransactionManager.Account.Address,
                            null,
                            null,
                            spenderAddress,
                            amount);

                        // Add 30% buffer to the estimated gas
                        var gasLimit = new HexBigInteger(estimatedGas.Value * 13 / 10);

                        Console.WriteLine($"Estimated gas for approval: {estimatedGas.Value}");
                        Console.WriteLine($"Gas limit with buffer: {gasLimit.Value}");

                        // Send with explicit parameters
                        approveTxHash = await approveFunction.SendTransactionAsync(
                            web3.TransactionManager.Account.Address,
                            gasLimit,
                            gasPrice,
                            null,
                            spenderAddress,
                            amount);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during gas estimation: {ex.Message}");

                        // Fallback to fixed gas limit if estimation fails
                        Console.WriteLine("Using fallback fixed gas limit of 100000");
                        var gasLimit = new HexBigInteger(100000);

                        approveTxHash = await approveFunction.SendTransactionAsync(
                            web3.TransactionManager.Account.Address,
                            gasLimit,
                            gasPrice,
                            null,
                            spenderAddress,
                            amount);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send approval transaction: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw new Exception("Token approval transaction failed to send", ex);
                }

                Console.WriteLine($"Approval transaction sent: {approveTxHash}");
                var receipt = await WaitForTransactionReceipt(web3, approveTxHash);
                
                Console.WriteLine("Token approval successful!");
            }
            else
            {
                Console.WriteLine("Token allowance is sufficient, no approval needed");
            }
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

        private string GeneratePermit2Signature(object eip712Data, string privateKey)
        {
            Console.WriteLine("Generating Permit2 signature...");

            // Convert EIP-712 data to proper format for signing
            var typedDataJson = JsonConvert.SerializeObject(eip712Data, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });

            // Create signer - remove 0x prefix if present
            var typedDataSigner = new Eip712TypedDataSigner();
            var cleanPrivateKey = privateKey.StartsWith("0x") ? privateKey.Substring(2) : privateKey;
            var ethECKey = new EthECKey(cleanPrivateKey);

            // Sign the data
            var signature = typedDataSigner.SignTypedDataV4(typedDataJson, ethECKey);

            Console.WriteLine($"Permit2 signature generated: {signature.Substring(0, 10)}...");

            return signature;
        }

        private async Task<string> ExecuteSwapWithPermit2Signature(Web3 web3, ZeroExQuoteResponse quote, string permit2Signature)
        {
            Console.WriteLine("Executing swap with Permit2 signature...");

            // Get the original transaction data
            string transactionData = quote.Transaction.Data;

            // Handle signature prefixes
            string dataHex = transactionData.StartsWith("0x") ? transactionData.Substring(2) : transactionData;
            string signatureHex = permit2Signature.StartsWith("0x") ? permit2Signature.Substring(2) : permit2Signature;

            // Calculate signature length in bytes
            int signatureLengthInBytes = signatureHex.Length / 2;

            // Create a BigInteger for the length and convert to hex
            var signatureLengthBigInt = new BigInteger(signatureLengthInBytes);

            // Convert to a hex string WITHOUT "0x" prefix, padded to 64 characters (32 bytes)
            string signatureLengthHex = signatureLengthBigInt.ToString("x").PadLeft(64, '0');

            // Concatenate: original tx data + signature length (32 bytes) + signature
            string fullTransactionData = "0x" + dataHex + signatureLengthHex + signatureHex;

            // Create the transaction input
            var txInput = new TransactionInput
            {
                From = web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = fullTransactionData,
                Value = new HexBigInteger(string.IsNullOrEmpty(quote.Transaction.Value)
                    ? BigInteger.Zero
                    : BigInteger.Parse(quote.Transaction.Value)),
                Gas = new HexBigInteger(BigInteger.Parse(quote.Transaction.Gas) * 12 / 10), // 20% buffer
                GasPrice = new HexBigInteger(BigInteger.Parse(quote.Transaction.GasPrice))
            };

            // Send the transaction
            return await web3.Eth.TransactionManager.SendTransactionAsync(txInput);
        }

        private async Task<string> ExecuteStandardSwap(Web3 web3, ZeroExQuoteResponse quote)
        {
            Console.WriteLine("Executing standard swap...");

            // Create the transaction input
            var txInput = new TransactionInput
            {
                From = web3.TransactionManager.Account.Address,
                To = quote.Transaction.To,
                Data = quote.Transaction.Data,
                Value = new HexBigInteger(string.IsNullOrEmpty(quote.Transaction.Value)
                    ? BigInteger.Zero
                    : BigInteger.Parse(quote.Transaction.Value)),
                Gas = new HexBigInteger(BigInteger.Parse(quote.Transaction.Gas) * 12 / 10), // 20% buffer
                GasPrice = new HexBigInteger(BigInteger.Parse(quote.Transaction.GasPrice))
            };

            // Send the transaction
            return await web3.Eth.TransactionManager.SendTransactionAsync(txInput);
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

        private string GetTokenAbi(string tokenAddress)
        {
            // Return the appropriate ABI based on the token address
            if (tokenAddress.Equals(_appSettings.BrlaTokenAddress, StringComparison.OrdinalIgnoreCase))
            {
                return @"[{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""}]";
            }
            else if (tokenAddress.Equals(_appSettings.UsdcTokenAddress, StringComparison.OrdinalIgnoreCase))
            {
                return @"[{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""},{""name"":""_spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":""remaining"",""type"":""uint256""}],""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_spender"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""}]";
            }

            throw new ArgumentException($"ABI not found for token address: {tokenAddress}");
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
}