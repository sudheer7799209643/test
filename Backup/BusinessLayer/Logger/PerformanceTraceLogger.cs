using System;
using System.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

/// <summary>
/// Summary description for PerformanceTraceLogger
/// </summary>
namespace PCMTandberg.Logger
{
    public class PerformanceTraceLogger
    {
        #region Properties and Variables
        string connectionString = "";
        EventLogger objEventLogger;
        public bool IsEnable = false;
        Stopwatch stopWatch;
        bool traceStarted = false;
        DateTime startDate;
        int serialNumCount = 0;

        public int SerialNumCount
        {
            get { return serialNumCount; }
            set { serialNumCount = value; }
        }

        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }
        DateTime endDate;

        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        string requestID;

        public string RequestID
        {
            get { return requestID; }
            set { requestID = value; }
        }

        string webMethodName;

        public string WebMethodName
        {
            get { return webMethodName; }
            set { webMethodName = value; }
        }

        string taskName;

        public string TaskName
        {
            get { return taskName; }
            set { taskName = value; }
        }

        double duration;

        public double Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        #endregion

        #region .ctor
        public PerformanceTraceLogger()
        {
            objEventLogger = new EventLogger();
            connectionString = ConfigurationSettings.AppSettings["sqlperformancetraceconn"];
            string enableTrace = ConfigurationSettings.AppSettings["EnablePerformanceTrace"];
            

            if (!string.IsNullOrEmpty(enableTrace) && enableTrace.Equals("1"))
            {
                IsEnable = true;
            }
            startDate = DateTime.MinValue;
            endDate = DateTime.MinValue;
            stopWatch = new Stopwatch();
            traceStarted = false;
        }
        public PerformanceTraceLogger(string RequestID,string WebMethodName,string TaskName,int SerialNumCount):this()
        {
            this.RequestID = RequestID;
            this.WebMethodName = WebMethodName;
            this.TaskName = TaskName;
            this.SerialNumCount = SerialNumCount;
        }
        #endregion 

        public void StartTrace()
        {
            stopWatch.Reset();
            stopWatch.Start();
            this.StartDate = DateTime.UtcNow;
            traceStarted = true;
        }
        public void EndTrace()
        {
            if (traceStarted)
            {
                stopWatch.Stop();
                this.EndDate = DateTime.UtcNow;
                this.Duration = (stopWatch.ElapsedTicks * 1000.0) / Stopwatch.Frequency;
                WriteTrace(this.RequestID, this.WebMethodName, this.TaskName,this.SerialNumCount, this.StartDate, this.EndDate, this.Duration/1000);
                stopWatch.Reset();
                traceStarted = false;
            }
        }

        public void WriteTrace(string RequestID, string WebMethodName, string TaskName,int SerialNumCount, DateTime startTime, DateTime endTime, double duration)
        {
            try
            {

                SqlParameter[] arParms = new SqlParameter[7];


                arParms[0] = new SqlParameter("@RequestID", SqlDbType.VarChar, 50);
                arParms[0].Value = RequestID;
                arParms[0].Direction = ParameterDirection.Input;

                arParms[1] = new SqlParameter("@WebMethodName", SqlDbType.VarChar, 50);
                arParms[1].Value = WebMethodName;
                arParms[1].Direction = ParameterDirection.Input;

                arParms[2] = new SqlParameter("@TaskName", SqlDbType.VarChar, 255);
                arParms[2].Value = TaskName;
                arParms[2].Direction = ParameterDirection.Input;

                arParms[3] = new SqlParameter("@SNumCount", SqlDbType.Int);
                arParms[3].Value = SerialNumCount;
                arParms[3].Direction = ParameterDirection.Input;


                arParms[4] = new SqlParameter("@StartTime", SqlDbType.DateTime);
                arParms[4].Value = startTime;
                arParms[4].Direction = ParameterDirection.Input;

                arParms[5] = new SqlParameter("@EndTime", SqlDbType.DateTime);
                arParms[5].Value = endTime;
                arParms[5].Direction = ParameterDirection.Input;

                arParms[6] = new SqlParameter("@Duration", SqlDbType.Float);
                arParms[6].Value = duration;
                arParms[6].Direction = ParameterDirection.Input;

                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQL = @"INSERT INTO TAA_PerformanceAnalyzerTrace (RequestID, WebMethodName, [TaskName],[SNumCount], [StartTime], [EndTime], Duration)
						                    values
				        			 (@RequestID, @WebMethodName, @TaskName,@SNumCount,@StartTime, @EndTime, @Duration)";

                    objCon.Open();

                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                    {
                        objCMD.CommandType = CommandType.Text;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                objEventLogger.WriteEntry(ex.Message);
            }
        }
    }
}
