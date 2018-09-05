using System;
using System.IO;
using DryIoc;
using EyeCatcher.DataCollection;
using EyeCatcherDatabase;
using EyeCatcherDatabase.Records;
using EyeCatcherLib;
using EyeCatcherLib.Providers;

namespace EyeCatcher
{
    internal static class IocHelper
    {
        private const string ApplicationName = "EyeCatcher";
        private static Container _container;

        public static Container Container => _container ?? (_container = new Container(rules => rules.WithoutThrowOnRegisteringDisposableTransient()));

        public static void Register()
        {
            var databaseFile = GetDatabasePath();
            Container.RegisterMany<ScreenLayoutRecordProvider>(Reuse.Singleton);
            Container.Register<IObservable<UserPresenceRecord>, UserPresenceProvider>(Reuse.Singleton);
            Container.Register<CursorPointProvider>(Reuse.Singleton);
            Container.Register<LastUserInputTicksProvider>(Reuse.Singleton);
            Container.Register<FixationPointProvider>(Reuse.Singleton);
            Container.Register<IWriteAsyncDatabase, SqLiteAsyncDatabase>(Made.Of(() => new SqLiteAsyncDatabase(Arg.Index<string>(0)), dbFilePath => databaseFile));
            Container.Register<IWindowRecordProvider, WindowRecordProvider>(Reuse.Singleton);
            Container.Register<IWindowRecordCollection, WindowRecordCollection>();
            // Decorator for WindowRecordCollection
            Container.Register<ObservableWindowRecordCollection>();
            Container.Register<WindowManager>();
            Container.Register<DataCollector>();
        }

        private static string GetDatabasePath()
        {
            var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var eyeCatcherFolder = Path.Combine(localFolder, ApplicationName);
            Directory.CreateDirectory(eyeCatcherFolder);
            var databaseFile = Path.Combine(eyeCatcherFolder, "EyeCatcher.sqlite3");
            return databaseFile;
        }
    }
}
