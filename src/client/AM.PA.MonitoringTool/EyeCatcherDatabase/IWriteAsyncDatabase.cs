using System.Collections.Generic;
using System.Threading.Tasks;
using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase
{
    public interface IWriteAsyncDatabase
    {
        Task InsertAsync(ScreenLayoutRecord screens);
        Task InsertAllAsync<T>(IEnumerable<T> records) where T : Record;
        Task InsertOrReplaceAsync(DesktopRecord desktopRecord);
        Task InsertAsync(CopyPasteRecord copyPasteRecord);
        Task InsertAsync(UserPresenceRecord userPresence);
    }
}
