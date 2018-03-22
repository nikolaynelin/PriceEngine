using System;
using System.Collections.Generic;
using System.Linq;
using STN.PriceEngine.Context;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Extensions;
using STN.PriceEngine.Services;

namespace STN.PriceEngine.Test.TestServices
{
    public class FakeDataService : IDataService
    {
        public static List<PeAuto_RuleTicketEvents4> Data { get; set; }

        public List<PeAuto_RuleTicketEvents4> GetNewSuggestions(int top)
        {
            return Data;
        }

        public void SaveResultToDatabase(PeAuto_RuleTicketEvents4 sug, bool runRule) { }

        public bool HasForPast(int days, PeAuto_RuleTicketEvents4 sug, int minPriceDifference = -1, int maxPriceDifference = -1)
        {
            sug.CheckRequiredFields(price: false, sugPrice: false, runDate: true, eventDate: false);

            var minDate = sug.RunDate.Value.AddDays(-days);

            var suggestions = Data.Where(x =>
                 x.local_ticket_id == sug.local_ticket_id
                 && x.Action == sug.Action
                 && x.RunDate >= minDate
                 && x.RunDate < sug.RunDate.Value)
                  .OrderBy(x => x.RunDate)
                  .ToList();

            if (suggestions.Count < days)
                return false;

            var i = 0;
            foreach (var curr in suggestions)
            {
                sug.CheckRequiredFields(price: true, sugPrice: true, runDate: true, eventDate: false);

                if (curr.RunDate != minDate.AddDays(i++))
                    return false;

                var diff = curr.Price.Value - curr.SugPrice.Value;

                if (maxPriceDifference != -1)
                {
                    if (sug.Action == SuggestionRulesStrings.Raise)
                        diff = curr.SugPrice.Value - curr.Price.Value;

                    if (diff < minPriceDifference || diff > maxPriceDifference)
                        return false;
                }
                else if (minPriceDifference != -1)
                {
                    if (Math.Abs(diff) > minPriceDifference)
                        return false;
                }
            }

            return true;
        }
    }
}
