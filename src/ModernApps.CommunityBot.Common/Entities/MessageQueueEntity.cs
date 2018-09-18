// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using ModernApps.CommunityBot.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Entities
{
    [Serializable]
    public class MessageEntity : TableEntity
    {

        public MessageEntity()
        {
            Timestamp = DateTime.UtcNow;
        }

        private string question;
        private Guid questionCorerlationId;

        public string Question
        {
            get
            {
                return question;
            }
            set
            {
                question = value;
                PartitionKey = MessageHelper.NormalizeString(value).GetHashCode().ToString();
            }
        }

        public string OriginalAnswer { get; set; }
        public string ExpertAnswer { get; set; }
        public MessageType MessageType { get; set; }

        public string OriginalQuestion
        {
            get;
            set;
        }

        public Guid QuestionCorrelationId
        {
            get { return questionCorerlationId; }
            set { questionCorerlationId = value; RowKey = value.ToString(); }

        }
    }
}
