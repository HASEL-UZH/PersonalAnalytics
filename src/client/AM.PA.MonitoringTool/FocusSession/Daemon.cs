// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using System;
using Shared;

namespace FocusSession
{
    public sealed class Daemon : BaseTrackerDisposable, ITracker
    {
        public override void CreateDatabaseTablesIfNotExist()
        {
            throw new NotImplementedException();
        }

        public override string GetVersion()
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