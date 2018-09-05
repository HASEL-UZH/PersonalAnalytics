using System.Collections.Generic;
using System.Threading.Tasks;
using EyeCatcherDatabase.Records;
using SQLite;
using SQLiteNetExtensions.Extensions;

namespace EyeCatcherDatabase
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification: IoC
    /// <summary>
    /// https://bitbucket.org/twincoders/sqlite-net-extensions
    /// https://stackoverflow.com/questions/tagged/sqlite-net-extensions
    /// 
    /// Performance
    /// https://medium.com/@JasonWyatt/squeezing-performance-from-sqlite-insertions-971aff98eef2
    /// -> Use transactions explicitly, otherwise every query will be wrapped with an implicit transaction
    /// -> SQLite only writes the inserts to disk once the transaction has been committed (minimize the number of transactions = maximize performance).
    /// </summary>
    public class SqLiteAsyncDatabase : IWriteAsyncDatabase
    {
        private readonly string _dbFilePath;
        private SQLiteAsyncConnection _dbConnection;

        public SqLiteAsyncDatabase(string dbFilePath)
        {
            _dbFilePath = dbFilePath;
        }

        private async Task Initialize()
        {
            if (_dbConnection != null)
            {
                return;
            }
            _dbConnection = new SQLiteAsyncConnection(_dbFilePath);
            await _dbConnection.CreateTableAsync<ScreenLayoutRecord>();
            await _dbConnection.CreateTableAsync<ScreenRecord>();
            await _dbConnection.CreateTableAsync<WindowRecord>();
            await _dbConnection.CreateTableAsync<DesktopPointRecord>();
            await _dbConnection.CreateTableAsync<CopyPasteRecord>();
            await _dbConnection.CreateTableAsync<DesktopTransitionRecord>();
            await _dbConnection.CreateTableAsync<DesktopWindowLinkRecord>();
            await _dbConnection.CreateTableAsync<DesktopRecord>();
            await _dbConnection.CreateTableAsync<UserPresenceRecord>();
            await _dbConnection.CreateTableAsync<LastUserInputRecord>();
        }

        #region Insert

        public async Task InsertAsync(ScreenLayoutRecord screens)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertWithChildren(screens));
        }

        public async Task InsertAsync(UserPresenceRecord userPresence)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertWithChildren(userPresence));
        }

        public async Task InsertAllAsync(IEnumerable<DesktopPointRecord> desktopPoints)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertAllWithChildren(desktopPoints));
        }

        public async Task InsertAllAsync(IEnumerable<LastUserInputRecord> lastUserInputRecords)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertAllWithChildren(lastUserInputRecords));
        }


        /// <summary>
        /// Need InsertOrReplace here, sinde WindowRecords should stay unique.
        /// Otherwise already existing WindowRecords will be inserted a second time 
        /// </summary>
        public async Task InsertOrReplaceAsync(DesktopRecord desktopRecord)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertOrReplaceWithChildren(desktopRecord, true));
        }

        public async Task InsertAsync(CopyPasteRecord copyPasteRecord)
        {
            await Initialize();
            await _dbConnection.RunInTransactionAsync(connection => connection.InsertWithChildren(copyPasteRecord));
        }

        #endregion
    }
}
