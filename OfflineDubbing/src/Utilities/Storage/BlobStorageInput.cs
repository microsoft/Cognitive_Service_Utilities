//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Utilities.Storage
{
    public class BlobStorageInput
    {
        public BlobStorageConfiguration StorageConfiguration { get; set; }

        /// <summary>
        /// Write one or more Binary file to storage
        /// </summary>
        public List<byte[]> BinaryFiles { get; set; }

        /// <summary>
        /// Write one or more Text file to storage
        /// </summary>
        public List<string> TextFiles { get; set; }

        public BlobStorageInput(BlobStorageConfiguration storageConfiguration)
        {
            StorageConfiguration = storageConfiguration;
            TextFiles = new List<string> { };
            BinaryFiles = new List<byte[]> { };
        }

        public BlobStorageInput(BlobStorageConfiguration storageConfiguration, List<byte[]> binaryFiles) : this(storageConfiguration)
        {
            BinaryFiles = binaryFiles;
        }

        public BlobStorageInput(BlobStorageConfiguration storageConfiguration, List<string> textFiles) : this(storageConfiguration)
        {
            TextFiles = textFiles;
        }

    }
}
