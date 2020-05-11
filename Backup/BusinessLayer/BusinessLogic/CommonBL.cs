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
    public class CommonBL
    {
        #region Property & Variables
        string connectionString = string.Empty;
        string TandbergCAPrimary = string.Empty;
        string TandbergCASecondary = string.Empty;
        LicenseBL objBL;
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        #endregion

        #region .ctor
        public CommonBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            TandbergCAPrimary = ConfigurationSettings.AppSettings["TandbergCAPrimary"];
            TandbergCASecondary = ConfigurationSettings.AppSettings["TandbergCASecondary"];
            objTransactionLogger = new TransactionLogger();
            objEventLogger = new EventLogger();

        }
        #endregion

        #region GetSKUListForGivenSNum
        /// <summary>
        /// Get SKU List for Given SNum
        /// </summary>
        /// <param name="sNum"></param>
        public SKUInfo[] GetSKUListForGivenSNum(string sNum, string versionFilter)
        {
            List<SKUInfo> skuList = new List<SKUInfo>();
            string versionString = "LATEST";
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_GetSKUListForSNum";
                    SqlParameter[] arParms = new SqlParameter[1];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = sNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    using (SqlCommand objCMD = new SqlCommand(strSQLCommand, objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        SqlDataReader sdr = objCMD.ExecuteReader();

                        if (!string.IsNullOrEmpty(versionFilter) && Constants.VersionFilter.Contains(versionFilter))
                        {
                            versionString = versionFilter;
                        }

                        while (sdr.Read())
                        {
                            if (sdr["SKUID"] != DBNull.Value)
                            {
                                // FOR UPGRADE REQUEST ONLY LATEST VERSION  LICENSE KEY NEED TO RETURN
                                skuList.Add(new SKUInfo() { SKUID = Convert.ToString(sdr["SKUID"]), QtySpecified = false, VersionFilter = versionString });
                            }
                        }
                        sdr.Close();
                    }

                }

            }
            catch (Exception ex)
            {
                throw new Exception("Get SKU List for Given SNum Failed. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return skuList.ToArray<SKUInfo>();
        }
        #endregion

        #region GetSKUDetailInfoData
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sKUID"></param>
        /// <param name="sNum"></param>
        /// <returns></returns>
        public DataSet GetSKUDetailInfoData(string[] sKUID, string sNum)
        {
            DataSet ds = new DataSet();
            try
            {
                //BUILD PARAMETER STRING
                string xmlParameter = Util.BuildXmlString("SKUID", sKUID);
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_GetSKUAllInformation";
                    SqlParameter[] arParms = new SqlParameter[2];

                    arParms[0] = new SqlParameter("@SKUID", SqlDbType.Xml);
                    arParms[0].Value = xmlParameter;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[1].Value = sNum;
                    arParms[1].Direction = ParameterDirection.Input;


                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Retriving SKU Details from Database Failed. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return ds;
        }
        #endregion

        #region GetSKUDetailInfo
        /// <summary>
        /// Build SKU Details Info Object for all SKU ID
        /// </summary>
        /// <param name="sKUID"></param>
        public SKUDetailInfo[] GetSKUDetailInfo(string[] sKUID, string licensableSNum)
        {
            List<SKUDetailInfo> skuDetailInfoList = new List<SKUDetailInfo>();
            DataSet skuDataSet = new DataSet();
            SKUDetailInfo skuDetailInfo;
            List<VersionInfoWithLicense> skuVersionInfoList;

            try
            {
                //get all data
                skuDataSet = GetSKUDetailInfoData(sKUID, licensableSNum);

                if (skuDataSet != null && skuDataSet.Tables != null && skuDataSet.Tables.Count > 0)
                {
                    if (skuDataSet.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < skuDataSet.Tables[0].Rows.Count; i++)
                        {

                            DataRow sdr = null;
                            string currentSKUID = string.Empty;
                            string previousSKUID = string.Empty;
                            sdr = skuDataSet.Tables[0].Rows[i];

                            if (sdr["SKUID"] != DBNull.Value && sdr["SKUType"] != DBNull.Value)
                            {
                                currentSKUID = Convert.ToString(sdr["SKUID"]);
                                previousSKUID = Convert.ToString(sdr["SKUID"]);

                                skuDetailInfo = new SKUDetailInfo();

                                //BUILD OBJECT FROM DB
                                skuDetailInfo.SKUID = Convert.ToString(sdr["SKUID"]);
                                skuDetailInfo.SKUName = (sdr["SKUName"] != DBNull.Value) ? Convert.ToString(sdr["SKUName"]) : "";
                                skuDetailInfo.TAAPartNo = (sdr["TAAPartNo"] != DBNull.Value) ? Convert.ToString(sdr["TAAPartNo"]) : "";
                                skuDetailInfo.TAAPartName = (sdr["TAAPartName"] != DBNull.Value) ? Convert.ToString(sdr["TAAPartName"]) : "";
                                skuDetailInfo.SKUType = (sdr["SKUType"] != DBNull.Value) ? Convert.ToString(sdr["SKUType"]) : "";
                                skuDetailInfo.Qty = (sdr["Qty"] != DBNull.Value) ? Convert.ToInt32(sdr["Qty"]) : -1;
                                skuDetailInfo.QtySpecified = false;
                                if (skuDetailInfo.Qty != -1)
                                    skuDetailInfo.QtySpecified = true;

                                skuDetailInfo.UpdatedTimestampSpecified = true;
                                if (skuDetailInfo.SKUType == Constants.SKUType.OPTION.ToString())
                                {
                                    if (skuDetailInfo.UpdatedTimestamp == DateTime.MinValue)
                                    {
                                        skuDetailInfo.UpdatedTimestamp = (sdr["UpdatedOn"] != DBNull.Value) ? Convert.ToDateTime(sdr["UpdatedOn"]) : DateTime.MinValue;
                                        skuDetailInfo.UpdatedTimestampSpecified = true;
                                    }
                                    else
                                    {
                                        skuDetailInfo.UpdatedTimestamp = Convert.ToDateTime((sdr["UpdatedOn"]));
                                        skuDetailInfo.UpdatedTimestampSpecified = true;
                                    }
                                }

                                skuDetailInfo.OptionLicenseKey = (sdr["OptionLicenseKey"] != DBNull.Value) ? Convert.ToString(sdr["OptionLicenseKey"]) : "";

                                if (skuDetailInfo.SKUType == Constants.SKUType.SW.ToString())
                                {
                                    skuVersionInfoList = new List<VersionInfoWithLicense>();
                                    while (previousSKUID == currentSKUID)
                                    {
                                        VersionInfoWithLicense versionObject = new VersionInfoWithLicense();
                                        versionObject.VersionPartNo = (sdr["VersionPartNo"] != DBNull.Value) ? Convert.ToString(sdr["VersionPartNo"]) : "";
                                        versionObject.VersionPartName = (sdr["VersionPartName"] != DBNull.Value) ? Convert.ToString(sdr["VersionPartName"]) : "";
                                        versionObject.VersionSeqNo = (sdr["VersionSeqNo"] != DBNull.Value) ? Convert.ToInt32(sdr["VersionSeqNo"]) : -1;
                                        versionObject.Version = (sdr["Version"] != DBNull.Value) ? Convert.ToString(sdr["Version"]) : "";
                                        versionObject.VersionType = (sdr["VersionType"] != DBNull.Value) ? Convert.ToString(sdr["VersionType"]) : "";
                                        versionObject.VersionReleaseClassification = (sdr["VersionReleaseClassification"] != DBNull.Value) ? Convert.ToString(sdr["VersionReleaseClassification"]) : "";
                                        versionObject.IsShippedVersion = (sdr["IsShippedVersion"] != DBNull.Value) ? Convert.ToBoolean(sdr["IsShippedVersion"]) : false;
                                        versionObject.LicenseKey = (sdr["LicenseKey"] != DBNull.Value) ? Convert.ToString(sdr["LicenseKey"]) : "";
                                        versionObject.ReleaseDateSpecified = false;
                                        versionObject.ReleaseDate = (sdr["ReleaseDate"] != DBNull.Value) ? Convert.ToDateTime(sdr["ReleaseDate"]) : DateTime.MinValue;
                                        if (versionObject.ReleaseDate != DateTime.MinValue)
                                            versionObject.ReleaseDateSpecified = true;

                                        versionObject.VersionUpgradedOnSpecified = true;
                                        versionObject.VersionUpgradedOn = (sdr["VersionUpgradedDate"] != DBNull.Value) ? Convert.ToDateTime(sdr["VersionUpgradedDate"]) : DateTime.MinValue;
                                        if (versionObject.VersionUpgradedOn != DateTime.MinValue)
                                            versionObject.VersionUpgradedOnSpecified = true;


                                        versionObject.ImageUrl = (sdr["ImageUrl"] != DBNull.Value) ? Convert.ToString(sdr["ImageUrl"]) : "";

                                        skuDetailInfo.UpdatedTimestampSpecified = true;
                                        bool del = (sdr["Deleted"] != DBNull.Value) ? Convert.ToBoolean(sdr["Deleted"]) : true;
                                        if (del == false)
                                        {

                                            if (skuDetailInfo.UpdatedTimestamp == DateTime.MinValue)
                                            {
                                                skuDetailInfo.UpdatedTimestamp = (sdr["UpdatedOn"] != DBNull.Value) ? Convert.ToDateTime(sdr["UpdatedOn"]) : DateTime.MinValue;
                                                skuDetailInfo.UpdatedTimestampSpecified = true;
                                            }
                                            else
                                            {
                                                skuDetailInfo.UpdatedTimestamp = Convert.ToDateTime((sdr["UpdatedOn"]));
                                                skuDetailInfo.UpdatedTimestampSpecified = true;
                                            }
                                        }

                                        skuVersionInfoList.Add(versionObject);

                                        i++;
                                        if (i < skuDataSet.Tables[0].Rows.Count)
                                        {
                                            sdr = skuDataSet.Tables[0].Rows[i];
                                            currentSKUID = Convert.ToString(sdr["SKUID"]);
                                        }
                                        else
                                            currentSKUID = string.Empty;

                                    }
                                    // if come out reset to original place
                                    i--;
                                    skuDetailInfo.VersionList = skuVersionInfoList.ToArray();

                                }
                                skuDetailInfoList.Add(skuDetailInfo);

                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Building SKU Details Failed. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return skuDetailInfoList.ToArray();
        }

        #endregion

        #region GetSKUDetailInfoWithLatestVersionLicense
        /// <summary>
        /// Build SKU Details Info Object for all SKU ID and Gets latest Version License if missing
        /// </summary>
        /// <param name="sKUID"></param>
        /// /*new CR user logging 08/2013*/
        public SKUDetailInfo[] GetSKUDetailInfoWithLatestVersionLicense(string[] sKUID, string licensableSNum,string requestingSystem)
        {
            List<SKUDetailInfo> skuDetailInfoList = new List<SKUDetailInfo>();
            DataSet skuDataSet = new DataSet();
            SKUDetailInfo skuDetailInfo;
            List<VersionInfoWithLicense> skuVersionInfoList;

            try
            {
                //get all data
                skuDataSet = GetSKUDetailInfoData(sKUID, licensableSNum);

                if (skuDataSet != null && skuDataSet.Tables != null && skuDataSet.Tables.Count > 0)
                {
                    if (skuDataSet.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < skuDataSet.Tables[0].Rows.Count; i++)
                        {
                            DataRow sdr = null;
                            string currentSKUID = string.Empty;
                            string previousSKUID = string.Empty;
                            sdr = skuDataSet.Tables[0].Rows[i];

                            if (sdr["SKUID"] != DBNull.Value && sdr["SKUType"] != DBNull.Value)
                            {
                                currentSKUID = Convert.ToString(sdr["SKUID"]);
                                previousSKUID = Convert.ToString(sdr["SKUID"]);

                                skuDetailInfo = new SKUDetailInfo();

                                //BUILD OBJECT FROM DB
                                skuDetailInfo.SKUID = Convert.ToString(sdr["SKUID"]);
                                skuDetailInfo.SKUName = (sdr["SKUName"] != DBNull.Value) ? Convert.ToString(sdr["SKUName"]) : "";
                                skuDetailInfo.TAAPartNo = (sdr["TAAPartNo"] != DBNull.Value) ? Convert.ToString(sdr["TAAPartNo"]) : "";
                                skuDetailInfo.TAAPartName = (sdr["TAAPartName"] != DBNull.Value) ? Convert.ToString(sdr["TAAPartName"]) : "";
                                skuDetailInfo.SKUType = (sdr["SKUType"] != DBNull.Value) ? Convert.ToString(sdr["SKUType"]) : "";
                                skuDetailInfo.Qty = (sdr["Qty"] != DBNull.Value) ? Convert.ToInt32(sdr["Qty"]) : -1;
                                skuDetailInfo.QtySpecified = false;
                                if (skuDetailInfo.Qty != -1)
                                    skuDetailInfo.QtySpecified = true;

                                skuDetailInfo.OptionLicenseKey = (sdr["OptionLicenseKey"] != DBNull.Value) ? Convert.ToString(sdr["OptionLicenseKey"]) : "";

                                if (skuDetailInfo.SKUType == Constants.SKUType.SW.ToString())
                                {
                                    skuVersionInfoList = new List<VersionInfoWithLicense>();
                                    while (previousSKUID == currentSKUID)
                                    {
                                        VersionInfoWithLicense versionObject = new VersionInfoWithLicense();
                                        versionObject.VersionPartNo = (sdr["VersionPartNo"] != DBNull.Value) ? Convert.ToString(sdr["VersionPartNo"]) : "";
                                        versionObject.VersionPartName = (sdr["VersionPartName"] != DBNull.Value) ? Convert.ToString(sdr["VersionPartName"]) : "";
                                        versionObject.VersionSeqNo = (sdr["VersionSeqNo"] != DBNull.Value) ? Convert.ToInt32(sdr["VersionSeqNo"]) : -1;
                                        versionObject.Version = (sdr["Version"] != DBNull.Value) ? Convert.ToString(sdr["Version"]) : "";
                                        versionObject.VersionType = (sdr["VersionType"] != DBNull.Value) ? Convert.ToString(sdr["VersionType"]) : "";
                                        versionObject.VersionReleaseClassification = (sdr["VersionReleaseClassification"] != DBNull.Value) ? Convert.ToString(sdr["VersionReleaseClassification"]) : "";
                                        versionObject.IsShippedVersion = (sdr["IsShippedVersion"] != DBNull.Value) ? Convert.ToBoolean(sdr["IsShippedVersion"]) : false;
                                        versionObject.LicenseKey = (sdr["LicenseKey"] != DBNull.Value) ? Convert.ToString(sdr["LicenseKey"]) : "";
                                        versionObject.ReleaseDateSpecified = false;
                                        versionObject.ReleaseDate = (sdr["ReleaseDate"] != DBNull.Value) ? Convert.ToDateTime(sdr["ReleaseDate"]) : DateTime.MinValue;
                                        if (versionObject.ReleaseDate != DateTime.MinValue)
                                            versionObject.ReleaseDateSpecified = true;

                                        versionObject.VersionUpgradedOnSpecified = true;
                                        versionObject.VersionUpgradedOn = (sdr["VersionUpgradedDate"] != DBNull.Value) ? Convert.ToDateTime(sdr["VersionUpgradedDate"]) : DateTime.MinValue;
                                        if (versionObject.VersionUpgradedOn != DateTime.MinValue)
                                            versionObject.VersionUpgradedOnSpecified = true;


                                        versionObject.ImageUrl = (sdr["ImageUrl"] != DBNull.Value) ? Convert.ToString(sdr["ImageUrl"]) : "";

                                        skuVersionInfoList.Add(versionObject);


                                        i++;
                                        if (i < skuDataSet.Tables[0].Rows.Count)
                                        {
                                            sdr = skuDataSet.Tables[0].Rows[i];
                                            currentSKUID = Convert.ToString(sdr["SKUID"]);
                                        }
                                        else
                                        {
                                            currentSKUID = "";
                                            i--;
                                        }
                                    }
                                    VersionInfoWithLicense latestVersion = skuVersionInfoList.OrderByDescending(obj => obj.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();
                                    if (latestVersion != null)
                                    {
                                        skuVersionInfoList = new List<VersionInfoWithLicense>();

                                        if (string.IsNullOrEmpty(latestVersion.LicenseKey))
                                        {
                                            //call get license
                                            //GetLicenseKey(string SNum, string SKUID, string SKUType, int Qty, string Version, DateTime ExpiryDate);
                                            /*new CR user logging 08/2013*/
                                            latestVersion.LicenseKey = GetLicenseKey(licensableSNum, latestVersion.VersionPartName, skuDetailInfo.SKUType, skuDetailInfo.Qty, 1, latestVersion.Version, DateTime.MinValue,requestingSystem);

                                        }
                                        skuVersionInfoList.Add(latestVersion);
                                    }
                                    skuDetailInfo.VersionList = skuVersionInfoList.ToArray();

                                }

                                skuDetailInfoList.Add(skuDetailInfo);
                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                throw new Exception("Building SKU Details Failed. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return skuDetailInfoList.ToArray();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <param name="Qty"></param>
        /// <param name="Version"></param>
        /// <param name="ExpiryDate"></param>
        public string GetLicenseKey(string SNum, string SKUID, string SKUType, int Qty, int Index, string Version, DateTime ExpiryDate, string requestingSystem)
        {
            int HistoricQty = 0;
            return GetLicenseKey(SNum, SKUID, SKUType, Qty, Index, Version, ExpiryDate, 0, out HistoricQty, requestingSystem);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <param name="Qty"></param>
        /// <param name="Version"></param>
        /// <param name="ExpiryDate"></param>
        ///  /*new CR user logging 08/2013*/
        public string GetLicenseKey(string SNum, string SKUID, string SKUType, int Qty, int Index, string Version, DateTime ExpiryDate, int ShippedVersion, string requestingSystem)
        {
            int HistoricQty = 0;
            return GetLicenseKey(SNum, SKUID, SKUType, Qty, Index, Version, ExpiryDate, ShippedVersion, out HistoricQty,requestingSystem);
        }
        #region GetLicenseKey
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <param name="Qty"></param>
        /// <param name="Version"></param>
        /// <param name="ExpiryDate"></param>
        ///  /*new CR user logging 08/2013*/
        public string GetLicenseKey(string SNum, string SKUID, string SKUType, int Qty, int Index, string Version, DateTime ExpiryDate, int ShippedVersion, out int HistoricQty,string requestingSystem)
        {
            string licenseKey = string.Empty;
            DFLicenseRequest objDFLicenseRequest = new DFLicenseRequest();
            DFLicenseResponse objDFLicenseResponse = new DFLicenseResponse();

            objDFLicenseRequest.RequestID = Guid.NewGuid().ToString();
            objDFLicenseRequest.RequestingSystem = requestingSystem;//"INERNAL-REQ";
            objDFLicenseRequest.RequestDateTime = DateTime.UtcNow;
            HistoricQty = 0;
            objDFLicenseRequest.SNum = SNum;
            objDFLicenseRequest.SKUID = SKUID;
            if (Qty > 0)
            {
                objDFLicenseRequest.Qty = Qty;
                objDFLicenseRequest.QtySpecified = true;
            }
            if (Version.IsValidString())
                objDFLicenseRequest.Version = Version;

            if (ExpiryDate != null && ExpiryDate != DateTime.Parse("1/1/0001"))
            {
                objDFLicenseRequest.ExpDate = ExpiryDate;
                objDFLicenseRequest.ExpDateSpecified = true;
            }
            //Commented as this was stopping from creating key for Allow Many =1
            //if (SKUType == Constants.SKUType.OPTION.ToString() && Index >= 1)
                if (SKUType == Constants.SKUType.OPTION.ToString() && Index > 1)
                {
                    objDFLicenseRequest.Index = Index;
                    objDFLicenseRequest.IndexSpecified = true;
                }
            objDFLicenseResponse = GenerateNewLicense(objDFLicenseRequest, ShippedVersion, out HistoricQty);
            if (objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success && !string.IsNullOrEmpty(objDFLicenseResponse.LicenseKey))
            {
                licenseKey = objDFLicenseResponse.LicenseKey;
            }
            else
            {
                //Changes made for Defect 2848
                if (objDFLicenseRequest.AlgType == "12")
                {
                    licenseKey = "Request with SKU: " + objDFLicenseRequest.SKUID + " .SKU Count has exceeded Key Limit.";
                }
                else
                {
                    licenseKey = "Fail to Generate Key";
                }
                //End of changes for Defect 2848
            }


            return licenseKey;
        }
        #endregion

        #region GetLicenseKey5153
        /// <summary>
        /// Used for algorithm 51 and 53
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <param name="SKUType"></param>
        /// <param name="Qty"></param>
        /// <param name="Index"></param>
        /// <param name="Version"></param>
        /// <param name="ExpiryDate"></param>
        /// <param name="ShippedVersion"></param>
        /// <param name="HistoricQty"></param>
        /// <returns></returns>
        ///  /*new CR user logging 08/2013*/
        public string GetLicenseKey5153(string SNum, string SKUID, string SKUType, int Qty, int Index, string Version, DateTime ExpiryDate, int ShippedVersion, string[] MacAddr, out int HistoricQty,string requestingSystem)
        {
            string licenseKey = string.Empty;
            DFLicenseRequest objDFLicenseRequest = new DFLicenseRequest();
            DFLicenseResponse objDFLicenseResponse = new DFLicenseResponse();

            objDFLicenseRequest.RequestID = Guid.NewGuid().ToString();
             /*new CR user logging 08/2013*/
            objDFLicenseRequest.RequestingSystem = requestingSystem;//"INERNAL-REQ";
            objDFLicenseRequest.RequestDateTime = DateTime.UtcNow;
            HistoricQty = 0;
            objDFLicenseRequest.SNum = SNum;
            objDFLicenseRequest.SKUID = SKUID;
            if (Qty > 0)
            {
                objDFLicenseRequest.Qty = Qty;
                objDFLicenseRequest.QtySpecified = true;
            }
            if (Version.IsValidString())
                objDFLicenseRequest.Version = Version;

            if (ExpiryDate != null && ExpiryDate != DateTime.Parse("1/1/0001"))
            {
                objDFLicenseRequest.ExpDate = ExpiryDate;
                objDFLicenseRequest.ExpDateSpecified = true;
            }
            if (SKUType == Constants.SKUType.OPTION.ToString() && Index > 1)
            {
                objDFLicenseRequest.Index = Index;
                objDFLicenseRequest.IndexSpecified = true;
            }

            if (MacAddr != null)
            {
                objDFLicenseRequest.MacAddress = MacAddr.Min();
            }

            objDFLicenseResponse = GenerateNewLicense(objDFLicenseRequest, ShippedVersion, out HistoricQty);
            if (objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success && !string.IsNullOrEmpty(objDFLicenseResponse.LicenseKey))
            {
                licenseKey = objDFLicenseResponse.LicenseKey;
            }
            else
            {
                licenseKey = "Fail to Generate Key";
            }


            return licenseKey;
        }
        #endregion

        #region GenerateNewLicense
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objDFLicenseRequest"></param>
        /// <returns></returns>
        private DFLicenseResponse GenerateNewLicense(DFLicenseRequest objDFLicenseRequest, int ShippedVersion, out int HistoricQty)
        {
            DFLicenseResponse objDFLicenseResponse = new DFLicenseResponse();
            objDFLicenseResponse.RequestID = objDFLicenseRequest.RequestID;
            objDFLicenseResponse.ResponseID = objDFLicenseRequest.RequestID;
            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objBL = new LicenseBL();
            HistoricQty = 0;
            bool flag = ShippedVersion == 0 ? false : true;

            try
            {
                objBL.GetLicense(objDFLicenseRequest, objDFLicenseResponse, false, flag);
                HistoricQty = objBL.HistoricQty;
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();
                objDFLicenseResponse.LicenseKey = "";

                //LOG EXCEPTION
                objEventLogger.WriteLog("GetLicense:" + ex.Message + Environment.NewLine + ex.StackTrace, objDFLicenseRequest.RequestingSystem, DateTime.UtcNow, objDFLicenseRequest.RequestID);
            }
            finally
            {
                objDFLicenseResponse.ResponseDateTime = DateTime.UtcNow;

                //SERIALIZE REQUEST/RESPONSE
                string request = "GetLicenseRequest";
                string response = "GetLicenseResponse";

                try
                {
                    request = Util.SerializeObject(objDFLicenseRequest);
                    response = Util.SerializeObject(objDFLicenseResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("GetLicense:Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objDFLicenseRequest.RequestingSystem, DateTime.UtcNow, objDFLicenseRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "GetLicenseRequest";
                if (!response.IsValidString())
                    response = "GetLicenseResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objDFLicenseRequest.RequestID, objDFLicenseRequest.RequestDateTime, objDFLicenseRequest.RequestingSystem,
                    request, objDFLicenseResponse.ResponseID, objDFLicenseResponse.ResponseDateTime, response,
                    objDFLicenseResponse.ResponseStatus, objDFLicenseResponse.ResponseMessage, 0);

                //End Processing GetLicense
            }
            return objDFLicenseResponse;
        }
        #endregion
        //Code for RMASwap CR
        #region RMASWap 
          /*new CR user logging 08/2013*/
        public string CallRMASwapLicense(string SNum, string SKUID, string SKUType, int Qty, int Index, string Version, DateTime ExpiryDate, int ShippedVersion,string requestingSystem)
        {
            string licenseKey = string.Empty;
            DFLicenseRequest objDFLicenseRequest = new DFLicenseRequest();
            DFLicenseResponse objDFLicenseResponse = new DFLicenseResponse();

            objDFLicenseRequest.RequestID = Guid.NewGuid().ToString();
            objDFLicenseRequest.RequestingSystem = requestingSystem;//"INERNAL-REQ";
            objDFLicenseRequest.RequestDateTime = DateTime.UtcNow;
            objDFLicenseRequest.SNum = SNum;
            objDFLicenseRequest.SKUID = SKUID;

            if (Qty > 0)
            {
                objDFLicenseRequest.Qty = Qty;
                objDFLicenseRequest.QtySpecified = true;
            }
            if (Version.IsValidString())
                objDFLicenseRequest.Version = Version;

            if (ExpiryDate != null && ExpiryDate != DateTime.Parse("1/1/0001"))
            {
                objDFLicenseRequest.ExpDate = ExpiryDate;
                objDFLicenseRequest.ExpDateSpecified = true;
            }
            if (Index > 0)
            {
                objDFLicenseRequest.Index = Index;
                objDFLicenseRequest.IndexSpecified = true;
            }


            objDFLicenseResponse.RequestID = objDFLicenseRequest.RequestID;
            objDFLicenseResponse.ResponseID = objDFLicenseRequest.RequestID;
            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objBL = new LicenseBL();

            try
            {
                objDFLicenseResponse = objBL.GetRMALicense(objDFLicenseRequest, objDFLicenseResponse);

            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();
                objDFLicenseResponse.LicenseKey = "";

                //LOG EXCEPTION
                objEventLogger.WriteLog("GetLicense:" + ex.Message + Environment.NewLine + ex.StackTrace, objDFLicenseRequest.RequestingSystem, DateTime.UtcNow, objDFLicenseRequest.RequestID);
            }
            finally
            {
                objDFLicenseResponse.ResponseDateTime = DateTime.UtcNow;

                //SERIALIZE REQUEST/RESPONSE
                string request = "GetLicenseRequest";
                string response = "GetLicenseResponse";

                try
                {
                    request = Util.SerializeObject(objDFLicenseRequest);
                    response = Util.SerializeObject(objDFLicenseResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("GetLicense:Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objDFLicenseRequest.RequestingSystem, DateTime.UtcNow, objDFLicenseRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "GetLicenseRequest";
                if (!response.IsValidString())
                    response = "GetLicenseResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objDFLicenseRequest.RequestID, objDFLicenseRequest.RequestDateTime, objDFLicenseRequest.RequestingSystem,
                    request, objDFLicenseResponse.ResponseID, objDFLicenseResponse.ResponseDateTime, response,
                    objDFLicenseResponse.ResponseStatus, objDFLicenseResponse.ResponseMessage, 0);

                //End Processing GetLicense
            }



            if (objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success && !string.IsNullOrEmpty(objDFLicenseResponse.LicenseKey))
            {
                licenseKey = objDFLicenseResponse.LicenseKey;
            }
            else
            {
                licenseKey = "Fail to Generate Key";
            }


            return licenseKey;
        }
        #endregion

        #region GetSKUIndex - old
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <returns></returns>
        public int GetSKUIndex(string SNum, string SKU)
        {
            int index = 1;

            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[3];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 100);
                    arParms[1].Value = SKU;
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                    arParms[2].Direction = ParameterDirection.Output;

                    string strSQLCommand = "pTAA_GetSKUIndex";


                    using (SqlCommand objCMD = new SqlCommand(strSQLCommand, objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                    index = arParms[2] == null ? 1 : int.Parse(arParms[2].Value.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetSKUIndex Failed. " + ex.Message);
            }
            return index;

        }
        #endregion

        //Code for Duplicate Issue CR - Checks the existence of the license key in the License key table
        #region  FindDuplicateLicenseHistory
        public DataSet FindDuplicateLicenseHistory(string SNum, string SKUID, int Index, int Quantity)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_FindDuplicateLicenseHistory";
                    SqlParameter[] arParms = new SqlParameter[4];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 40);
                    arParms[1].Value = SKUID;
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                    arParms[2].Value = Index;
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@Quantity", SqlDbType.Int);
                    arParms[3].Value = Quantity;
                    arParms[3].Direction = ParameterDirection.Input;

                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:Find Duplicate License History  Failed. " + ex.Message);
            }
            return ds;
        }
        #endregion

        /// <summary>
        /// Get the record which satisfies the input criteria.
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKUID"></param>
        /// <param name="Index"></param>
        /// <param name="Quantity"></param>
        /// <param name="VersionName"></param>
        /// <returns></returns>
        public DataSet FindDuplicateLicenseHistoryForRMASWAP(string SNum, string SKUID, int Index, int Quantity,string VersionName)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_FindDuplicateLicenseHistoryForRMASWAP";
                    SqlParameter[] arParms = new SqlParameter[5];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;
                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 40);
                    arParms[1].Value = SKUID;
                    arParms[1].Direction = ParameterDirection.Input;
                    arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                    arParms[2].Value = Index;
                    arParms[2].Direction = ParameterDirection.Input;
                    arParms[3] = new SqlParameter("@Quantity", SqlDbType.Int);
                    arParms[3].Value = Quantity;
                    arParms[3].Direction = ParameterDirection.Input;
                    arParms[4] = new SqlParameter("@VersionName", SqlDbType.VarChar, 40);
                    arParms[4].Value = VersionName;
                    arParms[4].Direction = ParameterDirection.Input;
                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Find Duplicate License History For RMA SWAP  Failed. " + ex.Message);
            }
            return ds;
        }
        /// <summary>
        /// Get the records with SKUS deleted=0 for options and for softwares get the latest SKU record
        /// </summary>
        /// <param name="SNum"></param>
        /// <returns></returns>
        public DataSet FindLicenseHistoryForRMASWAP(string SNum)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_FindLicenseHistoryForRMASwapFiltered";
                    SqlParameter[] arParms = new SqlParameter[1];
                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;                                   
                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:Find Duplicate License History  Failed. " + ex.Message);
            }
            return ds;
        }
        
        //Code Duplicate Issue CR for Algorithm 12
        #region GetLicenseHistoryDataForAlgID12
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNUM"></param>
        /// <param name="SKUID"></param>
        /// <param name="Index"></param>
        /// <param name="Quantity"></param>
        /// <returns></returns>
        public DataSet GetLicenseHistoryDataForAlgID12(string SNUM, string SKUID, int Index, int Quantity)
        {
            DataSet ds = new DataSet();

            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_GetLicenseHistoryDataForAlgID12";
                    SqlParameter[] arParms = new SqlParameter[4];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNUM;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 40);
                    arParms[1].Value = SKUID;
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                    arParms[2].Value = Index;
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@Quantity", SqlDbType.Int);
                    arParms[3].Value = Quantity;
                    arParms[3].Direction = ParameterDirection.Input;

                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);

                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:AlgType 12 Failed. " + ex.Message);
            }
            return ds;
        }
        #endregion

        internal void DeleteExistingAlg12PortLicenseKey(string SNum,string PartName,string requestingSystem)
        {

            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[3];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 50);
                    arParms[1].Value = PartName;
                    arParms[1].Direction = ParameterDirection.Input;
                    /*new CR user logging 08/2013*/
                    arParms[2] = new SqlParameter("@UpdatedBy", SqlDbType.VarChar, 50);
                    arParms[2].Value = requestingSystem;
                    arParms[2].Direction = ParameterDirection.Input;

                    string strSQLCommand = "pTAA_DeleteExistingAlg12PortLicenseKey";


                    using (SqlCommand objCMD = new SqlCommand(strSQLCommand, objCon))
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
                throw new Exception("DeleteExistingAlg12PortLicenseKey Failed. " + ex.Message);
            }
        }

    }

    #region SNumsComparer Class
    public class SNumsComparer : IEqualityComparer<SNums>
    {
        #region IEqualityComparer<SNums> Members

        public bool Equals(SNums x, SNums y)
        {
            return (x.SNum == y.SNum && x.ParentSNum == y.ParentSNum);
        }

        public int GetHashCode(SNums Num)
        {
            if (string.IsNullOrEmpty(Num.SNum))
                return 0;

            return Num.SNum.GetHashCode();
        }

        #endregion
    }

    #endregion
}
