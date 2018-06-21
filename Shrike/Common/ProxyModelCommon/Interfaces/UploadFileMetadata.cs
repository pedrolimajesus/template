using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    /// <summary>
    /// Metadata about an uploaded file
    /// </summary>
    public class UploadFileMetadata
    {
        /// <summary>
        /// Type of file uploaded
        /// </summary>
        public FileType FileType { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// UTC creation time
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// Description of the file content
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Types of file
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// An image file
        /// </summary>
        Image
    }
}
