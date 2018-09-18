// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.Events
{
    public class NewQuestionEvent : EventBase, IEvent
    {
        public string ChannelId {get;set;}

        public string UserId => string.Empty;

        public string ConversationId { get; set; }

        public EventType EventType => EventType.NEWQUESTION;

        public string Question { get; set; }

        public string OriginalAnswer { get; set; }

        public MessageType MessageType { get; set; }
        
        public Guid QuestionCorrelationId { get; set; }
    }
}