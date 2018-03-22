using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Extensions;

// Key - Rule name, Value - number of suggestions and number of accepted.
using Result = System.Collections.Generic.Dictionary<STN.PriceEngine.Enums.SuggestionRules, STN.PriceEngine.Services.SuggestionsAccepted>;
using SourceSug = STN.PriceEngine.Context.PeAuto_RuleTicketEvents4;

namespace STN.PriceEngine.Services
{
    public delegate void PriceEngineEventHandler(string message);

    public delegate void PriceEngineProgressChangedEventHandler(double percentage);

    public class PriceEngineService : IPriceEngineService
    {
        public event PriceEngineEventHandler EventHappened;
        public event PriceEngineProgressChangedEventHandler ProgressChanged;

        public const int MaxPrice = 400;
        public const int MinPrice = 30;

        //Days constants
        public const int OneDay = 1;
        public const int TwoDays = 2;
        public const int ThreeDays = 3;
        public const int FourDays = 4;
        public const int FiveDays = 5;
        public const int SixDays = 6;
        public const int SevenDays = 7;

        public const int TenDays = 10;
        public const int TwelveDays = 12;
        public const int ThirtyDays = 30;

        //Percentage constants
        public const int Five = 5;
        public const int Ten = 10;
        public const int Fifteen = 15;
        public const int Twenty = 20;
        public const int Thirty = 30;

        public const int HighestChange = 30;
        public const int LowestChange = 10;

        public const int TopSuggestionsNumber = 100000;

        private int _processed;
        private double _oneSuggestionPercentage;

        private readonly IDataService _dataService;

        public PriceEngineService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public List<SourceSug> GetNewSuggestions()
        {
            return _dataService.GetNewSuggestions(TopSuggestionsNumber);
        }

        public Result ProcessSuggestions(List<SourceSug> suggestions)
        {
            if (suggestions == null)
                throw new ArgumentNullException(nameof(suggestions));

            var result = InitResult(suggestions);

            _oneSuggestionPercentage = 100.0/suggestions.Count;

            Parallel.ForEach(suggestions, suggestion =>
            {
                //Unbroadcast any listings that are suggested for higher than $400
                if (suggestion.SugPrice > MaxPrice)
                {
                    Unbroadcast(suggestion, result, forceUnbroadcast: true);
                }
                else
                {
                    switch (suggestion.Action)
                    {
                        case SuggestionRulesStrings.Double:
                            //Automatically accept
                            Double(suggestion, result);
                            break;
                        case SuggestionRulesStrings.Lower:
                            //Set Price to 30$ if SugPrice < 30$
                            Lower(suggestion, result);
                            break;
                        case SuggestionRulesStrings.Raise:
                            Raise(suggestion, result);
                            break;
                        case SuggestionRulesStrings.Unbroadcast:
                            //May change Price
                            Unbroadcast(suggestion, result);
                            break;
                        case SuggestionRulesStrings.RaiseAndBroadcast:
                            RaiseAndBroadcast(suggestion, result);
                            break;
                    }
                }
            });

            return result;
        }

        private void SaveResultToDatabase(SourceSug sug, Result result, bool runRule)
        {
            _dataService.SaveResultToDatabase(sug, runRule);

            UpdateResult(sug, result, runRule);

            OnProgressChanged(GetProcessedPercentage());
        }

        private static void UpdateResult(SourceSug suggestion, Result result, bool runRule)
        {
            if (runRule)
            {
                switch (suggestion.Action)
                {
                    case SuggestionRulesStrings.Lower:
                        result[SuggestionRules.Lower].Accepted++;
                        break;
                    case SuggestionRulesStrings.Raise:
                        result[SuggestionRules.Raise].Accepted++;
                        break;
                    case SuggestionRulesStrings.Double:
                        result[SuggestionRules.Double].Accepted++;
                        break;
                    case SuggestionRulesStrings.RaiseAndBroadcast:
                        result[SuggestionRules.RaiseAndBroadcast].Accepted++;
                        break;
                    case SuggestionRulesStrings.Unbroadcast:
                        result[SuggestionRules.Unbroadcast].Accepted++;
                        break;
                }
            }
        }

        private double GetProcessedPercentage()
        {
            return (_oneSuggestionPercentage * (++_processed));
        }

        private static Result InitResult(List<SourceSug> suggestions)
        {
            var result = new Result();

            var suggested = suggestions.Count(x => x.Action == SuggestionRulesStrings.RaiseAndBroadcast);
            result.Add(SuggestionRules.RaiseAndBroadcast, new SuggestionsAccepted { Suggestions = suggested });

            suggested = suggestions.Count(x => x.Action == SuggestionRulesStrings.Unbroadcast);
            result.Add(SuggestionRules.Unbroadcast, new SuggestionsAccepted { Suggestions = suggested });

            suggested = suggestions.Count(x => x.Action == SuggestionRulesStrings.Double);
            result.Add(SuggestionRules.Double, new SuggestionsAccepted { Suggestions = suggested });

            suggested = suggestions.Count(x => x.Action == SuggestionRulesStrings.Raise);
            result.Add(SuggestionRules.Raise, new SuggestionsAccepted { Suggestions = suggested });

            suggested = suggestions.Count(x => x.Action == SuggestionRulesStrings.Lower);
            result.Add(SuggestionRules.Lower, new SuggestionsAccepted { Suggestions = suggested });

            return result;
        }

        //RAISE + BROADCAST
        private void RaiseAndBroadcast(SourceSug suggestion, Result results)
        {
            var runRule = false;

            //Event within 7 days
            if (suggestion.EventDateWithin(SevenDays)
                //Must have had RAISE+BROADCAST for past 5 days with price +/‐ $10
                && HasForPast(FiveDays, suggestion, LowestChange))
            {
               runRule = true;
            }
            //Event 7‐30 days
            else if (suggestion.EventDateFromTo(SevenDays, ThirtyDays)
                //Must have had RAISE+BROADCAST for past 4 days with price +/‐ $10
                && HasForPast(FourDays, suggestion, LowestChange))
            {
                runRule = true;
            }
            //Event > 30 days
            else if (
                //Must have had RAISE+BROADCAST for past 3 days with price +/‐ $10
                HasForPast(ThreeDays, suggestion, LowestChange))
            {
                runRule = true;
            }

            SaveResultToDatabase(suggestion, results, runRule);
        }

        //RAISE
        private void Raise(SourceSug suggestion, Result results)
        {
            if (!suggestion.Event_Date.HasValue)
                throw new ArgumentNullException(nameof(suggestion.Event_Date));

            var runRule = false;

            //Event within 7 days
            if (suggestion.EventDateWithin(SevenDays))
            {
                if (
                    //Change < $10 and sug has RAISE for past 2 days
                    (suggestion.ChangeLower(LowestChange) && HasForPast(TwoDays, suggestion))
                    //Change $10 ‐ $30 and sug has RAISE $10‐$30 for past 1 days
                    || (suggestion.ChangeHigher(LowestChange) && suggestion.ChangeLowerOrEquals(HighestChange)
                        && HasForPast(OneDay, suggestion, LowestChange, HighestChange))
                    //Change > $30
                    || suggestion.ChangeHigher(HighestChange))
                {
                    runRule = true;
                }
            }
            //Event 7-30 days
            else if (suggestion.EventDateFromTo(SevenDays, ThirtyDays))
            {
                if (
                    //Change < $10 and sug has RAISE for past 4 days
                    (suggestion.ChangeLower(LowestChange) && HasForPast(FourDays, suggestion))
                    //Change $10 ‐ $30 and sug has RAISE $10‐$30 for past 3 days
                    || (suggestion.ChangeHigherOrEquals(LowestChange) && suggestion.ChangeLowerOrEquals(HighestChange) 
                        && HasForPast(ThreeDays, suggestion, LowestChange, HighestChange))
                    //Change > $30 and sug has RAISE +/‐ $10 for past 2 days
                    || (suggestion.ChangeHigher(HighestChange) && HasForPast(TwoDays,suggestion, LowestChange)))
                {
                    runRule = true;
                }
            }
            //Event > 30 days
            else
            {
                if (
                    //Change < $10 and sug has RAISE for past 6 days
                    (suggestion.ChangeLower(LowestChange) && HasForPast(SixDays,suggestion))
                    //Change $10 ‐ $30 and sug has RAISE $10‐$30 for past 5 days
                    || (suggestion.ChangeHigher(LowestChange) && suggestion.ChangeLowerOrEquals(HighestChange) 
                        && HasForPast(FiveDays, suggestion, LowestChange, HighestChange))
                    //Change > $30 and sug has RAISE +/‐ $10 for past 4 days
                    || (suggestion.ChangeHigher(HighestChange) && HasForPast(FourDays, suggestion, LowestChange)))
                {
                    runRule = true;
                }
            }
            SaveResultToDatabase(suggestion, results, runRule);
        }

        //LOWER
        private void Lower(SourceSug sug, Result results)
        {
            if (sug == null)
                throw new ArgumentNullException(nameof(sug));

            sug.CheckRequiredFields(price: false, sugPrice: true, runDate: false, eventDate: false);

            //Any listings that are suggested for lower than $30 should be priced at $30, don’t run rule
            if (sug.SugPrice < MinPrice)
            {
                sug.Price = MinPrice;

                SaveResultToDatabase(sug, results, runRule: true);

                OnEventHappened(SuggestionRulesStrings.Lower + ": SugPrice is lower than " + MinPrice + "$! Price was set to " + MinPrice + "$.");
                return;
            }

            sug.CheckRequiredFields(price: true, sugPrice:false, runDate:false, eventDate:true);

            var runRule = false;

            //Event within 7 days
            if (sug.EventDateWithin(SevenDays))
            {
                if (
                    //Change < $10 and sug has LOWER for past 2 days
                    (sug.ChangeLower(LowestChange) && HasForPast(TwoDays, sug))
                    //Change $10 ‐ $30 and sug has LOWER $10‐$30 for past 3 days
                    || (sug.ChangeHigherOrEquals(LowestChange) && sug.ChangeLowerOrEquals(HighestChange)
                        && HasForPast(ThreeDays, sug, LowestChange, HighestChange)))
                {
                    runRule = true;
                }
            }
            //Event 7‐30 days
            else if (sug.EventDateFromTo(SevenDays,ThirtyDays))
            {
                if (
                    //Change < $10 and sug has LOWER for past 3 days
                    (sug.ChangeLower(LowestChange) && HasForPast(ThreeDays, sug))
                    //Change $10 ‐ $30 and sug has LOWER $10‐$30 for past 4 days
                    || (sug.ChangeHigherOrEquals(LowestChange) && sug.ChangeLowerOrEquals(HighestChange)
                        && HasForPast(FourDays, sug, LowestChange, HighestChange))
                    //Change > $30 and sug has LOWER +/‐ $10 for past 7 days
                    || (sug.ChangeHigher(HighestChange) && HasForPast(SevenDays, sug, LowestChange)))
                {
                    runRule = true;
                }
            }
            //Event > 30 days
            else
            {
                if (
                    //Change < $10 and sug has LOWER for past 5 days
                    (sug.ChangeLower(LowestChange) && HasForPast(FiveDays, sug))
                    //Change $10 ‐ $30 and sug has LOWER $10‐$30 for past 7 days
                    || (sug.ChangeHigherOrEquals(LowestChange) && sug.ChangeLowerOrEquals(HighestChange)
                        && HasForPast(SevenDays, sug, LowestChange, HighestChange))
                    //Change > $30 and sug has LOWER +/‐ $10 for past 10 days
                    || (sug.ChangeHigher(HighestChange) && HasForPast(TenDays, sug, LowestChange)))
                {
                    runRule = true;
                }
            }
            SaveResultToDatabase(sug, results, runRule);
        }

        private bool HasForPast(int days, SourceSug sug, int minPriceDifference = -1, int maxPriceDifference = -1)
        {
            return _dataService.HasForPast(days, sug, minPriceDifference, maxPriceDifference);
        }

        //DOUBLE
        private void Double(SourceSug suggestion, Result results)
        {
            SaveResultToDatabase(suggestion, results, runRule: true);
        }

        //UNBROADCAST
        private void Unbroadcast(SourceSug sug, Result results, bool forceUnbroadcast = false)
        {
            if (forceUnbroadcast)
            {
                sug.Action = SuggestionRulesStrings.Unbroadcast;

                SaveResultToDatabase(sug, results, runRule: true);

                OnEventHappened("Suggested price is higher than " + MaxPrice + "$ (actually " + sug.SugPrice + ").");
                OnEventHappened("Force " + SuggestionRulesStrings.Unbroadcast + " applied.");

                return;
            }

            var runRule = true;

            //Event < 12 days
            if (sug.EventDateWithin(TwelveDays))
            {
                int day = GetDayForUnbroudcast(sug);
                
                switch (day)
                {
                    //First day -> add 20% to price
                    case 1:
                        sug.IncreasePrice(percentage: Twenty);
                        break;
                    //Second day -> add 20% to price
                    case 2:
                        sug.IncreasePrice(percentage: Twenty);
                        break;
                    //Third day -> add 30% to price
                    case 3:
                        sug.IncreasePrice(percentage: Thirty);
                        break;
                    //Fourth day -> Unbroadcast
                    case 4:
                        break;
                    default:
                        runRule = false;
                        break;
                }
            }
            //Event 12‐30 days
            else if (sug.EventDateFromTo(TwelveDays, ThirtyDays))
            {
                int day = GetDayForUnbroudcast(sug);

                // 12 == it is first day, 13 == second day, etc
                switch (day)
                {
                    //Second day -> add 10% to price
                    case 1 + TwelveDays:
                        sug.IncreasePrice(percentage: Ten);
                        break;
                    //Third day -> add 15% to price
                    case 2 + TwelveDays:
                        sug.IncreasePrice(percentage: Fifteen);
                        break;
                    //Fourth day -> add 20% to price
                    case 3 + TwelveDays:
                        sug.IncreasePrice(percentage: Twenty);
                        break;
                    //Fifth day -> Unbroadcast
                    case 4 + TwelveDays:
                        break;
                    default:
                        runRule = false;
                        break;
                }
            }
            //Event > 30 days
            else
            {
                int day = GetDayForUnbroudcast(sug);

                switch (day)
                {
                    //Second day -> add 5% to price
                    case 2 + ThirtyDays:
                        sug.IncreasePrice(percentage: Five);
                        break;
                    //Third day -> add 10% to price
                    case 3 + ThirtyDays:
                        sug.IncreasePrice(percentage: Ten);
                        break;
                    //Fourth day or fifth day -> add 15% to price
                    case 4 + ThirtyDays:
                    case 5 + ThirtyDays:
                        sug.IncreasePrice(percentage: Fifteen);
                        break;
                    //Sixth day -> add 20% to price
                    case 6 + ThirtyDays:
                        sug.IncreasePrice(percentage: Twenty);
                        break;
                    //Seventh day - Unbroadcast
                    case 7 + ThirtyDays:
                        break;
                    default:
                        runRule = false;
                        break;
                }
            }
            SaveResultToDatabase(sug, results, runRule);
        }

        private int GetDayForUnbroudcast(SourceSug sug)
        {
            var nowDate = DateTime.Now.GetDate();
            return sug.Event_Date.Value.Subtract(nowDate).Days;
        }

        private void OnEventHappened(string message)
        {
            EventHappened?.Invoke(message);
        }

        private void OnProgressChanged(double percentage)
        {
            ProgressChanged?.Invoke(percentage);
        }
        
    }
    public sealed class SourceSugDto
    {
        public int? local_ticket_id { get; set; }

        public string Action { get; set; }

        public int? Price { get; set; }

        public int? SugPrice { get; set; }

        public DateTime? RunDate { get; set; }

        public DateTime? Event_Date { get; set; }
    }

    public class SuggestionsAccepted
    {
        public int Suggestions { get; set; }
        public int Accepted { get; set; }
    }

    public class PriceEngineServiceSavingSuggestionException : Exception
    {
        private readonly DateTime _runDate;
        private readonly int _localTicketId;
        private readonly string _message;

        public PriceEngineServiceSavingSuggestionException(DateTime runDate, int localTicketId, string message = null)
        {
            _runDate = runDate;
            _localTicketId = localTicketId;
            _message = message;
        }
        public override string Message
        {
            get
            {
                return (string.IsNullOrEmpty(_message)
                    ? "Error during saving suggestion to Database! "
                    : _message) 
                    + " RunDate: "+_runDate.ToString("MM/dd/yyyy") + ", local_ticket_id: "+_localTicketId+".";
            }
        }
    }
}
