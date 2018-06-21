using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Command
{
    public enum CommandTypes
    {
        Shutdown=1000,
        Restart,
        SetVolume,
        GatherLogs,
        ClearDeployments,
        RunScript

    }

    public class Command
    {
        public Guid Id { get; set; }
        public DateTime Issued { get; set; }
        public int CommandType { get; set; }
        public string CommandArg { get; set; }

        public Guid InitiatingUserId { get; set; }
        public string InitiationUserName { get; set; }
        public string Comment { get; set; }
    }

    public class CommandsManifestItem: ManifestItem
    {
        public CommandsManifestItem()
        {
            Commands = new List<Command>();

        }


        public IList<Command> Commands { get; set; } 
    }
}
