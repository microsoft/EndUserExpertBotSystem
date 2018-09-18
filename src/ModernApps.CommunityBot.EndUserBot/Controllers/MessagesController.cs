// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.BotCommon;
using ModernApps.CommunityBot.EndUserBot.Dialogs;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Entities;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.ConnectorEx;

namespace ModernApps.CommunityBot.EndUserBot
{
    public class MessagesController : MessagesControllerBase
    {
        private ITableStorageProvider tableStorageProvider;

        public MessagesController(IMessageProvider messageProvider, ITableStorageProvider tableStorageProvider) : base(messageProvider)
        {
            this.tableStorageProvider = tableStorageProvider;
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            RemoveBotNameFromMessage(activity);


            if (activity.Type == ActivityTypes.Message)
            {
                await UpdateUserTableEntity(activity);
                await Conversation.SendAsync(activity, () => (RootDialog)Configuration.DependencyResolver.GetService(typeof(RootDialog)));
            }
            else
            {
                HandleSystemMessage(activity, messageProvider.GetMessage("BotWelcome"));
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task UpdateUserTableEntity(Activity activity)
        {
            var entity = new UserEntity()
            {
                ChannelId = activity.ChannelId,
                UserId = activity.From.Id,
                ConversationReference = JsonConvert.SerializeObject(activity.AsMessageActivity().ToConversationReference()),
                Notifications = true,
                LastActiveTime = DateTime.UtcNow
            };

            var existingEntity = await tableStorageProvider.RetrieveFromTableAsync<UserEntity>("users", entity.PartitionKey, entity.RowKey);
            if (existingEntity != null)
            {
                entity.Notifications = existingEntity.Notifications;
            }

            await tableStorageProvider.SendToTableAsync("users", entity);
        }
    }
}