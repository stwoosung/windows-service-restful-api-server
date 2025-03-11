using System;
using System.Diagnostics;
using System.IO;

namespace RestAPIServer.LibClass
{
    public class LogControl
    {
        // 디버그 모드 사용 유무
        public static string isDebugMode = string.Empty; 

        /// <summary>
        /// 로그를 콘솔로 출력하는 함수
        /// </summary>
        /// <param name="strComment">로그 내용</param>
        public void WriteConsoleLog(string strComment) 
        {
            Console.WriteLine(strComment);
        }

        /// <summary>
        /// 로그를 파일로 출력하는 함수
        /// </summary>
        /// <param name="strServiceName">서비스명(로그 파일명)</param>
        /// <param name="strTitle">함수명(로그 타이틀)</param>
        /// <param name="strComment">로그 내용</param>
        public void WriteLog(string strServiceName, string strTitle, string strComment)
        {
            if (isDebugMode.Equals("-debug", StringComparison.OrdinalIgnoreCase))
            {
                string strMsg = string.Format("{0} - {1}", strTitle, strComment);
                WriteConsoleLog(strMsg);
            }
            DirectoryInfo topDir = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location);
            string PreDirPath = topDir.Parent.FullName;
            string FolderPath = string.Format("{0}\\{1}\\{2}", "Log", DateTime.Now.ToString("yyyyMM"), DateTime.Now.ToString("dd"));
            string DirPath = Path.Combine(PreDirPath, FolderPath);
            string FilePath = DirPath + string.Format("\\{0}.log", strServiceName);
            string temp;


            DirectoryInfo di = new DirectoryInfo(DirPath);
            FileInfo fi = new FileInfo(FilePath);

            try
            {
                if (!di.Exists) Directory.CreateDirectory(DirPath);
                if (!fi.Exists)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {
                        temp = string.Format("[{0}] {1} : {2}", DateTime.Now, strTitle, strComment);
                        sw.WriteLine(temp);
                        sw.Close();
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {
                        temp = string.Format("[{0}] {1} : {2}", DateTime.Now, strTitle, strComment);
                        sw.WriteLine(temp);
                        sw.Close();
                    }
                }
            }
            catch (Exception a)
            {
                Console.WriteLine(a.Message);
            }
        }
    }
}
