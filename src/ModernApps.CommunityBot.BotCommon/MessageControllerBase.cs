// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ModernApps.CommunitiyBot.Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ModernApps.CommunityBot.BotCommon
{
    [BotAuthentication]
    public abstract class MessagesControllerBase : ApiController
    {
        protected IMessageProvider messageProvider;

        public MessagesControllerBase(IMessageProvider messageProvider)
        {
            this.messageProvider = messageProvider;
        }

        protected void RemoveBotNameFromMessage(Activity message)
        {
            if (message.Type == ActivityTypes.Message)
            {
                    var mentions = message.GetMentions();
                    foreach (var mention in mentions)
                    {
                        message.Text = message.Text.Replace(mention.Text, "");
                    }
            }
        }


        protected Activity HandleSystemMessage(Activity message, string welcomeMesseage)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                if (message.MembersAdded.Any(x => x.Id == message.Recipient.Id))
                {
                    var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    var reply = message.CreateReply(welcomeMesseage);
                    connector.Conversations.ReplyToActivity(reply);

                }
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

       
    }
}
