using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;


namespace SerialListenerApplication
{
    class ListenerWorker
    {
        private SerialPort serialPort1;
        private string portName;
        private int baudRate;

        private string dataString;
        private string sensorName;

        public delegate void LineReceivedEventHandler(object sender, EventArgs e);
        public event LineReceivedEventHandler LineReceived;

        private void OnLineReceived(EventArgs e)
        {
            if (LineReceived != null)
            {
                LineReceived(this, e);
            }
        }

        public ListenerWorker (string name, string port, int baud)
	    {
            sensorName = name;
            portName = port;
            baudRate = baud;
            serialPort1 = new SerialPort();
	    }

        public bool IsRunning() 
        {
            return serialPort1.IsOpen;
        }

        public string GetData()
        {
            return dataString;
        }

        public string GetSensorName()
        {
            return sensorName;
        }
        
        public bool StartListening()
        {
            serialPort1.PortName = portName;
            serialPort1.BaudRate = baudRate;

            try
            {
                serialPort1.Open();
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);
            }
            catch (Exception ex)
            {
                dataString = ex.Message;
                //BeginInvoke(new EventHandler(ParseText));
            }

            return true;
        }

        public bool StopListening()
        {
            serialPort1.Close();

            return true;
        }

        private void SerialDataReceived
            (object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string line = serialPort1.ReadLine();

            OnLineReceived(EventArgs.Empty);

           // BeginInvoke(new SerialListenerApplication.SerialListenerForm.LineReceivedEvent(LineReceived), line);
        }

    }
}
