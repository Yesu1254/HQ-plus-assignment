using ApiCore.Commands;
using ApiCore.CoreModels;
using ApiCore.Managers;
using System;
using System.Threading;
namespace ConsoleApplication1
{
    public class SendHotelRateReportAuto : CommandBase
    {
        private DateTime lastRun = DateTime.Now.AddMinutes(-10);

        //This is the called method, every 5 mins
        public override Result AutomationRun(DateTime now, RouteRequest request)
        {
            var minutes = (DateTime.Now - lastRun).TotalMinutes;
            if (minutes >= 5) return Run(request);

            else return AutomationNotRun();
        }


        public Result Run(RouteRequest request)
        {
            UnityManager.GetCommand<GetHotelRatesReport>().Run(request, true);
            return Result.Success();
        }
    }
}