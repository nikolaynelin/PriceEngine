namespace STN.PriceEngine.Enums
{
    public enum SuggestionRules
    {
        Lower,
        Raise,
        Double,
        Unbroadcast,
        RaiseAndBroadcast
    }

    public static class SuggestionRulesStrings
    {
        public const string Lower = "LOWER";
        public const string Raise = "RAISE";
        public const string Double = "DOUBLE";
        public const string Unbroadcast = "UNBROADCAST";
        public const string RaiseAndBroadcast = "RAISE + BROADCAST";
        public const string None = "NONE";
    }
}
