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
    public class SoftwareVersionsBL
    {
        #region Property & Variables

        string connectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        CommonBL objCommonBL;
        #endregion

        #region .ctor
        public SoftwareVersionsBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            objCommonValidationBL = new CommonValidationBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftwareVersionsRequest"></param>
        /// <param name="objSoftwareVersionsResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(GetSoftwareVersionsRequest objSoftwareVersionsRequest, GetSoftwareVersionsResponse objSoftwareVersionsResponse)
        {
            bool ValidationStatus = true;

            if (!objSoftwareVersionsRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objSoftwareVersionsRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objSoftwareVersionsRequest.RequestID, objSoftwareVersionsRequest.RequestingSystem) && ValidationStatus)
            {
                objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if (!objSoftwareVersionsRequest.SKUID.IsValidString())
            {
                objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objSoftwareVersionsResponse.ResponseMessage = "Invalid SKU Format";
                ValidationStatus = false;
            }












            return ValidationStatus;
        }
        #endregion

        #region ProcessRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftwareVersionsRequest"></param>
        /// <param name="objSoftwareVersionsResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(GetSoftwareVersionsRequest objSoftwareVersionsRequest, GetSoftwareVersionsResponse objSoftwareVersionsResponse)
        {
            bool flag = false;

            try
            {
                List<VersionInfoWithImageUrl> versionInfoList;

                // Perform a DB lookup for all versions
                // FIND DETAILS (SOFTWARE & OPTION) FOR GIVEN ID.
                if (objSoftwareVersionsRequest.SKUID.IsValidString())
                {



                    string[] arr = new string[1];
                    arr[0] = objSoftwareVersionsRequest.SKUID;

                    DataSet ds = GetSKUVersionInfo(arr);




                    if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
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


                            if (objSoftwareVersionsRequest.VersionFilter.IsValidString() && objSoftwareVersionsRequest.VersionFilter == "LATEST")
                            {
                                VersionInfoWithImageUrl latestVersion = versionInfoList.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithImageUrl>();
                                if (latestVersion != null)
                                {
                                    versionInfoList.Clear();
                                    versionInfoList.Add(latestVersion);
                                }

                            }
                            else if (objSoftwareVersionsRequest.VersionFilter.IsValidString() && objSoftwareVersionsRequest.VersionFilter != "LATEST"  && !objSoftwareVersionsRequest.VersionFilter.Contains("+"))
                            {
                                VersionInfoWithImageUrl specificVersion = versionInfoList.Where(o => o.Version == objSoftwareVersionsRequest.VersionFilter).FirstOrDefault<VersionInfoWithImageUrl>();
                                if (specificVersion != null)
                                {
                                    versionInfoList.Clear();
                                    versionInfoList.Add(specificVersion);
                                }
                            }
                                //NEW CR
                            #region WITH ADDITION VERSION KEY

                            else if (objSoftwareVersionsRequest.VersionFilter.IsValidString() && objSoftwareVersionsRequest.VersionFilter != "LATEST" && objSoftwareVersionsRequest.VersionFilter.Contains("+"))
                            {
                                versionInfoList.Clear();
                                string strSKUVersion = string.Empty;
                                if (objSoftwareVersionsRequest.VersionFilter.Contains('+'))
                                {
                                    int intindex = objSoftwareVersionsRequest.VersionFilter.LastIndexOf('+');
                                    strSKUVersion = objSoftwareVersionsRequest.VersionFilter.Substring(0, intindex);
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

                            objSoftwareVersionsResponse.AvailableVersion = versionInfoList.ToArray();
                        }

                    }

                }

                if (objSoftwareVersionsResponse.AvailableVersion == null || objSoftwareVersionsResponse.AvailableVersion.Length <= 0)
                {
                    objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                    objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseMessage[2].ToString();
                }
                flag = true;
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

