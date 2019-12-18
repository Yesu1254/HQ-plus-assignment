using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public static class GlobalsManager
    {

        public static string Version { get; set; }

        public static string GetCurrentDirectory { get; set; } = "C:\\FakeDir";

        public static DateTime StartupTime { get; set; }

        public static CultureInfo Culture { get; private set; }

        public static CancellationTokenSource ProcessCancellationToken { get; set; }

        static GlobalsManager()
        {
            StartupTime = DateTime.Now;

            var cultureInfo = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name);
            cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
            cultureInfo.NumberFormat.PercentDecimalSeparator = ".";
            cultureInfo.DateTimeFormat.LongDatePattern = "yyyy-MM-dd HH:mm:ss";
            cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            Culture = cultureInfo;
        }

        public static List<string> ProjectSourcePaths { get; set; }
        public static bool IsRunningTest { get; set; }

        public static void Setup(IApplicationBuilder app, List<string> basePaths, string assemblyNames, bool runWithAutomations = true)
        {
            try
            {
                app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowCredentials().AllowAnyOrigin());

                //app.UseSignalR2();
                app.Use(ActiveRequestMiddleware.ActionHandler);
                //app.UseMaxConcurrentRequests().Use(ActionDelegator.ActionHandler);

                //app.Use(next => async (context) =>
                //{
                //    //var hubContext = context.RequestServices.GetRequiredService<IHubContext<EventHub>>();
                //    //SendSignalRMessage.hubContext = hubContext;
                //});

                //Force the current culture
                var currentCulture = new System.Globalization.CultureInfo("en-ZA");
                var currentUiCulture = new System.Globalization.CultureInfo("en-ZA");

                currentCulture.NumberFormat.PercentDecimalSeparator = ".";
                currentCulture.NumberFormat.CurrencyDecimalSeparator = ".";
                currentCulture.NumberFormat.CurrencyDecimalSeparator = ".";

                currentUiCulture.NumberFormat.PercentDecimalSeparator = ".";
                currentUiCulture.NumberFormat.CurrencyDecimalSeparator = ".";
                currentUiCulture.NumberFormat.CurrencyDecimalSeparator = ".";

                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentUiCulture;

                //var connectionStringName = ApplicationName;

                //map all delegates
                //DelegateComposer.MapAll();


                try
                {
                    //TimeEventManager.Init();
                }
                catch (Exception ex)
                {

                    //throw;
                }

                ServiceRequestManager.Setup();


            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(GetCurrentDirectory + "Setup_Error.txt", DateTime.Now.ToString() + ": " + ex.Message + "\r");
                if (Debugger.IsAttached) throw;
            }

        }
    }
}
