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

        public void fnStartAndStopService(string[] args)
        {
            this.OnStart(args);
        }
        
        protected override void OnStart(string[] args)
        {
            try
            {
                fnStartRestAPIService(args);
            }
            catch (Exception e)
            {
                logControl.WriteLog("RestAPIServer", "OnStart", e.Message);
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
                logControl.WriteLog("RestAPIServer", "OnStart", e.Message);
            }
        }

        public void fnStartRestAPIService(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("-debug", StringComparison.OrdinalIgnoreCase))
            {
                restAPIScenario = new RestAPIScenarioControl(args[0]);
                restAPIScenario.Start();
            }
            else
            {
                restAPIScenario = new RestAPIScenarioControl("release");
                restAPIScenario.Start();
            }

        }

    }
}
