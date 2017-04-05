using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var Template = System.IO.File.ReadAllText("example.html");

            var HeatMap = new LibHeatmap.HeatMap("<API KEY HERE>", new Size(1920, 1080), Template);

            pictureBox1.Image = HeatMap.Generate();
        }
    }
}
