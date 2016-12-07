using Shared;
using System;

namespace BiometricsTracker
{
    public class Deamon : BaseTracker, ITracker
    {
        public override void CreateDatabaseTablesIfNotExist()
        {
            throw new NotImplementedException();
        }

        public override bool IsEnabled()
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public override void UpdateDatabaseTables(int version)
        {
            throw new NotImplementedException();
        }
    }
}
