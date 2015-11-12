// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
namespace Shared.Data
{
    public class StartEndTimeDto
    {
        public long StartTime { get; set; }
        public long EndTime { get; set; }
    }

    public class ProductivityTimeDto
    {
        public int UserProductvity { get; set; }
        public long Time { get; set; }
    }

    public class TasksWorkedOnTimeDto
    {
        public int TasksWorkedOn { get; set; }
        public long Time { get; set; }
    }

    public class ContextDto
    {
        public long StartTime { get; set; }
        public long EndTime { get; set; }

        public ContextInfos Context { get; set; }
    }

    public class ActivitiesDto
    {
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public ContextCategory Context { get; set; }
    }
}
