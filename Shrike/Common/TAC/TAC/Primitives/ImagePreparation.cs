using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;


namespace AppComponents.Primitives
{
    public static class ImageExtensions
    {
        public static void SaveJpeg(this Bitmap img, string filePath, long quality)
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            img.Save(filePath, GetEncoder(ImageFormat.Jpeg), encoderParameters);
        }

        public static void SaveJpeg(this Bitmap img, Stream stream, long quality)
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            img.Save(stream, GetEncoder(ImageFormat.Jpeg), encoderParameters);
        }

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.Single(codec => codec.FormatID == format.Guid);
        }

        public static Bitmap Resize(Bitmap value, int newWidth, int newHeight)
        {
            var resizedImage = new System.Drawing.Bitmap(newWidth, newHeight);
            Graphics.FromImage((System.Drawing.Image)resizedImage).DrawImage(value, 0, 0, newWidth, newHeight);
            return (resizedImage);
        }

        public static Bitmap Scale(Bitmap value, float scaleFactor)
        {
            return Resize(value, (int) (value.Width*scaleFactor), (int)(value.Height*scaleFactor));
        }
    }
}
