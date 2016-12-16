// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-09
// 
// Adapted from: http://stackoverflow.com/questions/4890915/is-there-a-task-based-replacement-for-system-threading-timer
//
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothLowEnergyConnector
{
    public static class PeriodicTaskFactory
    {
        public static Task Start(Action action, int intervalInMilliseconds = Timeout.Infinite, int duration = Timeout.Infinite, int maxIterations = -1, bool synchronous = false, CancellationToken cancelToken = new CancellationToken(), TaskCreationOptions periodicTaskCreationOptions = TaskCreationOptions.None)
        {
            Stopwatch stopWatch = new Stopwatch();
            Action wrapperAction = () =>
            {
                CheckIfCancelled(cancelToken);
                action();
            };

            Action mainAction = () =>
            {
                MainPeriodicTaskAction(intervalInMilliseconds, duration, maxIterations, cancelToken, stopWatch, synchronous, wrapperAction, periodicTaskCreationOptions);
            };

            return Task.Factory.StartNew(mainAction, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private static void MainPeriodicTaskAction(int intervalInMilliseconds, int duration, int maxIterations, CancellationToken cancelToken, Stopwatch stopWatch, bool synchronous, Action wrapperAction, TaskCreationOptions periodicTaskCreationOptions)
        {
            TaskCreationOptions subTaskCreationOptions = TaskCreationOptions.AttachedToParent | periodicTaskCreationOptions;

            CheckIfCancelled(cancelToken);
            
            if (maxIterations == 0) { return; }

            int iteration = 0;

            using (ManualResetEventSlim periodResetEvent = new ManualResetEventSlim(false))
            {
                while (true)
                {
                    CheckIfCancelled(cancelToken);

                    Task subTask = Task.Factory.StartNew(wrapperAction, cancelToken, subTaskCreationOptions, TaskScheduler.Current);

                    if (synchronous)
                    {
                        stopWatch.Start();
                        try
                        {
                            subTask.Wait(cancelToken);
                        }
                        catch { /* do not let an errant subtask to kill the periodic task...*/}
                        stopWatch.Stop();
                    }

                    if (intervalInMilliseconds == Timeout.Infinite) { break; }

                    iteration++;

                    if (maxIterations > 0 && iteration >= maxIterations) { break; }

                    try
                    {
                        stopWatch.Start();
                        periodResetEvent.Wait(intervalInMilliseconds, cancelToken);
                        stopWatch.Stop();
                    }
                    finally
                    {
                        periodResetEvent.Reset();
                    }

                    CheckIfCancelled(cancelToken);

                    if (duration > 0 && stopWatch.ElapsedMilliseconds >= duration) { break; }
                }
            }
        }

        private static void CheckIfCancelled(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException("cancellationToken");

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}