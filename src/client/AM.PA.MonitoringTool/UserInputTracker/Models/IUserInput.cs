// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;

namespace UserInputTracker.Models
{
    public interface IUserInput
    {
        DateTime Timestamp { get; }
    }
}
