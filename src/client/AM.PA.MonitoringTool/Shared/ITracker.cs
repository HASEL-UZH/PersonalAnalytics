// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Shared
{
    /// <summary>
    /// Interface for all trackers. 
    /// </summary>
    public interface ITracker
    {
        string Name { get; set; }
        bool IsRunning { get; set; }
        void Start();
        void Stop();
        void CreateDatabaseTablesIfNotExist();
        void UpdateDatabaseTables(int version);
        string GetStatus();
        bool IsEnabled();
        List<IVisualization> GetVisualizationsDay(DateTimeOffset date);

        List<IVisualization> GetVisualizationsWeek(DateTimeOffset date);
    }

    public abstract class BaseTracker : ITracker
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public abstract void Start();
        public abstract void Stop();
        public abstract void CreateDatabaseTablesIfNotExist();
        public abstract void UpdateDatabaseTables(int version);
        public virtual string GetStatus()
        {
            return IsRunning ? Name + " is running." : Name + " is NOT running.";
        }

        public abstract bool IsEnabled();

        public virtual List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization>(); // default: return an empty list
        }

        public virtual List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization>(); // default: return an empty list
        }
    }

    public abstract class BaseTrackerDisposable : BaseTracker, IDisposable
    {
        private bool disposed = false;

        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~BaseTrackerDisposable()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }
    }

    public abstract class BaseVisualizer : ITracker
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public virtual void Start()
        {
            IsRunning = true;
        }

        public virtual void Stop()
        {
            IsRunning = false;
        }

        public virtual void CreateDatabaseTablesIfNotExist()
        {
            // nothing to do here
        }

        public virtual void UpdateDatabaseTables(int version)
        {
            // nothing to do here
        }

        public virtual string GetStatus()
        {
            return IsRunning ? Name + " is running." : Name + " is NOT running.";
        }

        public abstract bool IsEnabled();

        public virtual List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization>(); // default: return an empty list
        }

        public virtual List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization>(); // default: return an empty list
        }
    }
}
