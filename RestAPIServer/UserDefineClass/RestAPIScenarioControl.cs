using Newtonsoft.Json;
using RestAPIServer.LibClass;
using RestAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            private string ServiceName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            private LogControl logControl = new LogControl();
            private JsonHelpClass JsonHelp = new JsonHelpClass();
            private OleDBService dbService = new OleDBService();

            private bool isExit = false;

            private int Period;
            private DateTime CurTime;
            private DateTime OldTime;

            private string ServerIP;
            private string ServerPort;
            private string DBConnectionString = string.Empty;
            private DataTable dt = new DataTable();

            private HttpListener httpListener;

            private List<DeviceInfo> DeviceList = new List<DeviceInfo>();

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

            private void Init()
            {
                try
                {
                    OldTime = DateTime.Now;

                    ServerIP = JsonHelp.FnGetEnvironmentInfo("REST_API", "HOST") ?? "127.0.0.1";
                    ServerPort = JsonHelp.FnGetEnvironmentInfo("REST_API", "PORT") ?? "8888";
                    Period = int.Parse(JsonHelp.FnGetEnvironmentInfo("REST_API", "PERIOD") ?? "1000");

                    DBConnectionString = string.Format("Provider=SQLOLEDB.1;Data Source={0},{1};Initial Catalog={2};User ID={3};Password={4};Persist Security Info=True;",
                        JsonHelp.FnGetEnvironmentInfo("DATABASE", "HOST") ?? "127.0.0.1",
                        JsonHelp.FnGetEnvironmentInfo("DATABASE", "PORT") ?? "1433",
                        JsonHelp.FnGetEnvironmentInfo("DATABASE", "DATABASE_NAME") ?? "DUMP",
                        JsonHelp.FnGetEnvironmentInfo("DATABASE", "USER_ID") ?? "sa",
                        JsonHelp.FnGetEnvironmentInfo("DATABASE", "USER_PW") ?? "1q2w3e4r!");
                }
                catch (Exception e)
                {
                    logControl.WriteLog(ServiceName, "Init", e.Message, LogControl.LogLevel.Error);
                }
            }


            public void Stop()
            {
                try
                {
                    isExit = true;
                    dbService.CloseDB();
                    FnStopRestAPIServer();
                    logControl.WriteLog(ServiceName, "Stop", "Stop Service", LogControl.LogLevel.Info);

                    // Environment.Exit(0);
                }
                catch (Exception a)
                {
                    logControl.WriteLog(ServiceName, "Stop", a.Message, LogControl.LogLevel.Error);
                }
            }


            public void Run()
            {
                Init();
                FnSetServerData();
                FnRunRestAPIServer();


                // 시험용
                isExit = false;

                while (!isExit)
                {
                    CurTime = DateTime.Now;
                    TimeSpan gap = CurTime - OldTime;

                    if ((int) gap.TotalMilliseconds > Period)
                    {
                        // FnUpdateDatabaseValue();
                        OldTime = CurTime;
                    }
                    Thread.Sleep(10);
                }
            }

            private void FnSetServerData()
            {
                try
                {
                    string refPath = JsonHelp.FnGetEnvironmentInfo("REFERENCE", "PATH") ?? "../Ref";
                    string deviceFilePath = Path.Combine(refPath, "DeviceList.cfg");
                    string deviceFileJson = JsonHelp.FnReadJson(deviceFilePath);

                    DeviceList = JsonConvert.DeserializeObject<List<DeviceInfo>>(deviceFileJson);
                    if (DeviceList == null || DeviceList.Count == 0) throw new Exception("DeviceList.cfg File not found");

                    foreach (DeviceInfo device in DeviceList)
                    {
                        string tagFilePath = Path.Combine(refPath, "Tags", string.Format("{0}.txt", device.DeviceName));
                        string tagFileJson = JsonHelp.FnReadJson(tagFilePath);
                        device.Tags = JsonConvert.DeserializeObject<List<TagInfo>>(tagFileJson);
                    }
                }
                catch (Exception a)
                {
                    isExit = true;
                    logControl.WriteLog(ServiceName, "FnSetServerData", a.Message, LogControl.LogLevel.Error);
                    logControl.WriteLog(ServiceName, "FnSetServerData", "Update thread stop", LogControl.LogLevel.Error);
                }
            }

            private void FnRunRestAPIServer()
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

                                // enum에 정의된 HTTP 요청만 허용
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

            private void FnStopRestAPIServer()
            {
                try
                {
                    if (httpListener != null || httpListener.IsListening)
                    {
                        httpListener.Stop();
                    }
                    httpListener = null;
                }
                catch (Exception a)
                {
                    logControl.WriteLog(ServiceName, "fnStopRestAPIServer", a.Message, LogControl.LogLevel.Error);
                }
            }


            private void FnUpdateDatabaseValue()
            {
                try
                {
                    bool ConnState = dbService.ConnectDB(DBConnectionString);
                    if (!ConnState) throw new Exception("Database connection failed");


                    DeviceList.SelectMany(device => device.Tags).ToList().ForEach(tag => tag.TagIsAlarm = false);

                    string strQuery = string.Format("SELECT * FROM TAG");
                    int queryResult = dbService.FnExecuteSelectQuery(strQuery, ref dt);

                    Debug.WriteLine(queryResult);
                }
                catch (Exception a)
                {
                    logControl.WriteLog(ServiceName, "FnUpdateDatabaseValue", a.Message, LogControl.LogLevel.Error);
                }
            }
        }
    }
}
