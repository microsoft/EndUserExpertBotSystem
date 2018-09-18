// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Entities
{
    public class StillInterestedEntity : TableEntity
    {
        public StillInterestedEntity()
        {
        }

        public StillInterestedEntity(string channelId, string userId, string question)
        {
            ChannelId = channelId;
            UserId = userId;
            Question = question;


            this.RowKey = $"{channelId}:{userId}";
            this.PartitionKey = question;
            QuestionDay = DateTime.UtcNow.ToString("dd-MM-yyyy");
        }

        public string Question
        {
            get; set;
        }

        public string Answer
        {
            get; set;
        }

        public string ChannelId { get; set; }
        public string UserId { get; set; }

        public string ConversationId { get; set; }

        public string ConversationReference { get; set; }

        public MessageType MessageType { get; set; }

        public bool ReceivedAnswer { get; set; }

        public string QuestionDay { get; }
    }
}
