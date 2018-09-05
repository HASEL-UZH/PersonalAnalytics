using System;

namespace EyeCatcherDatabase.Records
{
    public interface IRecord
    {
        int Id { get; set; }
        DateTime Timestamp { get; set; }
    }
}