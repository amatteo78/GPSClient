using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPSClient
{
    public partial class FrmInfo : Form
    {
        public FrmInfo()
        {
            InitializeComponent();
            //Get application version and print in Form Info
            var varVersion = Assembly.GetExecutingAssembly().GetName().Version;
            textBox2.Text = "Copyright © 2022 - Matteo Abrile - Ver. " +varVersion;
        }
    }
}
