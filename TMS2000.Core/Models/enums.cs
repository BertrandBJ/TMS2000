namespace TMS2000.Core.Models;

public enum PriceMode { Stable, SlowRise, FastRise, SlowFall, FastFall, Chaotic }

public enum ConditionOperator { LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual }

public enum EffectType { BuyBlocked, SellBlocked, QuantityLimit, TransactionTax }

public enum TransactionType { Buy, Sell }

public enum TransactionStatus { Pending, Accepted, Rejected }