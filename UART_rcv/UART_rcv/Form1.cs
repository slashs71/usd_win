using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;

namespace UART_rcv
{
    public partial class frmUSDoppler : Form
    {
        #region Local Variables

        // The main control for communicating through the RS-232 port
        private SerialPort comport = new SerialPort();
        private SerialPort comport_sig = new SerialPort();


        private List<int> buf = new List<int>();
        #endregion

        public frmUSDoppler()
        {
            InitializeComponent();
        }

        private void tmrCheckComPorts_Tick(object sender, EventArgs e)
        {
            // checks to see if COM ports have been added or removed
            // since it is quite common now with USB-to-Serial adapters
            RefreshComPortList();
        }

        private void RefreshComPortList()
        {
            // Determain if the list of com port names has changed since last checked
            string selected = RefreshComPortList(cmbPortName.Items.Cast<string>(), cmbPortName.SelectedItem as string, comport.IsOpen);

            // If there was an update, then update the control showing the user the list of port names
            if (!String.IsNullOrEmpty(selected))
            {
                cmbPortName.Items.Clear();
                cmbPortName.Items.AddRange(OrderedPortNames());
                cmbPortName.SelectedItem = selected;
            }
        }

        private string[] OrderedPortNames()
        {
            // Just a placeholder for a successful parsing of a string to an integer
            int num;

            // Order the serial port names in numberic order (if possible)
            return SerialPort.GetPortNames().OrderBy(a => a.Length > 3 && int.TryParse(a.Substring(3), out num) ? num : 0).ToArray();
        }

        private string RefreshComPortList(IEnumerable<string> PreviousPortNames, string CurrentSelection, bool PortOpen)
        {
            // Create a new return report to populate
            string selected = null;

            // Retrieve the list of ports currently mounted by the operating system (sorted by name)
            string[] ports = SerialPort.GetPortNames();

            // First determain if there was a change (any additions or removals)
            bool updated = PreviousPortNames.Except(ports).Count() > 0 || ports.Except(PreviousPortNames).Count() > 0;

            // If there was a change, then select an appropriate default port
            if (updated)
            {
                // Use the correctly ordered set of port names
                ports = OrderedPortNames();

                // Find newest port if one or more were added
                string newest = SerialPort.GetPortNames().Except(PreviousPortNames).OrderBy(a => a).LastOrDefault();

                // If the port was already open... (see logic notes and reasoning in Notes.txt)
                if (PortOpen)
                {
                    if (ports.Contains(CurrentSelection)) selected = CurrentSelection;
                    else if (!String.IsNullOrEmpty(newest)) selected = newest;
                    else selected = ports.LastOrDefault();
                }
                else
                {
                    if (!String.IsNullOrEmpty(newest)) selected = newest;
                    else if (ports.Contains(CurrentSelection)) selected = CurrentSelection;
                    else selected = ports.LastOrDefault();
                }
            }

            // If there was a change to the port list, return the recommended default selection
            return selected;
        }

        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            bool error = false;

            // If the port is open, close it.
            if (comport.IsOpen)
            {
                try
                {
                    String str = comport.ReadExisting();
                    comport.DiscardInBuffer();                    
                    comport.Close();
                    comport.DiscardInBuffer();

                }
                catch (Exception)
                {
                }
            }
            else
            {
                // Set the port's settings

                comport.DtrEnable = false;
                comport.RtsEnable = false;


                comport.BaudRate = 460800;
                comport.DataBits = (int)8;
                comport.StopBits = StopBits.One;
                comport.Parity = Parity.None;
                comport.PortName = cmbPortName.Text;
                comport.ReadTimeout = 500;
                comport.WriteTimeout = 500;
                //comport.ReceivedBytesThreshold = 2;


                try
                {
                    // Open the port
                    comport.Open();
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port.  Most likely it is already in use, has been removed, or is unavailable.", "COM Port Unavalible", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }

            // Change the state of the form's controls
            //EnableControls();
            if (comport.IsOpen) btnOpenPort.Text = "Закрыть порт";
            else btnOpenPort.Text = "Открыть порт";

            // open_comsig();
        }

        private void frmUSDoppler_Load(object sender, EventArgs e)
        {
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }

        private void open_comsig()
        {
            bool error = false;

            // If the port is open, close it.
            if (comport_sig.IsOpen)
            {
                comport_sig.Close();
            }
            else
            {
                // Set the port's settings

                comport_sig.DtrEnable = false;
                comport.RtsEnable = false;


                comport_sig.BaudRate = 921600;
                comport_sig.DataBits = (int)8;
                comport_sig.StopBits = StopBits.One;
                comport_sig.Parity = Parity.None;
                comport_sig.PortName = "com1";
                comport_sig.ReadTimeout = 500;
                comport_sig.WriteTimeout = 500;
                //comport.ReceivedBytesThreshold = 2;


                try
                {
                    // Open the port
                    comport_sig.Open();
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port SIGNAL GENERATOR.  Most likely it is already in use, has been removed, or is unavailable.", "COM Port Unavalible", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }


        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!comport.IsOpen) return;

            int NN = 512;
            int bytes;
            string str_bytes;

            bytes = comport.BytesToRead;
            while ((bytes > 0) && comport.IsOpen)
            {
                try
                {

                    //str_bytes = comport.ReadExisting();
                    buf.Add(comport.ReadByte());
                    bytes = comport.BytesToRead;
                    //comport.DiscardInBuffer();
                    if (buf.Count >= NN)
                    {
                        if (comport.IsOpen) this.Invoke(new EventHandler(delegate { updateGraph(buf.ToArray()); }));
                        buf.Clear();
                    }
                }
                catch
                {

                }

            }
        }

        private void updateGraph(int[] v)
        {
            
            int N_FFT = 512;
            int[] vv = new int[v.Length];

            alglib.complex[] f1;
            double[] d = new double[v.Length];

            for (int i = 0; i < v.Length; i++)
            {
                if (v[i] > 128)
                {
                    vv[i] = v[i] - 256;
                }
                else
                {
                    vv[i] = v[i];
                }
            }

            //this.Text = buf.Count.ToString();
            this.Text = v.Length.ToString();
            chart1.Series[0].Points.Clear();
            chart1.ChartAreas[0].AxisY.Minimum = -128;
            chart1.ChartAreas[0].AxisY.Maximum = 127;
            foreach (var b in vv)
            {

                chart1.Series[0].Points.AddY(b);
            }

            for (int i = 0; i < vv.Length; i++)
            {
                d[i] = Convert.ToDouble(vv[i]);
            }
            alglib.fftr1d(d, N_FFT, out f1);

            chart2.Series[0].Points.Clear();
            chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(UpDown_Amax.Value);
            for (int i = 0; i < N_FFT / 2; i++)
            {
                chart2.Series[0].Points.AddY(alglib.math.abscomplex(f1[i]));
            }

        }

        private void btnSigGenStart_Click(object sender, EventArgs e)
        {
            byte[] wrbuf = new byte[512];
            int N = wrbuf.Length;
            byte d = 0;

            double Fs = 20000;
            double Ts = 1 / Fs;
            double F = 1000;
            double arg = 0;

            for (int i = 0; i < 511; i++)
            {
                arg = 2 * Math.PI * F * Ts * i;
                d = Convert.ToByte((Math.Round(Math.Sin(arg) * 120) + 120 + alglib.math.randominteger(15)) % 255);

                wrbuf[i] = d;
            }

            if (comport_sig.IsOpen)
            {
                comport_sig.Write(wrbuf, 0, N);
            }
        }

        private void UpDown_Amax_ValueChanged(object sender, EventArgs e)
        {
            chart2.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(UpDown_Amax.Value);
        }
    }
}