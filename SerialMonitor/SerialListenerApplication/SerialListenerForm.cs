using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace SerialListenerApplication
{
    public partial class SerialListenerForm : Form
    {
        static string rxString;
        static string connectionString = "Server=192.168.1.32;Database=garden;User Id=garden_collector;Password=gnome123!@#;";
        static string sqlInsert = "INSERT INTO dbo.SENSORDATA (DEVICE, SENSOR, NUMVAL, OBSERVED, COLLECTED) VALUES (@DEVICE, @SENSOR, @NUMVAL, @OBSERVED, GETDATE())";

        private bool debug = true;

        private ListenerWorker listenerWorker = null;
        private Dictionary<string, Dictionary<string, List<double>>> accumulatedData;
        private Dictionary<Tuple<string, string>, System.Threading.Timer> timers;

        public SerialListenerForm()
        {
            InitializeComponent();
            accumulatedData = new Dictionary<string, Dictionary<string, List<double>>>();
            timers = new Dictionary<Tuple<string, string>, System.Threading.Timer>();

            // enumerate available serial ports
            string[] theSerialPortNames = System.IO.Ports.SerialPort.GetPortNames();

            comboBox1.DataSource = theSerialPortNames;

            debugCheckbox.DataBindings.Add("Checked", this, "DebugChecked", false, DataSourceUpdateMode.OnPropertyChanged);

        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            listenerWorker = new ListenerWorker("GARDEN1", comboBox1.Text, 9600);

            listenerWorker.DataReceived += new ListenerWorker.DataReceivedEventHandler(listenerWorker_LineReceived);
            listenerWorker.MessageBroadcast += new ListenerWorker.MessageBroadcastHandler(listenerWorker_MessageBroadcast);

            listenerWorker.StartListening();



            if (listenerWorker.IsRunning())
            {
          
                buttonStop.Enabled = true;
                buttonStart.Enabled = false;

                comboBox1.Enabled = false;

                textBox1.ReadOnly = false;
            }
        }

        void listenerWorker_MessageBroadcast(object sender, EventArgs e)
        {
            if (e is MessageEventArgs)
            {
                MessageEventArgs me = (MessageEventArgs)e;
                rxString = me.Message;
                BeginInvoke(new EventHandler(UpdateInterface));
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (listenerWorker.IsRunning())
            {
                listenerWorker.StopListening();

                buttonStop.Enabled = false;
                buttonStart.Enabled = true;

                comboBox1.Enabled = true;

                textBox1.ReadOnly = true;
            }
        }

        public delegate void LineReceivedEvent(string line);

        // Process a line of text.  Format is:
        // SIGNATURE    #VALUES VALUE1  VALUE2  etc...
        // DHT22    2   54.3    21.0
        void listenerWorker_LineReceived(object sender, EventArgs e)
        {
            ListenerWorker l = (ListenerWorker)sender;
            string line = l.GetData();
            string deviceName = l.GetSensorName();

            // TODO: extrapolate the name of the sensor, perhaps from the bluetooth name
            accumulate(deviceName, line.Split('\t'));

            rxString = line;
            BeginInvoke(new EventHandler(UpdateInterface));

        }

        private void accumulate(string device, string[] data)
        {
            if (data == null || data.Length < 3)
            {
                // TODO: log this awful data
                return;
            }

            if (data[0].Equals("DHT22"))
            {
                // Fix that comma, yo
                data[2] = data[2].Replace(",", String.Empty);
            }

            if (!accumulatedData.Keys.Contains(device))
            {
                accumulatedData[device] = new Dictionary<string, List<double>>();
            }
            Dictionary<string, List<double>> collector = accumulatedData[device];
            for (int i = 0; i < data.Length - 2; i++)
            {
                string collectorName = string.Format("{0}{1}", data[0], i);
                double collectorValue = Convert.ToDouble(data[i + 2]);

                if (!collector.Keys.Contains(collectorName))
                {
                    collector[collectorName] = new List<double>();
                    Tuple<string, string> tuple = Tuple.Create(device, collectorName);
                    BeginInvoke(new EventHandler(AppendText), new MessageEventArgs(String.Format("Adding {0} {1}\n", device, collectorName)));
                    System.Threading.Timer t = new System.Threading.Timer(commit, tuple, 30000, System.Threading.Timeout.Infinite);
                    timers[tuple] = t;
                }
                collector[collectorName].Add(collectorValue);

            }

        }

        void commit(object sender)
        {
            Tuple<string, string> t = (Tuple<string, string>)sender;
            Dictionary<string, List<double>> collector = accumulatedData[t.Item1];

            dbCommit(t.Item1, t.Item2, collector[t.Item2].Average());
            collector[t.Item2] = new List<double>();
            collector.Remove(t.Item2);
            
            timers[t].Dispose();
            timers[t] = null;
        }

        private void dbCommit(string device, string sensor, double value)
        {
            if (debug)
            {
                BeginInvoke(new EventHandler(AppendText), new MessageEventArgs(String.Format("insert {0} {1} {2}", device, sensor, value)));
                return;
            }

            SqlConnection conn = null;
            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = sqlInsert;

                        cmd.Parameters.Add(new SqlParameter("@DEVICE", device));
                        cmd.Parameters.Add(new SqlParameter("@SENSOR", sensor));
                        cmd.Parameters.Add(new SqlParameter("@NUMVAL", value));
                        cmd.Parameters.Add(new SqlParameter("@OBSERVED", DateTime.Now));

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                BeginInvoke(new EventHandler(AppendText), new MessageEventArgs(e.Message));
            }
            finally
            {
                try
                {
                    if (conn != null)
                    {
                        conn.Close();
                    }
                }
                catch (Exception)
                {
                    // probably not much more to do here
                }
            }
        }

        private void AppendText(object sender, EventArgs e)
        {
            if (debug && sender is MessageEventArgs)
            {
                MessageEventArgs m = (MessageEventArgs)sender;
                textBox1.AppendText(m.Message);
            }
        }

        private void UpdateInterface(object sender, EventArgs e)
        {   
            if (string.IsNullOrWhiteSpace(rxString) )
            {
                return;
            }

            string[] parts = rxString.Split('\t');

            if (parts.Length > 2)
            {
                switch (parts[0])
                {
                    case "LIGHT":
                        // parse light sensor
                        txtLight.Text = parts[2];
                        break;
                    case "SOIL":
                        // soil sensor
                        txtSoil.Text = parts[2];
                        break;
                    case "DHT22":
                        // this thing has two parts
                        txtHumidity.Text = parts[2];
                        txtTemperature.Text = parts[3];
                        break;
                    default:
                        // Did not recognize the signature
                        textBox1.AppendText(rxString + "\n");
                        break;
                }
            }
            else
            {
                textBox1.AppendText("Unrecognized: " + rxString + "\n");
            }
        }



        private void SerialListenerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (listenerWorker.IsRunning()) listenerWorker.StopListening();
        }

        public bool DebugChecked
        {
            get { return debug; }
            set { debug = value; }
        }
    }
}
