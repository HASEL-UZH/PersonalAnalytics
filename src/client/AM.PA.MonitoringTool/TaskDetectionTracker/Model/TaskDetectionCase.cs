// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

namespace TaskDetectionTracker.Model
{
    public enum TaskDetectionCase
    {
        NotValidated = 0, //just to make sure that this will be the default one
        Correct = 1,
        Wrong = 2,
        Missing = 3
    }
}