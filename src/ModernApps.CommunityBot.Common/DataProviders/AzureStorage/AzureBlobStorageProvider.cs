// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunitiyBot.Common.Providers
{

    [Serializable]
    public class AzureBlobStorageProvider : IBlobStorageProvider
    {
        private string storageAccountName;
        private string storageAccountKey;

        public AzureBlobStorageProvider(string storageAccountName, string storageAccountKey)
        {
            this.storageAccountName = storageAccountName;
            this.storageAccountKey = storageAccountKey;
        }

        public Task<bool> ExistsFileAsync(string containerName, string filePath)
        {
            CloudBlobContainer container = GetContainerReference(containerName);

            return container.GetBlobReference(filePath).ExistsAsync();
        }



        public async Task<string> GenerateSASUriAsync(string containerName, string filePath, int linkExpiracy)
        {
            CloudBlobContainer container = GetContainerReference(containerName);

            var blob = container.GetBlobReference(filePath);
            if (await blob.ExistsAsync())
            {
                var policy = new SharedAccessBlobPolicy();
                policy.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
                policy.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(linkExpiracy);
                policy.Permissions = SharedAccessBlobPermissions.Read;
                return blob.Uri + blob.GetSharedAccessSignature(policy);
            }
            return string.Empty;
        }

        public async Task<string> GetFileContentsAsync(string containerName, string filePath)
        {
            CloudBlobContainer container = GetContainerReference(containerName);

            var blob = container.GetBlobReference(filePath);

            var reader = new StreamReader(blob.OpenRead());

            return await reader.ReadToEndAsync();
        }

        public async Task WriteFileContentsAsync(string containerName, string fileName, string fileContent)
        {
            CloudBlobContainer container = GetContainerReference(containerName);

            var blob = container.GetBlockBlobReference(fileName);
            await blob.UploadTextAsync(fileContent);
        }

        private CloudBlobContainer GetContainerReference(string containerName)
        {
            StorageCredentials credentials = new StorageCredentials(storageAccountName, storageAccountKey);
            CloudStorageAccount account = new CloudStorageAccount(credentials, true);
            var client = new CloudBlobClient(account.BlobEndpoint, account.Credentials);
            CloudBlobContainer container = client.GetContainerReference(containerName);
            return container;
        }
    }
}
