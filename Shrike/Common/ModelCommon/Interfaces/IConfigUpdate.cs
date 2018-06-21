using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Interfaces
{
    public interface IConfigUpdate
    {
        void ResetToFactory();
        void ReloadSettings();

        void SetValue(string name, string value);
        void SetValue(string name, int value);
        void SetValue(string name, bool value);

        void SetEncryptedValue(string name, string value);
        void SetEncryptedValue(string name, int value);
        void SetEncryptedValue(string name, bool value);

        bool GetEncryptedValue(string name, out string value);
        bool GetEncryptedValue(string name, out int value);
        bool GetEncryptedValue(string name, out bool value);
    }
}
