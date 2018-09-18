// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using ModernApps.CommunityBot.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.Events
{
    public class KnowledgeBaseEvent : EventBase, IEvent
    {
        public string ChannelId { get; set; }

        public string UserId { get; set; }

        public string ConversationId { get; set; }

        public EventType EventType { get; set; }
    }
}