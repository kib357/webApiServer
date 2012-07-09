using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BacNetApi;
using BacNetTypes.Primitive;

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
            bacnet.Initialize(IPAddress.Parse("192.168.0.106"));
            object k = await bacnet[200].Objects["AV21"].GetAsync();
            object r = await bacnet[200].Objects["AV21"].GetAsync<List<BacNetObject>>();
            textBox1.Text = k != null ? k.ToString() : "null";
            s.Enabled = true;
        }
    }
}
