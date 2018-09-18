// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.LongRunningThreads
{
    public interface IQueueListenerPostChannel
    {
        ChannelTypes ChannelType { get; }

        string ChannelId { get; }

        Task<string> SendToChannel(IActivity activity);
        Task SendToConverstionData(string converstationId, IActivity activity, Dictionary<string, string> dataToSend);

    }
}