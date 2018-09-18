// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Events
{
    public class AzureEventHubProvider : IEventProvider
    {
        private EventHubClient client;

        public AzureEventHubProvider(string connectionString, string eventHubName)
        {
            client = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
        }

        public async Task SendEventAsync(IEvent eventToSend)
        {
            await client.SendAsync(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventToSend))));
        }
    }
}
