using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestAPIServer
{
    public partial class RestAPIServerService : ServiceBase
    {
            Thread workerThread = new Thread(DoWork);
        private static bool keepRunning = true;


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
                // logControl.LogWrite("S1RestAPIServer", "OnStart", e.Message);
            }
        }

        protected override void OnStop()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string logFilePath = Path.Combine(exePath, "log.log");

            // "Start" 메시지 로그 파일에 기록
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true)) // true로 설정하면 기존 내용 뒤에 추가
                {
                    writer.WriteLine("STOP");
                }

                Console.WriteLine("Log written to log.log");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }

            workerThread.Abort();
            Debug.WriteLine("Sss");
        }

        public void fnStartRestAPIService(string[] args)
        {
            Debug.WriteLine("sdsd");


            workerThread.Start();


        }

        static void DoWork()
        {
            while (keepRunning)
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string logFilePath = Path.Combine(exePath, "log.log");

                // "Start" 메시지 로그 파일에 기록
                try
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true)) // true로 설정하면 기존 내용 뒤에 추가
                    {
                        writer.WriteLine("Start");
                    }

                    Console.WriteLine("Log written to log.log");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }


                Thread.Sleep(1000); // 1초 대기
            }
        }
    }
}
