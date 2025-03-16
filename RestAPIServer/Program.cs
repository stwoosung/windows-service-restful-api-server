using RestAPIServer.LibClass;
using System;
using System.ServiceProcess;

namespace RestAPIServer
{
    internal static class Program
    {
        static LogControl logControl = new LogControl();

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
                            SelfInstaller.InstallMe();
                        }
                        else
                        {
                            if (args[0].Equals("-u", StringComparison.OrdinalIgnoreCase))
                            {
                                SelfInstaller.UninstallMe();
                            }
                            else if (args[0].Equals("-debug", StringComparison.OrdinalIgnoreCase))
                            {   
                                RestAPIServerService service = new RestAPIServerService();
                                service.FnStartService(args);
                            }
                            else
                            {
                                logControl.WriteConsoleLog("Invalid argument! Not running process!");
                            }
                        }
                    }
                    else
                    {
                        RestAPIServerService service = new RestAPIServerService();
                        service.FnStartService(args);
                    }
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog("RestAPIServer", "Main", e.Message, LogControl.LogLevel.Error);
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
