using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using LightService.Common;
using LightService.Monitor.LightControlReference;
using LightZone = LightService.Common.LightZone;

//using LightService.Monitor.LightControl;

namespace LightService.Monitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<LightZone> LightZones { get; set; }
		private readonly AstoriaLightServiceControlClient _client;
		private DispatcherTimer _updateTimer; 

		public MainWindow()
		{
			_client = new AstoriaLightServiceControlClient();
			LightZones = new ObservableCollection<LightZone>(_client.GetControlledObjects());
			_updateTimer = new DispatcherTimer();
			_updateTimer.Interval = new TimeSpan(0, 0, 5);
			_updateTimer.Tick += UpdateLightZones;
			InitializeComponent();
			_updateTimer.Start();
			DataContext = this;
		}

		private void UpdateLightZones(object sender, EventArgs e)
		{
			LightZones.Clear();
			foreach (var lightZone in _client.GetControlledObjects())
			{
				LightZones.Add(lightZone);
			}
			//LightZones = new ObservableCollection<LightZone>(_client.GetControlledObjects());
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
