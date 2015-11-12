// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

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
        string GetStatus();
        bool IsEnabled();
    }

    public abstract class BaseTracker : ITracker
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }

        public abstract void Start();
        public abstract void Stop();
        public abstract void CreateDatabaseTablesIfNotExist();

        public virtual string GetStatus()
        {
            return IsRunning ? Name + " is running." : Name + " is NOT running.";
        }

        public abstract bool IsEnabled();
    }
}
