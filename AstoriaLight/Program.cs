using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AstoriaLight
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			var servicesToRun = new ServiceBase[] { new MainService() };
			ServiceBase.Run(servicesToRun);
		}
	}
}
