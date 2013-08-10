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

        public delegate void DataReceivedEventHandler(object sender, EventArgs e);
        public event DataReceivedEventHandler DataReceived;

        public delegate void MessageBroadcastHandler(object sender, EventArgs e);
        public event MessageBroadcastHandler MessageBroadcast;

        private void OnDataReceived(EventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived(this, e);
            }
        }

        private void OnMessageBroadcast(EventArgs e)
        {
            if (MessageBroadcast != null)
            {
                MessageBroadcast(this, e);
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
                serialPort1.ErrorReceived += new SerialErrorReceivedEventHandler(SerialErrorReceived);
                // TODO: some external means of detecting when the hardware state changes
            }
            catch (Exception ex)
            {
//                dataString = ex.Message;
                OnMessageBroadcast(new MessageEventArgs(ex.Message));
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
            dataString = serialPort1.ReadLine();

            OnDataReceived(EventArgs.Empty);
        }

        private void SerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            OnMessageBroadcast(new MessageEventArgs(string.Format("{0} {1}", sp.PortName, e.EventType)));
        }

    }

    class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }
}
