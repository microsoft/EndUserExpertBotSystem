// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;

namespace ModernApps.CommunityBot.Common.Events
{
    public interface IEvent : IEventBase
    {
        string ChannelId { get; }

        string UserId { get; }

        string ConversationId { get; }

        EventType EventType { get; }
    }
}