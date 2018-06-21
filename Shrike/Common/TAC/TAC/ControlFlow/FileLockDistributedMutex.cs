using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AppComponents.ControlFlow
{
    public enum FileLockDistributedMutexLocalConfig
    {
        OperatingFolder
    }

    public class FileLockDistributedMutex: IDistributedMutex
    {

        private string _name;
        private string _fileName;
        private string _operatingFolder;
        private FileStream _file;

        public FileLockDistributedMutex()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = config[DistributedMutexLocalConfig.Name].ToLowerInvariant();
            _operatingFolder = config[FileLockDistributedMutexLocalConfig.OperatingFolder];

            _fileName = Path.Combine(_operatingFolder, string.Format("{0}.lock", _name));

            try
            {
                var di = new DirectoryInfo(_operatingFolder);
                var tombStone = DateTime.UtcNow - TimeSpan.FromMinutes(5.0);
                var old = from fi in di.EnumerateFiles("*.lock") where fi.LastAccessTimeUtc < tombStone select fi;
                foreach (var fi in old)
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception)
                    {
                        
                        
                    }
                }
            }
            catch (Exception)
            {
                
                
            }
        }

        public bool Open()
        {
            return Wait(TimeSpan.FromMilliseconds(100.0));
        }

        public void Release()
        {
            if (null != _file)
            {
                _file.Dispose();
                MaybeDestroy();
            }
        }

        private void MaybeDestroy()
        {
            try
            {
                File.Delete(_fileName);
            }
            catch (Exception)
            {
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            bool taken = false;
            var utcNow = DateTime.UtcNow;
            DateTime deadline = utcNow + timeout;

            do
            {
                try
                {
                    _file = File.Open(_fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    taken = true;
                    return true;
                }
                catch (IOException)
                {

                }

                if (DateTime.UtcNow > deadline)
                    return false;

                System.Threading.Thread.Sleep(25);
            } while (!taken);

            return true;
        }

        public void Dispose()
        {
            Release();
            System.Threading.Thread.Sleep(25);
            MaybeDestroy();
            
            
        }
    }
}
