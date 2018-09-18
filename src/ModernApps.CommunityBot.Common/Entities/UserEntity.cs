// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Entities
{
    public class UserEntity : TableEntity
    {
        public string UserId
        {
            get { return RowKey; }
            set { RowKey = value; }
        }

        public string ChannelId
        {
            get { return PartitionKey; }
            set { PartitionKey = value; }
        }

        public string ConversationReference { get; set; }

        public DateTime LastActiveTime { get; set; }

        public bool Notifications { get; set; }


    }
}
