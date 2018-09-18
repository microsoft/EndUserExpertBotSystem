// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ModernApps.CommunityBot.EndUserBot.Dialogs
{
    [Serializable]
    public class StillInterestedDialog : DialogBase
    {
        private string question;
        private string answer;
        private MessageType messageType;
        private string conversationReference;

        public StillInterestedDialog(StillInterestedEntity message)
        {
            question = message.Question;
            messageType = message.MessageType;
            conversationReference = message.ConversationReference;
            answer = message.Answer;
        }

        public override Task OnStart(IDialogContext context)
        {
            PromptDialog.Confirm(context, AfterAreYouStillInterested, string.Format(messageProvider.GetMessage("StillInterestedPrompt"), question));
            return Task.CompletedTask;
        }

        private async Task AfterAreYouStillInterested(IDialogContext context, IAwaitable<bool> result)
        {
            var confirm = await result;
            var entity = new StillInterestedEntity(context.Activity.ChannelId, context.Activity.From.Id, MessageHelper.NormalizeString(question))
            {
                ConversationId = context.Activity.Conversation.Id,
                Timestamp = DateTime.UtcNow,
                ConversationReference = conversationReference,
                Answer = answer,
            };
            if (confirm)
            {
                await context.PostAsync(messageProvider.GetMessage("ResendingQuestion"));
                var messageToSend = new MessageEntity()
                {
                    Question = question,
                    MessageType = messageType,
                    OriginalAnswer = answer,
                    QuestionCorrelationId = Guid.NewGuid()
                };
                await queueProvider.InsertInQueueAsync("usertoexpert", messageToSend);
                entity.ReceivedAnswer = false;
            }
            else
            {
                entity.ReceivedAnswer = true;
                await context.PostAsync(messageProvider.GetMessage("ForgettingQuestion"));
            }
            await tableStorageProvider.SendToTableAsync("stillInterested", entity);
            await context.PostAsync(messageProvider.GetMessage("WhatMoreCanIDo"));
            context.Done(0);
        }
    }
}