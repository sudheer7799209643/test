using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Net.Security;
using PCMTandberg.BusinessEntities;
using PCMTandberg.DataAccess;
using PCMTandberg.Logger;


namespace PCMTandberg.BusinessLogic
{
    public class GetSNHierarchyBL
    {   
        string connectionString = "";
        string connectionStringMfgProd = "";
        PerformanceTraceLogger objTraceLogger;

        public GetSNHierarchyBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            connectionStringMfgProd = ConfigurationSettings.AppSettings["sqlconnMfg"];
        }
        
        #region ValidateRequest
        
        /// <summary>
        /// Validate the Input request
        /// </summary>
        /// <param name="objGetSNHierarchyRequest"></param>
        /// <param name="objGetSNHierarchyResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(GetSNHierarchyRequest objGetSNHierarchyRequest, GetSNHierarchyResponse objGetSNHierarchyResponse)
        {
            CommonValidationBL objCommonValidationBL = new CommonValidationBL();
            bool ValidationStatus = true;
            Int64 countSN = Convert.ToInt64((ConfigurationSettings.AppSettings["countSN"]));

            if (!objGetSNHierarchyRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objGetSNHierarchyRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objGetSNHierarchyRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objGetSNHierarchyRequest.RequestID, objGetSNHierarchyRequest.RequestingSystem) && ValidationStatus)
            {
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if ((objGetSNHierarchyRequest.SNum == null || objGetSNHierarchyRequest.SNum.Length < 1 || objGetSNHierarchyRequest.SNum.Length > countSN) && ValidationStatus)  //VALIDATE SNUM UNIT (MAX 100)
            {
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objGetSNHierarchyResponse.ResponseMessage = "Invalid SNum List";
                ValidationStatus = false;
            }
            if (objGetSNHierarchyRequest.SNum != null && ValidationStatus)
            {
                for (int i = 0; i < objGetSNHierarchyRequest.SNum.Length; i++)
                {
                    if (!objGetSNHierarchyRequest.SNum[i].IsValidSNumFormat() && ValidationStatus)  //VALIDATE SNUM FORMAT
                    {
                        objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objGetSNHierarchyResponse.ResponseMessage = "Invalid SNum Format";
                        ValidationStatus = false;
                    }
                }
            }
            return ValidationStatus;
        }
         
        #endregion

        public bool ProcessRequest(GetSNHierarchyRequest objGetSNHierarchyRequest, GetSNHierarchyResponse objGetSNHierarchyResponse)
        {
            bool flag = false;
            try
            {
                //ASSIGNING ID'S
                objGetSNHierarchyResponse.RequestID = objGetSNHierarchyRequest.RequestID;
                objGetSNHierarchyResponse.ResponseID = objGetSNHierarchyRequest.RequestID;

                //validateRequest()

                if (objGetSNHierarchyRequest.SNum.Length > 0)
                {

                    SNumsAssociationSN[] association = new SNumsAssociationSN[objGetSNHierarchyRequest.SNum.Length];
                    string[] arrCommaSeparatedSN = new string[objGetSNHierarchyRequest.SNum.Length];

                    #region Tracing Start Code
                    objTraceLogger = new PerformanceTraceLogger(objGetSNHierarchyRequest.RequestID, "GetParentChildAssociation", "Getting SNum Mac Tree For GetSNHierarchyRequest", objGetSNHierarchyRequest.SNum.Length);

                    if (objTraceLogger.IsEnable)
                        objTraceLogger.StartTrace();
                    #endregion

                    DataSet objMfgDS = null;
                    for (int i = 0; i < association.Length; i++)
                    {
                        association[i] = new SNumsAssociationSN();
                        association[i].SNum = objGetSNHierarchyRequest.SNum[i];
                        string commaSepIndividualSN = "";
                        //get Snum & Mac tree
                        objMfgDS = GetSNumMacTreeForSNHierarchy(objGetSNHierarchyRequest.SNum[i]);
                        //populate SNumList

                        association[i].SNumList = GetSNumsList(objMfgDS, ref commaSepIndividualSN);
                        association[i].Message = "Success";
                        association[i].Status = 0;

                        arrCommaSeparatedSN[i] = commaSepIndividualSN;
                    }

                    #region Tracing End Code
                    if (objTraceLogger.IsEnable)
                        objTraceLogger.EndTrace();
                    #endregion

              
                    objGetSNHierarchyResponse.Association = association;
                }
                else
                {
                    objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                }
                flag = true;
            }
            catch(Exception ex)
            {
                throw new Exception("Error In GetSNHierarchy Method. " + ex.Message); 
            }
            return flag;
        }

        
        public DataSet GetSNumMacTreeForSNHierarchy(string SNum)
        {
            //DataTable snumMacData = new DataTable();
            DataSet objDS = null;
            try
            {
                using (SqlConnection objMFGCon = new SqlConnection(connectionStringMfgProd))
                {
                    SqlParameter[] arParms = new SqlParameter[1];
                    arParms[0] = new SqlParameter("@sernum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_GetSNumMacTreeForSNHierarchy", objMFGCon))
                    {
                        objMFGCon.Open();

                        //create our DataAdapter and DataSet objects
                        SqlDataAdapter objDA = new SqlDataAdapter(objCMD);
                        objDS = new DataSet();

                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Parameters.AddRange(arParms);
                        objDA.Fill(objDS);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading mfg data. " + ex.Message + ". ");
            }
            return objDS;
        }



        public SNumsHierarchy[] GetSNumsList(DataSet objDS,ref string commaSepIndividualSN)
        {
            DataTable dt = objDS.Tables[0];

            SNumsHierarchy[] arrSNums = new SNumsHierarchy[dt.Rows.Count];
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                SNumsHierarchy objSN = new SNumsHierarchy();
                objSN.SNum = (row["sernum"]!= DBNull.Value) ? row["sernum"].ToString() : "";
                string longStringMac = (row["macadr"] != DBNull.Value) ? row["macadr"].ToString() : "0"; 
                long tempLong = long.Parse(longStringMac);
                objSN.MacAddress = tempLong.ToString("X");
                objSN.MacAddress = (objSN.MacAddress.PadLeft(12, '0'));
                objSN.ParentSNum = (row["parentsernum"] != DBNull.Value) ? row["parentsernum"].ToString() : "";
                objSN.Level = (row["SNumLevel"] != DBNull.Value) ? row["SNumLevel"].ToString() : "";
                objSN.UutType = (row["uuttype"] != DBNull.Value) ? row["uuttype"].ToString() : "";
                objSN.RecTime = (row["rectime"] != DBNull.Value) ? row["rectime"].ToString() : ""; 
                arrSNums[i] = objSN;
                i++;
                if (objSN.SNum != "")
                    commaSepIndividualSN = commaSepIndividualSN + "," + objSN.SNum;
            }
            return arrSNums;
        }
            

        
    }
}
