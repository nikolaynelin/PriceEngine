using System;
using STN.PriceEngine.Context;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Extensions;
using STN.PriceEngine.Repositories;
using STN.PriceEngine.Test.Infrastructure.FactoryUnit;

namespace STN.PriceEngine.Test.Factories
{
    public class SuggestionFactory : BaseFactory<PeAuto_RuleTicketEvents4>
    {
        public SuggestionFactory(IRepository repository) : base(repository)
        {
        }

        protected override PeAuto_RuleTicketEvents4 CreateNew()
        {
            var now = DateTime.Now;
            return new PeAuto_RuleTicketEvents4
            {
                Action = SuggestionRulesStrings.Lower,
                Price = 30,
                SugPrice = 30,
                RunDate = now.GetDate(),
                local_ticket_id = 1,
                Event_Date = now.GetDate(),
                Event_Time = now
            };
        }
    }
}
