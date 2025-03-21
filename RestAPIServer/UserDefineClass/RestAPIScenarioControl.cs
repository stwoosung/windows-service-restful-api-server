using Newtonsoft.Json;
using RestAPIServer.LibClass;
using RestAPIServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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

                while (!isExit)
                {
                    CurTime = DateTime.Now;
                    TimeSpan gap = CurTime - OldTime;

                    if ((int)gap.TotalMilliseconds > Period)
                    {
                        // FnUpdateDatabaseValue();
                        OldTime = CurTime;
                    }
                    Thread.Sleep(10);
                }
            }

            private void FnSetServerData()
            {
                try {
                    string refPath = JsonHelp.FnGetEnvironmentInfo("REFERENCE", "PATH") ?? "../Ref";
                    string deviceFilePath = Path.Combine(refPath, "DeviceList.cfg");
                    string deviceFileJson = JsonHelp.FnReadJson(deviceFilePath);

                    DeviceList = JsonConvert.DeserializeObject<List<DeviceInfo>>(deviceFileJson);
                    if (DeviceList.Count == 0) throw new Exception("DeviceList.cfg File not found");

                    foreach (DeviceInfo device in DeviceList)
                    {
                        string tagFilePath = Path.Combine(refPath, "Tags", string.Format("{0}.txt", device.DeviceName));
                        string tagFileJson = JsonHelp.FnReadJson(tagFilePath);
                        device.Tags = JsonConvert.DeserializeObject<List<TagInfo>>(tagFileJson);
                    }
                }
                catch (Exception a)
                {
                    // isExit = true;
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

                                // HTTP GET 요청 시 데이터 가공
                                string rawurl = context.Request.RawUrl;
                                Dictionary<string, object> data = null;


                                // 전체 디바이스 리스트 조회
                                if (rawurl.StartsWith("/api/deviceList"))
                                {
                                    data = new Dictionary<string, object>
                                    {
                                        { "deviceList", FnAPIGetDeviceList() }
                                    };
                                }
                                // 특정 디바이스의 하위 태그리스트 조회
                                else if (rawurl.StartsWith("/api/device/"))
                                {
                                    string encodedParam = rawurl.Substring("/api/device/".Length);
                                    string deviceName = WebUtility.UrlDecode(encodedParam);

                                    data = new Dictionary<string, object> {
                                        { deviceName, FnAPIGetTagList(deviceName) }
                                    };
                                }
                                // 특정 태그 조회
                                else if (rawurl.StartsWith("/api/tag/"))
                                {
                                    string encodedParam = rawurl.Substring("/api/tag/".Length);
                                    string tagName = WebUtility.UrlDecode(encodedParam);

                                    data = new Dictionary<string, object> {
                                        { tagName, FnAPIGetTag(tagName) }
                                    };
                                }
                                // 알람 상태인 태그만 조회
                                else if (rawurl.StartsWith("/api/alarm"))
                                {
                                    data = new Dictionary<string, object> {
                                        { "tagList", FnAPIGetAlarmTagList() }
                                    };
                                }

                                string jsonResponse = JsonConvert.SerializeObject(data);
                                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);

                                context.Response.ContentLength64 = buffer.Length;
                                context.Response.ContentType = "Application/json";
                                context.Response.StatusCode = (int) HttpStatusCode.OK;

                                using (var output = context.Response.OutputStream)
                                {
                                    output.Write(buffer, 0, buffer.Length);
                                }

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
                    bool connState = dbService.ConnectDB(DBConnectionString);
                    if (!connState) throw new Exception("Database connection failed");


                    DeviceList.SelectMany(device => device.Tags).ToList().ForEach(tag => tag.TagIsAlarm = 0);

                    string strQuery = string.Format("SELECT * FROM TAG");
                    // int queryResult = dbService.FnExecuteSelectQuery(strQuery, ref dt);
                    if (dbService.FnExecuteSelectQuery(strQuery, ref dt) == 1)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            var (tagDevice, tagName) = (row["TagDevice"].ToString(), row["TagName"].ToString());

                            var device = DeviceList.FirstOrDefault(d => tagDevice.Equals(d.DeviceName, StringComparison.OrdinalIgnoreCase));
                            if (device != null)
                            {
                                var tag = device.Tags.FirstOrDefault(t => tagName.Equals(t.TagName, StringComparison.OrdinalIgnoreCase));
                                if (tag != null)
                                {
                                    tag.TagIsAlarm = Convert.ToInt32(row["TagIsAlarm"]);
                                    if (tag.TagType.Equals("Analog", StringComparison.OrdinalIgnoreCase))
                                    {
                                        tag.TagValueAnalog = Convert.ToSingle(row["TagValueAnalog"]);
                                    } 
                                    else if (tag.TagType.Equals("Digital", StringComparison.OrdinalIgnoreCase))
                                    {
                                        tag.TagValueDigital = Convert.ToInt32(row["TagValueDigital"]);
                                    }
                                    else if (tag.TagType.Equals("String", StringComparison.OrdinalIgnoreCase))
                                    {
                                        tag.TagValueString = row["TagValueString"].ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logControl.WriteLog(ServiceName, "FnUpdateDatabaseValue", e.StackTrace, LogControl.LogLevel.Error);
                }   
            }
        
            private List<object> FnAPIGetDeviceList()
            {
                var deviceList = new List<object>();
                foreach(DeviceInfo device in DeviceList)
                {
                    Dictionary<string, object> deviceData = new Dictionary<string, object>();
                    deviceData.Add("name", device.DeviceName);
                    deviceData.Add("description", device.DeviceDescription);
                    deviceData.Add("protocol", device.DeviceProtocol);
                    deviceData.Add("content1", device.DeviceCommContent1);
                    deviceData.Add("content2", device.DeviceCommContent2);
                    deviceData.Add("content3", device.DeviceCommContent3);
                    deviceList.Add(deviceData);
                }
                return deviceList;
            }

            private List<object> FnAPIGetTagList(string sDevice)
            {
                foreach (DeviceInfo device in DeviceList)
                {
                    if (device.DeviceName.Equals(sDevice, StringComparison.OrdinalIgnoreCase))
                    {
                        List<object> tagList = new List<object>();
                        foreach (TagInfo tag in device.Tags)
                        {
                            Dictionary<string, object> tagData = new Dictionary<string, object>();
                            tagData.Add("name", tag.TagName);
                            tagData.Add("description", tag.TagDescription);
                            tagData.Add("type", tag.TagType);
                            if (tag.TagType.Equals("Analog", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueAnalog);
                            }
                            else if (tag.TagType.Equals("Digital", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueDigital);
                            }
                            else if (tag.TagType.Equals("String", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueString);
                            }
                            tagData.Add("isAlarm", tag.TagIsAlarm);
                            tagList.Add(tagData);
                        }
                        return tagList;
                    }
                }
                return null;
            }

            private Dictionary<string, object> FnAPIGetTag(string sTag)
            {
                foreach (DeviceInfo device in DeviceList)
                {
                    foreach (TagInfo tag in device.Tags)
                    {
                        if (tag.TagName.Equals(sTag, StringComparison.OrdinalIgnoreCase))
                        {
                            Dictionary<string, object> tagData = new Dictionary<string, object>();
                            tagData.Add("name", tag.TagName);
                            tagData.Add("description", tag.TagDescription);
                            tagData.Add("type", tag.TagType);
                            if (tag.TagType.Equals("Analog", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueAnalog);
                            }
                            else if (tag.TagType.Equals("Digital", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueDigital);
                            }
                            else if (tag.TagType.Equals("String", StringComparison.OrdinalIgnoreCase))
                            {
                                tagData.Add("value", tag.TagValueString);
                            }
                            tagData.Add("isAlarm", tag.TagIsAlarm);
                            return tagData;
                        }
                    }
                }
                return null;
            }
        
            private List<object> FnAPIGetAlarmTagList()
            {
                List<object> tagList = new List<object>();
                foreach (DeviceInfo device in DeviceList)
                {
                    foreach (TagInfo tag in device.Tags)
                    {
                        if (tag.TagIsAlarm == 0) continue;
                        Dictionary<string, object> tagData = new Dictionary<string, object>();
                        tagData.Add("deviceName", device.DeviceName);
                        tagData.Add("deviceDescription", device.DeviceDescription);
                        tagData.Add("tagName", tag.TagName);
                        tagData.Add("tagDescription", tag.TagDescription);
                        tagData.Add("type", tag.TagType);
                        if (tag.TagType.Equals("Analog", StringComparison.OrdinalIgnoreCase))
                        {
                            tagData.Add("value", tag.TagValueAnalog);
                        }
                        else if (tag.TagType.Equals("Digital", StringComparison.OrdinalIgnoreCase))
                        {
                            tagData.Add("value", tag.TagValueDigital);
                        }
                        else if (tag.TagType.Equals("String", StringComparison.OrdinalIgnoreCase))
                        {
                            tagData.Add("value", tag.TagValueString);
                        }
                        tagData.Add("isAlarm", tag.TagIsAlarm);
                        tagList.Add(tagData);
                    }
                }
                return tagList;
            }
        }
    }
}
