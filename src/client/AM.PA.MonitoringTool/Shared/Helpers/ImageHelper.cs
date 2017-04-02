// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2015-04-02
// 
// Licensed under the MIT License.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Shared.Helpers
{
    public static class ImageHelper
    {

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

    }
}
