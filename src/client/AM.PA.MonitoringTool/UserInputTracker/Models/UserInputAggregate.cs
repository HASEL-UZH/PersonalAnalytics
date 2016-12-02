using System;

namespace UserInputTracker.Models
{
    public class UserInputAggregate
    {
        internal DateTime TsStart { get; set; }
        internal DateTime TsEnd { get; set; }

        public int KeyTotal { get; set; }
        internal int KeyOther { get; set; }
        internal int KeyBackspace { get; set; }
        internal int KeyNavigate { get; set; }

        public int ClickTotal { get; set; }
        internal int ClickOther { get; set; }
        internal int ClickLeft { get; set; }
        internal int ClickRight { get; set; }

        public int ScrollDelta { get; set; }

        public int MovedDistance { get; set; }
    }
}
