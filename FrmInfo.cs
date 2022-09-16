using System.Reflection;

namespace GPSClient
{
    public partial class FrmInfo : Form
    {
        public FrmInfo()
        {
            InitializeComponent();
            //Get application version and print in Form Info
            var varVersion = Assembly.GetExecutingAssembly().GetName().Version;
            textBox2.Text = "Copyright © 2022 - Matteo Abrile - Ver. " + varVersion;
        }
    }
}
