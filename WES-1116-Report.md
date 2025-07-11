# WES-1116-Report

# WES-1116 Report: 1inch vs 0x Protocol Implementation Analysis

## Executive Summary

This report analyzes the implementation of both 1inch and 0x Protocol integrations in the Nethereum PoC for DEX aggregation functionality. The analysis covers performance comparison, fee structures, implementation differences, and provides recommendations based on real transaction data.

## Background

The existing Nethereum PoC was originally built using 0x Protocol for DEX aggregation. As part of WES-1116, a new implementation using 1inch API was developed to evaluate and compare the two solutions for optimal DEX aggregation capabilities.

## Implementation Analysis

### 0x Protocol Implementation

**Key Features:**
- Permit2 signature for single-transaction swaps
- Uniswap V3 liquidity sourcing
- Efficient gas estimation and execution

**Transaction Example:**
- Input: 1 BRLA (1000000000000000000 wei)
- Output: 0.18405 USDC (184,050 units)
- Gas Estimated: 284,044
- Gas Used: 186,861 (65.8% efficiency)
- Transaction Fee: 0.0056 POL ($0.0011)

### 1inch Protocol Implementation

**Key Features:**
- Direct API integration with comprehensive quote system
- Dynamic gas calculation and optimization
- Approval checking and management
- Multiple successful execution attempts logged

**Transaction Examples:**

**Transaction 1:**
- Input: 1 BRLA (1000000000000000000 wei)
- Output: 0.182175 USDC (182,175 units)
- Gas Estimated: 250,000
- Gas Used: 193,690 (77.5% efficiency)
- Transaction Fee: 0.0053 POL ($0.0011)

**Transaction 2:**
- Input: 1 BRLA (1000000000000000000 wei)
- Output: 0.182088 USDC (182,088 units)
- Gas Used: 158,302
- Better gas efficiency achieved

## Fee Structure Analysis

### Protocol Fees

**1inch Protocol:**
- ✅ **Gas optimization features** - Chi gas token and optimization algorithms can reduce gas costs by up to 40%
- ✅ **Competitive advantage** - No on-top fees ensure users get the best possible rates

**1inch Infrastructure Fees (Classic Swap API):**
Infrastructure fees apply to all prepaid plans, exclusively for Classic Swap API transactions. These fees are deducted from the destination amount of each completed Network Request and are distinct from subscription fees.

| Plan | Fees for stable coins | Fees for non-stable coins |
| --- | --- | --- |
| Dev | 10 bps | 30 bps |
| Startup | 7 bps | 20 bps |
| Pro | 4 bps | 10 bps |
| Business | 2 bps | 5 bps |

**0x Protocol:**
- ⚠️ **Selective protocol fees** - 0x charges a 0.15% fee on swaps involving select token pairs (standard plan)
- ✅ **Network fees only for most pairs** - Many token pairs have no additional protocol fees
- ✅ **Custom pricing available** - High volume teams can negotiate custom fee structures
- ℹ️ **Fee transparency** - Protocol fees are charged on-chain during transactions when applicable

**0x Pricing Plans:**

| Feature | Free | Starter | Growth | Enterprise |
| --- | --- | --- | --- | --- |
| Monthly API calls | 1M | 3M | 6M | Custom |
| Additional API calls (per M) | - | $500 | $500 | $500 |
| Apps | 1 | 1 | 5 | Unlimited |
| On-chain swap fee | 15 bps | 15 bps | 0 bps | 10 bps |
| Performance |  |  |  |  |
| RPS (per app) | 1 RPS | 2 RPS | 25 RPS | Custom |
| 99% Uptime SLA | ✓ | ✓ | ✓ | ✓ |
| Support |  |  |  |  |
| Priority support | - | - | ✓ | ✓ |

### Network Gas Fees (Polygon)

Both protocols only charge standard Polygon network gas fees:
- **0x Transaction:** 0.0056 POL ($0.0011)
- **1inch Transaction:** 0.0053 POL ($0.0011)

**Key Finding:** Both protocols have comparable gas costs with minimal difference in network fees. However, fee structures differ significantly:

- **1inch**: Infrastructure fees range from 2-10 bps for stable coins and 5-30 bps for non-stable coins depending on plan tier
- **0x**: On-chain swap fees range from 0 bps (Growth plan) to 15 bps (Free/Starter plans), with Enterprise at 10 bps

For high-volume usage, 0x Growth plan offers 0 bps fees while 1inch Business plan charges 2-5 bps. For lower volume usage, both protocols have similar fee structures.

## Performance Comparison

### Execution Rates

| Protocol | Expected Output | Actual Output | Efficiency |
| --- | --- | --- | --- |
| 0x | 0.18405 USDC | 0.18405 USDC | 100% |
| 1inch (T1) | 0.182175 USDC | 0.182175 USDC | 100% |
| 1inch (T2) | 0.182088 USDC | 0.182088 USDC | 100% |

### Gas Efficiency

| Protocol | Gas Estimated | Gas Used | Efficiency Ratio |
| --- | --- | --- | --- |
| 0x | 284,044 | 186,861 | 65.8% |
| 1inch (T1) | 250,000 | 193,690 | 77.5% |
| 1inch (T2) | - | 158,302 | Optimized |

### Liquidity Sources

- **0x Protocol:** Uniswap V3
- **1inch Protocol:** 1inch aggregated liquidity

## Implementation Differences

### 0x Protocol Advantages

1. **Permit2 Integration**: Single-transaction swaps without separate approval
2. **Mature Ecosystem**: Well-established protocol with extensive documentation
3. **Gas Predictability**: Consistent gas estimation patterns
4. **Uniswap V3 Integration**: Direct access to concentrated liquidity

### 1inch Protocol Advantages

1. **No Protocol Fees**: Guaranteed no additional charges
2. **Advanced Gas Optimization**: Chi gas token and algorithmic optimizations
3. **Flexible API**: Comprehensive quote and swap endpoints
4. **Dynamic Gas Calculation**: Real-time gas optimization suggestions
5. **Approval Management**: Built-in allowance checking and management

## Technical Implementation Notes

### 0x Integration

- Uses Permit2 signatures for streamlined execution
- Requires careful gas estimation and buffer calculation
- Direct interaction with Uniswap V3 pools

### 1inch Integration

- RESTful API with clear endpoints
- Built-in approval checking reduces transaction failures
- Dynamic gas suggestions improve execution success rates
- Better handling of slippage and price impact

## Recommendations

### Primary Recommendation: 1inch Protocol

**Rationale:**

1. **Better User Experience**: Built-in approval management and dynamic gas optimization
2. **Reliability**: Multiple successful executions with consistent performance
3. **Future-Proof**: Continuous optimization and feature development

## Conclusion

Both 1inch and 0x Protocol provide excellent DEX aggregation capabilities with nuanced fee structures:

**Fee Comparison:**
- **0x Protocol**: Offers 0 bps fees on Growth plan ($500/month for 6M calls), 15 bps on Free/Starter plans
- **1inch Protocol**: Infrastructure fees of 2-10 bps (stable coins) and 5-30 bps (non-stable coins) across all plans

**Strategic Considerations:**
- **High Volume**: 0x Growth plan provides the most cost-effective solution with 0 bps fees
- **Medium Volume**: Both protocols offer competitive pricing with similar fee structures
- **Low Volume**: 1inch may be more cost-effective due to lower entry-level fees and no monthly subscription requirements

Additionally, 1inch offers superior gas optimization, better user experience through built-in approval management, and a more developer-friendly API structure.

The implementation successfully demonstrates that both protocols serve as viable solutions, with the optimal choice depending on transaction volume, fee sensitivity, and technical requirements.

## Verification

All findings can be verified through the following Polygonscan transactions:
- **0x Transaction**: [0x2b8befae6e726852c76a3066e4ea06be1af9b7e03782e5372b9a8ffbd75e6086](https://polygonscan.com/tx/0x2b8befae6e726852c76a3066e4ea06be1af9b7e03782e5372b9a8ffbd75e6086)
- **1inch Transaction 1**: [0x9d0c53ac18c448b83f0f1143795f189cd602570cb2df5008cb686af67980f91c](https://polygonscan.com/tx/0x9d0c53ac18c448b83f0f1143795f189cd602570cb2df5008cb686af67980f91c)
- **1inch Transaction 2**: [0xe837268865f98952f024deda22a057ab65e71e443e687d95bee4fc8ae2958a1e](https://polygonscan.com/tx/0xe837268865f98952f024deda22a057ab65e71e443e687d95bee4fc8ae2958a1e)

## Further considerations

- We have to account for the fact that the wallets we create for our customers must have a tiny bit of POL token to pay for network fees in order for the BRLA-to-USDC swaps to happen. We can use the gasless API or we can TOPUP POL from a Westy-owned wallet to their newly created non-custodial wallet. Check: https://0x.org/docs/gasless-api/guides/understanding-gasless-api. 1inch’s Fusion API has the same “gasless transaction” effect, even though it uses a different system - dutch auction -  when compared to 0x’s gasless api, which uses a relay wallet to pay for gas
- Consider the use of CHI Gas Tokens for gas fees optimization [https://soliditydeveloper.com/chi-gas](https://soliditydeveloper.com/chi-gas)
- Use advanced features like Fusion+ from 1inch for cross-chain swaps (something to look at if we consider using Solana - not supported as of 2025-0711  - [https://1inch.io/fusion/](https://1inch.io/fusion/))
    - 0x yet to support Solana too: [https://www.youtube.com/watch?v=CNwSQwxINPc&ab_channel=0x](https://www.youtube.com/watch?v=CNwSQwxINPc&ab_channel=0x)

## References

- https://www.youtube.com/watch?v=fSEaZmbt3K0&ab_channel=ETHGlobal

---

*Report generated for WES-1116: Rebuild Nethereum PoC using 1inch API instead of 0x*