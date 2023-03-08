//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Utilities.Storage
{
    public enum FileFormatEnum
    {
        Binary,
        Text
    }
    
    public class BlobStorageConfiguration
    {
        /// <summary>
        /// Folder path inside the container from where to read and write files
        /// </summary>
        [JsonProperty("FolderPath")]
        public string FolderPath { get; set; }

        /// <summary>
        /// Flag that is set to true by default. When writing file, if file with the same name exists, it will be overwritten
        /// </summary>
        [JsonProperty("OverWriteFile")]
        public bool OverWriteFile { get; set; }

        /// <summary>
        /// List of files to be read or written. The values here will be pointing to keys in the Pipeline.Dataset
        /// </summary>
        [JsonProperty("FileNames")]
        public List<string> FileNames { get; set; }

        /// <summary>
        /// FileFormat to read or write file. Supported types - Text and Binary
        /// </summary>
        [JsonProperty("FileFormat")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileFormatEnum FileFormat { get; set; }
        
        public BlobStorageConfiguration()
        {
            OverWriteFile = true;
            FileNames = new List<string>();
        }
    }
}
