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
    public class ValidateSKUBL
    {
        #region Property & Variables

        string connectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        CommonBL objCommonBL;
        #endregion

        #region .ctor
        public ValidateSKUBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            objCommonValidationBL = new CommonValidationBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objValidateSKURequest"></param>
        /// <param name="objValidateSKUResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(ValidateSKURequest objValidateSKURequest, ValidateSKUResponse objValidateSKUResponse)
        {
            bool ValidationStatus = true;
            Int64 countSN = Convert.ToInt64((ConfigurationSettings.AppSettings["countSN"]));
            if (!objValidateSKURequest.RequestID.IsValidString() && ValidationStatus)
            {
                objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objValidateSKUResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objValidateSKURequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objValidateSKUResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objValidateSKURequest.RequestID, objValidateSKURequest.RequestingSystem) && ValidationStatus)
            {
                objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objValidateSKUResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if ((objValidateSKURequest.SKUID == null || objValidateSKURequest.SKUID.Length < 1 || objValidateSKURequest.SKUID.Length > countSN) && ValidationStatus)  //VALIDATE SNUM UNIT (MAX 100)
            {
                objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objValidateSKUResponse.ResponseMessage = "Invalid SKU Details";
                ValidationStatus = false;
            }
            if (objValidateSKURequest.SKUID != null && ValidationStatus)
            {
                for (int j = 0; j < objValidateSKURequest.SKUID.Length; j++)
                {
                    if (!objValidateSKURequest.SKUID[j].SKUID.IsValidString() && ValidationStatus)
                    {
                        objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objValidateSKUResponse.ResponseMessage = "Invalid SKU Format";
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
        /// <param name="objValidateSKURequest"></param>
        /// <param name="objValidateSKUResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(ValidateSKURequest objValidateSKURequest, ValidateSKUResponse objValidateSKUResponse)
        {
            bool flag = false;

            try
            {
                List<VersionInfoWithImageUrl> versionInfoList;

                // Perform a DB lookup for all versions
                // FIND DETAILS (SOFTWARE & OPTION) FOR GIVEN ID.
                for (int j = 0; j < objValidateSKURequest.SKUID.Length; j++)
                {
                    if (objValidateSKURequest.SKUID[j].SKUID.IsValidString())
                    {

                        string[] arr = new string[1];
                        arr[0] = objValidateSKURequest.SKUID[j].SKUID;

                        DataSet ds = GetSKUVersionInfo(arr);




                        if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {

                            if (ds.Tables[0].Rows[0].ItemArray[1].ToString() == "SW")
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    versionInfoList = new List<VersionInfoWithImageUrl>();

                                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                    {

                                        DataRow dr = null;
                                        string currentSKUID = string.Empty;
                                        string previousSKUID = string.Empty;
                                        dr = ds.Tables[0].Rows[i];


                                        VersionInfoWithImageUrl objVersionInfoWithImageUrl = new VersionInfoWithImageUrl();
                                        objVersionInfoWithImageUrl.IsShippedVersion = (dr["IsShippedVersion"] != DBNull.Value) ? Convert.ToBoolean(dr["IsShippedVersion"]) : false;
                                        objVersionInfoWithImageUrl.ReleaseDateSpecified = false;
                                        objVersionInfoWithImageUrl.ReleaseDate = (dr["ReleaseDate"] != DBNull.Value) ? Convert.ToDateTime(dr["ReleaseDate"]) : DateTime.MinValue;
                                        if (objVersionInfoWithImageUrl.ReleaseDate != DateTime.MinValue)
                                            objVersionInfoWithImageUrl.ReleaseDateSpecified = true;
                                        objVersionInfoWithImageUrl.ImageUrl = (dr["ImageUrl"] != DBNull.Value) ? Convert.ToString(dr["ImageUrl"]) : "";

                                        objVersionInfoWithImageUrl.Version = (dr["Version"] != DBNull.Value) ? Convert.ToString(dr["Version"]) : "";
                                        objVersionInfoWithImageUrl.VersionSeqNo = (dr["VersionSeqNo"] != DBNull.Value) ? Convert.ToInt32(dr["VersionSeqNo"]) : -1;
                                        objVersionInfoWithImageUrl.VersionType = (dr["VersionType"] != DBNull.Value) ? Convert.ToString(dr["VersionType"]) : "";
                                        objVersionInfoWithImageUrl.VersionReleaseClassification = (dr["VersionReleaseClassification"] != DBNull.Value) ? Convert.ToString(dr["VersionReleaseClassification"]) : "";

                                        versionInfoList.Add(objVersionInfoWithImageUrl);
                                    }

                                    if (objValidateSKURequest.SKUID[j].VersionFilter.IsValidString() && objValidateSKURequest.SKUID[j].VersionFilter == "LATEST")
                                    {
                                        VersionInfoWithImageUrl latestVersion = versionInfoList.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithImageUrl>();
                                        if (latestVersion != null)
                                        {
                                            versionInfoList.Clear();
                                            versionInfoList.Add(latestVersion);
                                        }
                                    }
                                    else if (objValidateSKURequest.SKUID[j].VersionFilter.IsValidString() && objValidateSKURequest.SKUID[j].VersionFilter != "LATEST" && !objValidateSKURequest.SKUID[j].VersionFilter.Contains("+"))
                                    {
                                        VersionInfoWithImageUrl specificVersion = versionInfoList.Where(o => o.Version == objValidateSKURequest.SKUID[j].VersionFilter).FirstOrDefault<VersionInfoWithImageUrl>();
                                        if (specificVersion != null)
                                        {
                                            versionInfoList.Clear();
                                            versionInfoList.Add(specificVersion);
                                        }
                                        else
                                        {
                                            //objSoftwareVersionsResponse.SKUID[j].AvailableVersion = null;
                                            objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                                            objValidateSKUResponse.SKUID[j].ResponseMessage = "The Filter Value entered : " + objValidateSKURequest.SKUID[j].VersionFilter + "  is not Valid or unavilable . The list is shown below (only for SW features) :";
                                            objValidateSKUResponse.SKUID[j].AvailableVersion = null;
                                            //versionInfoList[j] = null;
                                        }
                                    }
                                        //NEW CR
                                    #region WITH ADDITION VERSION KEY
                                    else if (objValidateSKURequest.SKUID[j].VersionFilter.IsValidString() && objValidateSKURequest.SKUID[j].VersionFilter != "LATEST" && objValidateSKURequest.SKUID[j].VersionFilter.Contains("+"))
                                    {
                                        versionInfoList.Clear();
                                        string strSKUVersion = string.Empty;
                                        if (objValidateSKURequest.SKUID[j].VersionFilter.Contains('+'))
                                        {
                                            int intindex = objValidateSKURequest.SKUID[j].VersionFilter.LastIndexOf('+');
                                            strSKUVersion = objValidateSKURequest.SKUID[j].VersionFilter.Substring(0, intindex);
                                        }  
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            DataRow dr = null;
                                            dr = ds.Tables[0].Rows[i];

                                            VersionInfoWithImageUrl objVersionInfoWithImageUrl = new VersionInfoWithImageUrl();
                                            objVersionInfoWithImageUrl.IsShippedVersion = (dr["IsShippedVersion"] != DBNull.Value) ? Convert.ToBoolean(dr["IsShippedVersion"]) : false;
                                            objVersionInfoWithImageUrl.ReleaseDateSpecified = false;
                                            objVersionInfoWithImageUrl.ReleaseDate = (dr["ReleaseDate"] != DBNull.Value) ? Convert.ToDateTime(dr["ReleaseDate"]) : DateTime.MinValue;
                                            if (objVersionInfoWithImageUrl.ReleaseDate != DateTime.MinValue)
                                                objVersionInfoWithImageUrl.ReleaseDateSpecified = true;
                                            objVersionInfoWithImageUrl.ImageUrl = (dr["ImageUrl"] != DBNull.Value) ? Convert.ToString(dr["ImageUrl"]) : "";
                                            objVersionInfoWithImageUrl.Version = (dr["Version"] != DBNull.Value) ? Convert.ToString(dr["Version"]) : "";
                                            objVersionInfoWithImageUrl.VersionSeqNo = (dr["VersionSeqNo"] != DBNull.Value) ? Convert.ToInt32(dr["VersionSeqNo"]) : -1;
                                            objVersionInfoWithImageUrl.VersionType = (dr["VersionType"] != DBNull.Value) ? Convert.ToString(dr["VersionType"]) : "";
                                            objVersionInfoWithImageUrl.VersionReleaseClassification = (dr["VersionReleaseClassification"] != DBNull.Value) ? Convert.ToString(dr["VersionReleaseClassification"]) : "";


                                            int intResult = objVersionInfoWithImageUrl.Version.CompareTo(strSKUVersion);
                                            if (intResult == 0 || intResult == 1)
                                            {
                                                versionInfoList.Add(objVersionInfoWithImageUrl);
                                            }
                                        }
                                    }
                                    #endregion


                                    objValidateSKUResponse.SKUID[j].AvailableVersion = versionInfoList.ToArray();



                                }

                                if (objValidateSKUResponse.SKUID[j].AvailableVersion == null || objValidateSKUResponse.SKUID[j].AvailableVersion.Length <= 0)
                                {
                                    objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                    objValidateSKUResponse.SKUID[j].ResponseMessage = Constants.ResponseMessage[2].ToString();
                                }
                                flag = true;
                            }

                            else
                            {
                                objValidateSKUResponse.SKUID[j].AvailableVersion = null;
                                if ((ds.Tables[0].Rows.Count != 0) && (objValidateSKURequest.SKUID[j].VersionFilter == null))
                                {
                                    //for (int i = 0; i < objSoftwareVersionsRequest.SKUID[j].; i++)
                                    //{
                                    DataRow dr1 = null;
                                    dr1 = ds.Tables[0].Rows[0];
                                    objValidateSKUResponse.SKUID[j].SKUID = (dr1["SKUID"] != DBNull.Value) ? Convert.ToString(dr1["SKUID"]) : "";
                                    objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.Success;
                                    objValidateSKUResponse.SKUID[j].ResponseMessage = "The Option SKU " + objValidateSKUResponse.SKUID[j].SKUID + " is Valid.";
                                    // objSoftwareVersionsResponse.SKUID[j].SKUID = null;

                                    //}
                                }
                                else
                                {
                                    if (objValidateSKURequest.SKUID[j].VersionFilter != null)
                                    {
                                        objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                        objValidateSKUResponse.SKUID[j].ResponseMessage = "Filter cannot be used for an Option SKU : " + objValidateSKUResponse.SKUID[j].SKUID + " . Please try without using filter.";
                                    }
                                    else
                                    {

                                        if (objValidateSKUResponse.SKUID[0].AvailableVersion == null || objValidateSKUResponse.SKUID[0].AvailableVersion.Length <= 0)
                                        {
                                            objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                            objValidateSKUResponse.SKUID[j].ResponseMessage = Constants.ResponseMessage[2].ToString();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            objValidateSKUResponse.SKUID[j].ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                            objValidateSKUResponse.SKUID[j].ResponseMessage = Constants.ResponseMessage[2].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return flag;
        }
        #endregion

        #region GetSKUVersionInfo
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skuList"></param>
        public DataSet GetSKUVersionInfo(string[] sKUID)
        {
            DataSet skuDataSet = new DataSet();
            objCommonBL = new CommonBL();

            //THIS WILL RETURN ALL DETAILS WITH GENERATED LICENCEKEY
            try
            {
                //GET ALL SKU DATA
                skuDataSet = objCommonBL.GetSKUDetailInfoData(sKUID, string.Empty);

            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While GetSKUVersionInfo. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return skuDataSet;
        }
        #endregion

    }
}

