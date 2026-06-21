namespace Sqloom.Correlation.QueryStore;

public enum QueryStoreCorrelationMatchKind
{
    StatementHandleExact = 0,
    QueryTextExact = 1,
    FingerprintFallback = 2,
    Unmatched = 3,
}
