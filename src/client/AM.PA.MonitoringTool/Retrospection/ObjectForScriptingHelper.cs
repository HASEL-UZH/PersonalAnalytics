// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-02-14
// 
// Licensed under the MIT License.

using System.Windows;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System;
using Shared;
using Shared.Helpers;
using System.Globalization;
using FocusSession;

namespace Retrospection
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ObjectForScriptingHelper
    {
        public void JS_SendFeedback()
        {
            Handler.GetInstance().SendFeedback();
        }

        public void JS_FocusSessionCustomTimer()
        {
            //FocusSession.Controls.Timer.CustomTimer();
        }

        public void JS_StartFocusSession()
        {
            FocusSession.Controls.Timer.StartOpenFocusSession();
        }

        public void JS_StopFocusSession()
        {
            FocusSession.Controls.Timer.StopFocusSession();
        }

        public void JS_FocusSessionCountdown()
        {
            FocusSession.Controls.Timer.StartClosedFocusSession();
        }

        public void JS_ThumbsVote(string voteType, string chartTitle, string typeString, string dateString)
        {
            try
            {
                var type = (VisType)Enum.Parse(typeof(VisType), typeString);
                var date = DateTimeOffset.Parse(dateString, CultureInfo.InvariantCulture);

                var up = (voteType == "up") ? 1 : 0;
                var down = (voteType == "down") ? 1 : 0;

                FeedbackThumbs.GetInstance().SetFeedback(type, date, chartTitle, up, down);
            }
            catch { }
        }
    }
}
