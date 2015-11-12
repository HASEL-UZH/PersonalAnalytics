// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using Shared;

namespace OcrLibrary.Helpers
{
    /// <summary>
    /// Screenshot class to do some pre-processing before
    /// running the OCR algorithm
    /// </summary>
    public class Screenshot : IDisposable
    {
        public Bitmap Image;

        public Screenshot(Bitmap image)
        {
            if (image != null) Dispose();
            Image = image;
            Save("original");
        }

        /// <summary>
        /// Crops the image (removes some toolbars)
        /// </summary>
        /// <returns></returns>
        public void Crop()
        {
            if (Image == null) return;
            const int margin = 20;
            var cropArea = new Rectangle(
                0, //x 
                5 * margin, //y
                Image.Width, //width
                Image.Height - 7 * margin); // height

            var bmpImage = new Bitmap(Image);

            // TODO: memory exception because of image is not within boundaries sometimes
            var bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);

            Image = bmpCrop;
            Save("cropping");

            // free up memory
            bmpCrop.Dispose();
            bmpCrop = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// need to resize the image to 300dpi,
        /// otherwise tesseract will not be able to recognize anything
        /// 
        /// TODO: often fails due to some memory problems (invalid parameter)
        /// </summary>
        /// <returns></returns>
        public void Resize()
        {
            if (Image == null) return;
            const int dpi = 300;
            const int scale = dpi / 96; // 96 is default
            var width = Image.Width * scale;
            var height = Image.Height * scale;
            var result = new Bitmap(Image, width, height);
            Image = result;
            Save("resizing");
        }

        /// <summary>
        /// Filter: Grayscale
        /// convert to grayscale
        /// </summary>
        public void ToGrayscale()
        {
            var filter = new Grayscale(0.2125, 0.7154, 0.0721); // create filter
            var grayImage = filter.Apply(Image); // apply the filter
            Image = grayImage;
            Save("grayed");
        }

        /// <summary>
        /// Filter: Median Blur
        /// Subtract the medium blur (block size ~letter size) from the image
        /// </summary>
        public void SubtractMedianBlur()
        {
            var filter = new Median(); // create filter
            filter.ApplyInPlace(Image); // apply the filter
            Save("medianBlurred");
        }

        /// <summary>
        /// Filter: Threshold (Binary)
        /// Binarize the image (threshold)
        /// </summary>
        public void ToBinary()
        {
            var filter = new Threshold(230); // create filter
            filter.ApplyInPlace(Image); // apply the filter
            Save("binarized");

            /* 
             * Note: Since the filter can be applied as to 8 bpp and to 16 bpp images, the Threshold Value value
             * should be set appropriately to the pixel format. In the case of 8 bpp images the threshold value
             * is in the [0, 255] range, but in the case of 16 bpp images the threshold value is in the [0, 65535] range.
             */
        }

        /// <summary>
        /// Helper function to save an image to the desktop
        /// </summary>
        /// <param name="name"></param>
        /// <param name="forced"></param>
        /// <returns></returns>
        public void Save(string name = "screenshot", bool forced = false)
        {
            //var mem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64/1024/1024;
            //Console.WriteLine("##" + name + " -- Memory: " + mem); // temp: tracer

            if (!forced && !Settings.DebugSaveProcessingImageEnabled) return;
            var fileName = string.Format(@"C:\Users\André\Desktop\sc\{0}_{1}.jpg", name, DateTime.Now.Ticks);
            Image.Save(fileName, ImageFormat.Jpeg);
        }

        /// <summary>
        /// try to free up memory
        /// garbage collector
        /// </summary>
        public void Dispose()
        {
            if (Image == null) return;
            Image.Dispose();
            Image = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
