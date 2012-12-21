using System.Configuration;
using System.Linq;
using System.ServiceProcess;

namespace LightService.LightServiceHost
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			MainService service = null;
			//var obj = ConfigurationManager.GetSection("LightControl");

			if (args != null && args.Any() && args[0] == "/c")
			{
				service = new MainService();
				service.Start(args);
			}
			else
			{
				var servicesToRun = new ServiceBase[] { new MainService() };
				ServiceBase.Run(servicesToRun);
			}

			if (service != null)
				service.Close();
		}
	}
}
