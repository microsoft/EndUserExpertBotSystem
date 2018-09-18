// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.Common.Events;
using ModernApps.CommunityBot.Common.Helpers;
using ModernApps.CommunityBot.ExpertsBot.Events;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace ModernApps.CommunityBot.ExpertsBot.LongRunningThreads
{
    public class QueueListener
    {
        private IQueueProvider queueProvider;
        private IMessageProvider messageProvider;
        private ITableStorageProvider tableStorageProvider;
        private IConfigurationProvider configurationProvider;
        private IEventProvider eventProvider;



        public QueueListener(IQueueProvider queueProvider, IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider, IConfigurationProvider configurationProvider, IEventProvider eventProvider)
        {
            this.queueProvider = queueProvider;
            this.messageProvider = messageProvider;
            this.tableStorageProvider = tableStorageProvider;
            this.configurationProvider = configurationProvider;
            this.eventProvider = eventProvider;
        }

        public async Task AddChannel(IQueueListenerPostChannel queueListenerPostChannel)
        {
            var channels = await GetChannels();
            if (!channels.Any(x => x.Equals(queueListenerPostChannel)))
                await tableStorageProvider.SendToTableAsync("expertBotChannels", new QueueListenerPostChannelTableEntity()
                {
                    ChannelObject = JsonConvert.SerializeObject(queueListenerPostChannel),
                    Type = queueListenerPostChannel.ChannelType
                });
        }

        public async Task<IList<IQueueListenerPostChannel>> GetChannels()
        {
            var channelEntities = await tableStorageProvider.RetrieveTableAsync<QueueListenerPostChannelTableEntity>("expertBotChannels");
            return channelEntities.Select(x =>
            {
                if (x.Type == ChannelTypes.MSTEAMS)
                {
                    return JsonConvert.DeserializeObject<TeamsQueueListenerChannel>(x.ChannelObject);
                }
                else
                {
                    return (IQueueListenerPostChannel)JsonConvert.DeserializeObject<SkypeEmulatorListenerChannel>(x.ChannelObject);
                }
            }).ToList();

        }

        public async void Execute()
        {
            while (true)
            {
                MessageEntity message = null;
                var channels = await GetChannels();
                var runTime = DateTime.UtcNow;
                do
                {
                    if (channels.Any())
                    {
                        message = await queueProvider.DequeueAsync<MessageEntity>("usertoexpert");
                        if (message != null)
                        {

                            NormalizeQuestion(message);


                            await tableStorageProvider.SendToTableAsync("unansweredquestions", message);
                            var eventToSend = new NewQuestionEvent()
                            {
                                MessageType = message.MessageType,
                                Question = message.Question,
                                OriginalAnswer = message.OriginalAnswer,
                                QuestionCorrelationId = message.QuestionCorrelationId
                            };
                            await eventProvider.SendEventAsync(eventToSend);

                            try
                            {
                                foreach (var channel in channels)
                                {

                                    var messageActivity = Activity.CreateMessageActivity();
                                    messageActivity.Type = ActivityTypes.Message;
                                    if (message.MessageType == MessageType.NOANSWER)
                                    {
                                        messageActivity.Text = string.Format(messageProvider.GetMessage("PostMessageToExpertsNoAnswer"), message.Question);
                                        var conversationId = await channel.SendToChannel(messageActivity);
                                        eventToSend.ConversationId = conversationId;
                                        await channel.SendToConverstionData(conversationId, messageActivity, new Dictionary<string, string>() { { "question", message.Question } });
                                    }
                                    else
                                    {

                                        var actions = new List<CardAction>();
                                        actions.Add(new CardAction
                                        {
                                            Title = messageProvider.GetMessage("KeepOriginalWrong"),
                                            Type = "imBack",
                                            Value = messageProvider.GetMessage("KeepOriginalWrong"),
                                        });
                                        actions.Add(new CardAction
                                        {
                                            Title = messageProvider.GetMessage("KeepNewWrong"),
                                            Value = messageProvider.GetMessage("KeepNewWrong"),
                                            Type = "imBack"
                                        });

                                        var normalizedQuestion = MessageHelper.NormalizeString(message.Question);
                                        var normalizedMatchingQuestion = MessageHelper.NormalizeString(message.OriginalQuestion);

                                        if (string.Compare(normalizedQuestion, normalizedMatchingQuestion, StringComparison.InvariantCultureIgnoreCase) != 0)
                                        {
                                            actions.Add(new CardAction
                                            {
                                                Title = messageProvider.GetMessage("KeepBothWrong"),
                                                Value = messageProvider.GetMessage("KeepBothWrong"),
                                                Type = "imBack"
                                            });
                                        }

                                        var card = new HeroCard()
                                        {
                                            Text = string.Format(messageProvider.GetMessage("PostMessageToExpertsWrongAnswerCard"), normalizedQuestion, message.OriginalAnswer, normalizedMatchingQuestion),
                                            Buttons = actions
                                        };
                                        messageActivity.Attachments.Add(card.ToAttachment());

                                        var conversationId = await channel.SendToChannel(messageActivity);
                                        eventToSend.ConversationId = conversationId;
                                        await channel.SendToConverstionData(conversationId, messageActivity, new Dictionary<string, string>() {
                                            { "question", message.Question },
                                            { "originalQuestion", message.OriginalQuestion },
                                            { "originalAnswer", message.OriginalAnswer }
                                        });
                                    }

                                }
                            }
                            catch (Exception e)
                            {
                                await eventProvider.SendEventAsync(new TechnicalErrorEvent()
                                {
                                    Exception = JsonConvert.SerializeObject(e),
                                    ChannelId = string.Empty,
                                    ConversationId = string.Empty
                                });
                                continue;
                            }
                        }
                    }
                } while (message != null && (DateTime.UtcNow - runTime).TotalMinutes < configurationProvider.GetConfiguration<int>("expertBotChannelsPoll"));
                Thread.Sleep((int)TimeSpan.FromMinutes(configurationProvider.GetConfiguration<int>("ExpertBotQueuePoll")).TotalMilliseconds);
            }
        }

        private void NormalizeQuestion(MessageEntity message)
        {
            message.Question = MessageHelper.NormalizeString(message.Question);
        }

    }
}