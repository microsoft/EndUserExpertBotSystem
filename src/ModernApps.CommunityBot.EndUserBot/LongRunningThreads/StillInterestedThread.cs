// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
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
    public class StillInterestedThread
    {
        private IQueueProvider queueProvider;
        private IMessageProvider messageProvider;
        private ITableStorageProvider tableStorageProvider;
        private IConfigurationProvider configurationProvider;

        public StillInterestedThread(IQueueProvider queueProvider, IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider, IConfigurationProvider configurationProvider)
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
                IEnumerable<StillInterestedEntity> messages = null;
                do
                {
                    var day = DateTimeOffset.UtcNow.AddDays(-configurationProvider.GetConfiguration<int>("stillInterestedTimeoutDays"));

                    TableQuery<StillInterestedEntity> tableQuery = new TableQuery<StillInterestedEntity>().Where(TableQuery.CombineFilters(
                                                                                            TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, day),
                                                                                            TableOperators.And, TableQuery.GenerateFilterConditionForBool("ReceivedAnswer",QueryComparisons.Equal,false)));

                    messages = await tableStorageProvider.RetrieveFromTableAsync("stillInterested",tableQuery);
                    if (messages.Any(x=>!x.ReceivedAnswer))
                    {
                        foreach (var message in messages.Where(x=>!x.ReceivedAnswer))
                        {
                            var conversationReference = JsonConvert.DeserializeObject<ConversationReference>(message.ConversationReference).GetPostToBotMessage();

                            var client = new ConnectorClient(new Uri(conversationReference.ServiceUrl));

                            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, conversationReference))
                            {
                                var botData = scope.Resolve<IBotData>();
                                await botData.LoadAsync(CancellationToken.None);
                                var task = scope.Resolve<IDialogTask>();

                                //interrupt the stack
                                var dialog = new StillInterestedDialog(message);
                                task.Call(dialog.Void<object, IMessageActivity>(), null);

                                await task.PollAsync(CancellationToken.None);
                                //flush dialog stack
                                await botData.FlushAsync(CancellationToken.None);
                            }
                        }
                        await tableStorageProvider.DeleteFromTableAsync("stillInterested", messages);
                    }
                } while (messages.Any());
                Thread.Sleep(TimeSpan.FromHours(configurationProvider.GetConfiguration<int>("stillInterestedPollIntervalHours")));
            }
        }
    }
}