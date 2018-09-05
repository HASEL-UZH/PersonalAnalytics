using System.Collections.Generic;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib
{
    public interface IGetScreenRecords
    {
        IList<ScreenRecord> CurrentScreens { get; }
    }
}
