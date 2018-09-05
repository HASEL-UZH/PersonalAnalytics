using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EyeCatcherDatabase.ParticipantRecords;
using EyeCatcherDatabase.Records;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase
{

    /// <summary>
    /// Merges multiple databases of multiple participants
    /// </summary>
    public class SqLiteAsyncDatabaseMerger
    {
        /// <summary>
        /// Dictionary holding all types with their respective most up to date primary key...
        /// </summary>
        private readonly Dictionary<Type, int> _lastNewPrimaryKeyByType = new Dictionary<Type, int>();

        private readonly SQLiteAsyncConnection _destinationDbConnection;

        public SqLiteAsyncDatabaseMerger(SQLiteAsyncConnection destinationDbConnection)
        {
            _destinationDbConnection = destinationDbConnection ?? throw new ArgumentNullException(nameof(destinationDbConnection));
        }

        public int ParticipantNumber { get; private set; }

        public async Task InsertDatabase(SQLiteAsyncConnection sourceDbConnection)
        {
            ParticipantNumber += 1;
            var oldPkMappingByType = new Dictionary<Type, Dictionary<int, IParticipantRecord>>();
            var tables = new List<List<IParticipantRecord>>
            {
                await GetParticipantTable<LastUserInputRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<UserPresenceRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<ScreenLayoutRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<ScreenRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<WindowRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<DesktopPointRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<CopyPasteRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<DesktopTransitionRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<DesktopWindowLinkRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType),
                await GetParticipantTable<DesktopRecord>(sourceDbConnection, ParticipantNumber, oldPkMappingByType)
            };

            foreach (var records in tables)
            {
                foreach (var participantRecord in records)
                {
                    // updating foreign keys
                    UpdateForeignKeys(participantRecord, oldPkMappingByType);
                }
                var type = records.FirstOrDefault()?.GetType();
                if (type != null && type.IsClass)
                {
                    await _destinationDbConnection.CreateTablesAsync(CreateFlags.None, type);
                }
                await _destinationDbConnection.RunInTransactionAsync(con => con.InsertAll(records));
            }
        }


        private async Task<List<IParticipantRecord>> GetParticipantTable<T>(SQLiteAsyncConnection dbConnection, int participantId,
            IDictionary<Type, Dictionary<int, IParticipantRecord>> oldPkMappingByType) where T : Record, new()
        {

            var oldPkMapping = new Dictionary<int, IParticipantRecord>();
            oldPkMappingByType.Add(typeof(T), oldPkMapping);
            var paRecords = new List<IParticipantRecord>();

            // TODO RR: This could be read from the table and doesn't need to be handled in a dict
            if (_lastNewPrimaryKeyByType.TryGetValue(typeof(T), out var newPk))
            {
                _lastNewPrimaryKeyByType.Remove(typeof(T));
            }
            // otherwise newPk is 0
            try
            {
                var records = await dbConnection.Table<T>().ToListAsync();
                foreach (var screenRecord in records)
                {
                    var paRecord = ParticipantRecordProvider.GetParticipantRecord(screenRecord);
                    // adding old Pk to mapping
                    oldPkMapping.Add(paRecord.Id, paRecord);
                    // distributing a new Pk
                    paRecord.Id = ++newPk;
                    // distributing participant id
                    paRecord.ParticipantId = participantId;
                    paRecords.Add(paRecord);
                }


                // adding new last key
                _lastNewPrimaryKeyByType.Add(typeof(T), newPk);
            }
            catch (SQLiteException e)
            {
                // Most certainly the Table does not exist...
                // ignore
            }
            return paRecords;
        }

        private static void UpdateForeignKeys(IRecord record, IReadOnlyDictionary<Type, Dictionary<int, IParticipantRecord>> oldPkMappingByType)
        {
            var recordType = record.GetType();
            var propertiesWithForeignKey = recordType.GetProperties().Where(prop => prop.IsDefined(typeof(ForeignKeyAttribute), false));
            foreach (var propertyInfo in propertiesWithForeignKey)
            {
                var foreignKeyAttribute = propertyInfo.GetCustomAttributes(typeof(ForeignKeyAttribute), false).FirstOrDefault() as ForeignKeyAttribute;
                var foreignType = foreignKeyAttribute?.ForeignType;
                if (foreignType == null)
                {
                    continue;
                }

                // getting mapping for type
                if (!oldPkMappingByType.TryGetValue(foreignType, out var oldFkMapping))
                {
                    continue;
                }
                // getting old foreign key
                if (!(propertyInfo.GetValue(record) is int oldFk))
                {
                    continue;
                }
                // getting the object the old fk maps to
                if (!oldFkMapping.TryGetValue(oldFk, out var foreignObject))
                {
                    continue;
                }

                // setting new fk
                propertyInfo.SetValue(record, foreignObject.Id);
            }
        }

    }



}
