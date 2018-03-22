using System.Collections.Generic;
using STN.PriceEngine.Context;

namespace STN.PriceEngine.Services
{
    public interface IDataService
    {
        List<PeAuto_RuleTicketEvents4> GetNewSuggestions(int top);
        void SaveResultToDatabase(PeAuto_RuleTicketEvents4 sug, bool runRule);
        bool HasForPast(int days, PeAuto_RuleTicketEvents4 sug, int minPriceDifference = -1, int maxPriceDifference = -1);
    }
}
