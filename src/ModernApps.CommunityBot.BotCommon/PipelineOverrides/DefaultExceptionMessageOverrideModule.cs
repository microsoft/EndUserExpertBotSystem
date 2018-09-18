// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Autofac;
using Microsoft.Bot.Builder.Autofac.Base;
using Microsoft.Bot.Builder.Dialogs.Internals;

namespace ModernApps.CommunityBot.BotCommon.PipelineOverrides
{
    public class DefaultExceptionMessageOverrideModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterKeyedType<UserToBotFilter, IPostToBot>().InstancePerLifetimeScope();

            builder.RegisterAdapterChain<IPostToBot>(
                typeof(EventLoopDialogTask),
                typeof(SetAmbientThreadCulture),
                typeof(QueueDrainingDialogTask),
                typeof(PersistentDialogTask),
                typeof(ExceptionTranslationDialogTask),
                typeof(SerializeByConversation),
                typeof(UserToBotFilter),
                typeof(LogPostToBot)
                ).InstancePerLifetimeScope();
        }
    }
}