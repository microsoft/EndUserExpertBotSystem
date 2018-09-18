// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Events
{
    public class EventProviderHub
    {
        private IEnumerable<IEventProvider> eventProviders;

        public EventProviderHub(IEnumerable<IEventProvider> eventProviders)
        {
            this.eventProviders = eventProviders;
        }

        public async Task SendEventAsync(IEvent eventToSend)
        {
            foreach (var eventProvider in eventProviders)
            {
                await eventProvider.SendEventAsync(eventToSend);
            }
        }
    }
}
