using RestAPIServer.LibClass;
using RestAPIServer.UserDefineClass;
using System;
using System.ServiceProcess;

namespace RestAPIServer
{
    public partial class RestAPIServerService : ServiceBase
    {

        LogControl logControl = new LogControl();
        public RestAPIScenarioControl restAPIScenario;


        public RestAPIServerService()
        {
            InitializeComponent();
        }

        public void FnStartService(string[] args)
        {
            this.OnStart(args);
        }
        
        protected override void OnStart(string[] args)
        {
            try
            {
                if (args.Length != 0) LogControl.isDebugMode = args[0];

                restAPIScenario = new RestAPIScenarioControl();
                restAPIScenario.Start();
            }
            catch (Exception e)
            {
                logControl.WriteLog("RestAPIServer", "OnStart", e.Message, LogControl.LogLevel.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                restAPIScenario.Stop();
                restAPIScenario = null;
            }
            catch (Exception e)
            {
                logControl.WriteLog("RestAPIServer", "OnStop", e.Message, LogControl.LogLevel.Error);
            }
        }


    }
}
