using System;

namespace UserInputTracker.Models
{
    internal class UserInputAggregate
    {
        internal DateTime TsStart { get; set; }
        internal DateTime TsEnd { get; set; }

        internal int KeyTotal { get; set; }
        internal int KeyOther { get; set; }
        internal int KeyBackspace { get; set; }
        internal int KeyNavigate { get; set; }

        internal int ClickTotal { get; set; }
        internal int ClickOther { get; set; }
        internal int ClickLeft { get; set; }
        internal int ClickRight { get; set; }

        internal int ScrollDelta { get; set; }

        internal int MovedDistance { get; set; }
    }
}
