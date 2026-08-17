namespace FSO.Common
{
    public struct EventCatalogEntry
    {
        public string label;
        public int value;
        public string startDate;
        public string endDate;
    }

    public struct EventModifierGift
    {
        public string title;
        public string description;
        public uint[] guids;
    }

    public struct EventModifierOption
    {
        public string name;
        public string label;
        public string category;
        public string unique;
        public Dictionary<string, float> tuning;
        public bool enableTimed;

        // optional
        public EventModifierGift? gift;
        public string startDate;
        public string endDate;
        public bool enableManual;
    }

    public struct EventModifier
    {
        public string name;
        public string label;
        public string type;
        public string startDate;
        public string endDate;
        public EventModifierOption[] options;
    }

    public struct EventConfig
    {
        public bool timed;
        public EventCatalogEntry[] catalog;
        public EventModifier[] modifiers;
        public float? skillSpeed;
        public float? payoutScale;
        public float? singleplayerPenalty;
        public int? speedyJobProgression;

        public static EventConfig FromJson(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<EventConfig>(json);
        }

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static (DateTime, DateTime) GetNextRange(string start, string end)
        {
            var startDate = GetNextDayMonth(start);
            var endDate = GetNextDayMonth(end);

            var now = DateTime.UtcNow;

            if (startDate > endDate)
            {
                // This implies the event carries through the end of the year into next year.
                if (now > endDate)
                {
                    // Start date is this year, end date is next
                    endDate = endDate.AddYears(1);
                }
                else
                {
                    // Start date was last year (event is currently active)
                    startDate = startDate.AddYears(-1);
                }
            }
            else if (now > endDate)
            {
                // If we're after the end date, move it to next year.
                startDate = startDate.AddYears(1);
                endDate = endDate.AddYears(1);
            }

            return (startDate, endDate);
        }

        private static DateTime GetNextDayMonth(string dayMonth)
        {
            var split = dayMonth.Split('-');

            if (split.Length != 2 || !int.TryParse(split[0], out int day) || !int.TryParse(split[1], out int month))
            {
                throw new InvalidDataException("Event date not correctly formatted, should be day-month.");
            }

            var now = DateTime.UtcNow;

            return new DateTime(now.Year, month, day);
        }
    }
}
