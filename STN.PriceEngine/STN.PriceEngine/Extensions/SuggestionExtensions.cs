using System;
using STN.PriceEngine.Context;
using STN.PriceEngine.Enums;

namespace STN.PriceEngine.Extensions
{
    public static class SuggestionExtensions
    {
        //Event_Date < 7 days
        public static bool EventDateWithin(this PeAuto_RuleTicketEvents4 sug, int days)
        {
            return sug.Event_Date.Value.AddDays(-days) < GetNowDate();
        }

        private static DateTime GetNowDate()
        {
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        }

        public static bool ChangeLower(this PeAuto_RuleTicketEvents4 sug, int price)
        {
            switch (sug.Action)
            {
                case SuggestionRulesStrings.Lower:
                    return sug.Price.Value - sug.SugPrice.Value < price;
                case SuggestionRulesStrings.Raise:
                    return sug.SugPrice.Value - sug.Price.Value < price;
            }
            throw new ArgumentException(nameof(sug.Action));
        }

        public static bool ChangeHigherOrEquals(this PeAuto_RuleTicketEvents4 sug, int price)
        {
            switch (sug.Action)
            {
                case SuggestionRulesStrings.Lower:
                    return sug.Price.Value - sug.SugPrice.Value >= price;
                case SuggestionRulesStrings.Raise:
                    return sug.SugPrice.Value - sug.Price.Value >= price;
            }
            throw new ArgumentException(nameof(sug.Action));
        }

        public static bool ChangeLowerOrEquals(this PeAuto_RuleTicketEvents4 sug, int price)
        {
            switch (sug.Action)
            {
                case SuggestionRulesStrings.Lower:
                    return sug.Price.Value - sug.SugPrice.Value <= price;
                case SuggestionRulesStrings.Raise:
                    return sug.SugPrice.Value - sug.Price.Value <= price;
            }
            throw new ArgumentException(nameof(sug.Action));
        }

        public static void CheckRequiredFields(this PeAuto_RuleTicketEvents4 sug, bool price, bool sugPrice, bool runDate, bool eventDate)
        {
            if (price && !sug.Price.HasValue)
                throw new ArgumentNullException(nameof(sug.Price));

            if (sugPrice && !sug.SugPrice.HasValue)
                throw new ArgumentNullException(nameof(sug.SugPrice));

            if (runDate && !sug.RunDate.HasValue)
                throw new ArgumentNullException(nameof(sug.RunDate));

            if (eventDate && !sug.Event_Date.HasValue)
                throw new ArgumentNullException(nameof(sug.Event_Date));
        }

        public static bool ChangeHigher(this PeAuto_RuleTicketEvents4 sug, int price)
        {
            switch (sug.Action)
            {
                case SuggestionRulesStrings.Lower:
                    return sug.Price.Value - sug.SugPrice.Value > price;
                case SuggestionRulesStrings.Raise:
                    return sug.SugPrice.Value - sug.Price.Value > price;
            }
            throw new ArgumentException(nameof(sug.Action));
        }

        //Event_Date >= from and <= to
        public static bool EventDateFromTo(this PeAuto_RuleTicketEvents4 sug, int from, int to)
        {
            var nowDate = GetNowDate();

            return sug.Event_Date.Value.AddDays(-from) >= nowDate && sug.Event_Date.Value.AddDays(-to) <= nowDate;
        }

        public static void IncreasePrice(this PeAuto_RuleTicketEvents4 sug, int percentage)
        {
            double onePercentage = sug.Price.Value/100.0;
            sug.Price += (int)Math.Round(onePercentage * percentage, 0);
        }
    }
}
