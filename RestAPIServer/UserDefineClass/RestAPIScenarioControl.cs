using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RestAPIServer.UserDefineClass
{
    public class RestAPIScenarioControl
    {
        public RestAPIScenario mRestAPIScenario = null;
        Thread ThreadRestAPISecenario = null;

        string ServiceName = System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public RestAPIScenarioControl(string arg)
        {
            mRestAPIScenario = new RestAPIScenario(arg);
        }


        public void Start()
        {
            try
            {
                ThreadRestAPISecenario = new Thread(new ThreadStart(mRestAPIScenario.Run));
                ThreadRestAPISecenario.Priority = ThreadPriority.Normal;
                ThreadRestAPISecenario.Start();
            }
            catch (Exception a)
            {
                // logControl.LogWrite(ServiceName, "ServiceStart", a.Message);
            }
        }

        public void Stop()
        {
            try
            {
                mRestAPIScenario.Stop();
            }
            catch (Exception a)
            {
                // logControl.LogWrite(ServiceName, "ServiceStop", a.Message);
            }
        }


        public class RestAPIScenario
        {
            public string strArg = string.Empty;
            private bool bExit = false;

            public int Period;
            public DateTime CurTime;
            public DateTime OldTime;

            public string ServerIP = "127.0.0.1";
            public string ServerPort = "8888";

            public HttpListener httpListener;

            enum HttpMethodType
            {
                // POST,
                GET,
                // PUT,
                // DELETE
            }

            public RestAPIScenario(string arg)
            {
                strArg = arg;
            }

            public void Init()
            {
                try
                {
                    OldTime = DateTime.Now;
                }
                catch (Exception a)
                {

                }
            }

            public void fnRunRestAPIServer()
            {
                if (httpListener == null || !httpListener.IsListening)
                {
                    httpListener = new HttpListener();
                    httpListener.Prefixes.Add(string.Format("http://+:" + ServerPort + "/"));
                    httpListener.Start();

                    string msg = string.Format("http://{0}:{1} 구동 완료", ServerIP, ServerPort);
                    // logControl.LogWrite(ServiceName, "fnRunRestAPIServer", msg);

                    Task.Factory.StartNew(() =>
                    {
                        while (httpListener != null)
                        {

                            HttpListenerContext context = this.httpListener.GetContext();

                            // GET 요청만 판단
                            string httpmethod = context.Request.HttpMethod;
                            if (!Enum.IsDefined(typeof(HttpMethodType), httpmethod)) continue;




                            context.Response.Close();
                        }
                    });
                }
                try
                {
                    
                }
                catch (Exception a)
                {
                    Debug.WriteLine(a.StackTrace);
                    /*if (strArg.Equals("-debug", StringComparison.OrdinalIgnoreCase))
                    {
                        string strMsg = string.Format("{0} - {1}", "fnRunRestAPIServer", a.Message);

                        // logControl.ConsoleLogWrite(strMsg);
                    }
                    */
                    // logControl.LogWrite(ServiceName, "fnRunRestAPIServer", a.Message);
                    // logControl.LogWrite(ServiceName, "fnRunRestAPIServer", ServerIP);
                    // logControl.LogWrite(ServiceName, "fnRunRestAPIServer", ServerPort);
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
                    // logControl.LogWrite(ServiceName, "Stop", "서비스 종료");
                    Environment.Exit(0);
                }
                catch (Exception a)
                {
                    if (strArg.Equals("-debug", StringComparison.OrdinalIgnoreCase))
                    {
                        string strMsg = string.Format("{0} - {1}", "Stop", a.Message);
                        // logControl.ConsoleLogWrite(strMsg);
                    }
                    // logControl.LogWrite(ServiceName, "Stop", a.Message);
                }
            }


            public void Run()
            {
                fnRunRestAPIServer();

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
