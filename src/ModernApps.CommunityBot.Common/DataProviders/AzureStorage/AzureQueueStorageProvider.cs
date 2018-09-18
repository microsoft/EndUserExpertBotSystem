// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.DataProviders.AzureStorage
{
    [Serializable]
    public class AzureQueueStorageProvider : IQueueProvider
    {

        private string connectionString;

        public AzureQueueStorageProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<T> DequeueAsync<T>(string queueName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            var queueClient = cloudStorageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queueName);
            if (!queue.Exists())
            {
                return default(T);
            }
            else
            {
                var message = await queue.GetMessageAsync();
                if (message != null)
                {

                    var objectToReturn = JsonConvert.DeserializeObject<T>(message.AsString);
                    await queue.DeleteMessageAsync(message);

                    if (queue.ApproximateMessageCount == 0)
                    {
                        await queue.DeleteIfExistsAsync();
                    }

                    return objectToReturn;
                }
                return default(T);
            }
        }

        public async Task InsertInQueueAsync<T>(string queueName, T objectToInsert)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            var queueClient = cloudStorageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(objectToInsert));
            await queue.AddMessageAsync(message);
        }
    }
}
