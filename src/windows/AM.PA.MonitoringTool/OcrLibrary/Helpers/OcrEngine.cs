// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using OcrLibrary.Models;
using Shared;

namespace OcrLibrary.Helpers
{
    public class OcrEngine : IDisposable
    {
        private static OcrEngine _ocrEngine;
        private readonly tessnet2.Tesseract _tEngine;

        private string _temporaryOcrText;
        private double _tempConfidence;
        private ManualResetEvent _mEvent;

        public OcrEngine()
        {
            // PATH STUFF THAT DOESN'T WORK
            // ocr.Init(@"C:\", "eng", false); // the path here should be the parent folder of tessdata
            //var path = Path.Combine(GetDataDirectory(), @"tessdata");
            //var path = GetDataDirectory();
            //var path = @"tessdata\";
            //var path = @"C:\DATA\";

            const string path = ""; // keep in mind that the path should be parent of 'tessdata'

            _tEngine = new tessnet2.Tesseract();
            _tEngine.Init(path, Settings.OcrLanguage, false);
        }

        /// <summary>
        /// Singleton.
        /// </summary>
        /// <returns></returns>
        public static OcrEngine GetInstance()
        {
            return _ocrEngine ?? (_ocrEngine = new OcrEngine());
        }

        /// <summary>
        /// Gets a screenshot of the active window (already optimised for OCR),
        /// calculates the thresholded image, 
        /// runs the OCR,
        /// and returns the result as a context entry.
        /// </summary>
        /// <param name="ce"></param>
        /// <returns></returns>
        public ContextEntry RunOcr(ContextEntry ce)
        {
            if (_tEngine == null) return null;

            try
            {
                // run OCR preprocessing
                RunOcrPreProcessing(ce.Screenshot);

                // processed screenshot
                var processedScreenshot = ce.Screenshot.Image;

                // threshold
                //screenshot = _tEngine.GetThresholdedImage(screenshot, Rectangle.Empty); //TODO: enable or disable?
                //Screenshot.SaveImage(screenshot, "thresholded"); //TEMP

                _tEngine.OcrDone += OcrFinished;
                //var result = _tEngine.DoOCR(screenshot, Rectangle.Empty); // used for single-threading OCR processing
                _tEngine.DoOCR(processedScreenshot, Rectangle.Empty);

                processedScreenshot.Dispose();

                _mEvent = new ManualResetEvent(false);
                _mEvent.WaitOne(); // wait here until it's finished

                // add ocr'd text to context entry
                ce.OcrText = _temporaryOcrText;
                ce.Confidence = _tempConfidence;

                // reset temp values
                _temporaryOcrText = string.Empty;
                _tempConfidence = Settings.OcrConfidenceAcceptanceThreshold;

                // release sources
                ce.Screenshot.Dispose();
                ce.Screenshot = null;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return ce;
        }

        /// <summary>
        /// run Ocr-Preprocessing filters
        /// TODO: run pre-OCR optimizations on separate thread/process
        /// </summary>
        /// <param name="screenshot"></param>
        private static void RunOcrPreProcessing(Screenshot screenshot)
        {
            //return;
            try
            {
                //screenshot.Crop(); //TODO: re-enable when screenshotter works again
                //screenshot.Resize(); // resize to 300dpi //TODO: neccessary for screenshot?

                screenshot.ToGrayscale();
                //screenshot.SubtractMedianBlur(); //TODO: how to make it work?
                screenshot.ToBinary();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Called when OCR multi-thread process is finished. Reads text & confidence and stores
        /// them in temporary variables to be subsequently stored as a context entry.
        /// </summary>
        /// <param name="result"></param>
        private void OcrFinished(List<tessnet2.Word> result)
        {
            var text = string.Empty;
            var conf = 0.0;
            foreach (var word in result)
            {
                text += word.Text + "; "; // ';' separated entries as 'text'
                conf += word.Confidence;
            }
            _mEvent.Set();

            _temporaryOcrText = text;
            _tempConfidence = (conf / result.Count); // average

            // clean
            _tEngine.OcrDone -= OcrFinished;
            result = null;
        }

        public void Dispose()
        {
            _ocrEngine.Dispose();
            _tEngine.Dispose();
            _mEvent.Dispose();
        }
    }
}
