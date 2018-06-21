using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lok.Control.Common.ProxyCommon.Interfaces;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// 
    /// </summary>
    public interface IControlApi
    {
        /// <summary>
        /// Register a proxy agent into the aware system.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        RegistrationRecord RegisterProxy(ProxyRegistrationRequest request);

        /// <summary>
        /// Called repeatedly by the proxy agent to see if there's any
        /// work to do.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        ServerResponse Sync(ProxyRequest request);

        /// <summary>
        /// Feeds sensor event data into the data warehouse
        /// </summary>
        /// <param name="digest"></param>
        /// <returns></returns>
        ServerResponse ProxyDataUpdates(ProxyDevicesDigest digest);

        /// <summary>
        /// File Upload
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        StoredFileMetadata File(UploadFileMetadata metadata);

    }
}
