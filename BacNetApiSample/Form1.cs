using System;
using System.Net;
using System.Windows.Forms;
using BacNetApi;

namespace BacNetApiSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var s = sender as Button;
            s.Enabled = false;
            var bacnet = new BacNet();
            bacnet.Initialize(IPAddress.Parse("192.168.0.169"));
            //object k = await bacnet[200].Objects["AV210"].GetAsync();
            bacnet[200].Objects["AV21"].ValueChangedEvent += OnBacnetValueChanged;
            /*bacnet[200].Objects["AV21"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV21"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV5"].ValueChangedEvent += OnBacnetValueChanged;*/
            //float f = await bacnet[200].Objects["AV21"].GetAsync<float>();
            //object r = await bacnet[200].Objects["AV21"].GetAsync<List<BacNetObject>>();
            //textBox1.Text = k != null ? k.ToString() : "null";
            s.Enabled = true;
        }

        private void OnBacnetValueChanged(string address, string value)
        {
            textBox1.Text = value;
        }
    }
}
