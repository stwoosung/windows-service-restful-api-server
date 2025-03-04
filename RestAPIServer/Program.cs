using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RestAPIServer
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                if ((!Environment.UserInteractive))
                {
                    Program.RunAsAService();
                }
                else
                {
                    if (args != null && args.Length > 0)
                    {
                        if (args[0].Equals("-i", StringComparison.OrdinalIgnoreCase))
                        {
                            // SelfInstaller.InstallMe();
                        }
                        else
                        {
                            if (args[0].Equals("-u", StringComparison.OrdinalIgnoreCase))
                            {
                                // SelfInstaller.UninstallMe();
                            }
                            else if (args[0].Equals("-debug", StringComparison.OrdinalIgnoreCase))
                            {
                                RestAPIServerService service = new RestAPIServerService();
                                service.fnStartAndStopService(args);
                            }
                            else
                            {
                                Console.WriteLine("Invalid argument!");
                            }
                        }
                    }
                    else
                    {
                        RestAPIServerService service = new RestAPIServerService();
                        service.fnStartAndStopService(args);
                    }
                }
            }
            catch (Exception a)
            {
                // logControl.LogWrite("S1RestAPIServer", "Main", a.Message);
            }
        }

        static void RunAsAService()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new RestAPIServerService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
