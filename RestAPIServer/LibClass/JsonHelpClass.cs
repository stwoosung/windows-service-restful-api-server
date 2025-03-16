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

        public string FnGetPkgInfo(string key)
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

                    if (jsonObject.TryGetValue("REST_API", out JToken restApiToken) && restApiToken.Type == JTokenType.Object)
                    {
                        if (restApiToken[key] != null)
                        {
                            info = restApiToken[key].ToString(); 
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
