using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace RestAPIServer.LibClass
{
    internal class JsonHelpClass
    {
        private LogControl logControl = new LogControl();
        private string ServiceName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);


        public string FnReadJson(string path)
        {
            string jsonData = string.Empty;
            try
            {
                using (StreamReader jr = new StreamReader(path))
                {
                    jsonData = jr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "fnRunRestAPIServer", e.Message, LogControl.LogLevel.Error);
            }
            return jsonData;
        }

        /// <summary>
        /// Env 설정값 읽어오기
        /// </summary>
        /// <param name="pKey">상위(부모) Key</param>
        /// <param name="cKey">하위(자식) Key</param>
        /// <returns>
        /// Setting Value
        /// </returns>
        public string FnGetEnvironmentInfo(string pKey, string cKey)
        {
            DirectoryInfo topDir = Directory.GetParent(Assembly.GetEntryAssembly().Location);
            string PreDirPath = topDir.Parent.FullName;
            string FilePath = Path.Combine(PreDirPath, "process.env.production");

            string info = null;

            try
            {
                using (StreamReader jr = new StreamReader(FilePath))
                {
                    string jsonData = jr.ReadToEnd();
                    JObject jsonObject = JObject.Parse(jsonData);

                    if (jsonObject.TryGetValue(pKey, out JToken restApiToken) && restApiToken.Type == JTokenType.Object)
                    {
                        if (restApiToken[cKey] != null)
                        {
                            info = restApiToken[cKey].ToString(); 
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "fnRunRestAPIServer", e.Message, LogControl.LogLevel.Error);
            }

            return info;
        }
    }
}
