// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System.Globalization;
using ModernApps.CommunityBot.Common.Helpers;
using Microsoft.Bot.Builder.ConnectorEx;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using ModernApps.CommunityBot.EndUserBot.Events;
using System.Web;

namespace ModernApps.CommunityBot.EndUserBot.Dialogs
{
    [Serializable]
    public class RootDialog : DialogBase
    {

        private const string INTERNALTAG = @"\[internal\]";

        IBlobStorageProvider fileBlob;

        public RootDialog(ILuisService[] luisService, IBlobStorageProvider fileBlob) : base(luisService)
        {
            this.fileBlob = fileBlob;
        }

        [LuisIntent("AskQuestion")]
        private async Task AskQuestion(IDialogContext context, LuisResult luisResult)
        {
            var question = HttpUtility.HtmlDecode(context.Activity.AsMessageActivity().Text);
            var qnaAnswer = await qnaMakerProvider.GetQandAResponse(question);

            if (qnaAnswer.FoundAnswer)
            {
                await HandleQnaAnswer(context, qnaAnswer);
            }
            else
            {
                await SendEndUserQuestionEvent(context, question, string.Empty, false);
                await StoreNoAnswerFeedback(context, question);
                await context.PostAsync(messageProvider.GetMessage("NoAnswerOnQna"));
                await SendToQuestionToQueue(context, qnaAnswer, MessageType.NOANSWER);
                await SaveOnStillInterestedTable(context, qnaAnswer, MessageType.NOANSWER);
                await WhatMoreCanIDo(context);
            }
        }

        private async Task GiveQnAAnswer(IDialogContext context, IQnaResponse qnaAnswer)
        {
            await context.PostAsync(string.Format(messageProvider.GetMessage("FoundAnswer"), Regex.Replace(qnaAnswer.Answer, INTERNALTAG, "")));
            if (await HasEnoughCorrectFeedback(qnaAnswer))
            {
                await SendEndUserQuestionEvent(context, qnaAnswer.Question, qnaAnswer.Answer, true);
                await context.PostAsync(messageProvider.GetMessage("GladToHelp"));
                await WhatMoreCanIDo(context);
            }
            else
            {
                context.ConversationData.SetValue("qnaAnswer", qnaAnswer);
                PromptDialog.Confirm(context, AfterAnswerConfirmation, messageProvider.GetMessage("IsMessageCorrect"));
            }
        }

        private async Task AfterAnswerConfirmation(IDialogContext context, IAwaitable<bool> result)
        {
            var answerIsCorrect = await result;
            var qnaMakerResponse = context.ConversationData.GetValue<QandAResponse>("qnaAnswer");
            if (answerIsCorrect)
            {
                await SendEndUserQuestionEvent(context, qnaMakerResponse.Question, qnaMakerResponse.Answer, true);
                await StorePositiveFeedback(context, qnaMakerResponse);
                await StoreOnStillInterestedTable(context, qnaMakerResponse);
                await context.PostAsync(messageProvider.GetMessage("GladToHelp"));
                await WhatMoreCanIDo(context);
            }
            else
            {
                await SendEndUserQuestionEvent(context, qnaMakerResponse.Question, qnaMakerResponse.Answer, false);
                await context.PostAsync(messageProvider.GetMessage("SorryToNotHelpYou"));
                await StoreWrongAnswerFeedback(context, qnaMakerResponse);
                await SendToQuestionToQueue(context, qnaMakerResponse, MessageType.WRONGANSWER);
                await SaveOnStillInterestedTable(context, qnaMakerResponse, MessageType.WRONGANSWER);
                await WhatMoreCanIDo(context);
            }
        }


        private async Task SendToQuestionToQueue(IDialogContext context, IQnaResponse qnaAnswer, MessageType type)
        {
            var messageToSend = new MessageEntity()
            {
                Question = qnaAnswer.Question,
                OriginalAnswer = qnaAnswer.Answer,
                MessageType = type,
                OriginalQuestion = qnaAnswer.MatchingQuestion,
                QuestionCorrelationId = Guid.NewGuid()
            };
            await queueProvider.InsertInQueueAsync("usertoexpert", messageToSend);
        }

        private async Task SaveOnStillInterestedTable(IDialogContext context, IQnaResponse qnaAnswer, MessageType messageType)
        {
            var entity = new StillInterestedEntity(context.Activity.ChannelId, context.Activity.From.Id, MessageHelper.NormalizeString(qnaAnswer.Question))
            {
                ConversationId = context.Activity.Conversation.Id,
                Timestamp = DateTime.UtcNow,
                Answer = qnaAnswer.Answer,
                MessageType = messageType,
                ConversationReference = JsonConvert.SerializeObject(context.Activity.AsMessageActivity().ToConversationReference())
            };
            var tableContent = await tableStorageProvider.SendToTableAsync("stillInterested", entity);
        }

        private async Task StoreOnStillInterestedTable(IDialogContext context, IQnaResponse qnaAnswer)
        {
            var entity = new StillInterestedEntity(context.Activity.ChannelId, context.Activity.From.Id, MessageHelper.NormalizeString(qnaAnswer.Question))
            {
                ConversationId = context.Activity.Conversation.Id,
                Timestamp = DateTime.UtcNow,
                Answer = qnaAnswer.Answer,
                ReceivedAnswer = true,
                ConversationReference = JsonConvert.SerializeObject(context.Activity.AsMessageActivity().ToConversationReference())
            };
            var tableContent = await tableStorageProvider.SendToTableAsync("stillInterested", entity);
        }

        #region STORE FEEDBACK


        private async Task StoreWrongAnswerFeedback(IDialogContext context, IQnaResponse qnaMakerResponse)
        {
            var feedbackEntry = new FeedbackEntity()
            {
                Answer = qnaMakerResponse.Answer,
                Question = qnaMakerResponse.Question,
                FeedbackType = FeedbackType.NEGATIVE
            };
            await SentToFeedbackTable(feedbackEntry);
        }
        private async Task StorePositiveFeedback(IDialogContext context, IQnaResponse qnaResponse)
        {
            var feedbackEntry = new FeedbackEntity()
            {
                Answer = qnaResponse.Answer,
                Question = qnaResponse.Question,
                FeedbackType = FeedbackType.POSITIVE
            };
            await SentToFeedbackTable(feedbackEntry);
        }

        private async Task SentToFeedbackTable(FeedbackEntity feedbackEntry)
        {
            await tableStorageProvider.SendToTableAsync("feedbackTable", feedbackEntry);
        }

        private async Task StoreNoAnswerFeedback(IDialogContext context, string question)
        {
            var feedbackEntry = new FeedbackEntity()
            {
                Question = question,
                FeedbackType = FeedbackType.NOANSWER
            };
            await SentToFeedbackTable(feedbackEntry);
        }
        #endregion

        private async Task WhatMoreCanIDo(IDialogContext context)
        {
            await context.PostAsync(messageProvider.GetMessage("WhatMoreCanIDo"));
        }

        private async Task<bool> HasEnoughCorrectFeedback(IQnaResponse qnaAnswer)
        {
            TableQuery<FeedbackEntity> tableQuery = new TableQuery<FeedbackEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, FeedbackType.POSITIVE.ToString()),
                TableOperators.And,
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("Answer", QueryComparisons.Equal, qnaAnswer.Answer),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("Question", QueryComparisons.Equal, qnaAnswer.Question))));

            var positiveFeedbacks = await tableStorageProvider.RetrieveFromTableAsync("feedbackTable", tableQuery);

            return positiveFeedbacks.Count() >= configuration.GetConfiguration<int>("MinPositiveFeedback");
        }

        [LuisIntent("DownloadKnowledgeBase")]
        public async Task DownloadKnowledgeBase(IDialogContext context, LuisResult result)
        {
            if (await fileBlob.ExistsFileAsync(configuration.GetString("fileBlobContainer"), configuration.GetString("PublicKBDownloadFile")))
            {
                var linkExpiracy = configuration.GetConfiguration<int>("kbLinkExpiricy");
                var link = await GenerateKbLinkWithSASToken(linkExpiracy);
                await SendDownloadKnowledgeBaseEvent(context, true);
                await context.PostAsync(string.Format(messageProvider.GetMessage("KbLink"), link, linkExpiracy));
            }
            else
            {
                await SendDownloadKnowledgeBaseEvent(context, false);
                await context.PostAsync(messageProvider.GetMessage("KBNotAvailable"));
            }
        }

        private async Task SendDownloadKnowledgeBaseEvent(IDialogContext context, bool fileWasAvailable)
        {
            await eventProvider.SendEventAsync(new DownloadKnowledgeBaseEvent(context)
            {
                FileAvailable = fileWasAvailable,
            });
        }

        [LuisIntent("TurnOnNotifications")]
        public async Task TurnOnNotifications(IDialogContext context, LuisResult result)
        {
            await SetNotifications(true, context.Activity.ChannelId, context.Activity.From.Id);
            await context.PostAsync(messageProvider.GetMessage("NotificationsTurnedOn"));
        }



        [LuisIntent("TurnOffNotifications")]
        public async Task TurnOffNotifications(IDialogContext context, LuisResult result)
        {
            await SetNotifications(false, context.Activity.ChannelId, context.Activity.From.Id);
            await context.PostAsync(messageProvider.GetMessage("NotificationsTurnedOff"));
        }

        [LuisIntent("SendIssue")]
        public async Task SendIssue(IDialogContext context, LuisResult result)
        {
            var issue = result.Entities.FirstOrDefault(x => x.Type == "issue");

            if (issue == null)
            {
                await context.PostAsync(messageProvider.GetMessage("issueNotUnderstood"));
            }
            else
            {
                var text = context.Activity.AsMessageActivity().Text.Substring(issue.StartIndex ?? 0, (issue.EndIndex ?? 0) - (issue.StartIndex ?? 0) + 1);
                await context.PostAsync(string.Format(messageProvider.GetMessage("goingToSendIssue"),text));
                await queueProvider.InsertInQueueAsync("issues", new IssueQueueEntity()
                {
                    Text = text
                });
            }
        }

        private async Task SetNotifications(bool enable, string channelId, string userId)
        {
            var existingEntity = await tableStorageProvider.RetrieveFromTableAsync<UserEntity>("users", channelId, userId);
            if (existingEntity != null)
            {
                existingEntity.Notifications = enable;
            }


            await tableStorageProvider.SendToTableAsync("users", existingEntity);
        }

        private async Task<string> GenerateKbLinkWithSASToken(int linkExpiracy)
        {
            return await fileBlob.GenerateSASUriAsync(configuration.GetString("fileBlobContainer"), configuration.GetString("PublicKBDownloadFile"), linkExpiracy);
        }

        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            var qnaAnswer = await qnaMakerProvider.GetQandAResponse(result.Query);

            if (qnaAnswer.FoundAnswer)
            {
                await HandleQnaAnswer(context, qnaAnswer);
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("DidntUnderstand"));
            }

        }

        private async Task HandleQnaAnswer(IDialogContext context, IQnaResponse qnaAnswer)
        {
            if (Regex.IsMatch(qnaAnswer.Answer, GREETINGTAG, RegexOptions.IgnoreCase))
            {
                await SendEndUserQuestionEvent(context, qnaAnswer.Question, qnaAnswer.Answer, true);
                await context.PostAsync(Regex.Replace(qnaAnswer.Answer, GREETINGTAG, string.Empty));
            }
            else if (Regex.IsMatch(qnaAnswer.Answer, INTERNALTAG, RegexOptions.IgnoreCase))
            {
                if (configuration.GetConfiguration<List<string>>("InternalChannels").Contains(context.Activity.ChannelId))
                {
                    await GiveQnAAnswer(context, qnaAnswer);
                }
                else
                {
                    await SendEndUserQuestionEvent(context, qnaAnswer.Question, qnaAnswer.Answer, true);
                    await context.PostAsync(messageProvider.GetMessage("Confidential"));
                    await WhatMoreCanIDo(context);
                }
            }
            else
            {
                await GiveQnAAnswer(context, qnaAnswer);
            }
        }

        private async Task SendEndUserQuestionEvent(IDialogContext context, string question, string answer, bool positiveFeedback)
        {
            await eventProvider.SendEventAsync(new EndUserQuestionEvent(context)
            {
                IsAnswerCorrect = positiveFeedback,
                Answer = answer,
                Question = question
            });
        }
    }
}