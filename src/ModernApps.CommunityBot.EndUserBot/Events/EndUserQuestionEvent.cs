// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.BotCommon.Events;
using ModernApps.CommunityBot.Common.Events;

namespace ModernApps.CommunityBot.EndUserBot.Events
{
    internal class EndUserQuestionEvent : DialogEventBase, IEvent
    {
        public EndUserQuestionEvent(IDialogContext context) : base(context)
        {
        }

        public EventType EventType => EventType.ENDUSERQUESTION;

        public string Question { get; set; }

        public string Answer { get; set; }

        public bool IsAnswerCorrect { get; set; }

    }
}