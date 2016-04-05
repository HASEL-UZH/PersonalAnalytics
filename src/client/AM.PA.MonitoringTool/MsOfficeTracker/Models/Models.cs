using Microsoft.Office365.OutlookServices;
using System;
using System.Collections.Generic;

namespace MsOfficeTracker.Models
{
    public class DisplayEvent
    {
        public EmailAddress Organizer { get; private set; }
        public bool IsOrganizer { get; private set; }
        public string Subject { get; set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public List<string> Attendees { get; private set; }
        public ResponseType ResponseStatus { get; set; }

        public int DurationInMins { get; set; }

        public DisplayEvent(Recipient organizer, bool? isOrganizer, string subject, ResponseStatus status, string start, string end, IList<Attendee> attendees)
        {
            if (organizer != null && organizer.EmailAddress != null) Organizer = new EmailAddress(organizer.EmailAddress);
            if (isOrganizer != null && isOrganizer.Value) IsOrganizer = isOrganizer.Value;
            Subject = subject;
            Start = DateTime.Parse(start);
            End = DateTime.Parse(end);
            Attendees = new List<string>();

            foreach (var attendee in attendees)
            {
                try
                {
                    //if (attendee.Status == ResponseStatus.Equals)   cannot use it as it's always null
                    Attendees.Add(attendee.EmailAddress.Address);
                }
                catch { }
            }

            try
            {
                DurationInMins = (int)Math.Round(Math.Abs((End - Start).TotalMinutes), 0);
            }
            catch { }

            ResponseStatus = status.Response;
        }

        public DisplayEvent(string subject, int durationInMins)
        {
            Attendees = new List<string>(); // empty list
            Subject = subject;
            DurationInMins = durationInMins;
        }
    }

    public class DisplayEmail
    {
        public DisplayEmail(IMessage mail)
        {
            try
            {
                var recipients = new List<EmailAddress>();
                if (mail.ToRecipients != null)
                {
                    foreach (var r in mail.ToRecipients)
                        recipients.Add(new EmailAddress(r.EmailAddress));
                }

                if (mail.CcRecipients != null)
                {
                    foreach (var r in mail.CcRecipients)
                        recipients.Add(new EmailAddress(r.EmailAddress));
                }

                if (mail.Sender != null)
                {
                    Sender = new EmailAddress(mail.Sender.EmailAddress);
                }
                if (mail.SentDateTime != null)
                {
                    Sent = mail.SentDateTime.Value;
                }
                Subject = mail.Subject;
                Recepients = recipients;
            }
            catch { }
        }

        public string Subject { get; private set; }
        public DateTimeOffset Sent { get; private set; }
        public List<EmailAddress> Recepients { get; private set; }
        public EmailAddress Sender { get; private set; }
    }

    public class EmailAddress
    {
        public EmailAddress(Microsoft.Office365.OutlookServices.EmailAddress emailAddress)
        {
            Name = emailAddress.Name;
            Address = emailAddress.Address;
        }

        public string Name { get; private set; }
        public string Address { get; private set; }
    }

    public class ContactItem
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public int InteractionCount { get; set; }
        public ContactItem(string name, string email, int count)
        {
            Name = name;
            Email = email;
            InteractionCount = count;
        }
    }
}
