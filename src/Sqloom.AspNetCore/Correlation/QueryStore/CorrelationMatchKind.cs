namespace Sqloom.Correlation.QueryStore;

public enum CorrelationMatchKind
{
    StatementHandleExact = 0,
    QueryTextExact = 1,
    FingerprintFallback = 2,
    Unmatched = 3,
}
