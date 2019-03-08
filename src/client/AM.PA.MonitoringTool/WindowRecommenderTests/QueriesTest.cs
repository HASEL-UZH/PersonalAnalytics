using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Data;
using Shared.Data.Fakes;
using System;
using System.IO;
using WindowRecommender;
using WindowRecommender.Data;

namespace WindowRecommenderTests
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
                Queries.CreateTables();
                Assert.IsTrue(_db.HasTable(Settings.EventTable));
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
                Queries.SaveEvent(new IntPtr(1), "test_process", EventName.Focus, 2, 0.5, 1);
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Focus", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
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
                Queries.SaveEvent(new IntPtr(1), "test_process", EventName.Open);
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Open", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(-1D, dataTable.Rows[0]["score"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["zIndex"]);
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
                Queries.SaveEvent(new IntPtr(1), "test_process", EventName.Close, 1, 0.5);
                var dataTable = _db.ExecuteReadQuery($@"SELECT * FROM {Settings.EventTable};");
                Assert.AreEqual(1, dataTable.Rows.Count);
                Assert.AreEqual("1", dataTable.Rows[0]["windowId"]);
                Assert.AreEqual("Close", dataTable.Rows[0]["event"]);
                Assert.AreEqual("test_process", dataTable.Rows[0]["processName"]);
                Assert.AreEqual(1L, dataTable.Rows[0]["rank"]);
                Assert.AreEqual(0.5, dataTable.Rows[0]["score"]);
                Assert.AreEqual(-1L, dataTable.Rows[0]["zIndex"]);
            }
        }
    }
}
