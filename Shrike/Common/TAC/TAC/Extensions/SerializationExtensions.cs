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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using AppComponents.Extensions.ExceptionEx;

namespace AppComponents.Extensions.SerializationEx
{
    public static class SerializationExtensions
    {
        public static string XmlSerializeToString(this object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }

        public static T XmlDeserializeFromString<T>(string objectData)
        {
            return (T) XmlDeserializeFromString(objectData, typeof (T));
        }

        public static object XmlDeserializeFromString(string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }


        public static Byte[] ToBinary(this object that)
        {
            MemoryStream ms = null;
            Byte[] byteArray = null;
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                ms = new MemoryStream();
                serializer.Serialize(ms, that);
                byteArray = ms.ToArray();
            }
            catch (Exception unexpected)
            {
                unexpected.TraceInformation();
                throw;
            }
            finally
            {
                if (ms != null)
                    ms.Close();
            }
            return byteArray;
        }

        public static object FromBinary(Byte[] buffer)
        {
            MemoryStream ms = null;
            object deserializedObject = null;

            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                ms = new MemoryStream();
                ms.Write(buffer, 0, buffer.Length);
                ms.Position = 0;
                deserializedObject = serializer.Deserialize(ms);
            }
            catch (Exception unexpected)
            {
                unexpected.TraceInformation();
                throw;
            }
            finally
            {
                if (ms != null)
                    ms.Close();
            }
            return deserializedObject;
        }

        public static byte[] ToBytes(this Stream sourceStream)
        {
            var memoryStream = new MemoryStream();
            sourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}