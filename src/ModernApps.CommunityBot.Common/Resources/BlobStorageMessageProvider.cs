// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunitiyBot.Common.Providers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Resources
{
    [Serializable]
    public class BlobStorageMessageProvider : IMessageProvider
    {
        private Dictionary<string, string> messages;
        private IBlobStorageProvider blobStorageProvider;

        public BlobStorageMessageProvider(IBlobStorageProvider blobStorageMessageProvider, string containerName, IEnumerable<string> messageFilesPathes)
        {
            messages = new Dictionary<string, string>();
            blobStorageProvider = blobStorageMessageProvider;
            foreach (var file in messageFilesPathes)
            {
                var contents = blobStorageProvider.GetFileContentsAsync(containerName, file).GetAwaiter().GetResult();
                var fileDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);
                foreach (var value in fileDictionary)
                {
                    if(messages.ContainsKey(value.Key))
                    {
                        Trace.TraceWarning($"Messages dictionary has duplicate key {value.Key}");
                        messages[value.Key] = value.Value;
                    } else
                    {
                        messages.Add(value.Key, value.Value);
                    }
                }
            }
        }

        public string GetMessage(string key)
        {
            return messages.ContainsKey(key) ? messages[key] : string.Empty;
        }
    }
}
