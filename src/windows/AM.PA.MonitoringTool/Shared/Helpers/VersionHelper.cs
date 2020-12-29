// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-09
// 
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;

namespace Shared.Helpers
{
    public static class VersionHelper
    {
        public static string GetFormattedVersion(Version v)
        {
            return (v != null)
                ? string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision)
                : "?.?.?.?";
        }
    }
}
