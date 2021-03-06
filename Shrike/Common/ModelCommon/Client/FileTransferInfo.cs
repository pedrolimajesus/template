///////////////////////////////////////////////////////////
//  FileTransferInfo.cs
//  Implementation of the Class FileTransferInfo
//  Generated by Enterprise Architect
//  Created on:      14-Sep-2012 14:41:50
///////////////////////////////////////////////////////////

namespace Lok.Unik.ModelCommon.Client
{
    using System;

    using Lok.Unik.ModelCommon.Interfaces;

    public class FileTransferInfo : IFileTransferInfo
    {
        public Uri FileTransferUri { get; set; }

        public string ServerFile { get; set; }

        public FileTransferInfo()
        {
        }
    }

    //end FileTransferInfo
}

//end namespace System