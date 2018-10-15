using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared;
using Shared.Data;
using System;

namespace SharedTests
{
    [TestClass]
    public class DatabaseImplementationTest
    {
        private DatabaseImplementation _db;

        [TestInitialize]
        public void Initialize()
        {
            _db = new DatabaseImplementation(":memory:");
            _db.Connect();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db.Disconnect();
        }

        [TestMethod]
        public void ConnectTest()
        {
            Assert.IsTrue(_db.HasTable(Settings.SettingsDbTable));
        }

        [TestMethod]
        public void ReconnectTest()
        {
            _db.Reconnect();
            Assert.IsTrue(_db.HasTable(Settings.SettingsDbTable));
        }

        [TestMethod]
        public void DisconnectTest()
        {
            _db.Disconnect();
            _db.Disconnect(); // Twice to test consistent state
            Assert.AreEqual(0, _db.ExecuteDefaultQuery(""));
            Assert.AreEqual(0, _db.ExecuteScalar(""));
            Assert.AreEqual(null, _db.ExecuteScalar2(""));
            Assert.AreEqual(0.0, _db.ExecuteScalar3(""));
            Assert.AreEqual(null, _db.ExecuteReadQuery(""));
            Assert.AreEqual(0, _db.ExecuteBatchQueries(new[] { "" }));
        }

        [TestMethod]
        public void InvalidQueryTest()
        {
            const string query = "SELECT";

            Assert.AreEqual(0, _db.ExecuteDefaultQuery(query));
            Assert.AreEqual(0, _db.ExecuteScalar(query));
            Assert.AreEqual(null, _db.ExecuteScalar2(query));
            Assert.AreEqual(0.0, _db.ExecuteScalar3(query));
            Assert.AreEqual(null, _db.ExecuteReadQuery(query));
        }

        [TestMethod]
        public void HasTableTest()
        {
            _db.Disconnect();
            Assert.ThrowsException<Exception>(() => _db.HasTable("any"));
        }

        [TestMethod]
        public void DefaultSettingsTest()
        {
            const string key = "__test-key__";
            const string stringValue = "abc";
            const bool boolValue = true;
            const int intValue = 123;
            var dateValue = DateTimeOffset.Now;

            Assert.IsFalse(_db.HasSetting(key));
            Assert.AreEqual(stringValue, _db.GetSettingsString(key, stringValue));
            Assert.AreEqual(boolValue, _db.GetSettingsBool(key, boolValue));
            Assert.AreEqual(intValue, _db.GetSettingsInt(key, intValue));
            Assert.AreEqual(dateValue, _db.GetSettingsDate(key, dateValue));
        }

        [TestMethod]
        public void DisconnectedDefaultSettingsTest()
        {
            _db.Disconnect();
            const string key = "__test-key__";
            const string stringValue = "abc";
            const bool boolValue = true;
            const int intValue = 123;
            var dateValue = DateTimeOffset.Now;

            Assert.IsFalse(_db.HasSetting(key));
            Assert.AreEqual(stringValue, _db.GetSettingsString(key, stringValue));
            Assert.AreEqual(boolValue, _db.GetSettingsBool(key, boolValue));
            Assert.AreEqual(intValue, _db.GetSettingsInt(key, intValue));
            Assert.AreEqual(dateValue, _db.GetSettingsDate(key, dateValue));
        }

        [TestMethod]
        public void SettingsStringTest()
        {
            const string key = "__test-key__";
            const string value1 = "abc";
            const string value2 = "def";

            Assert.IsFalse(_db.HasSetting(key));
            _db.SetSettings(key, value1);
            Assert.AreEqual(value1, _db.GetSettingsString(key, ""));
            _db.SetSettings(key, value2);
            Assert.AreEqual(value2, _db.GetSettingsString(key, ""));
        }

        [TestMethod]
        public void SettingsBoolTest()
        {
            const string key = "__test-key__";
            const bool value1 = true;
            const bool value2 = false;

            Assert.IsFalse(_db.HasSetting(key));
            _db.SetSettings(key, value1);
            Assert.AreEqual(value1, _db.GetSettingsBool(key, false));
            _db.SetSettings(key, value2);
            Assert.AreEqual(value2, _db.GetSettingsBool(key, true));
        }

        [TestMethod]
        public void SettingsIntTest()
        {
            const string key = "__test-key__";
            const int value1 = 1;
            const int value2 = 2;

            Assert.IsFalse(_db.HasSetting(key));
            _db.SetSettings(key, value1.ToString());
            Assert.AreEqual(value1, _db.GetSettingsInt(key, 0));
            _db.SetSettings(key, value2.ToString());
            Assert.AreEqual(value2, _db.GetSettingsInt(key, 0));
        }

        [TestMethod]
        public void SettingsIntDefaultOverrideTest()
        // A value of 0 is ignored and the default is returned
        // This test case documents this unexpected behavior
        {
            const string key = "__test-key__";
            const int value = 0;
            const int defaultValue = 2;

            _db.SetSettings(key, value.ToString());
            Assert.AreEqual(defaultValue, _db.GetSettingsInt(key, defaultValue));
        }

        [TestMethod]
        public void SettingsDateTest()
        {
            const string key = "__test-key__";
            var value1 = DateTimeOffset.MinValue;
            var value2 = DateTimeOffset.Parse("2000-03-03 13:13:00");

            Assert.IsFalse(_db.HasSetting(key));
            _db.SetSettings(key, value1.ToString());
            Assert.AreEqual(value1, _db.GetSettingsDate(key, DateTimeOffset.Now));
            _db.SetSettings(key, value2.ToString());
            Assert.AreEqual(value2, _db.GetSettingsDate(key, DateTimeOffset.Now));
        }

        [TestMethod]
        public void PragmaVersionTest()
        {
            const int newVersion = 13;

            Assert.AreEqual(Settings.DatabaseVersion, _db.GetDbPragmaVersion());
            _db.UpdateDbPragmaVersion(newVersion);
            Assert.AreEqual(newVersion, _db.GetDbPragmaVersion());
        }

        [TestMethod]
        public void LogTest()
        {
            const string message = "__test-message__";
            const string query = "SELECT COUNT(*) FROM log WHERE message LIKE ?;";
            var parameter = new object[] { $@"%{message}%" };

            Assert.AreEqual(0, _db.ExecuteScalar(query, parameter));
            _db.LogInfo(message);
            _db.LogWarning(message);
            _db.LogError(message);
            _db.LogErrorUnknown(message);
            // Log table setting is not public
            Assert.AreEqual(4, _db.ExecuteScalar(query, parameter));
        }

        [TestMethod]
        public void TransactionWithParameterTest()
        {
            const string insertQuery = "INSERT INTO transaction_test VALUES (?);";
            var parameter = new object[] { "__test-value__" };

            _db.ExecuteDefaultQuery("CREATE TABLE transaction_test (value TEXT);");
            Assert.AreEqual(0, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE ?;", parameter));
            Assert.AreEqual(2, _db.ExecuteBatchQueries(new[] { insertQuery, insertQuery }, new[] { parameter, parameter }));
            Assert.AreEqual(2, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE ?;", parameter));
        }

        [TestMethod]
        public void TransactionWithFewerParameterTest()
        // When the number of queries and number of parameter arrays don't match, only the lower number of statements are executed.
        {
            const string insertQuery = "INSERT INTO transaction_test VALUES (?);";
            var parameter = new object[] { "__test-value__" };

            _db.ExecuteDefaultQuery("CREATE TABLE transaction_test (value TEXT);");
            Assert.AreEqual(0, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE ?;", parameter));
            Assert.AreEqual(1, _db.ExecuteBatchQueries(new[] { insertQuery, insertQuery }, new[] { parameter }));
            Assert.AreEqual(1, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE ?;", parameter));
        }

        [TestMethod]
        public void TransactionWithoutParameterTest()
        {
            const string insertQuery = "INSERT INTO transaction_test VALUES ('__test-value__');";

            _db.ExecuteDefaultQuery("CREATE TABLE transaction_test (value TEXT);");
            Assert.AreEqual(0, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE '__test-value__';"));
            Assert.AreEqual(2, _db.ExecuteBatchQueries(new[] { insertQuery, insertQuery }));
            Assert.AreEqual(2, _db.ExecuteScalar("SELECT COUNT(*) FROM transaction_test WHERE value LIKE '__test-value__';"));
        }
    }
}
