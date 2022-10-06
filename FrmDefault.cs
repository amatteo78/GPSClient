using System.Net.Sockets;
using System.Text.RegularExpressions;

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

        public static bool IsValidIP(string addr) //Check if IP is Valid
        {
            //Create our Regular Expression object
            Regex check = new("^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");

            //Check to make sure an ip address was provided
            if (addr == "")
            {
                //No address provided so return false
                return false;
            }
            else
            {
                //Address provided so use the IsMatch Method
                //Of the Regular Expression object
                return check.IsMatch(addr, 0);
            }
        }

        private void ReceiveDataGPS()
        {
            Task.Run(() =>
            {
                //Inizialize TcpCLient
                TcpClient tcpClient = new();
                //Check if IP Address and Port are present
                if (string.IsNullOrEmpty(txtGpsServer.Text) || string.IsNullOrEmpty(txtPort.Text))
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
                    IAsyncResult? resultConn = tcpClient.BeginConnect(txtGpsServer.Text, Convert.ToInt16(txtPort.Text), null, null);
                    bool resultSuccess = resultConn.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

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
                        StreamReader streamReader = new(tcpClient.GetStream());
                        try
                        {
                            //Read line by line and print each one in txtBox like debug
                            string? data = streamReader.ReadLine();
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
                                    string Lat;
                                    string Long;

                                    if (string.IsNullOrEmpty(arrLatLong[1]))
                                    {
                                        Lat = "Lat - GPS No Signal";
                                    }
                                    else
                                    {
                                        string BeforeDot = arrLatLong[1][..arrLatLong[1].IndexOf('.')];
                                        string Degree = BeforeDot[..^2];
                                        string Minutes = BeforeDot[^2..] + arrLatLong[1][arrLatLong[1].IndexOf('.')..];
                                        string MinutesCalculated = (Convert.ToDecimal(Minutes) / 60).ToString();
                                        Lat = Degree + "." + MinutesCalculated[(MinutesCalculated.IndexOf('.') + 1)..];
                                    }

                                    if (string.IsNullOrEmpty(arrLatLong[3]))
                                    {
                                        Long = "Long - GPS No Signal";
                                    }
                                    else
                                    {
                                        string BeforeDot = arrLatLong[3][..arrLatLong[3].IndexOf('.')];
                                        string Degree = BeforeDot[..^2];
                                        string Minutes = BeforeDot[^2..] + arrLatLong[3][arrLatLong[3].IndexOf('.')..];
                                        string MinutesCalculated = (Convert.ToDecimal(Minutes) / 60).ToString();
                                        Long = Degree + "." + MinutesCalculated[(MinutesCalculated.IndexOf('.') + 1)..];
                                    }

                                    //If South or West put a minus before the value.
                                    Lat = arrLatLong[2] == "S" ? ("-" + Lat) : Lat;
                                    Long = arrLatLong[4] == "W" ? ("-" + Long) : Long;

                                    //Converting to decimal to setting a format.
                                    Lat = Convert.ToDecimal(Lat).ToString("00.000000");
                                    Long = Convert.ToDecimal(Long).ToString("000.000000");

                                    txtLat.Invoke(new Action(() =>
                                    {
                                        //Read index 1 for keep Lat + 2 for N
                                        txtLat.Text = Lat;
                                    }));
                                    txtLong.Invoke(new Action(() =>
                                    {
                                        //Read index 3 for keep Long + 4 for E
                                        txtLong.Text = Long;
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
                        catch //(Exception ex)
                        {
                            btnStart.Invoke(new Action(() =>
                            {
                                btnStart.Enabled = true;
                            }));
                            streamReader.Close();
                            tcpClient.Close();
                            MessageBox.Show("Problem Occured !!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            //MessageBox.Show(ex.ToString());
                        }
                    }
                }
            });

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Change bool state to Stop stream
            boolStop = true;
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
            //Change bool state to Stop stream
            boolStop = true;
            btnStart.Enabled = true;
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmInfo frmInfo = new();
            frmInfo.ShowDialog();
        }

        private void FrmDefault_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Change bool state to Stop stream
            boolStop = true;
        }
    }
}