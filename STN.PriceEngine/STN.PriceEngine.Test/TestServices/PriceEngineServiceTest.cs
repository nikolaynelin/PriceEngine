using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using STN.PriceEngine.Enums;
using STN.PriceEngine.Test.Extensions;
using STN.PriceEngine.Extensions;
using STN.PriceEngine.Services;

using Sug = STN.PriceEngine.Context.PeAuto_RuleTicketEvents4;
using SugList = System.Collections.Generic.List<STN.PriceEngine.Context.PeAuto_RuleTicketEvents4>;

namespace STN.PriceEngine.Test.TestServices
{
    [TestFixture]
    public class PriceEngineServiceTest
    {
        public static readonly FakeDataService FakeDataService = new FakeDataService();

        #region Helpers

        private static Sug SetDefault(Sug sug = null)
        {
            var now = DateTime.Now;

            if (sug == null)
                sug = new Sug();

            sug.Action = sug.Action ?? SuggestionRulesStrings.Lower;
            sug.Price = sug.Price ?? 30;
            sug.SugPrice = sug.SugPrice ?? 30;
            sug.RunDate = sug.RunDate ?? now.GetDate();
            sug.Event_Date = sug.Event_Date ?? now.GetDate();
            sug.Event_Time = sug.Event_Time == DateTime.MinValue ? now : sug.Event_Time;

            return sug;
        }

        private static void SetDefaults(List<Sug> sugList)
        {
            sugList.ForEach(x =>
            {
                SetDefault(x);
            });
        }

        private static SugList PrepareData(Sug sug = null, SugList sugList = null)
        {
            if (sugList != null)
            {
                SetDefaults(sugList);
            }
            else if (sug != null)
            {
                sugList = new SugList();
                SetDefault(sug);
            }

            FakeDataService.Data = sugList;

            return sug != null ? new SugList { sug } : null;
        }
        
        #endregion The end of Helpers

        public IPriceEngineService PriceEngineService
        {
            get { return new PriceEngineService(FakeDataService); }
        }

        [Test]
        public void ProcessSuggestions_Must_Do_Unbroadcast_If_SugPrice_Higher_400()
        {
            //arrange
            var sug = new Sug
            {
                SugPrice = 401
            };

            var data = new SugList {sug};

            FakeDataService.Data = data;

            //act
            var results = PriceEngineService.ProcessSuggestions(data);

            //assert

            Assert.IsTrue(results[SuggestionRules.Unbroadcast].Accepted == 1);
        }


        #region TESTS FOR LOWER RULE

        [Test]
        public void ProcessSuggestions_For_Lower_Must_Set_Price_To_30_If_SugPriceLower30()
        {
            //arrange
            var minPrice = 30;
            var sug = new Sug
            {
                Price = minPrice,
                SugPrice = minPrice - 1,
                Action = SuggestionRulesStrings.Lower
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(30, sug.Price);
            Assert.AreEqual(1, result[SuggestionRules.Lower].Suggestions);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ProcessSuggestions_Must_Throw_ArgumentNullException_If_SugIsNull()
        {
            //arrange
            FakeDataService.Data = null;

            //act
            PriceEngineService.ProcessSuggestions(null);
        }

        [Test]
        [ExpectedException(typeof (AggregateException))]
        public void ProcessSuggestions_Must_Throw_ArgumentNullException_If_SugEventDateIsNull()
        {
            //arrange
            var sug = new Sug
            {
                Action = SuggestionRulesStrings.Lower
            };

            FakeDataService.Data = new SugList {sug};

            //act
            PriceEngineService.ProcessSuggestions(FakeDataService.Data);
        }

        #region Lower: Event within 7 days (< 7 days)

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [Test]
        public void ProcessSuggestions_For_Lower_MustAcceptSug_If_EventWithin7days_And_ChangeLower10_And_SugHasLowerForPast2days(int daysTillEvent)
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                Action = SuggestionRulesStrings.Lower,
                RunDate = runDate,
                Event_Date = eventDate,
                SugPrice = 40,
                Price = 41
            };

            var sugList = new SugList {sug};

            2.Times(x => sugList.Add(new Sug
            {
                RunDate = runDate.AddDays(-(++x)),
                Action = SuggestionRulesStrings.Lower,
                Price = sug.Price,
                SugPrice = sug.SugPrice
            }));

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Suggestions, "Suggestions");
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted, "Accepted");
        }

        [Test]
        public void ProcessSuggestions_For_Lower_Must_Reject_Sug_If_EventWithin7days_And_ChangeLower10_And_SugHasNotLowerForPast2days()
        {
            //arrange
            var sug = new Sug {Event_Date = DateTime.Now};

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.IsTrue(result.Values.All(x => x.Accepted == 0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [Test]
        public void ProcessSuggestions_For_Lower_Must_AcceptSug_If_EventWithin7days_And_ChangeBetween10And30_And_SugHasLowerForPast3days(int daysTillEvent)
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                Action = SuggestionRulesStrings.Lower,
                SugPrice = 35,
                Price = 50,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            3.Times(x =>
            {
                sugList.Add(new Sug
                {
                    Action = SuggestionRulesStrings.Lower,
                    RunDate = runDate.AddDays(-(++x)),
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert

            Assert.AreEqual(1, result[SuggestionRules.Lower].Suggestions);
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        [Test]
        public void ProcessSuggestions_For_Lower_Must_Reject_Sug_If_EventWithin7days_And_ChangeBetween10And30_And_SugHasNotLowerForPast3days()
        {
            //arrange
            var sug = new Sug
            {
                Price = 45,
                SugPrice = 34,
                Event_Date = DateTime.Now
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.IsTrue(result.Values.All(x => x.Accepted == 0));
        }

        [Test]
        [ExpectedException(typeof (AggregateException))]
        public void ProcessSuggestions_For_Lower_Must_Throw_ArgumentNullException_If_SugPriceIsNull()
        {
            //arrange
            var sug = SetDefault();
            sug.SugPrice = null;

            var data = new List<Sug> {sug};
            FakeDataService.Data = data;

            //act
            PriceEngineService.ProcessSuggestions(data);
        }

        [Test]
        [ExpectedException(typeof (AggregateException))]
        public void ProcessSuggestions_For_Lower_Must_Throw_ArgumentNullException_If_PriceIsNull()
        {
            //arrange
            var sug = SetDefault();
            sug.Price = null;

            var data = new SugList {sug};
            FakeDataService.Data = data;

            //act
            PriceEngineService.ProcessSuggestions(data);
        }

        #endregion The end of Lower - Event within 7 days

        #region Lower: Event 7-30 days

        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [Test]
        public void ProcessSuggestions_For_Lower_Must_AcceptSug_If_7to30daysTillEvent_And_ChangeLower10_And_SugHasLowerForPast3days(int daysTillEvent)
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 30,
                Price = 31,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            3.Times(x =>
            {
                var newSug = new Sug
                {
                    RunDate = runDate.AddDays(-(++x))
                };
                sugList.Add(newSug);
            });

            SetDefaults(sugList);

            var data = new SugList {sug};
            FakeDataService.Data = sugList;

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        //change must be => 10$ and <= 30$
        [Test]
        public void ProcessSuggestions_For_Lower_Must_AcceptSug_If_7to30daysTillEvent_And_ChangeBetween10And30_And_SugHasLowerForPast4daysWithChangeBetween10And30()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(29);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 69,
                Price = 80,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            4.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Price = 41,
                    SugPrice = 30
                });
            });

            SetDefaults(sugList);
            FakeDataService.Data = sugList;

            var data = new SugList { sug };

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        //change must be <= 10$ and >= -10$
        [Test]
        public void ProcessSuggestions_For_Lower_Must_AcceptSug_If_7to30daysTillEvent_And_ChangeHigher30_And_SugHasLowerForPast7daysWithChangeRange10()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(8);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 69,
                Price = 100,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            7.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x))
                });
            });

            SetDefaults(sugList);
            FakeDataService.Data = sugList;

            var data = new SugList { sug };

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        //change must be <= 10$ and >= -10$
        [Test]
        public void ProcessSuggestions_For_Lower_Must_AcceptSug_If_Event30days_And_ChangeHigher30_And_SugHasLowerForPast7daysWithChangeRange10()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(30);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 69,
                Price = 100,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            7.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x))
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        [Test]
        public void ProcessSuggestions_For_Lower_Must_Reject_Sug_If_7to30daysTillEvent_And_ChangeBetween10And30_And_SugHasNotLowerForPast()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(8);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 69,
                Price = 80,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(0, result[SuggestionRules.Lower].Accepted);
        }

        #endregion The end of Lower - Event 7-30 days

        #region Lower: Event > 30 days

        [TestCase(31)]
        [TestCase(40)]
        [TestCase(50)]
        [Test]
        public void ProcessSuggestions_ForLower_Must_AcceptSug_If_More30daysTillEvent_And_ChangeLower10_And_SugHasLowerForPast5days(int daysTillEvent)
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 69,
                Price = 70,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            5.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x))
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        //change must be >= 10$ and <= 30$
        [Test]
        public void ProcessSuggestions_ForLower_Must_AcceptSug_If_More30daysTillEvent_And_ChangeBetween10And30_And_SugHasLowerForPast7daysWithChangeBetween10And30()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(31);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                Event_Date = eventDate,
                SugPrice = 69,
                Price = 80,
                RunDate = runDate
            };

            var sugList = new SugList {sug};

            7.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Price = 41,
                    SugPrice = 30
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted);
        }

        //change must be < 10$
        [Test]
        public void ProcessSuggestions_ForLower_Must_AcceptSug_If_More30daysTillEvent_And_ChangeHigher30_And_SugHasLowerForPast10daysWithChangeLower10()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(31);
            var runDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                SugPrice = 30,
                Price = 71,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            10.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x))
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Lower].Accepted, "Accepted");
        }

        //change must be > 10$
        [Test]
        public void ProcessSuggestions_ForLower_Must_Reject_Sug_If_More30daysTillEvent_And_ChangeHigher30_And_SugHasLowerForPast10daysWithChangeHigher10()
        {
            //arrange
            var now = DateTime.Now;

            var eventDate = new DateTime(now.Year, now.Month, now.Day).AddDays(31);
            var runDate = new DateTime(now.Year, now.Month, now.Day);

            var sug = new Sug
            {
                SugPrice = 70,
                Price = 101,
                RunDate = runDate,
                Event_Date = eventDate
            };

            var sugList = new SugList {sug};

            10.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Price = 41,
                    SugPrice = 30
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(0, result[SuggestionRules.Lower].Accepted);
        }

        #endregion The end of Lower - Event > 30 days

        #endregion THE END OF TESTS FOR LOWER RULE


        #region TESTS FOR RAISE RULE

        #region Raise: Event within 7 days

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If_EventWithin7days_And_ChangeLower10_And_SugHasRaiseForPast2days(int daysTillEvent)
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 39,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            2.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        //change must be >= 10$ and <= 30$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If_EventWithin7days_And_ChangeBetween10And30_And_SugHas_RaiseForPast1day_With_ChangeBetween10And30()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 41,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            sugList.Add(new Sug
            {
                RunDate = runDate.AddDays(-1),
                Action = sug.Action,
                Price = sug.Price,
                SugPrice = sug.SugPrice
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        //change must be > 30$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If_EventWithin7days_And_ChangeHigher30()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate();

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 71,
                Action = SuggestionRulesStrings.Raise
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        #endregion The end of Raise - Event within 7 days

        #region Raise: Event 7-30 days

        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(10)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If7To30daysTillEvent_And_ChangeLower10_And_SugHasRaiseForPast4days(int daysTillEvent)
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 39,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            4.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        // change must be >= 10$ and <= 30$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If7To30daysTillEvent_And_ChangeBetween10And30_And_SugHasRaiseForPast4daysWithChangeBetween10and30()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(8);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 40,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            4.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        // change must be <= 10$ and >= -10$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If7To30daysTillEvent_And_ChangeHigher30_And_SugHasRaiseForPast2daysWithChangeRange10()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(8);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 61,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            2.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = 30,
                SugPrice = 40,
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        #endregion The end of Raise - Event 7-30 days

        #region Raise: Event > 30 days

        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_If_More30daysTillEvent_And_ChangeLower10_And_SugHasRaiseForPast6days()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(31);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 39,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            6.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        //change must be >= 10$ and <= 30$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_More30daysTillEvent_And_ChangeBetween10And30_And_SugHasRaiseForPast5daysWithChangeBenween10And30()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(31);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 41,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            5.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        //change must be <= 10$ and >= -10$
        [Test]
        public void ProcessSuggestions_For_Raise_Must_AcceptSug_More30daysTillEvent_And_ChangeHigher30_And_SugHasRaiseForPast4daysWithChangeRange10()
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(31);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 61,
                Action = SuggestionRulesStrings.Raise
            };

            var sugList = new SugList {sug};

            4.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = 30,
                    SugPrice = 30
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Raise].Accepted, "Result[RAISE].Accepted");
        }

        #endregion The end of Raise - Event > 30 days

        #endregion THE END OF TESTS FOR RAISE RULE


        #region TESTS FOR UNBROADCAST RULE

        #region Unbroadcast - Event < 12 days

        //event < 12 days and first day -> +20% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_Less12daysTillEvent_And_1stDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(1);

            const int price = 30;
            const int percentage = 20;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //event < 12 days and second day -> +20% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_Less12daysTillEvent_And_2ndDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(2);

            const int price = 30;
            const int percentage = 20;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //event < 12 days and third day -> +30% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add30PercToPrice_If_Less12daysTillEvent_And_3rdDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(3);

            const int price = 30;
            const int percentage = 30;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //event < 12 days and fouth day -> just set RunRule to true
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_If_Less12daysTillEvent_And_4thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(4);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        // > 4 and < 12 days till event -> reject suggestion
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_Reject_Sug_If_4to12daysTillEvent_And_4thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(5);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast,
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(0, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        #endregion The end of Unbroadcast - Event < 12 days

        #region Unbroadcast - Event 12-30 days

        //Event 12‐30 days and second (13 days till event) day -> +10% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add10PercToPrice_If_12to30DaysTillEvent_And_2ndDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(12 + 1);

            const int price = 30;
            const int percentage = 10;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast,
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event 12‐30 days and third day (14 days till event) -> +15% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add15PercToPrice_If_12to30DaysTillEvent_And_3rdDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(12 + 2);

            const int price = 30;
            const int percentage = 15;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event 12‐30 days and fouth day (15 days till event) -> +20% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_12to30DaysTillEvent_And_4thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(12 + 3);

            const int price = 30;
            const int percentage = 20;

            var sug = new Sug
            {
                Price = price,
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event 12‐30 days and fifth day (16 days till event) -> just set RunRule to true
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_If_12to30DaysTillEvent_And_5thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(12 + 4);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        // > 16 and <= 30 days till event -> reject suggestion
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_Reject_Sug_If_12to30DaysTillEvent_And_6thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(12 + 5);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(0, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        #endregion The end of Unbroadcast - Event 12-30 days

        #region Unbroadcast - Event > 30 days

        //Event > 30 days and second day (32 days till event) -> +5% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add5PercToPrice_If_More30daysTillEvent_And_2ndDay()
        {
            //arrange
            const int price = 30;
            const int percentage = 5;

            var eventDate = DateTime.Now.GetDate().AddDays(30 + 2);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = Math.Round(price/100.0*percentage + price, 0);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event > 30 days and third day (33 days till event) -> +10% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add10PercToPrice_If_More30daysTillEvent_And_3rdDay()
        {
            //arrange
            const int price = 30;
            const int percentage = 10;

            var eventDate = DateTime.Now.GetDate().AddDays(30 + 3);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event > 30 days and fourth day (34 days till event) -> +15% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add15PercToPrice_If_More30daysTillEvent_And_4thDay()
        {
            //arrange
            const int price = 30;
            const int percentage = 15;

            var eventDate = DateTime.Now.GetDate().AddDays(30 + 4);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event > 30 days and fifth day (35 days till event) -> +15% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add15PercToPrice_If_More30daysTillEvent_And_5thDay()
        {
            //arrange
            const int price = 30;
            const int percentage = 15;

            var eventDate = DateTime.Now.GetDate().AddDays(30 + 5);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event > 30 days and sixth day (36 days till event) -> +20% to price
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_More30daysTillEvent_And_6thDay()
        {
            //arrange
            const int price = 30;
            const int percentage = 20;

            var eventDate = DateTime.Now.GetDate().AddDays(30 + 6);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");

            var increasedPrice = (int) (price/100.0*percentage + price);
            Assert.AreEqual(increasedPrice, sug.Price, nameof(sug.Price));
        }

        //Event > 30 days and seventh day (37 days till event) -> just set RunRule to true
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_More30daysTillEvent_And_7thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(30 + 7);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        // > 37 days till event -> reject suggestion
        [Test]
        public void ProcessSuggestions_For_Unbroadcast_Must_AcceptSug_And_Add20PercToPrice_If_More30daysTillEvent_And_8thDay()
        {
            //arrange
            var eventDate = DateTime.Now.GetDate().AddDays(30 + 8);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Action = SuggestionRulesStrings.Unbroadcast
            };

            var data = PrepareData(sug);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(0, result[SuggestionRules.Unbroadcast].Accepted, "Result[Unbroadcast].Accepted");
        }

        #endregion The end of Unbroadcast - Event > 30 days

        #endregion THE END OF TESTS FOR UNBROADCAST RULE


        #region TESTS FOR RAISE + BROADCAST RULE

        //Event within 7 days and sug has RAISE+BROADCAST for past 5 days with price +/‐ $10 -> RunRule=true
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [Test]
        public void ProcessSuggestions_Must_AcceptSug_If_EventWithin7days_And_SugHasForPast5daysWithChangeRange10(int daysTillEvent)
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 30,
                Action = SuggestionRulesStrings.RaiseAndBroadcast
            };

            var sugList = new SugList {sug};

            5.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.RaiseAndBroadcast].Accepted, "Result[RAISE+BROADCAST].Accepted");
        }

        //7-30 days till event and sug has RAISE+BROADCAST for past 4 days with price +/‐ $10 -> RunRule=true
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [Test]
        public void ProcessSuggestions_Must_AcceptSug_If_7To30daysTillEvent_And_SugHasForPast4daysWithChangeRange10(int daysTillEvent)
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 30,
                Action = SuggestionRulesStrings.RaiseAndBroadcast
            };

            var sugList = new SugList {sug};

            4.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.RaiseAndBroadcast].Accepted, "Result[RAISE+BROADCAST].Accepted");
        }

        //More 30 days till event and sug has RAISE+BROADCAST for past 3 days with price +/‐ $10 -> RunRule=true
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(40)]
        [Test]
        public void ProcessSuggestions_Must_AcceptSug_If_More30daysTillEvent_And_SugHasForPast3daysWithChangeRange10(int daysTillEvent)
        {
            //arrange
            var runDate = DateTime.Now.GetDate();
            var eventDate = DateTime.Now.GetDate().AddDays(daysTillEvent);

            var sug = new Sug
            {
                Event_Date = eventDate,
                Price = 30,
                SugPrice = 30,
                Action = SuggestionRulesStrings.RaiseAndBroadcast
            };

            var sugList = new SugList {sug};

            3.Times(x =>
            {
                sugList.Add(new Sug
                {
                    RunDate = runDate.AddDays(-(++x)),
                    Action = sug.Action,
                    Price = sug.Price,
                    SugPrice = sug.SugPrice
                });
            });

            var data = PrepareData(sug, sugList);

            //act
            var result = PriceEngineService.ProcessSuggestions(data);

            //assert
            Assert.AreEqual(1, result[SuggestionRules.RaiseAndBroadcast].Accepted, "Result[RAISE+BROADCAST].Accepted");
        }

        #endregion THE END OF TESTS FOR RAISE + BROADCAST RULE

       }
}
