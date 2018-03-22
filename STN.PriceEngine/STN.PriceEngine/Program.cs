using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using STN.PriceEngine.Context;
using STN.PriceEngine.DependencyContainar;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Extensions;
using STN.PriceEngine.Services;
using STN.PriceEngine.UtilityServices;

// Key - Rule name, Value - number of suggestions and number of accepted.
using Result = System.Collections.Generic.Dictionary<STN.PriceEngine.Enums.SuggestionRules, STN.PriceEngine.Services.SuggestionsAccepted>;

namespace STN.PriceEngine
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly string NewLine = Environment.NewLine;
        private static readonly string StartFinishSign = "".PadRight(30,'=');

        private static readonly IPriceEngineService PriceEngineService;

        private static readonly object _sync = new object();

        private static DateTime _startTime;
        public static readonly string TimeFormat = "HH:mm:ss";
        private static int _totalNewSuggestions;

        static Program()
        {
            ContainerConfigurator.Configure(builder =>
            {
                DependencyContainer.Builder(builder);
            });

            PriceEngineService = ServiceLocator.GetInstance<IPriceEngineService>();
        }

        static void Main(string[] args)
        {
            Log(NewLine + StartFinishSign + " Starting program... " + StartFinishSign + NewLine);

            Log("Getting data... (top "+Services.PriceEngineService.TopSuggestionsNumber+ " suggestion(s))");

            List<PeAuto_RuleTicketEvents4> suggestions;
            try
            {
                suggestions = PriceEngineService.GetNewSuggestions();

                _totalNewSuggestions = suggestions.Count;

                Log(suggestions.Count+" new suggestion(s) found.");
            }
            catch (Exception ex)
            {
                Log("Error during access data! " + ex.Message, true);
                return;
            }

            // subscribe to listen price event engine events (writing to log file and console)
            PriceEngineService.EventHappened += Log;

            // subscribe to listen processing progress changed event (writing to console)
            PriceEngineService.ProgressChanged += HandleProcessingProgressChangedEvent;

            Result results;
            try
            {
                Logger.Info("Processing...");
                _startTime = DateTime.Now;

                results = PriceEngineService.ProcessSuggestions(suggestions);
            }
            catch (PriceEngineServiceSavingSuggestionException ex)
            {
                Log(ex.Message, true);
                return;
            }
            catch (Exception ex)
            {
                Log("Error during processing suggestions! " + ex.Message, true);

                if (!string.IsNullOrEmpty(ex.InnerException?.Message))
                    Log("Inner exception: "+ex.InnerException.Message);

                return;
            }

            var elapsedTime = new DateTime((DateTime.Now - _startTime).Ticks);

            var message = "Processing... 100% processed. Precessed "+_totalNewSuggestions+" suggestions. Elapsed: " + elapsedTime.ToString(TimeFormat);

            Logger.Info(message);
            Console.Write("\r"+message+NewLine+NewLine);

            DisplayResults(results);

            Log(NewLine + StartFinishSign + " Closing program...  " + StartFinishSign + NewLine);
        }

        private static void HandleProcessingProgressChangedEvent(double percentage)
        {
            lock (_sync)
            {
                var perc = string.Format("{0:0.000}", percentage);
                try
                {
                    perc = perc.RemoveEndZerosAfterDot();

                    if (perc.EndsWith("."))
                        perc = perc.Remove(perc.IndexOf('.'));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }

                var percMessage = ("Processing... " + perc + "% processed.").PadRight(33);
                Console.Write('\r' + percMessage);

                if (percentage % 10 == 0)
                    Logger.Info(percMessage);

                var elapsedTime = new DateTime((DateTime.Now - _startTime).Ticks);
                Console.Write("Elapsed: " + elapsedTime.ToString(TimeFormat));

                var remainingItems = (long) (_totalNewSuggestions - _totalNewSuggestions*percentage/100);
                var processedItems = (long)(_totalNewSuggestions * percentage / 100);

                var remainingTime = new DateTime((elapsedTime.Ticks / processedItems) * remainingItems);

                Console.Write(" Remaining: " + remainingTime.ToString(TimeFormat) + " ");
            }
        }

        private static void DisplayResults(Result results)
        {
            if (results == null || !results.Any())
            {
                Log("Any suggestions have not been accepted!"+NewLine);
                return;
            }

            const int digits = 5;
            foreach (var res in results)
            {
                var operation = res.Key == SuggestionRules.RaiseAndBroadcast ? SuggestionRulesStrings.RaiseAndBroadcast : res.Key.ToString().ToUpper();
                Log("RULE: "+ operation.PadLeft(SuggestionRulesStrings.RaiseAndBroadcast.Length) 
                    + "; SUGGESTED: "+ res.Value.Suggestions.ToString().PadLeft(digits) + "; ACCEPTED: " + res.Value.Accepted.ToString().PadLeft(digits) + ".");
            }
        }

        private static void Log(string message)
        {
            Logger.Info(message);
            Console.WriteLine(message+NewLine);
        }

        private static void Log(string message, bool error)
        {
            if (error)
                Logger.Error(message);
            else
                Logger.Info(message);

            Console.WriteLine(message+NewLine);
        }
    }
}
