namespace LightService.LightServiceHost
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.AstoriaLightProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.AstoriaLightServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// AstoriaLightProcessInstaller
			// 
			this.AstoriaLightProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.AstoriaLightProcessInstaller.Password = null;
			this.AstoriaLightProcessInstaller.Username = null;
			// 
			// AstoriaLightServiceInstaller
			// 
			this.AstoriaLightServiceInstaller.ServiceName = "AstoriaLight";
			this.AstoriaLightServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.AstoriaLightProcessInstaller,
            this.AstoriaLightServiceInstaller});

		}

		#endregion

		public System.ServiceProcess.ServiceProcessInstaller AstoriaLightProcessInstaller;
		public System.ServiceProcess.ServiceInstaller AstoriaLightServiceInstaller;
	}
}