using System.Collections.Generic;
using System.Windows;
using LightService.Monitor.LightControl;

namespace LightService.Monitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			AstoriaLightServiceControlClient client = new AstoriaLightServiceControlClient();
			client.UpdateControlledObjects(new[] { new LightZone { InputAddress = "1400.BV1108", OutputAddresses = new[] { "17811.AO1101" }, SetPointAddress = "1400.AV1108" } });
		}
	}
}
