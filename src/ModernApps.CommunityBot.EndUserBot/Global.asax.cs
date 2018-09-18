using Microsoft.Bot.Builder.Dialogs;
using ModernApps.CommunityBot.EndUserBot.LongRunningThreads;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace ModernApps.CommunityBot.EndUserBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var webApiConfig = new WebApiConfig(ConfigurationManager.AppSettings["dataTableName"] ,ConfigurationManager.AppSettings["configFileName"], ConfigurationManager.AppSettings["messageFileName"]);
            GlobalConfiguration.Configure(webApiConfig.Register);

            Task.Run(() =>
            {
                var task = (QueueListener)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(QueueListener));
                task.Execute();
            });
            Task.Run(() =>
            {
                var task = (StillInterestedThread)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(StillInterestedThread));
                task.Execute();
            });
            Task.Run(() =>
            {
                var task = (GlobalNotificationsThread)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(GlobalNotificationsThread));
                task.Execute();
            });
        }
    }
}
