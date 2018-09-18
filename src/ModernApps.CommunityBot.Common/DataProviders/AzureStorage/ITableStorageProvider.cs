// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernApps.CommunityBot.Common.Entities;

namespace ModernApps.CommunityBot.Common.DataProviders.AzureStorage
{
    public interface ITableStorageProvider
    {
        Task<IEnumerable<T>> RetrievePartitionFromTableAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new();
        Task<bool> SendToTableAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task<bool> SendToTableIfExistsAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> RetrieveTableAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task DeletePartitionAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new();
        Task<T> RetrieveFromTableAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();

        Task<IEnumerable<T>> RetrieveFromTableAsync<T>(string tableName, TableQuery<T> query) where T : class, ITableEntity, new();
        Task DeleteFromTableAsync<T>(string tableName, IEnumerable<T> entitiesToDelete) where T : class, ITableEntity, new();
        Task CreateIfNotExistsAsync<T>(string tableName, T userEntity) where T : class, ITableEntity, new();

    }
}
