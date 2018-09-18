// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.BotCommon.Events;
using ModernApps.CommunityBot.Common.Events;

namespace ModernApps.CommunityBot.EndUserBot.Events
{
    public class DownloadKnowledgeBaseEvent : DialogEventBase, IEvent
    {
        public DownloadKnowledgeBaseEvent(IDialogContext context) : base(context)
        {
        }

        public EventType EventType => EventType.DOWNLOADKB;

        public bool FileAvailable { get; set; }
    }
}