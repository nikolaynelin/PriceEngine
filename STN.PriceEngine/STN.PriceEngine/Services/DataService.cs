using System;
using System.Collections.Generic;
using System.Linq;
using STN.PriceEngine.Context;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Extensions;
using STN.PriceEngine.Repositories;

namespace STN.PriceEngine.Services
{
    public class DataService:IDataService
    {
        public List<PeAuto_RuleTicketEvents4> GetNewSuggestions(int top)
        {
            using (var context = new StnContext())
            {
                var repo = new Repository(context);
                return repo.SqlQuery<SourceSugDto>(@"SELECT TOP({0}) * FROM [dbo].[NewSuggestions]", top)
                    .Select(x => new PeAuto_RuleTicketEvents4
                    {
                        Action = x.Action,
                        Price = x.Price,
                        SugPrice = x.SugPrice,
                        RunDate = x.RunDate,
                        Event_Date = x.Event_Date,
                        local_ticket_id = x.local_ticket_id
                    }).ToList();
            }
        }

        public void SaveResultToDatabase(PeAuto_RuleTicketEvents4 sug, bool runRule)
        {
            try
            {
                using (var context = new StnContext())
                {
                    var repo = new Repository(context);

                    repo.ExecuteSqlCommand(@"EXEC [dbo].[pe_acceptSuggestion]
                                              @local_ticket_id = {0},
                                              @RunDate = {1},
                                              @RunRule = {2},
                                              @Action = {3},
                                              @Price = {4}", sug.local_ticket_id.Value, sug.RunDate.Value, runRule, sug.Action, sug.Price);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error during saving sug to Dabatase.";
                throw new PriceEngineServiceSavingSuggestionException(sug.RunDate.Value, sug.local_ticket_id.Value, message + " Message: " + ex.Message);
            }
        }

        public bool HasForPast(int days, PeAuto_RuleTicketEvents4 sug, int minPriceDifference = -1, int maxPriceDifference = -1)
        {
            sug.CheckRequiredFields(price: false, sugPrice: false, runDate: true, eventDate: false);

            var minDate = sug.RunDate.Value.AddDays(-days);

            List<PeAuto_RuleTicketEvents4> suggestions;

            using (var context = new StnContext())
            {
                var repo = new Repository(context);

                suggestions = repo.Get<PeAuto_RuleTicketEvents4>(x =>
                    x.local_ticket_id == sug.local_ticket_id
                    && x.Action == sug.Action
                    && x.RunDate >= minDate
                    && x.RunDate < sug.RunDate.Value)
                    .OrderBy(x => x.RunDate)
                    .ToList();
            }

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
