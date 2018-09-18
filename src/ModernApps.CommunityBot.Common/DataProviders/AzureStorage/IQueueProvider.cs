// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.DataProviders.AzureStorage
{
    public interface IQueueProvider
    {
        Task InsertInQueueAsync<T>(string queueName, T objectToInsert);

        Task<T> DequeueAsync<T>(string queueName);

    }
}
