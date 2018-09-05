using System;
using System.Linq;
using System.Reflection;
using EyeCatcherDatabase.ParticipantRecords;
using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase
{
    internal static class ParticipantRecordProvider
    {
        public static IParticipantRecord GetParticipantRecord(Record record)
        {
            switch (record)
            {
                case null:
                    return null;
                case IParticipantRecord _:
                    return record as IParticipantRecord;
                default:
                    var parentType = record.GetType();
                    var newType = GetParticipantSubtype(parentType);
                    // actually creating object
                    var participantRecord = Activator.CreateInstance(newType) as Record;
                    CopyAllPropertyValues(parentType, record, participantRecord);
                    return participantRecord as IParticipantRecord;
            }
        }

        private static Type GetParticipantSubtype(Type parentType)
        {
            var subTypes = parentType.Assembly.GetTypes().Where(type => type.IsSubclassOf(parentType));
            return subTypes.FirstOrDefault(t => typeof(IParticipantRecord).IsAssignableFrom(t));
        }

        private static void CopyAllPropertyValues(IReflect type, object source, object destination)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                prop.SetValue(destination, prop.GetValue(source));
            }
        }
    }
}
