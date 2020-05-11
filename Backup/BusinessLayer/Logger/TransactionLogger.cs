using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCMTandberg.DataAccess;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace PCMTandberg.Logger
{
    public partial class TransactionLogger
    {

        string connectionString;
        EventLogger objEventLogger;

        public TransactionLogger() {
            objEventLogger = new EventLogger();
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
        }
        
        //pTAA_LogTransaction
        //check if time can be tracked here




        public void LogTransaction(string RequestId, DateTime RequestDatetime,string RequestingSystem,string Request ,string ResponseID ,DateTime ResponseDateTime,
                                    string Response, int ResponseStatus, string ResponseMessage, int TransactionID)
        {
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[10];

                    arParms[0] = new SqlParameter("@RequestDatetime", SqlDbType.DateTime);
                    arParms[0].Value = RequestDatetime;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@RequestingSystem", SqlDbType.VarChar, 40);
                    arParms[1].Value = (RequestingSystem == null ? "-1" : RequestingSystem);
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Request", SqlDbType.Xml);
                    arParms[2].Value = (Request == null ? "" : Request);
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@ResponseID", SqlDbType.VarChar, 40);
                    arParms[3].Value = (ResponseID == null ? "-1" : ResponseID);
                    arParms[3].Direction = ParameterDirection.Input;

                    arParms[4] = new SqlParameter("@ResponseDateTime", SqlDbType.DateTime);
                    arParms[4].Value = ResponseDateTime;
                    arParms[4].Direction = ParameterDirection.Input;


                    arParms[5] = new SqlParameter("@Response", SqlDbType.Xml);
                    arParms[5].Value = (Response == null ? "" : Response);
                    arParms[5].Direction = ParameterDirection.Input;


                    arParms[6] = new SqlParameter("@ResponseStatus", SqlDbType.Int);
                    arParms[6].Value = ResponseStatus;
                    arParms[6].Direction = ParameterDirection.Input;

                    arParms[7] = new SqlParameter("@ResponseMessage", SqlDbType.VarChar, 100);
                    arParms[7].Value = ResponseMessage == null ? "" : ResponseMessage;
                    arParms[7].Direction = ParameterDirection.Input;

                    arParms[8] = new SqlParameter("@TransactionID", SqlDbType.Int);
                    if(TransactionID>0)
                    arParms[8].Value = TransactionID;
                    arParms[8].Direction = ParameterDirection.Input;

                    arParms[9] = new SqlParameter("@RequestID", SqlDbType.VarChar, 40);
                    arParms[9].Value = RequestId;
                    arParms[9].Direction = ParameterDirection.Input;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_LogTransaction", objCon))
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
                objEventLogger.WriteEntry("LogTransaction"+ex.Message+Environment.NewLine+ex.StackTrace);
                //WriteEntry(ex.Message);
            }
        }
    }
}
