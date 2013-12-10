using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SerialService
{
    class SerialService : ServiceBase
    {
        static void Main()
        {
            System.ServiceProcess.ServiceBase[] servicesToRun; 
            servicesToRun = new
                System.ServiceProcess.ServiceBase[] { new SerialService() };
            System.ServiceProcess.ServiceBase.Run(servicesToRun);
        }
    }
}
