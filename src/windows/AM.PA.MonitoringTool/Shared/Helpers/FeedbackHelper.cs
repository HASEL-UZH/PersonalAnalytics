// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-04-22
// 
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Shared.Helpers
{
    public static class FeedbackHelper
    {
        public static void SendFeedback(string subject = "Feedback", string body = "", string appVersion = "")
        {
            var email = CreateFeedbackMailtoString(subject, appVersion, body);
            Process.Start(email); // open email application
        }

        /// <summary>
        /// Creates a HTML mailto email (with encoded text)
        /// can add an optional subject (with the current app version if available), and body
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="publishedAppVersion"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private static string CreateFeedbackMailtoString(string subject = "Feedback", string publishedAppVersion = "", string body = "")
        {
            subject = Dict.ToolName + ": " + subject;
            if (!string.IsNullOrEmpty(publishedAppVersion)) subject = subject + string.Format(CultureInfo.InvariantCulture, " ({0})", publishedAppVersion);
            var encodedSubject = Uri.EscapeDataString(subject);

            var email = (string.IsNullOrWhiteSpace(Settings.EmailAddress2))
                ? string.Format(CultureInfo.InvariantCulture, "mailto:{0}?subject={1}", Settings.EmailAddress1, encodedSubject)
                : string.Format(CultureInfo.InvariantCulture, "mailto:{0}?CC={1}&subject={2}", Settings.EmailAddress1, Settings.EmailAddress2, encodedSubject);

            if (!string.IsNullOrEmpty(body))
            {
                var encodedBody = Uri.EscapeDataString(body);
                email += "&Body=" + encodedBody;
            }

            return email;
        }
    }
}
