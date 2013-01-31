using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AstoriaControlService.ControlServiceHost
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			MainService service = null;
			if (args != null && args.Any())
			{
				if (args.Contains("/c"))
				{
					service = new MainService();
					service.Start(args);
				}
				if (args.Contains("/i"))
					Install(false);
				if (args.Contains("/u"))
					Install(true);
			}
			else
			{
				var servicesToRun = new ServiceBase[] { new MainService() };
				ServiceBase.Run(servicesToRun);
			}

			if (service != null)
				service.Close();
		}

		static void Install(bool undo)
		{
			try
			{
				using (var inst = new AssemblyInstaller(typeof(Program).Assembly, null))
				{
					IDictionary state = new Hashtable();
					inst.UseNewContext = true;
					try
					{
						if (undo)
						{
							inst.Uninstall(state);
						}
						else
						{
							inst.Install(state);
							inst.Commit(state);
						}
					}
					catch
					{
						try
						{
							inst.Rollback(state);
						}
						catch { }
						throw;
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}
	}
}
