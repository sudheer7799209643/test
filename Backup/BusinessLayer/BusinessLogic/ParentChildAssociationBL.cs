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
    public class ParentChildAssociationBL
    {   
        string connectionString = "";
        string connectionStringMfgProd = "";
        PerformanceTraceLogger objTraceLogger;

        public ParentChildAssociationBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            connectionStringMfgProd = ConfigurationSettings.AppSettings["sqlconnMfg"];
        }
        
        #region ValidateRequest
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objParentChildAssociationRequest"></param>
        /// <param name="objParentChildAssociationResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(GetParentChildAssociationRequest objParentChildAssociationRequest, GetParentChildAssociationResponse objParentChildAssociationResponse)
        {
            CommonValidationBL objCommonValidationBL = new CommonValidationBL();
            bool ValidationStatus = true;
            Int64 countSN = Convert.ToInt64((ConfigurationSettings.AppSettings["countSN"]));

            if (!objParentChildAssociationRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objParentChildAssociationResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objParentChildAssociationRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objParentChildAssociationResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objParentChildAssociationRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objParentChildAssociationResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objParentChildAssociationRequest.RequestID, objParentChildAssociationRequest.RequestingSystem) && ValidationStatus)
            {
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objParentChildAssociationResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if ((objParentChildAssociationRequest.SNum == null || objParentChildAssociationRequest.SNum.Length < 1 || objParentChildAssociationRequest.SNum.Length > countSN) && ValidationStatus)  //VALIDATE SNUM UNIT (MAX 100)
            {
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objParentChildAssociationResponse.ResponseMessage = "Invalid SNum List";
                ValidationStatus = false;
            }
            if (objParentChildAssociationRequest.SNum != null && ValidationStatus)
            {
                for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                {
                    if (!objParentChildAssociationRequest.SNum[i].IsValidSNumFormat() && ValidationStatus)  //VALIDATE SNUM FORMAT
                    {
                        objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objParentChildAssociationResponse.ResponseMessage = "Invalid SNum Format";
                        ValidationStatus = false;
                    }
                }
            }
            return ValidationStatus;
        }
         
        #endregion

        public bool ProcessRequest(GetParentChildAssociationRequest objParentChildAssociationRequest, GetParentChildAssociationResponse objParentChildAssociationResponse)
        {
            bool flag = false;
            try
            {
                //ASSIGNING ID'S
                objParentChildAssociationResponse.RequestID = objParentChildAssociationRequest.RequestID;
                objParentChildAssociationResponse.ResponseID = objParentChildAssociationRequest.RequestID;

                //validateRequest()

                if (objParentChildAssociationRequest.SNum.Length > 0)
                {

                    SNumsAssociation[] association = new SNumsAssociation[objParentChildAssociationRequest.SNum.Length];
                    string[] arrCommaSeparatedSN = new string[objParentChildAssociationRequest.SNum.Length];

                    #region Tracing Start Code
                    objTraceLogger = new PerformanceTraceLogger(objParentChildAssociationRequest.RequestID, "GetParentChildAssociation", "Getting SNum Mac Tree", objParentChildAssociationRequest.SNum.Length);

                    if (objTraceLogger.IsEnable)
                        objTraceLogger.StartTrace();
                    #endregion

                    DataSet objMfgDS = null;
                    for (int i = 0; i < association.Length; i++)
                    {
                        association[i] = new SNumsAssociation();
                        association[i].SNum = objParentChildAssociationRequest.SNum[i];
                        string commaSepIndividualSN = "";
                        //get Snum & Mac tree
                        objMfgDS = GetSNumMacTree(objParentChildAssociationRequest.SNum[i]);
                        //populate SNumList

                        association[i].SNumList = GetSNumsList(objMfgDS, ref commaSepIndividualSN);
                        association[i].Message = "";
                        association[i].Status = 0;

                        arrCommaSeparatedSN[i] = commaSepIndividualSN;
                    }

                    #region Tracing End Code
                    if (objTraceLogger.IsEnable)
                        objTraceLogger.EndTrace();
                    #endregion

                    //For each associate
                    //go through SnumList array and create comma separated string of SNum
                    //call objBL.GetLicenseDataTree(objDS);
                    //from DS 
                    #region Tracing Start Code
                    objTraceLogger = new PerformanceTraceLogger(objParentChildAssociationRequest.RequestID, "GetParentChildAssociation", "GetLicenseAndSKUData", objParentChildAssociationRequest.SNum.Length);

                    if (objTraceLogger.IsEnable)
                        objTraceLogger.StartTrace();
                    #endregion

                    for (int i = 0; i < association.Length; i++)
                    {
                        DataSet objCryptoLgrDS = null;
                        //pass ds to cryptologger and get license data
                        //passing comma separated serial numbers for individual association
                        //comma separated includes the input snum too
                        //below method fetches two resultsets - 
                        //1) distinct skus with latest date from license key table
                        //2) second resultset gets all sku details including version
                        objCryptoLgrDS = GetLicenseAndSKUData(arrCommaSeparatedSN[i]);

                        //This method calls common bl method which gets sku details
                        association[i].SNumList = UpdateSNArrayWithLicenseAndSKU(objCryptoLgrDS, association[i].SNumList);
                    }
                    #region Tracing End Code
                    if (objTraceLogger.IsEnable)
                        objTraceLogger.EndTrace();
                    #endregion

                    objParentChildAssociationResponse.Association = association;
                }
                else
                {
                    objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objParentChildAssociationResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                }
                flag = true;
            }
            catch(Exception ex)
            {
                throw new Exception("Error In Get Parent Child Association. " + ex.Message); 
            }
            return flag;
        }

        public DataSet GetSNumMacTree(string SNum) {
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

                    using (SqlCommand objCMD = new SqlCommand("pTAA_GetSNumMacTree", objMFGCon))
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
            catch(Exception ex){
                throw new Exception("Error while reading mfg data. " + ex.Message + ". ");
            }
            return objDS;
        }

        public SNums[] GetSNumsList(DataSet objDS,ref string commaSepIndividualSN)
        {
            DataTable dt = objDS.Tables[0];

            SNums[] arrSNums = new SNums[dt.Rows.Count];
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                SNums objSN = new SNums();
                objSN.SNum = (row["sernum"]!= DBNull.Value) ? row["sernum"].ToString() : "";
                string longStringMac = (row["macadr"] != DBNull.Value) ? row["macadr"].ToString() : "0"; 
                long tempLong = long.Parse(longStringMac);
                objSN.MacAddress = tempLong.ToString("X");
                objSN.MacAddress = (objSN.MacAddress.PadLeft(12, '0'));
                objSN.ParentSNum = (row["parentsernum"] != DBNull.Value) ? row["parentsernum"].ToString() : "";
                objSN.Level = (row["SNumLevel"] != DBNull.Value) ? row["SNumLevel"].ToString() : ""; 

                arrSNums[i] = objSN;
                i++;
                if (objSN.SNum != "")
                    commaSepIndividualSN = commaSepIndividualSN + "," + objSN.SNum;
            }
            return arrSNums;
        }
            

        public DataSet GetLicenseAndSKUData(string CommaSeparatedSN) 
        {
            DataSet objDS = null;
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[1];
                    arParms[0] = new SqlParameter("@SNumList", SqlDbType.VarChar, 8000);
                    arParms[0].Value = CommaSeparatedSN;
                    arParms[0].Direction = ParameterDirection.Input;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_GetParentChildAssociation", objCon))
                    {
                        objCon.Open();
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
                throw new Exception("Error while reading License & SKU data. " + ex.Message + ". ");
            }
            return objDS;
        }
        public SNums[] UpdateSNArrayWithLicenseAndSKU(DataSet objCryptoLgrDS, SNums[] tempSNArray)
        {
            CommonBL objCommonBL = new CommonBL();
            DataTable dtSKU = objCryptoLgrDS.Tables[0];
            
            for (int n = 0; n < tempSNArray.Count(); n++)
            {
                DataRow[] Rows;
                
                string str = "SNumTemp like '" + tempSNArray[n].SNum + "'";
                if (str.Contains('%') || str.Contains('*') || str.Contains('-'))
                {
                    str = str.Replace('%', '/');
                }

                else
                {

                    if (str.IsValidString())
                    {
                        Rows = objCryptoLgrDS.Tables[0].Select(str);


                        if (Rows.Count() > 0)
                        {
                            string[] arrSKU = new string[Rows.Count()];

                            for (int s = 0; s < Rows.Count(); s++)
                            {
                                DataRow dr = Rows[s];
                                arrSKU[s] = dr["PartName"].ToString();
                                tempSNArray[n].IsLicensableSNum = true;
                            }
                            tempSNArray[n].SKU = objCommonBL.GetSKUDetailInfo(arrSKU, tempSNArray[n].SNum);
                        }
                    }
                }
            }
            return tempSNArray;
        }
    }
}
