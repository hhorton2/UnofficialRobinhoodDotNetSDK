using Robinhood.Domain.Enum;

namespace Robinhood.Domain.Requests
{
    public class HistoricalQuoteRequest
    {
        public string Symbol { get; set; }
        public HistoricalInterval Interval { get; set; }
        public HistoricalSpan Span { get; set; }
        public HistoricalBounds Bounds { get; set; }
    }
}