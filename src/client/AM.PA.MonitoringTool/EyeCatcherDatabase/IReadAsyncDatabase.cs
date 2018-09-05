using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase
{
    public interface IReadAsyncDatabase
    {
        Task<DesktopRecord> GetDesktopWithChildrenAsync(int id);
        Task<DesktopRecord> GetDesktopWithChildrenAsync(DateTime dateTime);
        Task<DesktopPointRecord> GetFixationPointAtTimeAsync(DateTime dateTime);
        Task<DesktopPointRecord> GetMousePointAtTimeAsync(DateTime dateTime);
        Task<DesktopRecord> GetFirstDesktopRecordWithChildrenAsync();
        Task<DesktopRecord> GetLastDesktopRecordAsync();
        Task<List<ScreenRecord>> GetScreensAsync();
        Task<Rectangle> GetVirtualScreenRectangleAsync();
    }
}
