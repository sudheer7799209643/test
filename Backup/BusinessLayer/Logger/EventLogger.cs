using System;
using System.Configuration;
using System.IO;

using System.Data;
using System.Data.SqlClient;


/// <summary>
/// Summary description for EventLogger
/// </summary>
/// 
namespace PCMTandberg.Logger
{
    public class EventLogger
    {
        string connectionString = "";
        public EventLogger()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
        }
        public void WriteEntry(string errorMessage)
        {
            try
            {
                //string path = @"C:\PCM.txt";
                string path = "~/Error/" + DateTime.Today.ToString("dd-mm-yy") + ".txt";
                if (!File.Exists(System.Web.HttpContext.Current.Server.MapPath(path)))
                {
                    File.Create(System.Web.HttpContext.Current.Server.MapPath(path)).Close();
                }

                using (StreamWriter w = File.AppendText(System.Web.HttpContext.Current.Server.MapPath(path)))
                {
                    w.WriteLine("\r\nLog Entry : ");
                    w.WriteLine("{0}", DateTime.Now.ToString());
                    string err = "Error Message:" + errorMessage;
                    w.WriteLine(err);
                    w.WriteLine("__________________________");
                    w.Flush();
                    w.Close();
                }
            }
            catch (Exception ex)
            {
                //Exception is terminated here            
                string message = ex.Message;
            }

        }

        //public void WriteLog(string message)
        //{
        //    WriteLog("1", "", "", "", "", "", message, "");
        //}
        public void WriteLog(string ErrorMessage,string Machine,DateTime Timestamp,string XReference){
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[4];

                    arParms[0] = new SqlParameter("@ErrorMessage", SqlDbType.VarChar);
                    arParms[0].Value = (ErrorMessage == null ? "" : ErrorMessage);  
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@Machine", SqlDbType.VarChar, 50);
                    arParms[1].Value = (Machine == null ? "-1" : Machine);
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Timestamp", SqlDbType.DateTime);
                    arParms[2].Value = Timestamp; 
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@XReference", SqlDbType.VarChar);
                    arParms[3].Value = (XReference == null ? "" : XReference);
                    arParms[3].Direction = ParameterDirection.Input;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_LogError", objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteEntry(ex.Message);
            }
        }
    }
}