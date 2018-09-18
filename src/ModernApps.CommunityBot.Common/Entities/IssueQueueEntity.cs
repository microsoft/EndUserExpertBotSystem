// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Entities
{
    public class IssueQueueEntity : TableEntity
    {
        private string text;

        public IssueQueueEntity()
        {
            RowKey = Guid.NewGuid().ToString();
        }

        public string Text
        {
            get
            {
                return text;
            }
            set { text = value; PartitionKey = text; }
        }
    }
}
