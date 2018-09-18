// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs.Internals;
using ModernApps.CommunityBot.BotCommon;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs;
using Autofac;

namespace ModernApps.CommunityBot.ExpertsBot.LongRunningThreads
{
    public class SkypeEmulatorListenerChannel : IQueueListenerPostChannel
    {
        public ChannelTypes ChannelType => ChannelTypes.SKYPE;

        public string ServiceUrl { get; set; }
        public string ChannelId { get; set; }

        public string BotId { get; set; }
        public string BotName { get; set; }

        public string ConversationId { get; set; }

        public async Task<string> SendToChannel(IActivity activity)
        {
            MicrosoftAppCredentials.TrustServiceUrl(ServiceUrl, DateTime.MaxValue);
            var connector = new ConnectorClient(new Uri(ServiceUrl));

            activity.ChannelId = ChannelId;
            activity.From = new ChannelAccount(id: BotId, name: BotName);
            activity.Conversation = new ConversationAccount(id: ConversationId);


            await connector.Conversations.SendToConversationAsync((Activity)activity);
            return ConversationId;
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
            if (obj is SkypeEmulatorListenerChannel)
            {
                var tqlc2 = obj as SkypeEmulatorListenerChannel;

                return tqlc2.BotId == BotId &&
                        tqlc2.BotName == BotName &&
                        tqlc2.ServiceUrl == ServiceUrl &&
                        tqlc2.ConversationId == ConversationId && tqlc2.ChannelId == ChannelId;
            }
            return false;
        }

        public Task StartWrongAnswerFlow(IMessageActivity messageActivity, string question, string originalQuestion, string originalAnswer)
        {
            throw new NotImplementedException();
        }
    }
}