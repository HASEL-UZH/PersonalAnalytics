// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
namespace OcrLibrary
{
    public class Settings
    {
        public const string OcrLanguage = "eng"; // change if other language; also change tessdata folder!
        public static bool DebugSaveProcessingImageEnabled = false;
        public const int OcrConfidenceAcceptanceThreshold = 180;
        public const int OcrTextLengthAcceptanceThreshold = 20;
    }
}
