// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.


using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.Common.Helpers;
using ModernApps.CommunityBot.ExpertsBot.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ModernApps.CommunityBot.ExpertsBot.Dialogs
{
    [Serializable]
    public abstract class ExpertDialogBase : DialogBase
    {
        public ExpertDialogBase()
        {
        }

        public ExpertDialogBase(ILuisService[] services) : base(services)
        {
        }

        protected async Task SendAnswerToUsers(IDialogContext context, string question, string answer)
        {
            var normalizedQuestion = MessageHelper.NormalizeString(question);
            var messageEntity = new MessageEntity()
            {
                Question = normalizedQuestion,
                ExpertAnswer = answer,
            };


            await queueProvider.InsertInQueueAsync("experttouser", messageEntity);

            var partitionKey = normalizedQuestion.GetHashCode().ToString();

            var questionsAnswered = await tableStorageProvider.RetrievePartitionFromTableAsync<MessageEntity>("unansweredquestions", partitionKey);
            var eventToSend = new AnswerQuestionEvent(context)
            {
                Answer = answer,
                ExpertName = context.Activity.From.Name,
                Question = question,
            };

            if (questionsAnswered.Any())
            {
                foreach (var q in questionsAnswered)
                {
                    eventToSend.QuestionCorrelationId = q.QuestionCorrelationId;
                    await eventProvider.SendEventAsync(eventToSend);
                }
            }
            else
            {
                await eventProvider.SendEventAsync(eventToSend);
            }

            await tableStorageProvider.DeletePartitionAsync<MessageEntity>("unansweredquestions", partitionKey);
            ClearContextData(context);
            await context.PostAsync(messageProvider.GetMessage("ThankYou"));
        }

        protected async Task KeepNewAnswer(IDialogContext context, string question, string answer, IQnaResponse qnaResponse)
        {
            await context.PostAsync(messageProvider.GetMessage("KeepingNew"));
            await qnaMakerProvider.ReplaceAnswer(question, answer, qnaResponse);
            await SendAnswerToUsers(context, question, answer);
        }
    }
}