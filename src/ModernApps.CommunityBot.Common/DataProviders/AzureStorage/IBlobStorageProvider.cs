// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Providers
{
    public interface IBlobStorageProvider
    {
        Task<string> GetFileContentsAsync(string containterName, string filePath);
        Task<bool> ExistsFileAsync(string containerName, string filePath);
        Task<string> GenerateSASUriAsync(string containerName, string filePath, int linkExpiracy);
        Task WriteFileContentsAsync(string containerName, string fileName, string fileContent);
    }
}
