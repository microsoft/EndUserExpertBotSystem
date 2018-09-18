// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using ModernApps.CommunityBot.ExpertsBot.LongRunningThreads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModernApps.CommunityBot.EndUserBot.LongRunningThreads
{
    public class IssuesThread
    {
        private IQueueProvider queueProvider;
        private IMessageProvider messageProvider;
        private ITableStorageProvider tableStorageProvider;
        private IConfigurationProvider configurationProvider;

        public IssuesThread(IQueueProvider queueProvider, IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider, IConfigurationProvider configurationProvider)
        {
            this.queueProvider = queueProvider;
            this.tableStorageProvider = tableStorageProvider;
            this.messageProvider = messageProvider;
            this.configurationProvider = configurationProvider;
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
                IssueQueueEntity message = null;
                do
                {
                    var channels = await GetChannels();
                    if (channels.Any())
                    {

                        message = await queueProvider.DequeueAsync<IssueQueueEntity>(configurationProvider.GetString("issueQueue"));
                        if (message != null)
                        {
                            await tableStorageProvider.SendToTableAsync("openIssues", message);
                            foreach (var channel in channels)
                            {

                                var messageActivity = Activity.CreateMessageActivity();
                                messageActivity.Type = ActivityTypes.Message;
                                messageActivity.Text = string.Format(messageProvider.GetMessage("NewIssue"), message.Text);
                                await channel.SendToChannel(messageActivity);
                            }
                        }
                    }
                } while (message != null);
                Thread.Sleep(TimeSpan.FromMinutes(configurationProvider.GetConfiguration<int>("IssuePollInterval")));
            }
        }
    }
}