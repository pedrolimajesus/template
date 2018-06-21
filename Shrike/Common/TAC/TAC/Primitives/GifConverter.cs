// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AppComponents
{
    public static class GifConverter
    {
        public static byte[] ConvertGif(Image Image)
        {
            using (MemoryStream objStream = new MemoryStream())
            {
                ImageCodecInfo objImageCodecInfo = GetEncoderInfo("image/jpeg");
                EncoderParameters objEncoderParameters;

                if (Image == null)
                    throw new InvalidOperationException("ImageObject is not initialized.");
                objEncoderParameters = new EncoderParameters(3);
                objEncoderParameters.Param[0] = new EncoderParameter(Encoder.Compression,
                                                                     (long) EncoderValue.CompressionLZW);
                objEncoderParameters.Param[1] = new EncoderParameter(Encoder.Quality, 100L);
                objEncoderParameters.Param[2] = new EncoderParameter(Encoder.ColorDepth, 24L);
                Image.Save(objStream, objImageCodecInfo, objEncoderParameters);

                byte[] data = objStream.ToArray();


                return data;
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}