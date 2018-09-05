using System.Collections.Generic;
using System.Threading.Tasks;
using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase
{
    public interface IWriteAsyncDatabase
    {
        Task InsertAsync(ScreenLayoutRecord screens);
        Task InsertAllAsync(IEnumerable<DesktopPointRecord> desktopPoints);
        Task InsertAllAsync(IEnumerable<LastUserInputRecord> lastUserInputRecords);
        Task InsertOrReplaceAsync(DesktopRecord desktopRecord);
        Task InsertAsync(CopyPasteRecord copyPasteRecord);
        Task InsertAsync(UserPresenceRecord userPresence);
    }
}
