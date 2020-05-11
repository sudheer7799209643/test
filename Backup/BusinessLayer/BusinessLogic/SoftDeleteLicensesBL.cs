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

namespace PCMTandberg.BusinessLogic
{
    public class SoftDeleteLicensesBL
    {
        #region Property & Variables

        string connectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        CommonBL objCommonBL;
        #endregion

        #region .ctor
        public SoftDeleteLicensesBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            objCommonValidationBL = new CommonValidationBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftDeleteLicensesRequest"></param>
        /// <param name="objSoftDeleteLicensesResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(SoftDeleteLicensesRequest objSoftDeleteLicensesRequest, SoftDeleteLicensesResponse objSoftDeleteLicensesResponse)
        {
            bool ValidationStatus = true;
            
            if (!objSoftDeleteLicensesRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objSoftDeleteLicensesRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objSoftDeleteLicensesRequest.RequestID, objSoftDeleteLicensesRequest.RequestingSystem) && ValidationStatus)
            {
                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }

            if (objSoftDeleteLicensesRequest.SNum == null || objSoftDeleteLicensesRequest.SNum.Length < 1 && ValidationStatus)
            {
                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftDeleteLicensesResponse.ResponseMessage = "Please enter Serial Number . This is Mandatory field.";
                ValidationStatus = false;
            }
            else
            {
                if (!objSoftDeleteLicensesRequest.SNum.IsValidString() && objSoftDeleteLicensesRequest.SNum.Length > 0 && ValidationStatus)
                {
                    objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSoftDeleteLicensesResponse.ResponseMessage = "Invalid SNum Format";
                    ValidationStatus = false;
                }
            }

            if ((objSoftDeleteLicensesRequest.SKU != null) && ValidationStatus && objSoftDeleteLicensesRequest.SNum.Length > 0)  //VALIDATE SNUM UNIT (MAX 100)
            {
                if (objSoftDeleteLicensesRequest.SKU.Length > 1)
                {
                    if (!objSoftDeleteLicensesRequest.SKU.IsValidString() && ValidationStatus)
                    {
                        objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objSoftDeleteLicensesResponse.ResponseMessage = "Invalid SKU Format";
                        ValidationStatus = false;
                    }
                    
                }
            }
                
            return ValidationStatus;
        }
        #endregion

        #region ProcessRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftDeleteLicensesRequest"></param>
        /// <param name="objSoftDeleteLicensesResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(SoftDeleteLicensesRequest objSoftDeleteLicensesRequest, SoftDeleteLicensesResponse objSoftDeleteLicensesResponse)
        {

            bool flag = false;
            Int32 flagVal = 0;
            try
            {
                #region codestart
                DataSet ds = new DataSet();
                /*new CR user logging 08/2013*/
                if (objSoftDeleteLicensesRequest.SKU != null)
                {
                    flagVal = GetSoftDeleteLicenses(objSoftDeleteLicensesRequest.SNum, objSoftDeleteLicensesRequest.SKU,objSoftDeleteLicensesRequest.RequestingSystem);
                }
                else
                {
                    flagVal = GetSoftDeleteLicenses(objSoftDeleteLicensesRequest.SNum, null, objSoftDeleteLicensesRequest.RequestingSystem);
                }



                if (flagVal > 0)
                {


                    if (flagVal == 1)
                    {

                        objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                        objSoftDeleteLicensesResponse.ResponseMessage = "All the Licensekeys related to the Serial Number  " + objSoftDeleteLicensesRequest.SNum + "  are marked deleted.";
                        flag = true;
                    }
                    else
                    {
                        if (flagVal == 2)
                        {
                            objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.SoftDeletionFailed;
                            objSoftDeleteLicensesResponse.ResponseMessage = "There are NO Licensekeys related to the serial Number  " + objSoftDeleteLicensesRequest.SNum + "  to be marked as deleted.";
                            flag = true;
                        }

                        else
                        {
                            if (flagVal == 3)
                            {
                                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                objSoftDeleteLicensesResponse.ResponseMessage = "All the License keys related to the serial Number  " + objSoftDeleteLicensesRequest.SNum + "  and SKU or Partname  " + objSoftDeleteLicensesRequest.SKU + "  are marked deleted.";
                                flag = true;
                            }
                            else
                            {
                                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.SoftDeletionFailed;
                                objSoftDeleteLicensesResponse.ResponseMessage = "There are NO Licensekeys related to the serial Number  " + objSoftDeleteLicensesRequest.SNum + "  and SKU or Partname  " + objSoftDeleteLicensesRequest.SKU + "  to be marked as deleted.";
                                flag = true;

                            }

                        }
                    }
                }

                else
                {
                    objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSoftDeleteLicensesResponse.ResponseMessage = "No soft deltions were done";
                }
                            

                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While getting the output of ProcessRequest method related to SoftDeleteLicenses . " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return flag;
        }
        #endregion

        #region GetSoftDeleteLicenses
       /// <summary>
       /// 
       /// </summary>
       /// <param name="snum"></param>
       /// <param name="sKUID"></param>
       /// <returns></returns>
        public Int32 GetSoftDeleteLicenses(string snum ,string sKUID,string requestingSystem)
        {
            DataSet ResultDataSet = new DataSet();
            int flagVal;
            
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_SoftDeleteLicenses";
                    SqlParameter[] arParms = new SqlParameter[4];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = snum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 100);
                    arParms[1].Value = sKUID;
                    arParms[1].Direction = ParameterDirection.Input;

                    /*new CR user logging 08/2013*/
                    arParms[2] = new SqlParameter("@UpdatedBy", SqlDbType.VarChar, 100);
                    arParms[2].Value = requestingSystem;
                    arParms[2].Direction = ParameterDirection.Input;

                    //arParms[2] = new SqlParameter("@RetMsg", SqlDbType.VarChar);
                    //arParms[2].Direction = ParameterDirection.Output;

                    arParms[3] = new SqlParameter("@flag", SqlDbType.Int);
                    arParms[3].Direction = ParameterDirection.Output;

                    ResultDataSet = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);
                    flagVal = arParms[3] == null ? 0 : int.Parse(arParms[3].Value.ToString());
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While getting the output of GetSoftDeleteLicenses method. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return flagVal;
        }
        #endregion

    }
}

