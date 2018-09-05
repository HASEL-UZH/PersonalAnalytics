namespace EyeCatcherDatabase.Records
{
    public class LastUserInputRecord : Record
    {
        /// <summary>
        /// The tick count when the last input event was received.
        /// 
        /// </summary>
        public uint Ticks { get; set; }
    }
}
