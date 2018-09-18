// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.BotCommon;
using Microsoft.Bot.Builder.Luis.Models;
using System.Linq;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Luis;
using ModernApps.CommunityBot.Common.Helpers;
using System.Text;
using System.Text.RegularExpressions;
using ModernApps.CommunityBot.ExpertsBot.Events;

namespace ModernApps.CommunityBot.ExpertsBot.Dialogs
{
    [Serializable]
    public class RootDialog : ExpertDialogBase
    {

        private IBlobStorageProvider blobStorageProvider;

        public RootDialog(ILuisService[] luisService,
                          IBlobStorageProvider blobStorageProvider,
                          IQueueProvider queueProvider) : base(luisService)
        {
            this.blobStorageProvider = blobStorageProvider;
        }

        [LuisIntent("HandlerWrongAnswer")]
        public async Task WrongAnswer(IDialogContext context, LuisResult result)
        {
            var question = context.ConversationData.GetValueOrDefault("question", string.Empty);
            var originalQuestion = context.ConversationData.GetValueOrDefault("originalQuestion", string.Empty);
            var originalAnswer = context.ConversationData.GetValueOrDefault("originalAnswer", string.Empty);

            var behavior = result.Entities.FirstOrDefault(x => x.Type == "ReplaceBehavior");
            if (behavior != null)
            {
                context.ConversationData.SetValue("requiredBehavior", ((List<object>)behavior.Resolution.FirstOrDefault().Value).FirstOrDefault().ToString());
            }

            context.Call(new WrongAnswerDialog(question, originalQuestion, originalAnswer), AfterWrongAnswer);
        }

        private async Task AfterWrongAnswer(IDialogContext context, IAwaitable<IDialogResult> result)
        {
            await result;
            ClearContextData(context);
            context.Wait(MessageReceived);
        }

        [LuisIntent("AnswerQuestion")]
        public async Task AnswerQuestion(IDialogContext context, LuisResult result)
        {
            var question = result.Entities.FirstOrDefault(x => x.Type == "question");

            string questionEntity = null;

            if (question != null)
            {
                questionEntity = question.Entity;
            }

            if (string.IsNullOrEmpty(questionEntity))
            {
                questionEntity = context.ConversationData.GetValueOrDefault("question", string.Empty);
            }


            var answer = result.Entities.FirstOrDefault(x => x.Type == "answer");
            if (answer != null)
            {
                context.ConversationData.SetValue("answer", answer.Entity);
            }
            if (string.IsNullOrEmpty(questionEntity))
            {
                IEnumerable<MessageEntity> unansweredQuestions = await GetUnansweredQuestions();
                await context.PostAsync(messageProvider.GetMessage("NoQuestionOrMatch"));
                await GenerateUnansweredQuestions(context, unansweredQuestions);
            }
            else
            {
                await AfterChooseQuestion(context, Awaitable.FromItem(questionEntity));
            }
        }

        private async Task<IEnumerable<MessageEntity>> GetUnansweredQuestions()
        {
            var unansweredQuestions = await tableStorageProvider.RetrieveTableAsync<MessageEntity>("unansweredquestions");

            var messageRetention = configuration.GetConfiguration<int>("MessageRetention");

            var questionsToDelete = unansweredQuestions.Where(x => (DateTime.UtcNow - x.Timestamp).TotalDays >= messageRetention);

            await tableStorageProvider.DeleteFromTableAsync("unansweredquestions", questionsToDelete);


            return unansweredQuestions.Where(x => (DateTime.UtcNow - x.Timestamp).TotalDays <= messageRetention);
        }

        private async Task AfterChooseQuestion(IDialogContext context, IAwaitable<string> result)
        {
            var question = await result;
            context.ConversationData.SetValue("questionToAnswer", question);
            if (string.IsNullOrEmpty(context.ConversationData.GetValueOrDefault("answer", string.Empty)))
            {
                PromptDialog.Text(context, AfterAnswer, string.Format(messageProvider.GetMessage("askForAnswer"), question));
            }
            else
            {
                await AfterAnswer(context, Awaitable.FromItem(context.ConversationData.GetValue<string>("answer")));
            }
        }

        private async Task AfterAnswer(IDialogContext context, IAwaitable<string> result)
        {
            var question = context.ConversationData.GetValueOrDefault("questionToAnswer", string.Empty);
            var answer = await result;
            context.ConversationData.SetValue("answer", answer);

            var currentQnaResponse = await qnaMakerProvider.GetQandAResponse(question);

            var normalizedQuestion = MessageHelper.NormalizeString(currentQnaResponse.Question);

            if (currentQnaResponse.FoundAnswer)
            {
            var normalizedKBQuestion = MessageHelper.NormalizeString(currentQnaResponse.MatchingQuestion);
                string[] promptOptions;
                if (string.Compare(normalizedQuestion, normalizedKBQuestion, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    promptOptions = new string[] {messageProvider.GetMessage("KeepNew"),messageProvider.GetMessage("KeepOriginal")
                };
                }
                else
                {
                    promptOptions = new string[] { messageProvider.GetMessage("KeepNew"), messageProvider.GetMessage("KeepBoth"), messageProvider.GetMessage("KeepOriginal") };
                }

                context.ConversationData.SetValue("qnaResponse", currentQnaResponse);
                PromptDialog.Choice(context, AfterStoringConfirmation, promptOptions, string.Format(messageProvider.GetMessage("ConfirmNewAnswer"),
                                                                                                normalizedQuestion, normalizedKBQuestion, currentQnaResponse.Answer));
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("GoingToStoreAnswer"));
                await qnaMakerProvider.StoreNewAnswer(normalizedQuestion, answer);
                ClearContextData(context);
                await SendAnswerToUsers(context, question, answer);
            }
        }





        private async Task AfterStoringConfirmation(IDialogContext context, IAwaitable<string> result)
        {
            var requiredBehavior = await result;
            var answer = context.ConversationData.GetValueOrDefault("answer", string.Empty);
            var question = context.ConversationData.GetValueOrDefault("questionToAnswer", string.Empty);
            var qnaResponse = context.ConversationData.GetValueOrDefault<QandAResponse>("qnaResponse", null); ;

            if (requiredBehavior == messageProvider.GetMessage("KeepNew"))
            {
                await context.PostAsync(messageProvider.GetMessage("KeepingNew"));
                await qnaMakerProvider.ReplaceAnswer(question, answer, qnaResponse);
                await SendAnswerToUsers(context, question, answer);
            }
            else if (requiredBehavior == messageProvider.GetMessage("KeepBoth"))
            {
                await context.PostAsync(messageProvider.GetMessage("KeepingBoth"));
                await qnaMakerProvider.StoreNewAnswer(MessageHelper.NormalizeString(question), answer);
                await SendAnswerToUsers(context, question, answer);
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("KeepingOriginal"));
                await SendAnswerToUsers(context, question, qnaResponse.Answer);
            }
            ClearContextData(context);
        }

        [LuisIntent("UnansweredQuestions")]
        public async Task UnansweredQuestions(IDialogContext context, LuisResult result)
        {
            var unansweredQuestions = await GetUnansweredQuestions();
            var anyUnansweredQuestions = unansweredQuestions.Any();
            if (anyUnansweredQuestions)
            {
                await GenerateUnansweredQuestions(context, unansweredQuestions);
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("NoUnansweredQuestions"));
            }
            ClearContextData(context);
            context.Wait(MessageReceived);
        }

        private async Task GenerateUnansweredQuestions(IDialogContext context, IEnumerable<MessageEntity> unansweredQuestions)
        {
            if (unansweredQuestions.Any())
            {
                await context.PostAsync(string.Format(messageProvider.GetMessage("UnansweredQuestionsList"), configuration.GetConfiguration<int>("MessageRetention")));

                var message = context.MakeMessage();
                message.AttachmentLayout = AttachmentLayoutTypes.List;
                List<CardAction> buttons = new List<CardAction>();
                foreach (var question in unansweredQuestions.Select(x => x.Question).Distinct())
                {
                    CardAction cardAction = new CardAction()
                    {
                        Value = string.Format(messageProvider.GetMessage("UnanweredQuestionTempalate"), question),
                        Title = question,
                        Type = "imBack"
                    };

                    buttons.Add(cardAction);
                }
                HeroCard card = new HeroCard()
                {
                    Buttons = buttons
                };
                message.Attachments.Add(new Attachment("application/vnd.microsoft.card.hero", content: card));
                await context.PostAsync(message);
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("NoUnansweredQuestions"));
            }
        }

        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {

            var qnaAnswer = await qnaMakerProvider.GetQandAResponse(result.Query);

            if (qnaAnswer.FoundAnswer)
            {
                if (Regex.IsMatch(qnaAnswer.Answer, GREETINGTAG, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    await context.PostAsync(Regex.Replace(qnaAnswer.Answer, GREETINGTAG, string.Empty));
                }
                else
                {
                    await context.PostAsync(qnaAnswer.Answer);
                }
            }
            else if (context.ConversationData.ContainsKey("question"))
            {
                context.ConversationData.SetValue("questionToAnswer", context.ConversationData.GetValue<string>("question"));
                await AfterAnswer(context, Awaitable.FromItem(result.Query));
            }
            else
            {
                await context.PostAsync(messageProvider.GetMessage("NoneIntent"));
            }
        }

        [LuisIntent("ListQuestions")]
        public async Task ListQuestions(IDialogContext context, LuisResult result)
        {
            var expiracy = configuration.GetConfiguration<int>("privateKbFileExpiracy");
            var link = await blobStorageProvider.GenerateSASUriAsync(configuration.GetString("KbFilesContainer"), configuration.GetString("privateKbFile"), expiracy);
            if (string.IsNullOrEmpty(link))
            {
                await GenerateKnowledgeBaseFiles();
                link = await blobStorageProvider.GenerateSASUriAsync(configuration.GetString("KbFilesContainer"), configuration.GetString("privateKbFile"), expiracy);
            }

            var eventToSend = new KnowledgeBaseEvent()
            {
                ChannelId = context.Activity.ChannelId,
                ConversationId = context.Activity.Conversation.Id,
                EventType = Common.Events.EventType.DOWNLOADKB,
                UserId = context.Activity.From.Id
            };
            await eventProvider.SendEventAsync(eventToSend);
            await context.PostAsync(string.Format(messageProvider.GetMessage("knowledgeBaseDownload"), link, expiracy));
        }

        [LuisIntent("RefreshKnowledgeBaseFile")]
        public async Task RefreshKnowledgeBaseFile(IDialogContext context, LuisResult result)
        {
            var eventToSend = new KnowledgeBaseEvent()
            {
                ChannelId = context.Activity.ChannelId,
                ConversationId = context.Activity.Conversation.Id,
                EventType = Common.Events.EventType.REFRESHKB,
                UserId = context.Activity.From.Id
            };

            await eventProvider.SendEventAsync(eventToSend);

            await GenerateKnowledgeBaseFiles();

            await context.PostAsync(messageProvider.GetMessage("KnowledgeBaseRefreshed"));
        }


        [LuisIntent("SendGlobalNotifications")]
        public async Task SendGlobalNotifications(IDialogContext context, LuisResult result)
        {
            var notification = result.Entities.FirstOrDefault(x => x.Type == "message");

            var eventToSend = new GlobalNotificationEvent()
            {
                ChannelId = context.Activity.ChannelId,
                ConversationId = context.Activity.Conversation.Id,
                OriginalMessage = result.Query,
                UserId = context.Activity.From.Id
            };

            if (notification == null)
            {
                eventToSend.Success = false;
                await context.PostAsync(messageProvider.GetMessage("NotificationNotFound"));
            }
            else
            {
                var text = context.Activity.AsMessageActivity().Text.Substring(notification.StartIndex ?? 0, (notification.EndIndex ?? 0) - (notification.StartIndex ?? 0) + 1);
                eventToSend.Success = true;
                eventToSend.Notification = text;
                await queueProvider.InsertInQueueAsync(configuration.GetString("notificationsQueue"), new NotificationQueueEntity()
                {
                    Text = text
                });

                await eventProvider.SendEventAsync(eventToSend);
                await context.PostAsync(string.Format(messageProvider.GetMessage("NotificationSent"), text));
            }

        }


        private async Task GenerateKnowledgeBaseFiles()
        {
            var knowledgeBase = await qnaMakerProvider.GetKnowledgeBase();

            if (knowledgeBase != null)
            {
                var publicFileSB = new StringBuilder();
                var privateFileSB = new StringBuilder();

                foreach (var qna in knowledgeBase.KnowledgeBase)
                {
                    publicFileSB.AppendLine(qna.Key);
                    privateFileSB.AppendLine(string.Format("{0}\t{1}", qna.Key, qna.Value));
                }

                var containerName = configuration.GetString("KbFilesContainer");

                await blobStorageProvider.WriteFileContentsAsync(containerName, configuration.GetString("publicKbFile"), publicFileSB.ToString());
                await blobStorageProvider.WriteFileContentsAsync(containerName, configuration.GetString("privateKbFile"), privateFileSB.ToString());
            }
        }
    }
}