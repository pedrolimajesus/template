using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Command
{

    public class CommandResult
    {
        public Guid CorrespondingId { get; set; }
        public DateTime TimeInvoked { get; set; }
        public bool Success { get; set; }
        public string ResultMessage { get; set; }
        public byte[] ResultData { get; set; }
    }


    public class CommandsManifestResponse:ManifestItem 
    {
        public CommandsManifestResponse()
        {
            CommandResults = new List<CommandResult>();

        }

        public IList<CommandResult> CommandResults;

    }
}
