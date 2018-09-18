// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ModernApps.CommunityBot.ExpertsBot.LongRunningThreads
{
    public class QueueListenerPostChannelTableEntity : TableEntity
    {

        public QueueListenerPostChannelTableEntity()
        {
            RowKey = Guid.NewGuid().ToString();
        }

        public ChannelTypes Type
        {
            get { return (ChannelTypes)Enum.Parse(typeof(ChannelTypes), PartitionKey); }
            set { PartitionKey = value.ToString(); }
        }

        public string ChannelObject { get; set; }
    }
}