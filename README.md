# BRLA/USDC DEX Aggregator Abstraction

This project demonstrates an abstraction over multiple DEX aggregators (0x and 1inch) to optimize token swaps based on transaction amounts. The implementation uses C# and Nethereum to interact with the respective APIs.

## Features

- **Automatic Aggregator Selection**: Uses 1inch for transactions under $100 and 0x for larger amounts
- **Common Interface**: Shared interface allowing seamless switching between aggregators
- **EIP-712 Support**: Implements Permit2 signing for 0x transactions
- **Gas Optimization**: Takes advantage of 1inch's gas optimizations for smaller transactions
- **Security**: Uses 0x's Permit2 protocol for larger transactions, reducing allowance risks

## Project Structure

- **Interfaces**:
  - `IDexAggregator`: Core interface that both aggregator implementations follow
  - `ISwapService`: Service for executing swaps using the appropriate aggregator
  - `IPriceService`: Service for retrieving token prices and USD values

- **Implementations**:
  - `ZeroExAggregator`: Implementation for the 0x Protocol
  - `OneInchAggregator`: Implementation for the 1inch Protocol
  - `AggregatorFactory`: Factory that selects the appropriate aggregator based on swap value
  - `SwapService`: Service that orchestrates the swap process
  - `PriceService`: Service that retrieves token prices

- **Models**:
  - `QuoteRequest/Response`: Models for quote requests and responses
  - `SwapRequest/Response`: Models for swap requests and responses
  - `ApprovalRequest/Response`: Models for approval requests and responses
  - `ZeroExResponseModels`: Specific models for 0x API responses
  - Various models for 1inch API responses (embedded in OneInchAggregator.cs)

## Configuration

The application is configured through the `appsettings.json` file, which includes:

- Wallet settings (address, private key)
- Token addresses and decimals
- Aggregator settings (API URLs, API keys)
- USD threshold for selecting between aggregators

## Usage

1. Set up your `appsettings.json` file with your wallet information and API keys
2. Run the application to swap tokens:
   ```
   dotnet run -- <amount> <direction>
   ```
   Where `<amount>` is the amount to swap and `<direction>` is 1 for BRLA→USDC or 2 for USDC→BRLA

3. Example commands:
   ```
   dotnet run -- 10.5 1   # Swap 10.5 BRLA to USDC
   dotnet run -- 50 2     # Swap 50 USDC to BRLA
   ```

## Implementation Details

### Design Patterns

- **Strategy Pattern**: Different aggregator implementations for the same interface
- **Factory Pattern**: AggregatorFactory creates the appropriate implementation
- **Adapter Pattern**: Adapters for both 0x and 1inch APIs to match our common interface
- **Dependency Injection**: Services are injected for better testability and modularity

### Key Technical Points

- **Permit2 Implementation**: 0x uses Permit2 for more secure token approvals
- **EIP-712 Signing**: Typed data signing for Permit2 transactions
- **Threshold-Based Selection**: Selects 1inch for smaller amounts (gas optimization) and 0x for larger amounts (better security)
- **Price Conversion**: Uses external APIs to convert token amounts to USD for threshold comparison

## Comparison Between 0x and 1inch

| Feature | 0x | 1inch |
|---------|-------|----------|
| **Security Model** | Permit2 (safer approvals) | Standard ERC20 approvals |
| **Gas Optimization** | Standard implementation | Assembly optimized (saves ~10%) |
| **Quote Response Time** | Faster (avg. 1.5s) | Slower (avg. 4s) |
| **API Structure** | Requires version headers | Clean REST API |
| **Authorization** | API key in header | Bearer token |
| **Approval Flow** | One-time Permit2 approval + signatures | Standard approve before each swap |
| **Transaction Format** | More complex with signatures | Simpler transaction data |

## Future Enhancements

- Add support for more DEX aggregators (ParaSwap, Matcha, etc.)
- Implement gas estimation comparison to dynamically select the best aggregator
- Add slippage protection mechanisms
- Implement batch swapping for larger amounts
- Add detailed logging and metrics collection

# BrlaUsdcSwap - Setup Guide

This guide will help you set up and run the BrlaUsdcSwap application using Visual Studio Code and .NET CLI.

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- API keys for [0x](https://0x.org/docs/api#get-started) and [1inch](https://portal.1inch.dev/)
- Polygon wallet with BRLA, USDC, and MATIC for gas

## Project Structure

Create the following folder structure:

```
BrlaUsdcSwap/
├── .vscode/                      # VS Code configuration
│   ├── launch.json               # Debug configurations
│   └── tasks.json                # Build tasks
├── Configuration/                # App configuration
│   ├── AggregatorConfig.cs
│   └── AppSettings.cs
├── Models/                       # Data models
│   ├── Shared/                   # Shared data models
│   │   ├── ApprovalRequest.cs
│   │   ├── ApprovalResponse.cs
│   │   ├── QuoteRequest.cs
│   │   ├── QuoteResponse.cs
│   │   ├── SwapRequest.cs
│   │   └── SwapResponse.cs
│   └── ZeroExResponseModels.cs   # 0x specific models
├── Services/                     # Service implementations
│   ├── Implementations/
│   │   ├── AggregatorFactory.cs
│   │   ├── OneInchAggregator.cs
│   │   ├── PriceService.cs
│   │   ├── SwapService.cs
│   │   └── ZeroExAggregator.cs
│   └── Interfaces/
│       ├── IDexAggregator.cs
│       ├── IPriceService.cs
│       └── ISwapService.cs
├── .gitignore                    # Git ignore file
├── appsettings.json              # Application settings
├── BrlaUsdcSwap.csproj           # Project file
├── Program.cs                    # Main program entry
└── README.md                     # Project documentation
```

## Setup Steps

1. **Create a new project**:
   ```bash
   mkdir BrlaUsdcSwap
   cd BrlaUsdcSwap
   dotnet new console
   ```

2. **Add required packages**:
   ```bash
   dotnet add package Microsoft.Extensions.Caching.Memory
   dotnet add package Microsoft.Extensions.Configuration
   dotnet add package Microsoft.Extensions.Configuration.Json
   dotnet add package Microsoft.Extensions.DependencyInjection
   dotnet add package Microsoft.Extensions.Http
   dotnet add package Microsoft.Extensions.Options.ConfigurationExtensions
   dotnet add package Nethereum.Web3
   dotnet add package Nethereum.Signer.EIP712
   dotnet add package Newtonsoft.Json
   ```

3. **Replace the project file** with the provided `BrlaUsdcSwap.csproj`.

4. **Create the folder structure** as outlined above.

5. **Create VS Code configuration**:
   ```bash
   mkdir -p .vscode
   ```
   Then add the provided `launch.json` and `tasks.json` files to the `.vscode` folder.

6. **Add the source code files** to their respective folders, as provided.

7. **Configure your API keys**:
   - Copy the `appsettings.json` template
   - Fill in your wallet's private key and address
   - Add your 0x and 1inch API keys

## Running the Application

### Using .NET CLI

1. **Build the project**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run
   ```

3. **Run with specific parameters**:
   ```bash
   # Swap 10 BRLA to USDC
   dotnet run -- 10 1
   
   # Swap 50 USDC to BRLA
   dotnet run -- 50 2
   ```

### Using VS Code

1. **Open the project** in VS Code:
   ```bash
   code .
   ```

2. **Open the Debug tab** (Ctrl+Shift+D or Cmd+Shift+D on macOS)

3. **Select a launch configuration** from the dropdown:
   - `.NET Core Launch (console)` - Interactive mode
   - `BRLA to USDC (10 tokens)` - Swap 10 BRLA to USDC
   - `USDC to BRLA (50 tokens)` - Swap 50 USDC to BRLA

4. **Start debugging** by pressing F5 or clicking the green play button

## Troubleshooting

If you encounter issues:

1. **Build errors**:
   - Check that you've added all required packages
   - Verify file and folder structure matches the expected paths
   - Make sure all namespaces are correct

2. **Runtime errors**:
   - Verify that your `appsettings.json` is correctly configured
   - Check that your API keys are valid
   - Ensure your wallet has sufficient token balances and MATIC for gas

3. **Transaction errors**:
   - Check your RPC URL in the `appsettings.json` file
   - Try with a smaller amount if running into slippage issues
   - Make sure the Polygon network is operating normally

## Additional Commands

- **Clean the project**:
  ```bash
  dotnet clean
  ```

- **Run in watch mode** (auto-rebuild on changes):
  ```bash
  dotnet watch run
  ```

- **Publish the application**:
  ```bash
  dotnet publish -c Release
  ```

## License

MIT