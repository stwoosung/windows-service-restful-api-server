using RestAPIServer.LibClass;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RestAPIServer.UserDefineClass
{
    public class RestAPIScenarioControl
    {
        private LogControl logControl = new LogControl();
        private RestAPIScenario mRestAPIScenario = null;
        private Thread ThreadRestAPISecenario = null;

        private string ServiceName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public RestAPIScenarioControl()
        {
            mRestAPIScenario = new RestAPIScenario();
        }


        public void Start()
        {
            try
            {
                ThreadRestAPISecenario = new Thread(new ThreadStart(mRestAPIScenario.Run));
                ThreadRestAPISecenario.Priority = ThreadPriority.Normal;
                ThreadRestAPISecenario.Start();
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "ServiceStart", e.Message, LogControl.LogLevel.Error);
            }
        }

        public void Stop()
        {
            try
            {
                mRestAPIScenario.Stop();
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "ServiceStop", e.Message, LogControl.LogLevel.Error);
            }
        }


        public class RestAPIScenario
        {
            private LogControl logControl = new LogControl();
            private string ServiceName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            private JsonHelpClass JsonHelp;

            private bool bExit = false;

            private int Period;
            private DateTime CurTime;
            private DateTime OldTime;

            private string ServerIP;
            private string ServerPort;

            private HttpListener httpListener;

            private enum HttpMethodType
            {
                // POST,
                GET,
                // PUT,
                // DELETE
            }

            public RestAPIScenario()
            {

            }

            public void Init()
            {
                try
                {
                    OldTime = DateTime.Now;
                    JsonHelp = new JsonHelpClass();


                    ServerIP = JsonHelp.FnGetPkgInfo("HOST") ?? "127.0.0.1";
                    ServerPort = JsonHelp.FnGetPkgInfo("PORT") ?? "8888";
                    Period = int.Parse(JsonHelp.FnGetPkgInfo("PERIOD") ?? "1000");
                }
                catch (Exception e)
                {
                    logControl.WriteLog(ServiceName, "Init", e.Message, LogControl.LogLevel.Error);
                }
            }

            public void FnRunRestAPIServer()
            {
                try
                {
                    if (httpListener == null || !httpListener.IsListening)
                    {
                        httpListener = new HttpListener();
                        httpListener.Prefixes.Add(string.Format("http://+:" + ServerPort + "/"));
                        httpListener.Start();

                        string msg = string.Format("Running api server process (http://{0}:{1})", ServerIP, ServerPort);
                        logControl.WriteLog(ServiceName, "fnRunRestAPIServer", msg, LogControl.LogLevel.Info);

                        Task.Factory.StartNew(() =>
                        {
                            while (httpListener != null)
                            {

                                HttpListenerContext context = this.httpListener.GetContext();

                                // enum에 포함된 HTTP 요청만 판단
                                string httpmethod = context.Request.HttpMethod;
                                if (!Enum.IsDefined(typeof(HttpMethodType), httpmethod)) continue;


                                context.Response.Close();
                            }
                        });
                    }
                }
                catch (Exception a)
                {
                    string msg = string.Format("Failed server information (http://{0}:{1})", ServerIP, ServerPort);
                    logControl.WriteLog(ServiceName, "fnRunRestAPIServer", a.Message, LogControl.LogLevel.Error);
                    logControl.WriteLog(ServiceName, "fnRunRestAPIServer", msg, LogControl.LogLevel.Error);
                }
            }


            public void Stop()
            {
                try
                {
                    bExit = true;
                    if (httpListener != null || httpListener.IsListening)
                    {
                        httpListener.Stop();
                    }
                    logControl.WriteLog(ServiceName, "Stop", "Stop Service", LogControl.LogLevel.Info);
                    Environment.Exit(0);
                }
                catch (Exception a)
                {
                    logControl.WriteLog(ServiceName, "Stop", a.Message, LogControl.LogLevel.Error);
                }
            }


            public void Run()
            {
                Init();
                FnRunRestAPIServer();

                while (!bExit)
                {
                    CurTime = DateTime.Now;
                    TimeSpan gap = CurTime - OldTime;

                    if ((int)gap.TotalMilliseconds > Period)
                    {
                        // fnUpdateTagValue();
                        OldTime = CurTime;

                    }
                    Thread.Sleep(10);
                }
            }

        }
    }
}
