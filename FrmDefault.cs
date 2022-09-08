using System;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;
using System.Drawing.Text;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GPSClient
{
    public partial class FrmDefault : Form
    {
        //Inizialize variable boolStop for Start/Stop Stream
        bool boolStop = false;

        public FrmDefault()
        {
            InitializeComponent();
            txtPort.Text = "5005";
            
        }

        public bool IsValidIP(string addr) //Check if IP is Valid
        {
            //Create our Regular Expression object
            Regex check = new Regex("^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            //Boolean variable to hold the status
            bool valid = false;
            //Check to make sure an ip address was provided
            if (addr == "")
            {
                //No address provided so return false
                valid = false;
            }
            else
            {
                //Address provided so use the IsMatch Method
                //Of the Regular Expression object
                valid = check.IsMatch(addr, 0);
            }
            //Return the results
            return valid;
        }

        private void ReceiveDataGPS()
        {
            Task.Run(() => {
                //Inizialize TcpCLient
                TcpClient tcpClient = new TcpClient();
                //Check if IP Address and Port are present
                if (String.IsNullOrEmpty(txtGpsServer.Text) || String.IsNullOrEmpty(txtPort.Text))
                {
                    MessageBox.Show("GPS Server IP Address and/or Port can't be Empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else if (!IsValidIP(txtGpsServer.Text))
                {
                    MessageBox.Show("IP Address must be need !!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    //'command.Invoke' manage object outside thread
                    btnStart.Invoke(new Action(() =>
                    {
                        btnStart.Enabled = false;
                    }));
                    //Estabilish connection and check if it's valid
                    var resultConn = tcpClient.BeginConnect(txtGpsServer.Text, Convert.ToInt16(txtPort.Text), null, null);
                    var resultSuccess = resultConn.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (!resultSuccess)
                    {
                        MessageBox.Show("Failed to connect !!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        btnStart.Invoke(new Action(() =>
                        {
                            btnStart.Enabled = true;
                        }));
                    }
                    else
                    {
                        
                        //Start capture stream from GPS Server
                        StreamReader streamReader = new StreamReader(tcpClient.GetStream());
                        try
                        {
                            //Read line by line and print each one in txtBox like debug
                            var data = streamReader.ReadLine();
                            while (data != null)
                            {
                                txtResult.Invoke(new Action(() =>
                                {
                                    txtResult.Text += data + Environment.NewLine;
                                    txtResult.SelectionStart = txtResult.TextLength;
                                    txtResult.ScrollToCaret();
                                    txtResult.Refresh();
                                }));
                                //Read lines start with "$GPGLL" to keep Lat and Long, then I'll copy them in boxs Lat and Long
                                if (data.StartsWith("$GPGLL"))
                                {
                                    //Format string NMEA Standard to keep Lat and Long, Index 1 for Lat and Index 3 for Long
                                    string LatLong = data.ToString();
                                    string[] arrLatLong = LatLong.Split(",");
                                    //Convert to DDD.MMMMM
                                    string Lat = arrLatLong[1].Substring(0, 2) + "." + Decimal.Truncate(Convert.ToDecimal(arrLatLong[1].Substring(2, 8)) / 60);
                                    string Long = arrLatLong[3].Substring(2, 1) + "." + Decimal.Truncate(Convert.ToDecimal(arrLatLong[3].Substring(3, 8)) / 60);
                                    txtLat.Invoke(new Action(() =>
                                    {
                                        //Read index 1 for keep Lat + 2 for N
                                        txtLat.Text = Lat + arrLatLong[2];
                                    }));
                                    txtLong.Invoke(new Action(() =>
                                    {
                                        //Read index 3 for keep Long + 4 for E
                                        txtLong.Text = Long + arrLatLong[4];
                                    }));

                                }
                                data = streamReader.ReadLine();
                                //Check variable Stop, if true stop Stream and close Tcpclient
                                if (boolStop)
                                {
                                    streamReader.Close();   
                                    tcpClient.Close();
                                    return;
                                }

                            }
                        }
                        catch
                        {
                            btnStart.Invoke(new Action(() =>
                            {
                                btnStart.Enabled = true;
                            }));
                            streamReader.Close();
                            tcpClient.Close();
                            MessageBox.Show("Problem Occured !!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                }
            });
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Call function "ReceiveDataGPS"
            //Manage separately thread for data received otherwise freeze application
            boolStop = false;
            txtResult.Text = "";
            txtLat.Text = "";
            txtLong.Text = "";
            ReceiveDataGPS();

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            boolStop = true;
            btnStart.Enabled = true;
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmInfo frmInfo = new FrmInfo();
            frmInfo.ShowDialog();
        }
    }
}