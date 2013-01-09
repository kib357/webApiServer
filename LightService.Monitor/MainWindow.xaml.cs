using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using LightService.Monitor.LightControl;
using LightZone = LightService.Common.LightZone;

//using LightService.Monitor.LightControl;

namespace LightService.Monitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public List<LightZone> LightZones { get; set; };
		private AstoriaLightServiceControlClient _client;

		public MainWindow()
		{
			InitializeComponent();
			_client = new AstoriaLightServiceControlClient();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			var s = new XmlSerializer(typeof(List<LightZone>));

			using (var m = File.OpenWrite("lightZones.xml"))
			{
				s.Serialize(m, LightControl.InitLightZones());
			}

			//var client = new AstoriaLightServiceControlClient();
			////client.UpdateControlledObjects(new[] { new LightZone { InputAddress = "1400.BV1108", OutputAddresses = new[] { "17811.AO1101" }, SetPointAddress = "1400.AV1108" } });
			//client.UpdateControlledObjects(Common.LightControl.InitLightZones());
		}
	}
}
