using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AppComponents.Extensions
{
    public static class FileExtensions
    {
        public static FileStream OpenExclusive(string path, FileMode mode, FileAccess access, TimeSpan timeout)
        {
            FileStream fs= null;
            DateTime deadline = DateTime.UtcNow + timeout;

            do
            {
                try
                {
                    fs = File.Open(path, mode, access, FileShare.None);
                }
                catch (IOException)
                {
                    
                    
                }
                System.Threading.Thread.Sleep(0);
            } while (fs == null && DateTime.UtcNow > deadline);
            return fs;
        }
    }
}
