// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using GoalSetting.Model;
using Shared.Data;
using System.Linq;

namespace GoalSetting.Data.Tests
{
    [TestClass()]
    public class DataHelperTests
    {
        [TestMethod()]
        public void MergeSameActivitiesTest()
        {
            DateTime startA1;
            DateTime startA2;
            DateTime startA3;
            DateTime startA4;
            DateTime startA5;
            DateTime endA1;
            DateTime endA2;
            DateTime endA3;
            DateTime endA4;
            DateTime endA5;

            ActivityContext a1;
            ActivityContext a2;
            ActivityContext a3;
            ActivityContext a4;
            ActivityContext a5;

            //Merge multiple activites into one
            startA1 = DateTime.Now;
            endA1 = startA1.AddHours(1);
            startA2 = startA1.AddHours(2);
            endA2 = startA1.AddHours(3);
            startA3 = startA1.AddHours(4);
            endA3 = startA1.AddHours(5);

            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.Other };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.Other };
            a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };

            var activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2, a3 }, 0);
            Assert.AreEqual(1, activities.Count);
            Assert.AreEqual(endA3 - startA1, activities.First().Duration);
            Assert.AreEqual(activities.First().Activity, ContextCategory.Other);
            Assert.AreEqual(activities.First().Start, startA1);
            Assert.AreEqual(activities.First().End, endA3);

            //Don't merge two activities
            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.Other };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2 }, 0);

            Assert.AreEqual(2, activities.Count);
            Assert.AreNotEqual(activities.First(), activities.Last());
            Assert.AreEqual(startA1, activities.First().Start);
            Assert.AreEqual(startA2, activities.Last().Start);
            Assert.AreEqual(endA1, activities.First().End);
            Assert.AreEqual(endA2, activities.Last().End);

            //Merge 4 activies into 2 activities
            startA4 = startA3.AddHours(1);
            endA4 = startA3.AddHours(2);

            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.DevCode };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };
            a4 = new ActivityContext { Start = startA4, End = endA4, Activity = ContextCategory.Other };

            activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2, a3, a4 }, 0);

            Assert.AreEqual(2, activities.Count);
            Assert.AreEqual(ContextCategory.DevCode, activities.First().Activity);
            Assert.AreEqual(ContextCategory.Other, activities.Last().Activity);
            Assert.AreEqual(startA1, activities.First().Start);
            Assert.AreEqual(endA2, activities.First().End);
            Assert.AreEqual(startA3, activities.Last().Start);
            Assert.AreEqual(endA4, activities.Last().End);

            //Merge 5 activities into 2 activities
            startA5 = startA4.AddHours(1);
            endA5 = startA4.AddHours(2);

            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.Other };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.DevCode };
            a4 = new ActivityContext { Start = startA4, End = endA4, Activity = ContextCategory.Other };
            a5 = new ActivityContext { Start = startA5, End = endA5, Activity = ContextCategory.Other };

            activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2, a3, a4, a5 }, 0);

            Assert.AreEqual(3, activities.Count);
            Assert.AreEqual(startA1, activities.First().Start);
            Assert.AreEqual(endA1, activities.First().End);
            Assert.AreEqual(startA2, activities.ElementAt(1).Start);
            Assert.AreEqual(endA3, activities.ElementAt(1).End);
            Assert.AreEqual(startA4, activities.Last().Start);
            Assert.AreEqual(endA5, activities.Last().End);

            //Skip one small switch
            startA1 = DateTime.Now;
            endA1 = startA1.AddHours(1);
            startA2 = endA1;
            endA2 = startA2.AddSeconds(9);
            startA3 = endA2;
            endA3 = startA3.AddHours(1);

            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.Other };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };

            activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2, a3 }, 10);

            Assert.AreEqual(1, activities.Count);
            Assert.AreEqual(startA1, activities.First().Start);
            Assert.AreEqual(endA3, activities.First().End);

            //Skip two small switches
            startA1 = DateTime.Now;
            endA1 = startA1.AddHours(1);
            startA2 = endA1;
            endA2 = startA2.AddSeconds(9);
            startA3 = endA2;
            endA3 = startA3.AddHours(1);
            startA4 = endA4;
            endA4 = startA4.AddSeconds(9);
            startA5 = endA4;
            endA5 = startA5.AddHours(1);

            a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.Other };
            a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };
            a4 = new ActivityContext { Start = startA4, End = endA4, Activity = ContextCategory.DevCode };
            a5 = new ActivityContext { Start = startA5, End = endA5, Activity = ContextCategory.Other };

            activities = DataHelper.MergeSameActivities(new List<ActivityContext>() { a1, a2, a3, a4, a5 }, 10);

            Assert.AreEqual(1, activities.Count);
            Assert.AreEqual(startA1, activities.First().Start);
            Assert.AreEqual(endA5, activities.First().End);
        }

        [TestMethod()]
        public void GetTotalTimeSpentOnActivityTest()
        {
            DateTime startA1 = DateTime.Now;
            DateTime endA1 = startA1.AddMinutes(10);

            DateTime startA2 = DateTime.Now;
            DateTime endA2 = startA2.AddMinutes(13);

            DateTime startA3 = DateTime.Now;
            DateTime endA3 = startA3.AddMinutes(243);

            ActivityContext a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.DevCode };
            ActivityContext a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            ActivityContext a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };

            //0 activities
            Assert.AreEqual(TimeSpan.FromTicks(0), DataHelper.GetTotalTimeSpentOnActivity(new List<ActivityContext>(), ContextCategory.DevCode));

            //1 activities
            Assert.AreEqual(a1.Duration, DataHelper.GetTotalTimeSpentOnActivity(new List<ActivityContext> { a1 }, ContextCategory.DevCode));

            //2 activities
            Assert.AreEqual(a1.Duration + a2.Duration, DataHelper.GetTotalTimeSpentOnActivity(new List<ActivityContext> { a1, a2 }, ContextCategory.DevCode));

            //2 different acitivties
            Assert.AreEqual(a1.Duration, DataHelper.GetTotalTimeSpentOnActivity(new List<ActivityContext> { a1, a3 }, ContextCategory.DevCode));
        }

        [TestMethod()]
        public void GetNumberOfSwitchesToActivityTest()
        {
            DateTime startA1 = DateTime.Now;
            DateTime endA1 = startA1.AddMinutes(10);

            DateTime startA2 = DateTime.Now;
            DateTime endA2 = startA2.AddMinutes(13);

            DateTime startA3 = DateTime.Now;
            DateTime endA3 = startA3.AddMinutes(243);

            ActivityContext a1 = new ActivityContext { Start = startA1, End = endA1, Activity = ContextCategory.DevCode };
            ActivityContext a2 = new ActivityContext { Start = startA2, End = endA2, Activity = ContextCategory.DevCode };
            ActivityContext a3 = new ActivityContext { Start = startA3, End = endA3, Activity = ContextCategory.Other };
            
            //0 activities
            Assert.AreEqual(0, DataHelper.GetNumberOfSwitchesToActivity(new List<ActivityContext>(), ContextCategory.DevCode));

            //1 activities
            Assert.AreEqual(1, DataHelper.GetNumberOfSwitchesToActivity(new List<ActivityContext> { a1 }, ContextCategory.DevCode));

            //2 acitivities
            Assert.AreEqual(2, DataHelper.GetNumberOfSwitchesToActivity(new List<ActivityContext> { a1, a2 }, ContextCategory.DevCode));

            //2 different acitivties
            Assert.AreEqual(1, DataHelper.GetNumberOfSwitchesToActivity(new List<ActivityContext> { a1, a3 }, ContextCategory.DevCode));
        }
    }
}