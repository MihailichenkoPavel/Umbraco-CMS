﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;
using Microsoft.Extensions.Logging;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging.Serilog;
using Umbraco.Core.Runtime;
using Umbraco.Core.Configuration;
using Umbraco.Core.Hosting;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Web.Runtime;
using ILogger = Umbraco.Core.Logging.ILogger;

namespace Umbraco.Web
{
    /// <summary>
    /// Represents the Umbraco global.asax class.
    /// </summary>
    public class UmbracoApplication : UmbracoApplicationBase
    {
        protected override IRuntime GetRuntime(Configs configs, IUmbracoVersion umbracoVersion, IIOHelper ioHelper, ILogger logger, ILoggerFactory loggerFactory, IProfiler profiler, IHostingEnvironment hostingEnvironment, IBackOfficeInfo backOfficeInfo)
        {

            var connectionStringConfig = configs.ConnectionStrings()[Constants.System.UmbracoConnectionName];

            var dbProviderFactoryCreator = new UmbracoDbProviderFactoryCreator();

            var globalSettings = configs.Global();
            var connectionStrings = configs.ConnectionStrings();

            // Determine if we should use the sql main dom or the default
            var appSettingMainDomLock = globalSettings.MainDomLock;

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var mainDomLock = appSettingMainDomLock == "SqlMainDomLock" || isWindows == false
                ? (IMainDomLock)new SqlMainDomLock(logger, globalSettings, connectionStrings, dbProviderFactoryCreator, hostingEnvironment)
                : new MainDomSemaphoreLock(logger, hostingEnvironment);

            var mainDom = new MainDom(logger, mainDomLock);

            var requestCache = new HttpRequestAppCache(() => HttpContext.Current != null ? HttpContext.Current.Items : null);
            var appCaches = new AppCaches(
                new DeepCloneAppCache(new ObjectCacheAppCache()),
                requestCache,
                new IsolatedCaches(type => new DeepCloneAppCache(new ObjectCacheAppCache())));

            var umbracoBootPermissionChecker = new AspNetUmbracoBootPermissionChecker();
            return new CoreRuntime(configs, umbracoVersion, ioHelper, logger, loggerFactory, profiler, umbracoBootPermissionChecker, hostingEnvironment, backOfficeInfo, dbProviderFactoryCreator, mainDom,
                GetTypeFinder(hostingEnvironment, logger, profiler), appCaches);
        }


    }
}
