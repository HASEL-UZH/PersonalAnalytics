using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using EyeCatcherDatabase.Enums;
using EyeCatcherDatabase.Records;
using SQLite;
using SQLiteNetExtensionsAsync.Extensions;

namespace EyeCatcherDatabase
{
    // ReSharper disable once ClassNeverInstantiated.Global - Justification: IoC
    public class SqLiteAsyncReadOnlyDatabase : IReadAsyncDatabase
    {
        private readonly SQLiteAsyncConnection _dbConnection;

        public SqLiteAsyncReadOnlyDatabase(string dbFilePath)
        {
            _dbConnection = new SQLiteAsyncConnection(dbFilePath);
        }

        #region Get

        public async Task<DesktopPointRecord> GetFixationPointAtTimeAsync(DateTime dateTime)
        {
            var record = await _dbConnection.Table<DesktopPointRecord>().Where(t => t.Type == DesktopPointType.Fixation && t.Timestamp <= dateTime).OrderByDescending(t => t.Timestamp).FirstOrDefaultAsync();
            return record?.Timestamp > dateTime - TimeSpan.FromSeconds(1) ? record : null;
        }

        public async Task<DesktopPointRecord> GetMousePointAtTimeAsync(DateTime dateTime)
        {
            var record = await _dbConnection.Table<DesktopPointRecord>().Where(t => t.Type == DesktopPointType.MousePosition && t.Timestamp <= dateTime).OrderByDescending(t => t.Timestamp).FirstOrDefaultAsync();
            return record?.Timestamp > dateTime - TimeSpan.FromSeconds(1) ? record : null;
        }

        public Task<DesktopRecord> GetDesktopWithChildrenAsync(int id)
        {
            return _dbConnection.GetWithChildrenAsync<DesktopRecord>(id, true);
        }

        public async Task<DesktopRecord> GetDesktopWithChildrenAsync(DateTime dateTime)
        {
            var desktopRecord = await _dbConnection.Table<DesktopRecord>().Where(t => t.Timestamp <= dateTime)
                .OrderByDescending(t => t.Timestamp).FirstOrDefaultAsync();
            if (desktopRecord == null)
            {
                return null;
            }

            desktopRecord = await _dbConnection.GetWithChildrenAsync<DesktopRecord>(desktopRecord.Id, true);
            return desktopRecord;
        }

        public async Task<DesktopRecord> GetFirstDesktopRecordWithChildrenAsync()
        {
            var desktopRecord = await _dbConnection.Table<DesktopRecord>().OrderBy(t => t.Timestamp).FirstOrDefaultAsync();
            if (desktopRecord == null)
            {
                return null;
            }
            return await _dbConnection.GetWithChildrenAsync<DesktopRecord>(desktopRecord.Id, true);
        }

        public Task<DesktopRecord> GetLastDesktopRecordAsync()
        {
            return _dbConnection.Table<DesktopRecord>().OrderByDescending(d => d.Timestamp).FirstOrDefaultAsync();
        }


        public Task<List<ScreenRecord>> GetScreensAsync()
        {
            return _dbConnection.Table<ScreenRecord>().ToListAsync();
        }

        public async Task<Rectangle> GetVirtualScreenRectangleAsync()
        {
            var screens = await GetScreensAsync();
            var firstScreen = screens.FirstOrDefault();
            if (firstScreen == null)
            {
                return Rectangle.Empty;
            }
            var virtualScreenRectangle = firstScreen.Bounds;
            for (var i = 1; i < screens.Count; i++)
            {
                if (screens[i] == null)
                {
                    continue;
                }

                if (screens[i].Bounds == Rectangle.Empty)
                {
                    continue;
                }

                virtualScreenRectangle = Rectangle.Union(virtualScreenRectangle, screens[i].Bounds);
            }
            return virtualScreenRectangle;
        }

        #endregion

    }
}
