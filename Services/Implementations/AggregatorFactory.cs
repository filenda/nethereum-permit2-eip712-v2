using BrlaUsdcSwap.Configuration;
using BrlaUsdcSwap.Services.Interfaces;
using Microsoft.Extensions.Options;
using System;

namespace BrlaUsdcSwap.Services.Implementations
{
    /// <summary>
    /// Factory class that creates the appropriate DEX aggregator based on transaction parameters
    /// </summary>
    public class AggregatorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AppSettings _appSettings;

        public AggregatorFactory(
            IServiceProvider serviceProvider,
            IOptions<AppSettings> appSettings)
        {
            _serviceProvider = serviceProvider;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// Creates the appropriate DEX aggregator based on the USD value of the swap
        /// </summary>
        /// <param name="amountInUsd">The USD value of the swap</param>
        /// <returns>An IDexAggregator implementation</returns>
        public IDexAggregator CreateAggregator(decimal amountInUsd)
        {
            Console.WriteLine($"Selecting aggregator for swap value: ${amountInUsd:F2} USD");

            // Determine which aggregator to use based on the threshold
            // For transactions under the threshold, use 1inch (better gas optimization)
            // For transactions above the threshold, use 0x (better security with Permit2)
            bool use0x = amountInUsd >= _appSettings.Aggregator.ThresholdAmountInUsd;

            if (use0x)
            {
                Console.WriteLine($"Selected 0x Protocol (amount >= ${_appSettings.Aggregator.ThresholdAmountInUsd} threshold)");
                return (IDexAggregator)_serviceProvider.GetService(typeof(ZeroExAggregator));
            }
            else
            {
                Console.WriteLine($"Selected 1inch Protocol (amount < ${_appSettings.Aggregator.ThresholdAmountInUsd} threshold)");
                return (IDexAggregator)_serviceProvider.GetService(typeof(OneInchAggregator));
            }
        }
    }
}