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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;

namespace PCMTandberg.BusinessLogic
{
    public class SetSKUMetadataBL
    {

        #region Property & Variables

        string connectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        CommonBL objCommonBL;
        #endregion

        #region .ctor
        public SetSKUMetadataBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            objCommonValidationBL = new CommonValidationBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSetSKUMetadataRequest"></param>
        /// <param name="objSetSKUMetadataResponse"></param>
        /// <returns></returns>  
        public bool ValidateRequest(SetSKUMetadataRequest objSetSKUMetadataRequest, SetSKUMetadataResponse objSetSKUMetadataResponse)
        {
            bool ValidationStatus = true;
            string strAlgIDs = ConfigurationSettings.AppSettings["Algorithms"];
            List<string> lstAlgIds = new List<string>(strAlgIDs.Split(','));

            if (!objSetSKUMetadataRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSetSKUMetadataResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objSetSKUMetadataRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSetSKUMetadataResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objSetSKUMetadataRequest.RequestID, objSetSKUMetadataRequest.RequestingSystem) && ValidationStatus)
            {
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objSetSKUMetadataResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if (((objSetSKUMetadataRequest.SKUMetadata.SKUID == null) || (objSetSKUMetadataRequest.SKUMetadata.SKUID != null && !objSetSKUMetadataRequest.SKUMetadata.SKUID.IsValidString())) && ValidationStatus)
            {
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                /*Bug no:4397 */
                objSetSKUMetadataResponse.ResponseMessage = "SKU element is required. Please enter valid SKU";//"Please enter valid SKU";
                ValidationStatus = false;
            }
            if ((objSetSKUMetadataRequest.SKUMetadata.SKUID != null) && ValidationStatus)
            {
                if (objSetSKUMetadataRequest.SKUMetadata.SKUID.Trim().ToLower().Equals("null"))
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                    objSetSKUMetadataResponse.ResponseMessage = "SKUID cannot be 'NULL'.Please enter valid SKUID";
                    ValidationStatus = false;
                }
            }

            if (objSetSKUMetadataRequest.SKUMetadata.ActionType == null && ValidationStatus)
            {
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSetSKUMetadataResponse.ResponseMessage = "Please enter valid Action Type";
                ValidationStatus = false;
            }

            if (objSetSKUMetadataRequest.SKUMetadata.ActionType != null && ValidationStatus)
            {
                if (!(objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("insert") || objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("update") || objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("delete")))
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Invalid ActionType. Valid Action Type(s) are : insert or Update or Delete";
                    ValidationStatus = false;
                }

            }
            if (objSetSKUMetadataRequest.SKUMetadata.ActionType != null)
            {
                if ((objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("insert") || objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("update")) && ValidationStatus)
                {
                    if ((objSetSKUMetadataRequest.SKUMetadata.AlgID == null || (objSetSKUMetadataRequest.SKUMetadata.AlgID != null && !objSetSKUMetadataRequest.SKUMetadata.AlgID.IsValidString())) && ValidationStatus)
                    {
                        objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        /*Bug no:4397 */
                        objSetSKUMetadataResponse.ResponseMessage = "Alg. ID element is required. Please enter valid Alg. ID";//"Invalid Alg ID";
                        ValidationStatus = false;
                    }
                    if (objSetSKUMetadataRequest.SKUMetadata.AlgID != null && ValidationStatus)
                    {
                        if (objSetSKUMetadataRequest.SKUMetadata.AlgID.Trim().ToLower().Equals("null"))
                        {
                            objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            objSetSKUMetadataResponse.ResponseMessage = "AlgID cannot be 'NULL'.Please enter proper value.";
                            ValidationStatus = false;
                        }
                        else if (!lstAlgIds.Contains(objSetSKUMetadataRequest.SKUMetadata.AlgID))
                        {
                            objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            objSetSKUMetadataResponse.ResponseMessage = "Not valid AlgID.AlgID Should be one among the range " + strAlgIDs;
                            ValidationStatus = false;
                        }

                    }


                    if ((objSetSKUMetadataRequest.SKUMetadata.Seed == null || (objSetSKUMetadataRequest.SKUMetadata.Seed != null && !objSetSKUMetadataRequest.SKUMetadata.Seed.IsValidString())) && ValidationStatus)
                    {

                        objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        /*Bug no:4397 */
                        objSetSKUMetadataResponse.ResponseMessage = "Seed element is required. Please enter valid Seed";//"Invalid seed";
                        ValidationStatus = false;

                    }
                    if (objSetSKUMetadataRequest.SKUMetadata.Seed != null && ValidationStatus)
                    {
                        if (objSetSKUMetadataRequest.SKUMetadata.Seed.Trim().ToLower().Equals("null"))
                        {
                            objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            objSetSKUMetadataResponse.ResponseMessage = "Seed cannot be 'NULL'.Please enter proper value.";
                            ValidationStatus = false;
                        }
                    }

                }
            }
            if (objSetSKUMetadataRequest.SKUMetadata.ProductType != null && ValidationStatus)
            {
                if (!(objSetSKUMetadataRequest.SKUMetadata.ProductType.Equals(1) || objSetSKUMetadataRequest.SKUMetadata.ProductType.Equals(2)))
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Invalid ProductType.Please enter either 1 or 2";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.ProductType.Equals(2) && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "SW cannot have  a  product type of 2.Please change the ProductType or uncheck the SoftwareMetadata";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("insert") || objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("update")) && ValidationStatus)
                {
                    if ((objSetSKUMetadataRequest.SKUMetadata.ProductType.Equals(1) && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata == null) && ValidationStatus)
                    {
                        objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objSetSKUMetadataResponse.ResponseMessage = "SW should have SoftwareMetadata.";
                        ValidationStatus = false;
                        //We cannot have a SW without Version metadata
                    }
                }

            }


            if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null && ValidationStatus)
            {
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.Version == null && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Please enter valid Version";
                    ValidationStatus = false;
                }
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionType == null && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Please enter valid VersionType";
                    ValidationStatus = false;
                }
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionSeqNo == null && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Please enter valid VersionSeqNo";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.Version != null && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.Version.Trim().ToLower().Equals("null")) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "Version cannot be 'NULL'.Please enter proper value.";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionType != null && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionType.Trim().ToLower().Equals("null")) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "VersionType cannot be 'NULL'.Please enter proper value.";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionSeqNo != null && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionSeqNo.ToString().ToLower().Equals("null")) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "VersionSeqNo cannot be 'NULL'.Please enter proper int value.";
                    ValidationStatus = false;
                }
                if ((objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionReleaseClassification != null && objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionReleaseClassification.Trim().ToLower().Equals("null")) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objSetSKUMetadataResponse.ResponseMessage = "VersionReleaseClassification cannot be 'NULL'.Please enter proper value.";
                    ValidationStatus = false;
                }


            }
            if (objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("insert") && ValidationStatus)
            {
                if (((objSetSKUMetadataRequest.SKUMetadata.CreatedBy == null) || (objSetSKUMetadataRequest.SKUMetadata.CreatedBy != null && !objSetSKUMetadataRequest.SKUMetadata.CreatedBy.IsValidString())) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    /*Bug no:4397 */
                    objSetSKUMetadataResponse.ResponseMessage = "CreatedBy element is required. Please enter valid user id for CreatedBy";//"Please enter valid user id for CreatedBy";
                    ValidationStatus = false;
                }
                if (((objSetSKUMetadataRequest.SKUMetadata.UpdatedBy == null) || (objSetSKUMetadataRequest.SKUMetadata.UpdatedBy != null && !objSetSKUMetadataRequest.SKUMetadata.UpdatedBy.IsValidString())) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    /*Bug no:4397 */
                    objSetSKUMetadataResponse.ResponseMessage = "UpdatedBy element is required. Please enter valid user id for UpdatedBy";//"Please enter valid user id for UpdatedBy";
                    ValidationStatus = false;
                }


            }
            if ((objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("update") || objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("delete")) && ValidationStatus)
            {
                if (((objSetSKUMetadataRequest.SKUMetadata.UpdatedBy == null) || (objSetSKUMetadataRequest.SKUMetadata.UpdatedBy != null && !objSetSKUMetadataRequest.SKUMetadata.UpdatedBy.IsValidString())) && ValidationStatus)
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    /*Bug no:4397 */
                    objSetSKUMetadataResponse.ResponseMessage = "UpdatedBy element is required. Please enter valid user id for UpdatedBy";//"Please enter valid user id for UpdatedBy";
                    ValidationStatus = false;
                }
            }

            return ValidationStatus;
        }
        #endregion

        #region ProcessRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSetSKUMetadataRequest"></param>
        /// <param name="objSetSKUMetadataResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(SetSKUMetadataRequest objSetSKUMetadataRequest, SetSKUMetadataResponse objSetSKUMetadataResponse)
        {

            bool flag = false;
            string statusMsg = string.Empty;

            try
            {
                #region codestart
                DataSet ds = new DataSet();
                if (objSetSKUMetadataRequest.SKUMetadata.SKUID != null)
                {
                    statusMsg = SetSKUMetadata(objSetSKUMetadataRequest);
                }
                if ((!string.IsNullOrEmpty(statusMsg)) && (statusMsg.Contains("Record already exists") || statusMsg.Contains("No record exists")))
                {
                    objSetSKUMetadataResponse.ResponseStatus = 1;
                    objSetSKUMetadataResponse.ResponseMessage = statusMsg;
                    flag = false;
                }
                else
                {
                    objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                    objSetSKUMetadataResponse.ResponseMessage = statusMsg;
                    flag = true;
                }



                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While getting the output of ProcessRequest method related to SetSKUMetadata. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return flag;
        }

        /// <summary>
        /// This method sets the input parameters and calls the SP to perform the insert/delete/update operations
        /// </summary>
        /// <param name="objSetSKUMetadataRequest"></param>
        /// <returns></returns>
        private string SetSKUMetadata(SetSKUMetadataRequest objSetSKUMetadataRequest)
        {
            string statusMsg = string.Empty;
            //bool status = true;           

            using (SqlConnection objCon = new SqlConnection(connectionString))
            {
                SqlParameter[] arParms = new SqlParameter[24];

                arParms[0] = new SqlParameter("@ActionType", SqlDbType.VarChar, 100);
                arParms[0].Value = objSetSKUMetadataRequest.SKUMetadata.ActionType;
                arParms[0].Direction = ParameterDirection.Input;

                arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 100);
                arParms[1].Value = objSetSKUMetadataRequest.SKUMetadata.SKUID;
                arParms[1].Direction = ParameterDirection.Input;

                arParms[2] = new SqlParameter("@TAASysPartNo", SqlDbType.VarChar, 100);
                arParms[2].Value = objSetSKUMetadataRequest.SKUMetadata.TAASysPartNo;
                arParms[2].Direction = ParameterDirection.Input;

                arParms[3] = new SqlParameter("@TAASysPartName", SqlDbType.VarChar, 100);
                arParms[3].Value = objSetSKUMetadataRequest.SKUMetadata.TAASysPartName;
                arParms[3].Direction = ParameterDirection.Input;

                arParms[4] = new SqlParameter("@TAAPartNo", SqlDbType.VarChar, 100);
                arParms[4].Value = objSetSKUMetadataRequest.SKUMetadata.TAAPartNo;
                arParms[4].Direction = ParameterDirection.Input;

                arParms[5] = new SqlParameter("@TAAPartName", SqlDbType.VarChar, 100);
                arParms[5].Value = objSetSKUMetadataRequest.SKUMetadata.TAAPartName; ;
                arParms[5].Direction = ParameterDirection.Input;


                arParms[6] = new SqlParameter("@ProductType", SqlDbType.Int);
                arParms[6].Value = objSetSKUMetadataRequest.SKUMetadata.ProductType;
                arParms[6].Direction = ParameterDirection.Input;

                arParms[7] = new SqlParameter("@Qty", SqlDbType.Int);
                arParms[7].Value = objSetSKUMetadataRequest.SKUMetadata.Qty;
                arParms[7].Direction = ParameterDirection.Input;


                arParms[8] = new SqlParameter("@AlgID", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.AlgID == null && objSetSKUMetadataRequest.SKUMetadata.ActionType.Trim().ToLower().Equals("delete"))
                    arParms[8].Value = "0";
                else
                    arParms[8].Value = objSetSKUMetadataRequest.SKUMetadata.AlgID;
                arParms[8].Direction = ParameterDirection.Input;

                arParms[9] = new SqlParameter("@AlgDesc", SqlDbType.VarChar, 100);
                arParms[9].Value = objSetSKUMetadataRequest.SKUMetadata.AlgDesc;
                arParms[9].Direction = ParameterDirection.Input;


                arParms[10] = new SqlParameter("@Seed", SqlDbType.VarChar, 100);
                arParms[10].Value = objSetSKUMetadataRequest.SKUMetadata.Seed;
                arParms[10].Direction = ParameterDirection.Input;


                arParms[11] = new SqlParameter("@TaaType", SqlDbType.VarChar, 100);
                arParms[11].Value = objSetSKUMetadataRequest.SKUMetadata.TaaType;
                arParms[11].Direction = ParameterDirection.Input;

                arParms[12] = new SqlParameter("@AllowMany", SqlDbType.Int);
                arParms[12].Value = objSetSKUMetadataRequest.SKUMetadata.AllowMany;
                arParms[12].Direction = ParameterDirection.Input;

                arParms[13] = new SqlParameter("@Version", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                    arParms[13].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.Version;
                else
                    arParms[13].Value = string.Empty;
                arParms[13].Direction = ParameterDirection.Input;

                arParms[14] = new SqlParameter("@VersionType", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                    arParms[14].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionType;
                else
                    arParms[14].Value = string.Empty;
                arParms[14].Direction = ParameterDirection.Input;

                arParms[15] = new SqlParameter("@VersionSeqNo", SqlDbType.Int);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                    arParms[15].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionSeqNo;
                else
                    arParms[15].Value = 0;
                arParms[15].Direction = ParameterDirection.Input;

                arParms[16] = new SqlParameter("@VersionReleaseClassification", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                    arParms[16].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.VersionReleaseClassification;
                else
                    arParms[16].Value = string.Empty;
                arParms[16].Direction = ParameterDirection.Input;

                arParms[17] = new SqlParameter("@ImageUrl", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                    arParms[17].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.ImageUrl;
                else
                    arParms[17].Value = string.Empty;
                arParms[17].Direction = ParameterDirection.Input;

                arParms[18] = new SqlParameter("@ReleaseDate", SqlDbType.DateTime, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata != null)
                {
                    //arParms[18].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.ReleaseDate;  
                    if (objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.ReleaseDate.ToString().Equals("1/1/0001 12:00:00 AM"))
                        arParms[18].Value = null;
                    else
                        arParms[18].Value = objSetSKUMetadataRequest.SKUMetadata.SoftwareMetadata.ReleaseDate;
                }
                else
                    arParms[18].Value = null;
                arParms[18].Direction = ParameterDirection.Input;

                arParms[19] = new SqlParameter("@CreatedBy", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.CreatedBy != null)
                    arParms[19].Value = objSetSKUMetadataRequest.SKUMetadata.CreatedBy;
                else
                    arParms[19].Value = "SWIFT";
                arParms[19].Direction = ParameterDirection.Input;

                arParms[20] = new SqlParameter("@CreatedOn", SqlDbType.VarChar, 100);
                arParms[20].Value = objSetSKUMetadataRequest.SKUMetadata.CreatedOn;
                arParms[20].Direction = ParameterDirection.Input;

                arParms[21] = new SqlParameter("@UpdatedBy", SqlDbType.VarChar, 100);
                if (objSetSKUMetadataRequest.SKUMetadata.UpdatedBy != null)
                    arParms[21].Value = objSetSKUMetadataRequest.SKUMetadata.UpdatedBy;
                else
                    arParms[21].Value = "SWIFT";
                arParms[21].Direction = ParameterDirection.Input;

                arParms[22] = new SqlParameter("@UpdatedOn", SqlDbType.VarChar, 100);
                arParms[22].Value = objSetSKUMetadataRequest.SKUMetadata.UpdatedOn;
                arParms[22].Direction = ParameterDirection.Input;

                arParms[23] = new SqlParameter("@ErrorMessage", SqlDbType.VarChar, 200);
                arParms[23].Direction = ParameterDirection.Output;


                using (SqlCommand objCMD = new SqlCommand("pTAA_SetSKUMetadata", objCon))
                {
                    objCon.Open();
                    objCMD.CommandType = CommandType.StoredProcedure;
                    objCMD.Connection = objCon;
                    objCMD.Parameters.AddRange(arParms);
                    objCMD.ExecuteNonQuery();
                }

                statusMsg = arParms[23].Value.ToString();



            }

            return statusMsg;


        }

        #endregion

    }
}
