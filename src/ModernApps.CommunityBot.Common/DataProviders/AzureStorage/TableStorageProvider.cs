// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.DataProviders.AzureStorage
{
    [Serializable]
    public class TableStorageProvider : ITableStorageProvider
    {
        private CloudTableClient tableClient;

        public TableStorageProvider(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            tableClient = storageAccount.CreateCloudTableClient();
        }

        #region CREATE
        public async Task CreateIfNotExistsAsync<T>(string tableName, T userEntity) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);
            if (RetrieveFromTableAsync<T>(tableName, userEntity.PartitionKey, userEntity.RowKey) == default(T))
            {
                await SendToTableAsync(tableName, userEntity);
            }
        }

        public async Task<bool> SendToTableAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            if (table != null)
            {
                TableOperation retrieveOperation = TableOperation.InsertOrReplace(entity);

                var response = await table.ExecuteAsync(retrieveOperation);
                if (response != null)
                {
                    return response.HttpStatusCode >= 200 && response.HttpStatusCode < 300;
                }
            }
            return false;
        }

        public async Task<bool> SendToTableIfExistsAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
        {
            var contents = RetrieveFromTableAsync<T>(tableName, entity.PartitionKey, entity.RowKey);

            if (contents != default(T))
            {
                return true;
            }

            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null)
            {
                TableOperation retrieveOperation = TableOperation.InsertOrReplace(entity);

                var response = await table.ExecuteAsync(retrieveOperation);
                if (response != null)
                {
                    return response.HttpStatusCode >= 200 && response.HttpStatusCode < 300;
                }
            }
            return false;
        }
        #endregion

        #region READ
        public async Task<T> RetrieveFromTableAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {

            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null)
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

                var response = await table.ExecuteAsync(retrieveOperation);
                if (response != null && response.Result != null && response.Result is T)
                {
                    return (T)response.Result;
                }
            }
            return default(T);
        }

        public async Task<IEnumerable<T>> RetrievePartitionFromTableAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null && await table.ExistsAsync())
            {
                TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
                var response = table.ExecuteQuery(query);
                if (response != null)
                {
                    return response;
                }
            }
            return new List<T>();
        }

        public async Task<IEnumerable<T>> RetrieveTableAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null && await table.ExistsAsync())
            {
                TableQuery<T> query = new TableQuery<T>();
                var response = table.ExecuteQuery<T>(query);
                if (response != null)
                {
                    return response;
                }
            }
            return new List<T>();
        }

        public async Task<IEnumerable<T>> RetrieveFromTableAsync<T>(string tableName, TableQuery<T> query) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null && query != null && await table.ExistsAsync())
            {
                var response = table.ExecuteQuery<T>(query);
                if (response != null)
                {
                    return response;
                }
            }
            return new List<T>();
        }
        #endregion

        #region DELETE
        public async Task DeletePartitionAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            var partitionObjects = await RetrievePartitionFromTableAsync<T>(tableName, partitionKey);
            if (table != null)
            {
                var batch = new TableBatchOperation();
                foreach (var obj in partitionObjects)
                {
                    batch.Delete(obj);
                }
                if (batch.Any())
                    await table.ExecuteBatchAsync(batch);
            }
        }
        public async Task DeleteFromTableAsync<T>(string tableName, IEnumerable<T> entitiesToDelete) where T : class, ITableEntity, new()
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            if (table != null && await table.ExistsAsync())
            {
                foreach (var obj in entitiesToDelete)
                {
                    TableOperation retrieveOperation = TableOperation.Delete(obj);
                    await table.ExecuteAsync(retrieveOperation);
                }
            }
        }
        #endregion
    }
}