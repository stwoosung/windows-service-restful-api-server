using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace RestAPIServer.LibClass
{
    public class OleDBService
    {
        private string strConnString = string.Empty;
        private OleDbConnection ConnDB = null;
        private OleDbDataAdapter DBAdapter = new OleDbDataAdapter();

        private DataSet ds = new DataSet();
        private DataTable dt = new DataTable();

        private string ServiceName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
        private LogControl logControl = new LogControl();

        public OleDBService()
        {

        }

        ~OleDBService()
        {
            try
            {
                if (ConnDB != null)
                {
                    ConnDB.Close();
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "OleDBService Destructor", e.Message, LogControl.LogLevel.Error);
            }
        }

        /// <summary>
        /// DB 연결
        /// </summary>
        /// <param name="strConn">DB 커넥션 스트링</param>
        /// <returns>
        /// 1 : 연결 성공
        /// -1: 연결 실패
        /// </returns>
        public bool ConnectDB(string strConn)
        {
            try
            {
                if (ConnDB == null || ConnDB.ConnectionString != strConn)
                {
                    strConnString = strConn;
                    ConnDB = new OleDbConnection(strConnString);
                }

                // 커넥션 Open 상태이면, 기존 커넥션 유지
                if (ConnDB.State == ConnectionState.Open)
                {
                    return true;
                }

                ConnDB.Open();
                if (ConnDB.State == ConnectionState.Open)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "ConnectDB", e.Message, LogControl.LogLevel.Error);
                if (ConnDB != null)
                {
                    ConnDB.Close();
                }
                return false;
            }
        }


        /// <summary>
        /// DB 연결 종료
        /// </summary>
        public void CloseDB()
        {
            try
            {
                if (ConnDB != null)
                {
                    ConnDB.Close();
                }
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "CloseDB", e.Message, LogControl.LogLevel.Error);
            }
        }


        public int FnExecuteSelectQuery(string strQuery, ref DataTable dt)
        {
            try
            {
                dt.Clear();
                this.dt.Clear();
                DBAdapter.SelectCommand = new OleDbCommand(strQuery, ConnDB);

                DBAdapter.Fill(this.dt);
                DBAdapter.Dispose();

                dt = this.dt;

                return 1;
            }
            catch (Exception e)
            {
                logControl.WriteLog(ServiceName, "FnExecuteSelectQuery", e.Message, LogControl.LogLevel.Error);
                return -1;
            }
        }
    }
}
