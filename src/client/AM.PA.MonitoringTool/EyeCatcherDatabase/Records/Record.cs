using System;
using SQLite;

namespace EyeCatcherDatabase.Records
{
    /// <summary>
    /// Using sqlite-net-extensions
    /// see
    /// 
    /// https://bitbucket.org/twincoders/sqlite-net-extensions
    /// https://stackoverflow.com/questions/tagged/sqlite-net-extensions
    /// https://docs.microsoft.com/en-us/xamarin/android/data-cloud/data-access/using-sqlite-orm
    /// </summary>
    public abstract class Record : IRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
