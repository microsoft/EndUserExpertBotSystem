// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace ModernApps.CommunityBot.BotCommon
{
    [Serializable]
    public abstract class DialogBase : LuisDialog<IDialogResult>, IDialog<IDialogResult>
    {
        protected const string GREETINGTAG = @"\[greeting\]";

        protected IConfigurationProvider configuration =>(IConfigurationProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IConfigurationProvider));
        protected IMessageProvider messageProvider => (IMessageProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IMessageProvider));
        protected IQnAMakerProvider qnaMakerProvider => (IQnAMakerProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IQnAMakerProvider));
        protected ITableStorageProvider tableStorageProvider => (ITableStorageProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(ITableStorageProvider));
        protected IQueueProvider queueProvider => (IQueueProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IQueueProvider));

        protected EventProviderHub eventProvider => (EventProviderHub)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(EventProviderHub));

        public DialogBase()
        {

        }

        public DialogBase(ILuisService[] services) : base(services)
        {
        }

        public async sealed override Task StartAsync(IDialogContext context)
        {
            await OnStart(context);
            if (services.Any())
            {
                await base.StartAsync(context);
            }
        }

        public virtual async Task OnStart(IDialogContext context)
        {
            //EMPTY
        }

        protected override LuisRequest ModifyLuisRequest(LuisRequest request)
        {
            request.Query = Regex.Replace(request.Query, @"[""']", "");
            return base.ModifyLuisRequest(request);
        }


        protected string GetLuisEntity(LuisResult result, EntityRecommendation question)
        {
            return result.Query.Substring(question.StartIndex.Value, question.EndIndex.Value - question.StartIndex.Value +1);
        }

        protected void ClearContextData(IDialogContext context)
        {
            context.ConversationData.Clear();
            context.UserData.Clear();
        }

    }
}
