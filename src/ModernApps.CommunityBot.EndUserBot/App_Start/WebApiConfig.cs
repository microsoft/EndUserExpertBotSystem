// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ModernApps.CommunityBot.BotCommon;
using Autofac;
using ModernApps.CommunitiyBot.Common.Resources;
using ModernApps.CommunitiyBot.Common.Providers;
using ModernApps.CommunitiyBot.Common.Configuration;
using System.Configuration;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunityBot.EndUserBot.Dialogs;
using ModernApps.CommunityBot.EndUserBot.LongRunningThreads;
using System.Reflection;
using Autofac.Integration.WebApi;
using Microsoft.Bot.Builder.Luis;
using ModernApps.CommunityBot.Common.Events;

namespace ModernApps.CommunityBot.EndUserBot
{
    public class WebApiConfig : WebApiConfigBase
    {
        public WebApiConfig(string botDataTableName, string configFileName, string messageFileName) : base(Assembly.GetExecutingAssembly(), botDataTableName, configFileName,messageFileName)
        {
        }


        protected override void RegisterBotSpecificDependencies(ContainerBuilder container)
        {
            
            
            container.Register(x =>
            {
                var config = x.Resolve<IConfigurationProvider>();
                return new QnAMakerProvider(config.GetString("qnaMakerRootUrl"),config.GetString("qnaMakerManagementUrl"), config.GetString("qnaMakerKBId"), config.GetString("qnaMakerKey"), config.GetString("qnaMakerManagementKey"),config.GetConfiguration<double>("qnaMakerScoreThreshold"));
            }).As<IQnAMakerProvider>().SingleInstance();
            
            container.Register(x =>
            {
                var config = x.Resolve<IConfigurationProvider>();
                return new RootDialog(new ILuisService[] { new LuisService(new LuisModelAttribute(config.GetString("LuisModelId"), config.GetString("LuisSubscriptionKey"), LuisApiVersion.V2, domain: config.GetString("LuisDomain"))) },
                    x.Resolve<IBlobStorageProvider>());
            }).As<RootDialog>();



            container.RegisterType<QueueListener>().AsSelf().SingleInstance();
            container.RegisterType<StillInterestedThread>().AsSelf().SingleInstance();
            container.RegisterType<GlobalNotificationsThread>().AsSelf().SingleInstance();
        }
    }
}
