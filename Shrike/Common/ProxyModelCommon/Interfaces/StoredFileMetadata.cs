using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{

    /// <summary>
    /// Metadata about device files
    /// </summary>
    public class StoredFileMetadata
    {
        /// <summary>
        /// Type of file
        /// </summary>
        public FileType FileType { get; set; }

        /// <summary>
        /// Name of the file
        /// </summary>
        public string FileName { get; set; }


        /// <summary>
        /// UTC Time created
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// Description of the file 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Path to the file
        /// </summary>
        public string ContentURL { get; set; }
    }
}
