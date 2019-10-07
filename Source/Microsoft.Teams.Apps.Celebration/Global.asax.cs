// <copyright file="Global.asax.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Autofac;
    using Autofac.Integration.Mvc;
    using Autofac.Integration.WebApi;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Bot.Builder.Azure;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Teams.Apps.Common;
    using Microsoft.Teams.Apps.Common.Configuration;

    /// <summary>
    /// Define the events and data accessed globally in application.
    /// </summary>
    public class Global : System.Web.HttpApplication
    {
        /// <summary>
        /// Application start event.
        /// </summary>
        protected void Application_Start()
        {
            // Set Application Insights instrumentation key
            var configProvider = new LocalConfigProvider();
            TelemetryConfiguration.Active.InstrumentationKey = configProvider.GetSetting(CommonConfig.ApplicationInsightsInstrumentationKey);

            Conversation.UpdateContainer(
               builder =>
               {
                   builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));
                   builder.RegisterModule(new CelebrationsAppModule());

                   builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
                   builder.RegisterWebApiFilterProvider(GlobalConfiguration.Configuration);

                   builder.RegisterControllers(Assembly.GetExecutingAssembly());
               });

            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            DependencyResolver.SetResolver(new AutofacDependencyResolver(Conversation.Container));
        }
    }
}
