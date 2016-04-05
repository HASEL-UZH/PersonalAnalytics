// Created by André Meyer at MSR
// Created: 2015-12-10
// 
// Licensed under the MIT License.

using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PeopleVisualizer.Visualizations
{
    /// <summary>
    /// Visualizes the poeple a user is in contact with
    /// 
    /// 1. from Emails
    /// 2. from Meetings
    /// 3. from Skype for Business Events
    /// </summary>
    public class DayPeopleGrid : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        private const int numberOfPeopleShown = 10;

        public DayPeopleGrid(DateTimeOffset date)
        {
            this._date = date;

            Title = "People"; //hint: overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 6; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
/*
            var peopleAll = new List<ContactItem>();
            var peopleFromSkype = Shared.Data.Extractors.WindowTitlePeopleExtractor.GetPeopleInContact(_date);
            peopleAll.AddRange(peopleFromSkype);

            var peopleFromEmailsCall = Office365Api.GetInstance().LoadPeopleFromEmails(_date);
            peopleFromEmailsCall.Wait(); // wait for call to complete (as it's async)
            var peopleFromEmails = peopleFromEmailsCall.Result;
            peopleAll.AddRange(peopleFromEmails);

            var peopleFromMeetingsCall = Office365Api.GetInstance().LoadPeopleFromMeetings(_date);
            peopleFromMeetingsCall.Wait(); // wait for call to complete (as it's async)
            var peopleFromMeetings = peopleFromMeetingsCall.Result;
            peopleAll.AddRange(peopleFromMeetings);


            /////////////////////
            // merge & clean data sets
            /////////////////////
            var people = new List<ContactItem>();
            foreach (var person in peopleAll)
            {
                // only if enough information is available
                if (string.IsNullOrEmpty(person.Email) && string.IsNullOrEmpty(person.Name)) continue;

                // only add if not authenticated user itself
                if (Office365Api.GetInstance().IsAuthenticatedUser(person.Name, person.Email)) continue;

                var items = people.FindAll(i => (i.Email == person.Email && i.Email != null) || (i.Name == person.Name && i.Name != null));
                // increase interaction count if already in list
                if (items.Count > 0)
                {
                    people.Where(i => (i.Email == person.Email && i.Email != null) || (i.Name == person.Name && i.Name != null)).First().InteractionCount++;
                }
                // add if not already in list
                else
                {
                    people.Add(person);
                }
            }


            var sortedList = people.OrderByDescending(c => c.InteractionCount)
                .Take(numberOfPeopleShown);

            /////////////////////
            // visualize data sets
            /////////////////////
            Title = "Top " + sortedList.Count() + " People";

            foreach (var person in sortedList)
            {
                if (!string.IsNullOrEmpty(person.Email))
                {
                    var photoRes = Office365Api.GetInstance().GetPhotoForUser(person.Email);
                    photoRes.Wait();
                    var photo = photoRes.Result;
                    //var photo = File.ReadAllText(@"C:\Users\t-anmeye\Desktop\am.txt"); //todo: use
                    if (! string.IsNullOrEmpty(photo))
                    {
                        html += "<img src='data:image/jpeg;base64," + photo + "' alt='" + person.Name + " (" + person.Email + ")' title='" + person.Name + " (" + person.Email + ")' height='3.125em' max-width='3.125em' style='margin-right:5px;' />";
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(person.Name))
                            html += "<p>" + person.Email + " </p>";
                        else
                            html += "<p>" + person.Name + " </p>";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(person.Name))
                        html += "<p>" + person.Email + " </p>";
                    else
                        html += "<p>" + person.Name + " </p>";
                }
            }

            html += "<p>Hint: from Skype for Business conversations , emails and meeting attendees.</p>";
*/
            return html;
        }
    }
}
