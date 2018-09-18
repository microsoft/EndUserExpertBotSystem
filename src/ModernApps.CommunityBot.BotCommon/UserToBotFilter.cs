// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs.Internals;
using System;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Resources;
using Microsoft.Bot.Builder.Internals.Fibers;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http;
using Newtonsoft.Json;
using ModernApps.CommunitiyBot.Common.Resources;

namespace ModernApps.CommunityBot.BotCommon
{
    public class UserToBotFilter : IPostToBot
    {
        private readonly IPostToBot inner;
        private readonly IBotToUser botToUser;
        private readonly ResourceManager resources;
        private readonly TraceListener trace;

        private IMessageProvider messageProvider;

        public UserToBotFilter(IPostToBot inner, IBotToUser botToUser, ResourceManager resources, TraceListener trace)
        {
            this.messageProvider = (IMessageProvider)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IMessageProvider));
            SetField.NotNull(out this.inner, nameof(inner), inner);
            SetField.NotNull(out this.botToUser, nameof(botToUser), botToUser);
            SetField.NotNull(out this.resources, nameof(resources), resources);
            SetField.NotNull(out this.trace, nameof(trace), trace);
        }

        async Task IPostToBot.PostAsync(IActivity activity, CancellationToken token)
        {
            try
            {
                await inner.PostAsync(activity, token);
            }
            catch (Exception ex)
            {
                trace.WriteLine(ex.Message);
                var messageActivity = activity.AsMessageActivity();
                if (messageActivity != null)
                {
                    await botToUser.PostAsync(messageProvider.GetMessage("ExceptionMessage"), cancellationToken: token);
                    await botToUser.PostAsync(messageProvider.GetMessage("WhatMoreCanIDo"), cancellationToken: token);

                    using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, messageActivity))
                    {
                        var botData = scope.Resolve<IBotData>();
                        await botData.LoadAsync(default(CancellationToken));
                        var stack = scope.Resolve<IDialogStack>();
                        stack.Reset();
                        botData.ConversationData.Clear();
                        await botData.FlushAsync(default(CancellationToken));
                    }
                }
            }
        }


    }
}
