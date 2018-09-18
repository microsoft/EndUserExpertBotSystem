// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.BotCommon
{
    public class AddressKey : IAddress
    {
        public string BotId { get; set; }
        public string ChannelId { get; set; }
        public string ConversationId { get; set; }
        public string ServiceUrl { get; set; }
        public string UserId { get; set; }
    }
}
