using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace L1
{
    public partial class Form1 : Form
    {
        RobotData Rdata = new RobotData();
        RobotMessage Rmess = new RobotMessage();
        public Form1()
        {
            InitializeComponent();
        }

        Bitmap bitmap;
        private void Form1_Load(object sender, EventArgs e)
        {
            bitmap = new Bitmap(pictureBox_map.Size.Width, pictureBox_map.Size.Height);
        }
        private void ShowUDPMessageMethod(string message)
        {
            PrintLog("Remote >" + message);
        }
        private void PrintLog(string s)
        {
            const int CMaxVisibleLogLines = 20;
            ReportListBox.Items.Add(s);
            while (ReportListBox.Items.Count > CMaxVisibleLogLines)
            {
                ReportListBox.Items.RemoveAt(0);
            }
            ReportListBox.SelectedIndex = ReportListBox.Items.Count - 1;
            ReportListBox.SelectedIndex = -1;
        }
        private void CheckStartStopUDPClient()
        {
            if (udpClient != null)
            {
                StartStopUDPClientButton.Text = "Stop";
                RemoteIPTextBox.Enabled = false;
                RemoteIPTextBox.BackColor = Color.LightGray;
                RemotePortTextBox.Enabled = false;
                RemotePortTextBox.BackColor = Color.LightGray;
                LocalIPTextBox.Enabled = false;
                LocalIPTextBox.BackColor = Color.LightGray;
                LocalPortTextBox.Enabled = false;
                LocalPortTextBox.BackColor = Color.LightGray;
            }
            else
            {
                StartStopUDPClientButton.Text = "Start";
                RemoteIPTextBox.Enabled = true;
                RemoteIPTextBox.BackColor = Color.White;
                RemotePortTextBox.Enabled = true;
                RemotePortTextBox.BackColor = Color.White;
                LocalIPTextBox.Enabled = true;
                LocalIPTextBox.BackColor = Color.White;
                LocalPortTextBox.Enabled = true;
                LocalPortTextBox.BackColor = Color.White;
            }
        }

        UdpClient udpClient;
        Thread thread;
        int localPort;
        private void StartUDPClient()
        {
            if (thread != null)
            {
                thread.Abort();
            }
            if (udpClient != null)
            {
                udpClient.Close();
            }

            localPort = Int32.Parse(LocalPortTextBox.Text);
            try
            {
                udpClient = new UdpClient(localPort);
                thread = new Thread(new ThreadStart(ReceiveUDPMessage));
                thread.IsBackground = true;
                thread.Start();
                PrintLog("UDPClient started");
            }
            catch
            {
                PrintLog("UDPClient's start failed");
            }
            CheckStartStopUDPClient();
        }
        private void StopUDPClient()
        {
            if ((thread != null) && (udpClient != null))
            {
                thread.Abort();
                udpClient.Close();
                thread = null;
                udpClient = null;
            }
            PrintLog("UDPClient stopped");
            CheckStartStopUDPClient();
        }
        private void ReceiveUDPMessage()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] content = udpClient.Receive(ref remoteIPEndPoint);
                    if (content.Length > 0)
                    {
                        string message = Encoding.ASCII.GetString(content);
                        this.Invoke(new MethodInvoker(() =>
                        {
                            try
                            {
                                Rdata = JsonSerializer.Deserialize<RobotData>(message);
                                PrintLog(message);
                                if (checkBox_AI.Checked)
                                {
                                    Trigger();
                                }

                                map();
                            }
                            catch
                            {
                                PrintLog("Incorrect message");
                            }
                        }));
                    }
                }
                catch
                {
                    string errmessage = "RemoteHost lost";
                    this.Invoke(new MethodInvoker(() =>
                    {
                        PrintLog(errmessage);
                    }));
                }
            }
        }
        private void SendUDPMessage()
        {
            if (udpClient != null)
            {
                Int32 port = Int32.Parse(RemotePortTextBox.Text);
                IPAddress ip = IPAddress.Parse(RemoteIPTextBox.Text.Trim());
                IPEndPoint ipEndPoint = new IPEndPoint(ip, port);
                string text = JsonSerializer.Serialize<RobotMessage>(Rmess);
                text += "\n";
                byte[] content = Encoding.ASCII.GetBytes(text);
                try
                {
                    int count = udpClient.Send(content, content.Length, ipEndPoint);
                    if (count > 0)
                    {
                        PrintLog("Sent message:" + text);
                    }
                }
                catch
                {
                    PrintLog("Error occurs.");
                }

            }
        }
        private void SendUDPMessageButton_Click(object sender, EventArgs e)
        {
            SendUDPMessage();
            if (checkBox_N.Checked)
            {
                Rmess.N++;
                UpD();
            }
        }

        private void UDPRegularSenderTimer_Tick(object sender, EventArgs e)
        {
            SendUDPMessage();
        }

        private void pictureBox_control_MouseDown(object sender, MouseEventArgs e)
        {
            Bitmap control = new Bitmap(120, 120);
            Graphics g = Graphics.FromImage(control);
            Rectangle[] rectangles =
            {
                new Rectangle(-1,70,51,50),
                new Rectangle(70,70,50,50),
                new Rectangle(-1,-1,51,51),
                new Rectangle(70,-1,50,51),
            };
            g.DrawRectangles(new Pen(Color.Black, 1), rectangles);
            g.DrawLine(new Pen(Color.Red, 1), e.X, e.Y + 5, e.X, e.Y - 5);
            g.DrawLine(new Pen(Color.Red, 1), e.X + 5, e.Y, e.X - 5, e.Y);
            pictureBox_control.Image = control;
            if (e.X < 50)
            {
                Rmess.B = 50 - e.X;
            }
            else if (e.X > 70)
            {
                Rmess.B = 70 - e.X;
            }
            else { Rmess.B = 0; }
            if (e.Y < 50)
            {
                Rmess.F = 50 - e.Y;
            }
            else if (e.Y > 70)
            {
                Rmess.F = 70 - e.Y;
            }
            else { Rmess.F = 0; }
            Rmess.B = Rmess.B * 2;
            Rmess.F = Rmess.F * 2;
            Rmess.N++;
            UpD();
            SendUDPMessage();
        }

        private void StartStopUDPClientButton_Click(object sender, EventArgs e)
        {
            if (udpClient == null)
            {
                StartUDPClient();
            }
            else
            {
                StopUDPClient();
            }
        }
        public void UpD()
        {
            up_B.Value = Rmess.B;
            up_F.Value = Rmess.F;
            up_M.Value = Rmess.M;
            up_T.Value = Rmess.T;
            up_N.Value = Rmess.N;
        }
        int mode = 0;
        public void Trigger()
        {
            switch (mode)
            {
                case 0:
                    Rmess.B = -50;
                    Rmess.N++;
                    SendUDPMessage();
                    mode = 1;
                    break;
                case 1:
                    if (Convert.ToInt16(Rdata.d2) < 25)
                    {
                        Rmess.B = 0;
                        Rmess.N++;
                        SendUDPMessage();
                        mode = 2;
                    }
                    break;
                case 2:
                    if (Convert.ToInt16(Rdata.d0) > 10)
                    {
                        if (Convert.ToInt16(Rdata.d2) < 20)
                        {
                            Rmess.B = 20;
                            if (Convert.ToInt16(Rdata.d1) < 40)
                            {
                                Rmess.F = 0;
                            }
                            else
                            {
                                Rmess.F = 100;
                            }
                            Rmess.N++;
                            SendUDPMessage();
                        }
                        else if (Convert.ToInt16(Rdata.d2) > 30)
                        {
                            Rmess.B = -20;
                            if (Convert.ToInt16(Rdata.d1) > 60)
                            {
                                Rmess.F = 0;
                            }
                            else
                            {
                                Rmess.F = 100;
                            }
                            Rmess.N++;
                            SendUDPMessage();
                        }
                        else
                        {
                            Rmess.B = 0;
                            Rmess.F = 100;
                            Rmess.N++;
                            SendUDPMessage();
                        }
                    }
                    else
                    {
                        Rmess.B = -30;
                        Rmess.F = 0;
                        Rmess.N++;
                        SendUDPMessage();
                    }
                    break;
            }

        }

        void map()
        {
            int mnozetel = 20;
            Graphics g = Graphics.FromImage(bitmap);
            double dx = Convert.ToDouble(Rdata.x, NumberFormatInfo.InvariantInfo);
            double dy = Convert.ToDouble(Rdata.y, NumberFormatInfo.InvariantInfo);
            int x = Convert.ToInt32(dx * mnozetel);
            int y = Convert.ToInt32(dy * mnozetel);
            g.FillRectangle(Brushes.Red, x, pictureBox_map.Size.Height - y, 1, 1);
            label_X.Text = x.ToString();
            label_Y.Text = y.ToString();
            pictureBox_map.Image = bitmap;
        }

        private void checkBox_AI_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_AI.Checked)
            {
                Trigger();
            }
            //    else
            //    {

            //    }
        }


        private void up_N_ValueChanged(object sender, EventArgs e)
        {
            Rmess.N = (int)up_N.Value;
        }

        private void up_M_ValueChanged(object sender, EventArgs e)
        {
            Rmess.M = (int)up_M.Value;
        }

        private void up_F_ValueChanged(object sender, EventArgs e)
        {
            Rmess.F = (int)up_F.Value;
        }

        private void up_B_ValueChanged(object sender, EventArgs e)
        {
            Rmess.B = (int)up_B.Value;
        }

        private void up_T_ValueChanged(object sender, EventArgs e)
        {
            Rmess.T = (int)up_T.Value;
        }

    }
}
