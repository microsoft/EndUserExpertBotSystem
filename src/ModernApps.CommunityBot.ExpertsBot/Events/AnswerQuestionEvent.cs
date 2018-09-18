// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.BotCommon.Events;
using ModernApps.CommunityBot.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.Events
{
    public class AnswerQuestionEvent : DialogEventBase, IEvent
    {
        public AnswerQuestionEvent(IDialogContext context) : base(context)
        {
        }

        public EventType EventType => EventType.ANSWERQUESTION;

        public string ExpertName { get; set; }

        public string Answer { get; set; }

        public string Question { get; set; }

        public Guid QuestionCorrelationId { get; set; }
    }
}