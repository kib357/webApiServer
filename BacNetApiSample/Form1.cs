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
            object k = await bacnet[200].Objects["AV21"].GetAsync();
            //float f = await bacnet[200].Objects["AV21"].GetAsync<float>();
            //object r = await bacnet[200].Objects["AV21"].GetAsync<List<BacNetObject>>();
            textBox1.Text = k != null ? k.ToString() : "null";
            s.Enabled = true;
        }
    }
}
