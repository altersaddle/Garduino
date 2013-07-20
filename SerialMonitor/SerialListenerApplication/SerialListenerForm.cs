using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class SerialListenerForm : Form
    {
        static string rxString;

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
