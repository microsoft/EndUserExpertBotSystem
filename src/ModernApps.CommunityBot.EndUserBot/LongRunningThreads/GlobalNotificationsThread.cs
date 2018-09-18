// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.EndUserBot.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.EndUserBot.LongRunningThreads
{
    public class GlobalNotificationsThread
    {
        private IQueueProvider queueProvider;
        private IMessageProvider messageProvider;
        private ITableStorageProvider tableStorageProvider;
        private IConfigurationProvider configurationProvider;

        public GlobalNotificationsThread(IQueueProvider queueProvider, IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider, IConfigurationProvider configurationProvider)
        {
            this.queueProvider = queueProvider;
            this.tableStorageProvider = tableStorageProvider;
            this.messageProvider = messageProvider;
            this.configurationProvider = configurationProvider;
        }

        public async void Execute()
        {
            while (true)
            {
                NotificationQueueEntity message = null;
                do
                {
                    message = await queueProvider.DequeueAsync<NotificationQueueEntity>(configurationProvider.GetString("notificationsQueue"));
                    if (message != null)
                    {
                        var users = await tableStorageProvider.RetrieveTableAsync<UserEntity>("users");
                        Parallel.ForEach(users, (user) =>
                         {
                             var conversationReference = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationReference);
                             var client = new ConnectorClient(new Uri(conversationReference.ServiceUrl));

                             var messageActivity = Activity.CreateMessageActivity();

                             messageActivity.Conversation = new ConversationAccount(id: conversationReference.Conversation.Id);
                             messageActivity.Recipient = new ChannelAccount(id: conversationReference.User.Id, name: conversationReference.User.Name);
                             messageActivity.From = new ChannelAccount(id: conversationReference.Bot.Id, name: conversationReference.Bot.Name);
                             messageActivity.ChannelId = conversationReference.ChannelId;
                             messageActivity.Text = string.Format(messageProvider.GetMessage("NewNotification"), message.Text);

                             client.Conversations.SendToConversation((Activity)messageActivity);
                         });
                    }
                } while (message != null);
                Thread.Sleep(TimeSpan.FromMinutes(configurationProvider.GetConfiguration<int>("NotificationPollInterval")));
            }
        }
    }
}