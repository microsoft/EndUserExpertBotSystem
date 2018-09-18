// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.Common.Events
{
    public class TechnicalErrorEvent : EventBase, IEvent
    {
        public string ChannelId { get; set; }

        public string UserId => string.Empty;

        public string ConversationId { get; set; }

        public EventType EventType => EventType.TECHNICALERROR;

        public string Exception { get; set; }

    }
}
