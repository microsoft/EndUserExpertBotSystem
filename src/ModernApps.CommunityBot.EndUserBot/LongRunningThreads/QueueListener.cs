// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.Common.Events;
using Newtonsoft.Json;
using System;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace ModernApps.CommunityBot.EndUserBot.LongRunningThreads
{
    public class QueueListener
    {
        private IQueueProvider queueProvider;
        private IMessageProvider messageProvider;
        private ITableStorageProvider tableStorageProvider;
        private EventProviderHub eventProvider;

        public QueueListener(IQueueProvider queueProvider, IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider, EventProviderHub eventProvider)
        {
            this.queueProvider = queueProvider;
            this.messageProvider = messageProvider;
            this.tableStorageProvider = tableStorageProvider;
            this.eventProvider = eventProvider;
        }

        public async void Execute()
        {
            while (true)
            {
                MessageEntity message = null;
                do
                {
                    message = await queueProvider.DequeueAsync<MessageEntity>("experttouser");


                    if (message != null)
                    {
                        var questionFromStillInterested = await tableStorageProvider.RetrievePartitionFromTableAsync<StillInterestedEntity>("stillinterested", message.Question);

                        var users = await tableStorageProvider.RetrieveTableAsync<UserEntity>("users");
                        if (questionFromStillInterested.Any())
                        {
                            foreach (var question in questionFromStillInterested)
                            {
                                var user = users.FirstOrDefault(x => x.UserId == question.UserId);
                                if (user != null)
                                {
                                    var conversationReference = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationReference);

                                    var connector = new ConnectorClient(new Uri(conversationReference.ServiceUrl));
                                    MicrosoftAppCredentials.TrustServiceUrl(conversationReference.ServiceUrl, DateTime.MaxValue);
                                    var messageActivity = Activity.CreateMessageActivity();

                                    if (!question.ReceivedAnswer)
                                    {
                                        question.ReceivedAnswer = true;
                                        if (string.Compare(message.OriginalAnswer, message.ExpertAnswer, StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            messageActivity.Text = string.Format(messageProvider.GetMessage("ReceivedOriginalAnswer"), message.Question, message.ExpertAnswer);
                                        }
                                        else
                                        {
                                            messageActivity.Text = string.Format(messageProvider.GetMessage("ReceivedAnswer"), message.Question, message.ExpertAnswer);
                                        }
                                    }
                                    else if (user.Notifications)

                                    {
                                        messageActivity.Text = string.Format(messageProvider.GetMessage("ReceivedAnswerUpdate"), message.Question, message.ExpertAnswer);
                                    }
                                    else
                                    {
                                        //Notifications are off. We clean the entry
                                        await tableStorageProvider.DeleteFromTableAsync("stillinterested", new StillInterestedEntity[] { question });
                                        return;
                                    }

                                    messageActivity.Conversation = new ConversationAccount(id: conversationReference.Conversation.Id);
                                    messageActivity.Recipient = new ChannelAccount(id: conversationReference.User.Id, name: conversationReference.User.Name);
                                    messageActivity.From = new ChannelAccount(id: conversationReference.Bot.Id, name: conversationReference.Bot.Name);
                                    messageActivity.ChannelId = conversationReference.ChannelId;

                                    try
                                    {
                                        await connector.Conversations.SendToConversationAsync((Activity)messageActivity);
                                        await tableStorageProvider.SendToTableAsync("stillinterested", question);
                                    }
                                    catch (Exception e)
                                    {
                                        await eventProvider.SendEventAsync(new TechnicalErrorEvent()
                                        {
                                            Exception = JsonConvert.SerializeObject(e),
                                            ChannelId = conversationReference.ChannelId,
                                            ConversationId = conversationReference.Conversation.Id,
                                        });
                                        continue;
                                    }
                                }
                            }

                        }
                    }
                } while (message != null);
                Thread.Sleep(10000);
            }
        }
    }
}