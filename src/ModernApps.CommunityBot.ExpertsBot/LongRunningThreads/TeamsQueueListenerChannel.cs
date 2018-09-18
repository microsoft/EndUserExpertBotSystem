// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams.Models;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using ModernApps.CommunityBot.BotCommon;
using Autofac;
using System.Threading;
using ModernApps.CommunityBot.ExpertsBot.Dialogs;

namespace ModernApps.CommunityBot.ExpertsBot.LongRunningThreads
{
    public class TeamsQueueListenerChannel : IQueueListenerPostChannel
    {
        public ChannelTypes ChannelType => ChannelTypes.MSTEAMS;
        public string ServiceUrl { get; set; }
        public string TeamsChannelId { get; set; }

        public string ChannelId { get; set; }

        public string BotId { get; set; }
        public string BotName { get; set; }

        public async Task<string> SendToChannel(IActivity activity)
        {
            MicrosoftAppCredentials.TrustServiceUrl(ServiceUrl, DateTime.MaxValue);
            var connector = new ConnectorClient(new Uri(ServiceUrl));
            var channelData = new TeamsChannelData(channel: new ChannelInfo(TeamsChannelId));

            var conversationParameters = new ConversationParameters(
                isGroup: true,
                bot: new ChannelAccount(id: BotId, name: BotName),
                channelData: channelData,
                topicName: "New question",
                activity: (Activity)activity
                );

            return (await connector.Conversations.CreateConversationAsync(conversationParameters)).Id;


        }

        public async Task SendToConverstionData(string converstationId, IActivity activity, Dictionary<string, string> dataToSend)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, (IMessageActivity)activity))
            {
                MicrosoftAppCredentials.TrustServiceUrl(ServiceUrl, DateTime.MaxValue);
                var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                var key = new AddressKey
                {
                    BotId = BotId,
                    ChannelId = ChannelId,
                    ConversationId = converstationId,
                    ServiceUrl = ServiceUrl
                };

                var conversationData = await botDataStore.LoadAsync(key, BotStoreType.BotConversationData, CancellationToken.None);

                foreach (var data in dataToSend)
                {
                    conversationData.SetProperty(data.Key, data.Value);
                }

                await botDataStore.SaveAsync(key, BotStoreType.BotConversationData, conversationData, CancellationToken.None);
                await botDataStore.FlushAsync(key, CancellationToken.None);
            }
        }


        public override bool Equals(object obj)
        {
            if (obj is TeamsQueueListenerChannel)
            {
                var tqlc2 = obj as TeamsQueueListenerChannel;

                return tqlc2.BotId == BotId &&
                        tqlc2.BotName == BotName &&
                        tqlc2.ServiceUrl == ServiceUrl &&
                        tqlc2.TeamsChannelId == TeamsChannelId;
            }
            return false;
        }
    }
}