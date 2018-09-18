// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunityBot.ExpertsBot.Dialogs;
using ModernApps.CommunityBot.ExpertsBot.LongRunningThreads;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Connector.Teams.Models;

namespace ModernApps.CommunityBot.ExpertsBot
{

    public class MessagesController : MessagesControllerBase
    {
        private QueueListener listener;

        public MessagesController(IMessageProvider messageProvider, QueueListener queueListener) : base(messageProvider)
        {
            this.listener = queueListener;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            RemoveBotNameFromMessage(activity);
            await SetQueueListenerForChannel(activity);

            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => (RootDialog)Configuration.DependencyResolver.GetService(typeof(RootDialog)));
            }
            else
            {
                HandleSystemMessage(activity, messageProvider.GetMessage("BotWelcome"));
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task SetQueueListenerForChannel(Activity activity)
        {
            IQueueListenerPostChannel channel = null;
            switch (activity.ChannelId)
            {
                case "msteams":
                    channel = new TeamsQueueListenerChannel()
                    {
                        ServiceUrl = activity.ServiceUrl,
                        BotId = activity.Recipient.Id,
                        ChannelId = activity.ChannelId,
                        BotName = activity.Recipient.Name,
                        TeamsChannelId = activity.GetChannelData<TeamsChannelData>().Channel.Id
                    };
                    break;
                default:
                    channel = new SkypeEmulatorListenerChannel()
                    {
                        ServiceUrl = activity.ServiceUrl,
                        BotId = activity.Recipient.Id,
                        BotName = activity.Recipient.Name,
                        ChannelId = activity.ChannelId,
                        ConversationId = activity.Conversation.Id
                    };
                    break;
            }

            if (listener != null)
            {
               await listener.AddChannel(channel);
            }
        }
    }
}