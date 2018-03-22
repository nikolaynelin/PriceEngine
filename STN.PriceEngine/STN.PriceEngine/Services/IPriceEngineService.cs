using System.Collections.Generic;
using STN.PriceEngine.Context;
using STN.PriceEngine.Enums;

namespace STN.PriceEngine.Services
{
    public interface IPriceEngineService
    {
        event PriceEngineEventHandler EventHappened;
        event PriceEngineProgressChangedEventHandler ProgressChanged;

        List<PeAuto_RuleTicketEvents4> GetNewSuggestions();
        Dictionary<SuggestionRules, SuggestionsAccepted> ProcessSuggestions(List<PeAuto_RuleTicketEvents4> suggestions);
    }
}
