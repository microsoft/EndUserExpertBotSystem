// Copyright (c) Microsoft Corporation. All rights reserved.// Licensed under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using System;
using Autofac.Integration.WebApi;
using System.Web.Http.Dependencies;
using Autofac;
using ModernApps.CommunitiyBot.Common.Providers;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.Reflection;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Azure;
using ModernApps.CommunityBot.BotCommon.PipelineOverrides;
using ModernApps.CommunitiyBot.Common.Configuration;
using ModernApps.CommunityBot.Common.Events;
using ModernApps.CommunityBot.Common.DataProviders.AzureStorage;
using ModernApps.CommunitiyBot.Common.Resources;

namespace ModernApps.CommunityBot.BotCommon
{
    public abstract class WebApiConfigBase
    {
        private Assembly entryAssembly;
        private string botDataTableName;
        private string configFileName;
        private string messageFileName;

        public WebApiConfigBase(Assembly entryAssembly, string botDataTableName, string configFileName,string messageFileName)
        {
            this.entryAssembly = entryAssembly;
            this.botDataTableName = botDataTableName;
            this.configFileName = configFileName;
            this.messageFileName = messageFileName;
        }

        public void Register(HttpConfiguration config)
        {
            var store = new TableBotDataStore(ConfigurationManager.AppSettings["StorageConnectionString"], botDataTableName);
            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterModule(new DefaultExceptionMessageOverrideModule());
                builder.Register(c => store)
                       .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                       .AsSelf()
                       .SingleInstance();

                builder.Register(c => new CachingBotDataStore(store,
                           CachingBotDataStoreConsistencyPolicy.LastWriteWins))
                           .As<IBotDataStore<BotData>>()
                           .AsSelf()
                           .InstancePerLifetimeScope();
            });


            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var container = RegisterDIContainer();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private ILifetimeScope RegisterDIContainer()
        {
            var container = new ContainerBuilder();

            container.RegisterApiControllers(entryAssembly);

            container.Register(x => new AzureBlobConfigurationProvider(x.Resolve<IBlobStorageProvider>(), "configurations", new string[] { configFileName })).As<IConfigurationProvider>().SingleInstance();
            container.Register(x => new AzureBlobStorageProvider(ConfigurationManager.AppSettings["storageAccount"], ConfigurationManager.AppSettings["storageAccountKey"])).As<IBlobStorageProvider>();
            container.Register(x => new TableStorageProvider(x.Resolve<IConfigurationProvider>().GetString("TableConnectionString"))).As<ITableStorageProvider>().SingleInstance();
            container.Register(x => new BlobStorageMessageProvider(x.Resolve<IBlobStorageProvider>(), "configurations", new string[] { messageFileName })).As<IMessageProvider>().SingleInstance();
            container.Register(x => new AzureQueueStorageProvider(x.Resolve<IConfigurationProvider>().GetString("QueueConnectionString"))).As<IQueueProvider>().SingleInstance();

            container.Register(x =>
            {
                var config = x.Resolve<IConfigurationProvider>();
                return new AzureEventHubProvider(config.GetString("EventHubConnectionString"), config.GetString("EventHubName"));
            }).As<IEventProvider>().SingleInstance();

            container.RegisterType<EventProviderHub>().AsSelf().SingleInstance();

            RegisterBotSpecificDependencies(container);

            return container.Build();
        }

        protected virtual void RegisterBotSpecificDependencies(ContainerBuilder container)
        {
            //EMPTY BY DEFAULT
        }
    }
}
