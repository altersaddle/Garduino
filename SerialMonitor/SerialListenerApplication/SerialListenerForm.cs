using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace WindowsFormsApplication1
{
    public partial class SerialListenerForm : Form
    {
        static string rxString;
        static string connectionString = "Server=192.168.1.32;Database=garden;User Id=garden_collector;Password=gnome123!@#;";
        static string sqlInsert = "INSERT INTO dbo.SENSORDATA (DEVICE, SENSOR, NUMVAL, OBSERVED, COLLECTED) VALUES (@DEVICE, @SENSOR, @NUMVAL, @OBSERVED, GETDATE())";

        public SerialListenerForm()
        {
            InitializeComponent();

            // enumerate available serial ports
            string[] theSerialPortNames = System.IO.Ports.SerialPort.GetPortNames();

            comboBox1.DataSource = theSerialPortNames;

        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            serialPort1.PortName = comboBox1.Text;
            
            serialPort1.BaudRate = 9600;

            try
            {
                serialPort1.Open();
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);
            }
            catch (Exception ex)
            {
                rxString = ex.Message;
                BeginInvoke(new EventHandler(ParseText));
            }

            if (serialPort1.IsOpen)
            {
          
                buttonStop.Enabled = true;
                buttonStart.Enabled = false;

                comboBox1.Enabled = false;

                textBox1.ReadOnly = false;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();

                buttonStop.Enabled = false;
                buttonStart.Enabled = true;

                comboBox1.Enabled = true;

                textBox1.ReadOnly = true;
            }
        }

        private delegate void LineReceivedEvent(string line);
        private void LineReceived(string line)
        {
            // Process a line of text.  Format is:
            // SIGNATURE    #VALUES VALUE1  VALUE2  etc...
            // DHT22    2   54.3    21.0
            string[] parts = line.Split('\t');

            // TODO: extrapolate the name of the sensor, perhaps from the bluetooth name
            dbCommit("GARDEN1", parts);

            if (parts.Length > 2) {
                switch (parts[0]) {
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
                        textBox1.AppendText(line);
                        break;
                }
            }

        }

        private void dbCommit(string sensor, string[] data)
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

            SqlConnection conn = null;
            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    for (int i = 0; i < data.Length - 2; i++)
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = sqlInsert;

                            cmd.Parameters.Add(new SqlParameter("@DEVICE", sensor));
                            cmd.Parameters.Add(new SqlParameter("@SENSOR", string.Format("{0}_1}", data[0], i)));
                            cmd.Parameters.Add(new SqlParameter("@NUMVAL", Convert.ToDouble(data[i + 2])));
                            cmd.Parameters.Add(new SqlParameter("@OBSERVED", DateTime.Now));

                            cmd.ExecuteNonQuery();
                        }
                    }

                }
            }
            catch (Exception e)
            {
                rxString = e.Message;
                BeginInvoke(new EventHandler(ParseText));
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

        private void ParseText(object sender, EventArgs e)
        {
            
            textBox1.AppendText(rxString);
        }

        private void SerialDataReceived
          (object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string line = serialPort1.ReadLine();
            BeginInvoke(new LineReceivedEvent(LineReceived), line);
        }


        private void SerialListenerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen) serialPort1.Close();
        }

    }
}
