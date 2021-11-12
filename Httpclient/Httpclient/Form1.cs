using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using Httpclient;
namespace Httpclient
{
    public partial class Form1 : Form
    {

        private HttpServer _Server;
        public Form1()
        {
            InitializeComponent();
            //string.Format("http://192.168.0.2:11000", Environment.MachineName);
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _Server = new HttpServer();
            _Server.Start(11000);
        }

       
    }
}
