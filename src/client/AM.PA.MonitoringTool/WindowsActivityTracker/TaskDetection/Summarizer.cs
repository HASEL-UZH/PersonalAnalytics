// Created by Patrick Gousseau (pcgousseau@gmail.com) from the University of British Columbia
// Created: 2021-08-20

using System;
using System.Collections.Generic;

namespace WindowsActivityTracker.TaskDetection
{
    class Summarizer
    {

        private List<Task> tasks; // List of all tasks found
        private double similarityThreshold; // Minimum cosine similarity for two bags of words to be considered alike
        private int numTasks = 0;
        private int totalTime = 0;
        private int numSecs; // Duration of each block of time

        public Summarizer(int numSecs, double similarityThreshold)
        {
            this.tasks = new List<Task>();
            this.numSecs = numSecs;
            this.similarityThreshold = similarityThreshold;
        }

        /// <summary>
        /// Adds task to the list of tasks
        /// </summary>
        /// <param name="newTask"></param>
        /// <returns></returns>
        public void addTask(Task newTask)
        {
            // If bag is empty
            if (tasks.Count == 0)
            {
                numTasks++;
                tasks.Add(newTask);
                newTask.setTaskNum(numTasks);
                newTask.setTimes(0, numSecs);
            }
            else
            {
                // Collapse if preceding bag is similar to the new one
                if (similarTasks(tasks[tasks.Count - 1], newTask, similarityThreshold))
                {
                    tasks[tasks.Count - 1].collapseTasks(newTask, numSecs);
                }
                else
                {
                    Boolean isNewTask = true;
                    int newTaskNum = -1; // Task number for newBag

                    // Check if newBag represents the same task as a previous bag
                    foreach (Task task in tasks)
                    {
                        if (similarTasks(task, newTask, similarityThreshold))
                        {
                            newTaskNum = task.getTaskNum();
                            isNewTask = false;
                            break;
                        }
                    }

                    if (isNewTask)
                    {
                        numTasks++;
                        newTaskNum = numTasks;
                    }

                    // If bag is not similar enough to previous bag yet has same task number
                    if (tasks[tasks.Count - 1].getTaskNum() == newTaskNum)
                    {
                        tasks[tasks.Count - 1].collapseTasks(newTask, numSecs);
                    }
                    else
                    {
                        newTask.setTaskNum(newTaskNum);
                        newTask.setTimes(totalTime, numSecs + totalTime);
                        tasks.Add(newTask);
                    }
                }
            }
            totalTime += numSecs;
        }

        /// <summary>
        /// Returns whether or not the tasks are similar
        /// </summary>
        /// <param name="task1"></param>
        /// <param name="task2"></param>
        /// <param name="similarityThreshold"></param>
        /// <returns></returns>
        public Boolean similarTasks(Task task1, Task task2, double similarityThreshold)
        {
            double[] task1Vector = task1.getVector();
            double[] task2Vector = task2.getVector();
            double cosineSimilarity = 1 - Accord.Math.Distance.Cosine(task1Vector, task2Vector);

            return cosineSimilarity >= similarityThreshold;
        }

        public List<Task> getTasks()
        {
            return this.tasks;
        }
    }
}
