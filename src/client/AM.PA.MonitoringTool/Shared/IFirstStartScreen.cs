// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

namespace Shared
{
    public interface IFirstStartScreen
    {
        void NextClicked();
        void PreviousClicked();
        string GetTitle();
    }
}
