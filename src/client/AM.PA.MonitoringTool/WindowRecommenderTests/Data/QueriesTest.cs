using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Data;
using Shared.Data.Fakes;
using System;
using System.IO;
using WindowRecommender;
using WindowRecommender.Data;

namespace WindowRecommenderTests.Data
{
    [TestClass]
    public class QueriesTest
    {
        private DatabaseImplementation _db;
        private string _exportFilePath;

        [TestInitialize]
        public void Initialize()
        {
            // Set file path to temp to keep tests messages out of log
            _exportFilePath = Shared.Settings.ExportFilePath;
            Shared.Settings.ExportFilePath = Path.GetTempPath();
            _db = new DatabaseImplementation(":memory:");
            _db.Connect();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db.Disconnect();
            Shared.Settings.ExportFilePath = _exportFilePath;
        }

        [TestMethod]
        public void TestCreateTables()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Assert.IsFalse(_db.HasTable(Settings.EventTable));
                Queries.CreateTables();
                Assert.IsTrue(_db.HasTable(Settings.EventTable));
            }
        }

        [TestMethod]
        public void TestCreateTables_Again()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Assert.IsFalse(_db.HasTable(Settings.EventTable));
                Queries.CreateTables();
                Assert.IsTrue(_db.HasTable(Settings.EventTable));
                Queries.CreateTables();
                Assert.IsTrue(_db.HasTable(Settings.EventTable));
            }
        }

        [TestMethod]
        public void TestDropTables()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Assert.IsFalse(_db.HasTable(Settings.EventTable));
                Queries.CreateTables();
                Assert.IsTrue(_db.HasTable(Settings.EventTable));
                Queries.DropTables();
                Assert.IsFalse(_db.HasTable(Settings.EventTable));
            }
        }

        [TestMethod]
        public void TestSaveEvent_Focus()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Queries.CreateTables();
                Assert.AreEqual(0L, _db.ExecuteScalar2($@"SELECT COUNT(*) FROM {Settings.EventTable};"));
                Queries.SaveEvent(EventName.Focus, new DatabaseEntry(new IntPtr(1), "test_title", "test_process", 1, 2, 0.5));
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Focus", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual("test_title", dataTable.Rows[0]["windowTitle"]);
                Assert.AreEqual(2L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(0.5, dataTable.Rows[0]["score"]);
                Assert.AreEqual(1L, dataTable.Rows[0]["zIndex"]);
            }
        }

        [TestMethod]
        public void TestSaveEvent_Open()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Queries.CreateTables();
                Assert.AreEqual(0L, _db.ExecuteScalar2($@"SELECT COUNT(*) FROM {Settings.EventTable};"));
                Queries.SaveEvent(EventName.Open, new DatabaseEntry(new IntPtr(1), "test_title", "test_process", 0));
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Open", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual("test_title", dataTable.Rows[0]["windowTitle"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(-1D, dataTable.Rows[0]["score"]);
                Assert.AreEqual(0L, dataTable.Rows[0]["zIndex"]);
            }
        }

        [TestMethod]
        public void TestSaveEvent_Close()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Queries.CreateTables();
                Assert.AreEqual(0L, _db.ExecuteScalar2($@"SELECT COUNT(*) FROM {Settings.EventTable};"));
                Queries.SaveEvent(EventName.Close, new DatabaseEntry(new IntPtr(1), "test_title", "test_process", 1, 2, 0.5));
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Close", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual("test_title", dataTable.Rows[0]["windowTitle"]);
                Assert.AreEqual(2L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(0.5, dataTable.Rows[0]["score"]);
                Assert.AreEqual(1L, dataTable.Rows[0]["zIndex"]);
            }
        }

        [TestMethod]
        public void TestSaveEvent_Minimize()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Queries.CreateTables();
                Assert.AreEqual(0L, _db.ExecuteScalar2($@"SELECT COUNT(*) FROM {Settings.EventTable};"));
                Queries.SaveEvent(EventName.Minimize, new DatabaseEntry(new IntPtr(1), "test_title", "test_process", 1, 2, 0.5));
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Minimize", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual("test_title", dataTable.Rows[0]["windowTitle"]);
                Assert.AreEqual(2L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(0.5, dataTable.Rows[0]["score"]);
                Assert.AreEqual(1L, dataTable.Rows[0]["zIndex"]);
            }
        }

        [TestMethod]
        public void TestSaveEvents()
        {
            using (ShimsContext.Create())
            {
                ShimDatabase.GetInstance = () => _db;
                Queries.CreateTables();
                Assert.AreEqual(0L, _db.ExecuteScalar2($@"SELECT COUNT(*) FROM {Settings.EventTable};"));
                Queries.SaveEvents(EventName.Initial, new[]
                {
                    new DatabaseEntry(new IntPtr(1), "test_title1", "test_process1", 1),
                    new DatabaseEntry(new IntPtr(2), "test_title2", "test_process2", 2)
                });
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(2, dataTable.Rows.Count);

                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Initial", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process1", dataTable.Rows[0]["processName"]);
                Assert.AreEqual("test_title1", dataTable.Rows[0]["windowTitle"]);
                Assert.AreEqual(1L, dataTable.Rows[0]["zIndex"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(-1D, dataTable.Rows[0]["score"]);

                Assert.AreEqual("2", dataTable.Rows[1]["windowId"]);
                Assert.AreEqual("Initial", dataTable.Rows[1]["event"]);
                Assert.AreEqual("test_process2", dataTable.Rows[1]["processName"]);
                Assert.AreEqual("test_title2", dataTable.Rows[1]["windowTitle"]);
                Assert.AreEqual(2L, dataTable.Rows[1]["zIndex"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(-1D, dataTable.Rows[0]["score"]);
            }
        }
    }
}
