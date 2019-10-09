using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            bool isAdminRun = CheckIsAdminRun();

            if (isAdminRun)
            {
                var serviceName = "Xly_WebApi";
                //var serviceController = new ServiceController(serviceName);
                //if (serviceController.Status == ServiceControllerStatus.Stopped)
                //{
                //    Action<ServiceController> action = p => p.Start();
                //    AsyncCallback callback = t =>
                //    {

                //    };
                //    IAsyncResult asyncResult = action.BeginInvoke(serviceController, callback, null);
                //    asyncResult.AsyncWaitHandle.WaitOne();
                //}
                //if (serviceController.CanStop)
                //{
                //    Action<ServiceController> action = p => p.Stop();
                //    AsyncCallback callback = t =>
                //    {

                //    };
                //    IAsyncResult asyncResult = action.BeginInvoke(serviceController, callback, null);
                //    asyncResult.AsyncWaitHandle.WaitOne();
                //}
                bool checkService = IsServiceExisted(serviceName);

                bool result1 = StartService(serviceName);
                bool result2 = StopService(serviceName);
            }




            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        /// <summary>
        /// 判断是否以管理员身份运行
        /// </summary>
        /// <returns>true~以管理员身份运行</returns>
        private static bool CheckIsAdminRun()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            bool isAdminRun = windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            return isAdminRun;
        }

        private bool StartService(string serviceName)
        {
            Func<ServiceController, bool> func = p => p.Status == ServiceControllerStatus.Stopped || p.Status == ServiceControllerStatus.Paused;
            Action<ServiceController> action = p =>
            {
                p.Start();
                p.WaitForStatus(ServiceControllerStatus.Running);
            };
            return ChangeServiceState(serviceName, func, action);
        }

        private bool StopService(string serviceName)
        {
            Func<ServiceController, bool> func = p => p.Status == ServiceControllerStatus.Running;
            Action<ServiceController> action = p =>
            {
                p.Stop();
                p.WaitForStatus(ServiceControllerStatus.Stopped);
            };
            return ChangeServiceState(serviceName, func, action);
        }

        private bool IsServiceExisted(string serviceName)
        {
            bool result = ServiceController.GetServices()?.Select(p => p.ServiceName)?.Distinct()?.Contains(serviceName) ?? false;
            return result;
        }

        private bool ChangeServiceState(string serviceName, Func<ServiceController, bool> CheckState, Action<ServiceController> UpdateState)
        {
            ServiceController serviceController = new ServiceController(serviceName);

            bool result = false;
            if (!CheckState(serviceController))
                return result;

            try
            {
                UpdateState(serviceController);
                result = true;
                return result;
            }
            catch (Exception ex)
            {
                result = false;
                /*日志记录*/
                return result;
            }
        }
    }
}
