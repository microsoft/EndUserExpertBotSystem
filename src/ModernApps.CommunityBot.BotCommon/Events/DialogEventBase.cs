// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.BotCommon.Events
{
    public abstract class DialogEventBase : EventBase
    {
        public string ChannelId { get; set; }

        public string UserId { get; set; }

        public string ConversationId { get; set; }

        protected DialogEventBase(IDialogContext context)
        {
            ChannelId = context.Activity.ChannelId;
            UserId = context.Activity.From.Id;
            ConversationId = context.Activity.Conversation.Id;
        }
    }
}
