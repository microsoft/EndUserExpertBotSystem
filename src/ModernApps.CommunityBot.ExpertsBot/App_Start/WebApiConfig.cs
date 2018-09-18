// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ModernApps.CommunityBot.BotCommon;
using System.Reflection;
using Autofac;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunityBot.ExpertsBot.LongRunningThreads;
using ModernApps.CommunityBot.ExpertsBot.Dialogs;
using Microsoft.Bot.Builder.Luis;
using ModernApps.CommunityBot.EndUserBot.LongRunningThreads;

namespace ModernApps.CommunityBot.ExpertsBot
{
    public class WebApiConfig : WebApiConfigBase
    {
        public WebApiConfig(string botDataTableName, string configFileName, string messageFileName) : base(Assembly.GetExecutingAssembly(), botDataTableName, configFileName, messageFileName)
        {
        }

        protected override void RegisterBotSpecificDependencies(ContainerBuilder container)
        {
            container.Register(x => new AzureBlobConfigurationProvider(x.Resolve<IBlobStorageProvider>(), "configurations", new string[] { "ExpertBotConfiguration.json" })).As<IConfigurationProvider>().SingleInstance();
            container.Register(x => new BlobStorageMessageProvider(x.Resolve<IBlobStorageProvider>(), "configurations", new string[] { "ExpertBotMessages.json" })).As<IMessageProvider>().SingleInstance();
            container.Register(x => new AzureQueueStorageProvider(x.Resolve<IConfigurationProvider>().GetString("QueueConnectionString"))).As<IQueueProvider>().SingleInstance();
            container.Register(x =>
            {
                var config = x.Resolve<IConfigurationProvider>();
                return new QnAMakerProvider(config.GetString("qnaMakerRootUrl"), config.GetString("qnaMakerManagementUrl"), config.GetString("qnaMakerKBId"), config.GetString("qnaMakerKey"), config.GetString("qnaMakerManagementKey"), config.GetConfiguration<double>("qnaMakerScoreThreshold"));
            }).As<IQnAMakerProvider>().SingleInstance();
            container.Register(x => new TableStorageProvider(x.Resolve<IConfigurationProvider>().GetString("TableConnectionString"))).As<ITableStorageProvider>().SingleInstance();
            container.Register(x =>
            {
                var config = x.Resolve<IConfigurationProvider>();
                return new RootDialog(new ILuisService[] { new LuisService(new LuisModelAttribute(config.GetString("LuisModelId"), config.GetString("LuisSubscriptionKey"), LuisApiVersion.V2, config.GetString("LuisDomain"))) },
                    x.Resolve<IBlobStorageProvider>(),
                    x.Resolve<IQueueProvider>()
                    );
            }
            ).AsSelf();

            container.RegisterType<QueueListener>().AsSelf().SingleInstance();
            container.RegisterType<IssuesThread>().AsSelf().SingleInstance();
        }
    }
}
