using System.Collections.Generic;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    public class ScreenLayoutRecord : Record
    {
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<ScreenRecord> Screens { get; set; }
    }
}
