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
    public class RMASwapBL
    {
        #region Property & Variables

        string connectionString = string.Empty;
        string TSTConnectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        ParentChildAssociationBL objParentChildAssociationBL;
        CommonBL objCommonBL;
        LicenseBL objBL;
        EventLogger objEventLogger;
        //NEW CR - added to delete the source SNum after RMASWAP
        SoftDeleteLicensesBL objSoftDeleteLicensesBL;
        #endregion

        #region .ctor
        public RMASwapBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            TSTConnectionString = ConfigurationSettings.AppSettings["sqlconnMfgTST"];
            objCommonValidationBL = new CommonValidationBL();
            objCommonBL = new CommonBL();
            objBL = new LicenseBL();
            objParentChildAssociationBL = new ParentChildAssociationBL();
            objEventLogger = new EventLogger();
            objSoftDeleteLicensesBL = new SoftDeleteLicensesBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objRMASwapRequest"></param>
        /// <param name="objRMASwapResponse"></param>
        /// <returns></returns>
        public bool ValidateRequest(RMASwapRequest objRMASwapRequest, RMASwapResponse objRMASwapResponse)
        {
            bool ValidationStatus = true;

            if (!objRMASwapRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objRMASwapResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objRMASwapRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objRMASwapResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objRMASwapRequest.RequestID, objRMASwapRequest.RequestingSystem) && ValidationStatus)
            {
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if (!objRMASwapRequest.SourceLicenseSNum.IsValidSNumFormat())
            {
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvSrcLicensableSNum;
                objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[9].ToString();
                ValidationStatus = false;
            }
            if (!objRMASwapRequest.DestLicenseSNum.IsValidSNumFormat())
            {
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvDstLicensableSNum;
                objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[10].ToString();
                ValidationStatus = false;
            }
            return ValidationStatus;
        }
        #endregion

        #region ProcessRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objRMASwapRequest"></param>
        /// <param name="objRMASwapResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(RMASwapRequest objRMASwapRequest, RMASwapResponse objRMASwapResponse)
        {
            // START PROCESSING  

            bool flag = false;
            string[] SKUList = null;
            SKUInfo[] SKUInfoList = null;

            string ParentSNum = string.Empty;
            string message = string.Empty;
            SKUDetailInfo[] skuDetailList;
            DataTable SNumAssocation;
            try
            {
                if (objRMASwapRequest.SourceLicenseSNum.IsValidString() && objRMASwapRequest.DestLicenseSNum.IsValidString())
                {
                    //GET SKULIST FOR GIVEN SERIAL NO
                    SKUInfoList = objCommonBL.GetSKUListForGivenSNum(objRMASwapRequest.SourceLicenseSNum, string.Empty);

                    if (SKUInfoList == null || SKUInfoList.Length < 1)
                    {
                        objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.NoLicSNUMFound;
                        objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[7].ToString();
                        return flag;
                    }

                    //FIND ALL PARENT SERIAL NUM AND SKULIST USING PARENTCHILD METHOD
                    #region FIND PARENTCHILD RECORDS FOR BOTH SERIAL NO
                    SNumAssocation = GetUpdatedSNumAssocation(objRMASwapRequest);
                    #endregion

                    #region CHECK ASSOCIATION RESPONSE
                    if (SNumAssocation == null || SNumAssocation.Rows.Count == 0)
                    {
                        objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.NoParentChildFound;
                        objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[8].ToString();
                        return flag;
                    }
                    #endregion

                    #region CHECK DESTINATION LICENSABLE SNUM IS AVAILABLE IN TST OR NOT
                    if (!CheckTSTRecord(objRMASwapRequest.DestLicenseSNum))
                    {
                        objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objRMASwapResponse.ResponseMessage = "Destination Licensable SNum is Not available in System.";
                        return flag;
                    }
                    #endregion

                    #region Old Code
                    //// REPLACE LICENSABLE SOURCE SNUM WITH DESTINATION SNUM IN TST TABLE 
                    //if (ReplaceSourceLicenseSNumWithDestLicenseSNum(SNumAssocation))
                    //{
                    //    if (SKUInfoList != null)
                    //        SKUList = SKUInfoList.Select(o => o.SKUID).ToArray<string>();

                    //    if (SKUList != null && SKUList.Length > 0)
                    //    {
                    //        // FIND ALL SKU DETAILS (SOFTWARE & OPTION) FOR GIVEN ID.
                    //        skuDetailList = objCommonBL.GetSKUDetailInfo(SKUList, objRMASwapRequest.SourceLicenseSNum);

                    //        //CHECK RETURN OBJECT AND GENERATED LICENSE AS PER VERSION FILTER IF REQUIRED
                    //        if (skuDetailList != null && skuDetailList.Length > 0)
                    //        {
                    //            List<LicenseInfo> LicenseInfoList = new List<LicenseInfo>();
                    //            for (int j = 0; j < skuDetailList.Length; j++)
                    //            {

                    //                string releaseKey = string.Empty;
                    //                //FOR SOFTWARE
                    //                if (skuDetailList[j].SKUType != Constants.SKUType.OPTION.ToString())
                    //                {
                    //                    VersionInfoWithLicense[] versions = skuDetailList[j].VersionList;
                    //                    //remove all old keys.
                    //                    for (int k = 0; k < versions.Length; k++)
                    //                    {
                    //                        versions[k].IsShippedVersion = false;
                    //                        versions[k].LicenseKey = "";
                    //                    }
                    //                    if (versions != null && versions.Length > 0)
                    //                    {
                    //                        VersionInfoWithLicense latestVersion = versions.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();

                    //                        // CALL GET LICENSE FOR GIVEN SKU AND GET LICENSE KEY
                    //                        if (latestVersion != null && string.IsNullOrEmpty(latestVersion.LicenseKey))
                    //                        {
                    //                            releaseKey = string.Empty;
                    //                            releaseKey = objCommonBL.GetLicenseKey(objRMASwapRequest.DestLicenseSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, latestVersion.Version, DateTime.MinValue, 1);
                    //                            latestVersion.LicenseKey = releaseKey;
                    //                            latestVersion.IsShippedVersion = true;
                    //                        }
                    //                    }

                    //                    //add it to response licenseinfo object
                    //                    LicenseInfo obj = CreateLicenseInfo(skuDetailList[j]);
                    //                    LicenseInfoList.Add(obj);
                    //                }
                    //                else if (skuDetailList[j].SKUType == Constants.SKUType.OPTION.ToString()) //FOR OPTION
                    //                {
                    //                    skuDetailList[j].OptionLicenseKey = "";
                    //                    releaseKey = objCommonBL.GetLicenseKey(objRMASwapRequest.DestLicenseSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, "", DateTime.MinValue, 1);
                    //                    skuDetailList[j].OptionLicenseKey = releaseKey;

                    //                    //add it to response licenseinfo object
                    //                    LicenseInfo obj = CreateLicenseInfo(skuDetailList[j]);
                    //                    LicenseInfoList.Add(obj);
                    //                }
                    //            }

                    //            objRMASwapResponse.Licenses = LicenseInfoList.ToArray();
                    //        }
                    //        else
                    //        {

                    //            objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                    //            objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[2].ToString();
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    //    objRMASwapResponse.ResponseMessage = "Invalid TST Record for SNum.Fail to Replace SNum.";
                    //}
                    #endregion

                    #region NEW CR
                    // REPLACE LICENSABLE SOURCE SNUM WITH DESTINATION SNUM IN TST TABLE 
                    if (ReplaceSourceLicenseSNumWithDestLicenseSNum(SNumAssocation))
                    {
                        if (SKUInfoList != null)
                            SKUList = SKUInfoList.Select(o => o.SKUID).ToArray<string>();

                        if (SKUList != null && SKUList.Length > 0)
                        {
                            // FIND ALL SKU DETAILS (SOFTWARE & OPTION) FOR GIVEN ID.
                            skuDetailList = objCommonBL.GetSKUDetailInfo(SKUList, objRMASwapRequest.SourceLicenseSNum);

                            //CHECK RETURN OBJECT AND GENERATED LICENSE AS PER VERSION FILTER IF REQUIRED
                            if (skuDetailList.Length > 0)
                            {
                                DataTable dtChkLicenseData = null;
                                //RMA SWAP CR - Get the records with SKUS deleted=0 for options and for softwares get the latest SKU record
                                DataSet dsChkLicenseData = objCommonBL.FindLicenseHistoryForRMASWAP(objRMASwapRequest.SourceLicenseSNum);
                                if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                                    dtChkLicenseData = dsChkLicenseData.Tables[0];
                                List<LicenseInfo> LicenseInfoList = new List<LicenseInfo>();
                                string releaseKey = string.Empty;
                                foreach (DataRow row in dtChkLicenseData.Rows)
                                {

                                    string strSKUID = (row["PartName"] != DBNull.Value) ? row["PartName"].ToString() : string.Empty;
                                    string strSKUType = (row["ProductType"] != DBNull.Value) ? row["ProductType"].ToString() : string.Empty;
                                    if (strSKUType == "2")
                                        strSKUType = "OPTION";
                                    else
                                        strSKUType = "SW";
                                    int intQty = (row["Qty"] != DBNull.Value) ? Convert.ToInt16(row["Qty"]) : 1;
                                    int intIndex = (row["Index"] != DBNull.Value) ? Convert.ToInt16(row["Index"]) : 1;
                                    //Do not pass Version as we need to fetch the latest version for the SKU
                                    //string strVersion = (row["VersionName"] != DBNull.Value) ? row["VersionName"].ToString() : string.Empty;
                                    //For Software
                                    if (strSKUType != Constants.SKUType.OPTION.ToString())
                                    {

                                        // CALL GET LICENSE FOR GIVEN SKU AND GET LICENSE KEY

                                        releaseKey = string.Empty;
                                        //pass the version as empty here
                                        /*new CR user logging 08/2013*/
                                        releaseKey = objCommonBL.CallRMASwapLicense(objRMASwapRequest.DestLicenseSNum, strSKUID, strSKUType, intQty, intIndex, string.Empty, DateTime.MinValue, 1,objRMASwapRequest.RequestingSystem);
                                        //skuDetailList[j].OptionLicenseKey = releaseKey;                                                       
                                        if (string.IsNullOrEmpty(releaseKey) || releaseKey.Equals("Fail to Generate Key"))
                                            flag = false;
                                        else
                                            flag = true;


                                        LicenseInfo obj = new LicenseInfo();
                                        obj.SKUID = strSKUID;
                                        obj.SKUType = strSKUType;
                                        obj.Qty = intQty;
                                        //obj.QtySpecified = skuDetail.QtySpecified;
                                        VersionInfoWithLicense[] versions = null;
                                        VersionInfoWithLicense latestVersion = null;
                                        for (int i = 0; i < skuDetailList.Length; i++)
                                        {
                                            if (skuDetailList[i].SKUID.Equals(strSKUID))
                                            {
                                                versions = skuDetailList[i].VersionList;
                                                break;
                                            }
                                        }
                                        if(versions != null)
                                             latestVersion = versions.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();
                                        if (latestVersion != null)
                                        {
                                            obj.ImageUrl = latestVersion.ImageUrl;
                                            obj.IsShippedVersion = latestVersion.IsShippedVersion;
                                            obj.Version = latestVersion.Version;
                                            obj.VersionSeqNo = latestVersion.VersionSeqNo;
                                            obj.VersionType = latestVersion.VersionType;
                                            obj.LicenseKey = releaseKey;
                                        }

                                        //add it to response licenseinfo object                                               
                                        LicenseInfoList.Add(obj);

                                    }
                                    else
                                    {

                                        LicenseInfo obj = new LicenseInfo();
                                        obj.SKUID = strSKUID;
                                        obj.SKUType = strSKUType;
                                        obj.Qty = intQty;
                                        //obj.QtySpecified = skuDetail.QtySpecified;  
                                        /*new CR user logging 08/2013*/
                                        releaseKey = objCommonBL.CallRMASwapLicense(objRMASwapRequest.DestLicenseSNum, strSKUID, strSKUType, intQty, intIndex, "", DateTime.MinValue, 1,objRMASwapRequest.RequestingSystem);
                                        obj.VersionSeqNo = -1;
                                        obj.LicenseKey = releaseKey;
                                        obj.ReleaseDateSpecified = false;

                                        //add it to response licenseinfo object                                               
                                        LicenseInfoList.Add(obj);
                                        if (string.IsNullOrEmpty(releaseKey)||releaseKey.Equals("Fail to Generate Key"))
                                            flag = false;
                                        else
                                            flag = true;
                                    }
                                }


                                objRMASwapResponse.Licenses = LicenseInfoList.ToArray();
                            }
                            else
                            {
                                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[2].ToString();
                            }
                        }

                    }
                    else
                    {
                        objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objRMASwapResponse.ResponseMessage = "Invalid TST Record for SNum.Fail to Replace SNum.";
                    }

                    #endregion

                    //flag = true;
                }



                //NEW CR
                #region Soft Delete the Source Serial Number
                //Soft delete the Source Serial number
                if (flag)
                {
                    var objSoftDeleteLicensesResponse = new SoftDeleteLicensesResponse();
                    var objSoftDeleteLicensesRequest = new SoftDeleteLicensesRequest();
                    objSoftDeleteLicensesRequest.RequestID = objRMASwapRequest.RequestID;
                    objSoftDeleteLicensesRequest.RequestDateTime = objRMASwapRequest.RequestDateTime;
                    objSoftDeleteLicensesRequest.RequestingSystem = objRMASwapRequest.RequestingSystem;
                    objSoftDeleteLicensesRequest.SNum = objRMASwapRequest.SourceLicenseSNum;


                    objSoftDeleteLicensesResponse.RequestID = objSoftDeleteLicensesRequest.RequestID;
                    objSoftDeleteLicensesResponse.ResponseID = objSoftDeleteLicensesRequest.RequestID;
                    objSoftDeleteLicensesResponse.ResponseDateTime = DateTime.UtcNow;
                    objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                    objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();
                    objSoftDeleteLicensesResponse.SNum = objSoftDeleteLicensesRequest.SNum;

                    objSoftDeleteLicensesBL.ProcessRequest(objSoftDeleteLicensesRequest, objSoftDeleteLicensesResponse);
                    if (objSoftDeleteLicensesResponse.ResponseStatus != (int)Constants.ResponseStatus.Success)
                    {
                        objRMASwapResponse.ResponseMessage += objSoftDeleteLicensesResponse.ResponseMessage.ToString();
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While Processing RMASwap. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return flag;
        }


        #endregion

        #region GetUpdatedSNumAssocation
        public DataTable GetUpdatedSNumAssocation(RMASwapRequest objRMASwapRequest)
        {
            //CREATE REQUEST/RESPONSE OBJECT FOR PARENT CHILD
            GetParentChildAssociationRequest objGetParentChildAssociationRequest = new GetParentChildAssociationRequest();
            GetParentChildAssociationResponse objGetParentChildAssociationResponse = new GetParentChildAssociationResponse();

            DataTable SNumAssocation = GetAssociationDataTable();
            DataTable SourceSNumAssocation = GetAssociationDataTable();
            DataTable DestSNumAssocation = GetAssociationDataTable();

            string[] sNumArray = new string[2];
            sNumArray[0] = objRMASwapRequest.SourceLicenseSNum;
            sNumArray[1] = objRMASwapRequest.DestLicenseSNum;
            string[] sNum = sNumArray;
            string SourceParentSNum = string.Empty;
            string DestParentSNum = string.Empty;
            DataRow drImport = null;


            if (sNum != null && sNum.Length > 0)
            {
                #region REQUEST TO PARENT CHILD METHOD
                objGetParentChildAssociationRequest.RequestID = Guid.NewGuid().ToString();
                objGetParentChildAssociationRequest.RequestDateTime = DateTime.UtcNow;
                objGetParentChildAssociationRequest.RequestingSystem = objRMASwapRequest.RequestingSystem;
                objGetParentChildAssociationRequest.SNum = sNum;

                bool flag = objParentChildAssociationBL.ProcessRequest(objGetParentChildAssociationRequest, objGetParentChildAssociationResponse);
                #endregion

                if (objGetParentChildAssociationResponse != null && objGetParentChildAssociationResponse.Association != null && objGetParentChildAssociationResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                {
                    // FIND RESPONSE OBJECT FOR GIVEN SOURCE SNUM
                    var objSourceSNumsAssociation = objGetParentChildAssociationResponse.Association.Where(obj => obj.SNum == objRMASwapRequest.SourceLicenseSNum && obj.Status == (int)Constants.ResponseStatus.Success).FirstOrDefault<SNumsAssociation>();
                    var objDestSNumsAssociation = objGetParentChildAssociationResponse.Association.Where(obj => obj.SNum == objRMASwapRequest.DestLicenseSNum && obj.Status == (int)Constants.ResponseStatus.Success).FirstOrDefault<SNumsAssociation>();



                    #region BUILD HIERARCHY FOR SOURCE TREE
                    if (objSourceSNumsAssociation != null && objSourceSNumsAssociation.SNumList != null)
                    {
                        var objSourceSNumList = objSourceSNumsAssociation.SNumList.Distinct(new SNumsComparer()).ToArray();
                        if (objSourceSNumList != null && objSourceSNumList.Length > 0)
                        {
                            var topParentSNum = objSourceSNumList.Where(obj => String.IsNullOrEmpty(obj.ParentSNum) == true).FirstOrDefault<SNums>();

                            if (topParentSNum != null)
                            {
                                string parentSNum = string.Empty;
                                parentSNum = topParentSNum.SNum;

                                //GET DATA TABLE FOR WHOLE HIERARCHY SO WE CAN CREATE INSERT STATMENTS
                                CreateHierarchy(ref SourceSNumAssocation, objSourceSNumList, parentSNum, null);

                                if (SourceSNumAssocation != null && SourceSNumAssocation.Rows.Count > 0)
                                {
                                    //debugging
                                    for (int i = 0; i < SourceSNumAssocation.Rows.Count; i++)
                                    {
                                        string sernum = Convert.ToString(SourceSNumAssocation.Rows[i]["SerialNum"]);
                                        string parentsernum = Convert.ToString(SourceSNumAssocation.Rows[i]["ParentSerialNum"]);
                                    }

                                    //FIND CHILD ROWS OF SOURCE SNUM
                                    DataRow[] sourceChildRows = SourceSNumAssocation.Select("ParentSerialNum = '" + objRMASwapRequest.SourceLicenseSNum + "'");
                                    for (int j = 0; j < sourceChildRows.Length; j++)
                                    {
                                        //REPLACE PARENT OF ALL CHILDS WITH DEST SNUM
                                        sourceChildRows[j]["ParentSerialNum"] = objRMASwapRequest.DestLicenseSNum;
                                    }

                                    //REPLACE SOURCE SNUM WITH DEST SNUM
                                    DataRow[] sourceParentRow = SourceSNumAssocation.Select("SerialNum = '" + objRMASwapRequest.SourceLicenseSNum + "'");
                                    for (int j = 0; j < sourceParentRow.Length; j++)
                                    {
                                        sourceParentRow[j]["SerialNum"] = objRMASwapRequest.DestLicenseSNum;
                                    }

                                }
                            }
                        }
                    }
                    #endregion

                    #region BUILD HIERARCHY FOR DESTINATION CHILDS
                    if (objDestSNumsAssociation != null && objDestSNumsAssociation.SNumList != null)
                    {
                        var objDestSNumList = objDestSNumsAssociation.SNumList.Distinct(new SNumsComparer()).ToArray();
                        if (objDestSNumList != null && objDestSNumList.Length > 0)
                        {
                            var DestChildSNum = objDestSNumList.Where(obj => obj.ParentSNum == objRMASwapRequest.DestLicenseSNum).ToArray();

                            if (DestChildSNum != null && DestChildSNum.Length > 0)
                            {
                                CreateHierarchy(ref DestSNumAssocation, objDestSNumList, objRMASwapRequest.DestLicenseSNum, null);

                                if (DestSNumAssocation != null && DestSNumAssocation.Rows.Count > 0)
                                {
                                    for (int i = 0; i < DestSNumAssocation.Rows.Count; i++)
                                    {
                                        string sernum = Convert.ToString(DestSNumAssocation.Rows[i]["SerialNum"]);
                                        string parentsernum = Convert.ToString(DestSNumAssocation.Rows[i]["ParentSerialNum"]);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region INSERT DESTINATION CHILD HIERARCHY AT SOURCE SIDE
                    if (DestSNumAssocation != null && DestSNumAssocation.Rows.Count > 0)
                    {

                        for (int i = 0; i < SourceSNumAssocation.Rows.Count; i++)
                        {
                            //FIND DESTINATION SNUM IN UPDATED SOURCE TREE && ADD ALL CHILDS OF DESTINATION TO FINAL DATATABLE
                            if (Convert.ToString(SourceSNumAssocation.Rows[i]["SerialNum"]) == objRMASwapRequest.DestLicenseSNum)
                            {
                                for (int j = 0; j < DestSNumAssocation.Rows.Count; j++)
                                {
                                    //avoid null parent entry of destination tree as we are already setting new parent on source side.
                                    if (!String.IsNullOrEmpty(Convert.ToString(DestSNumAssocation.Rows[j]["ParentSerialNum"])))
                                    {
                                        drImport = SNumAssocation.NewRow();
                                        drImport["SerialNum"] = Convert.ToString(DestSNumAssocation.Rows[j]["SerialNum"]); ;
                                        drImport["ParentSerialNum"] = Convert.ToString(DestSNumAssocation.Rows[j]["ParentSerialNum"]);
                                        SNumAssocation.Rows.Add(drImport);
                                    }
                                }
                            }

                            drImport = SNumAssocation.NewRow();
                            drImport["SerialNum"] = Convert.ToString(SourceSNumAssocation.Rows[i]["SerialNum"]);
                            drImport["ParentSerialNum"] = Convert.ToString(SourceSNumAssocation.Rows[i]["ParentSerialNum"]);
                            SNumAssocation.Rows.Add(drImport);
                        }
                    }
                    else
                    {   //IF THERE IS NO CHILD OF DESTINATION SNUM THAN JUST USE UPDATED SOURCE
                        SNumAssocation = SourceSNumAssocation;


                    }

                    /* NOT REQUIRE AS PER CHARLES COMMENT
                     
                    //UNLINK SOURCE SNUM FROM TREE
                    drImport = SNumAssocation.NewRow();
                    drImport["SerialNum"] = objRMASwapRequest.SourceLicenseSNum;
                    drImport["ParentSerialNum"] = objRMASwapRequest.SourceLicenseSNum;
                    SNumAssocation.Rows.Add(drImport);      */

                    #endregion

                }

            }

            //debugging
            for (int i = 0; i < SNumAssocation.Rows.Count; i++)
            {
                string sernum = Convert.ToString(SNumAssocation.Rows[i]["SerialNum"]);
                string parentsernum = Convert.ToString(SNumAssocation.Rows[i]["ParentSerialNum"]);

            }
            return SNumAssocation;
        }
        #endregion

        #region CreateLicenseInfo
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skuDetail"></param>
        /// <returns></returns>
        public LicenseInfo CreateLicenseInfo(SKUDetailInfo skuDetail)
        {
            LicenseInfo obj = new LicenseInfo();
            obj.SKUID = skuDetail.SKUID;
            obj.SKUType = skuDetail.SKUType;
            obj.Qty = skuDetail.Qty;
            obj.QtySpecified = skuDetail.QtySpecified;

            if (skuDetail.SKUType == Constants.SKUType.OPTION.ToString())
            {
                obj.VersionSeqNo = -1;
                obj.LicenseKey = skuDetail.OptionLicenseKey;
                obj.ReleaseDateSpecified = false;
            }
            else
            {
                VersionInfoWithLicense[] versions = skuDetail.VersionList;
                VersionInfoWithLicense latestVersion = versions.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();
                // CALL GET LICENSE FOR GIVEN SKU AND GET LICENSE KEY
                if (latestVersion != null)
                {
                    obj.ImageUrl = latestVersion.ImageUrl;
                    obj.IsShippedVersion = latestVersion.IsShippedVersion;
                    obj.Version = latestVersion.Version;
                    obj.VersionSeqNo = latestVersion.VersionSeqNo;
                    obj.VersionType = latestVersion.VersionType;
                    obj.LicenseKey = latestVersion.LicenseKey;
                }
            }

            return obj;
        }
        #endregion

        #region ReplaceSourceLicenseSNumWithDestLicenseSNum
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SourceLicenseSNum"></param>
        /// <param name="DestLicenseSNum"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool ReplaceSourceLicenseSNumWithDestLicenseSNum(DataTable SNumAssocation)
        {
            // if its found else error
            // Replace licensable source serial number in TST with destination serial number - unlink source serial number from parent serial number and link destination serial number with same parent serial number
            bool flag = false;

            SqlTransaction tn;

            try
            {
                if (SNumAssocation != null && SNumAssocation.Rows.Count > 0)
                {
                    SNumAssocation.DefaultView.Sort = "RowID ASC";

                    bool machineuutflag = GetTSTData(ref SNumAssocation);

                    if (machineuutflag)
                    {
                        using (SqlConnection objCon = new SqlConnection(TSTConnectionString))
                        {
                            objCon.Open();
                            //tn = objCon.BeginTransaction();

                            try
                            {
                                for (int i = 0; i < SNumAssocation.Rows.Count; i++)
                                {
                                    System.Threading.Thread.Sleep(1500);


                                    SqlParameter[] arParms = new SqlParameter[1];


                                    string strXMLSQL = "<tst sernum=\'" + SNumAssocation.Rows[i]["SerialNum"] + "\' rectime ='" + DateTime.UtcNow + "' parentsernum=\'" + SNumAssocation.Rows[i]["ParentSerialNum"] + "\' "
                                                      + "timestamp = '" + DateTime.UtcNow + "' machine=\'" + SNumAssocation.Rows[i]["Machine"] + "' "
                                                      + "uuttype=\'" + SNumAssocation.Rows[i]["UUTType"] + "\' area=\'" + SNumAssocation.Rows[i]["Area"] + "\' passfail=\'P\' cell=\'1\' />";

                                    arParms[0] = new SqlParameter("@xmldata", SqlDbType.VarChar, 8000);
                                    arParms[0].Value = strXMLSQL;
                                    arParms[0].Direction = ParameterDirection.Input;

                                    /*
                                    string strSQL = @"INSERT INTO tst (sernum, rectime, parentsernum, [timestamp], serverid, machineid, uutid, areaid, passfail, cell, bflush, username)
						                    values
				        			        (@SerialNumber, getutcdate(), @ParentSerialNumber, getutcdate(), 38, 0, '0', 909, 'P', 0, 0, 'TB_Sync')";        */

                                    string strSQL = "sp_inserttst";

                                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                                    {

                                        objCMD.CommandType = CommandType.StoredProcedure;
                                        objCMD.Connection = objCon;
                                        objCMD.Parameters.AddRange(arParms);

                                        // copy insert statment

                                        objEventLogger.WriteEntry(strXMLSQL);
                                        objCMD.CommandTimeout = 10000;

                                        objCMD.ExecuteNonQuery();

                                    }
                                }

                                //tn.Commit();
                                flag = true;
                            }
                            catch (Exception ex)
                            {
                                //tn.Rollback();
                                throw new Exception("Insert TST Record Failed. " + ex.Message);
                            }
                            finally
                            {
                                // tn.Dispose();
                            }


                        }
                    }
                    else
                    {
                        flag = false;
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fail to Replace Source LicenseSNum With DestLicenseSNum in TST. " + ex.Message);
            }

            return flag;
        }
        #endregion

        #region GetTSTData
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNumAssocation"></param>
        public bool GetTSTData(ref DataTable SNumAssocation)
        {
            bool flag = true;
            using (SqlConnection objCon = new SqlConnection(TSTConnectionString))
            {
                objCon.Open();
                //tn = objCon.BeginTransaction();

                try
                {

                    for (int i = 0; i < SNumAssocation.Rows.Count; i++)
                    {
                        SqlParameter[] arParms = new SqlParameter[4];

                        arParms[0] = new SqlParameter("@SerialNumber", SqlDbType.VarChar, 40);
                        arParms[0].Value = SNumAssocation.Rows[i]["SerialNum"];
                        arParms[0].Direction = ParameterDirection.Input;

                        arParms[1] = new SqlParameter("@Machine", SqlDbType.VarChar, 50);
                        arParms[1].Direction = ParameterDirection.Output;

                        arParms[2] = new SqlParameter("@UUTType", SqlDbType.VarChar, 50);
                        arParms[2].Direction = ParameterDirection.Output;

                        arParms[3] = new SqlParameter("@Area", SqlDbType.VarChar, 50);
                        arParms[3].Direction = ParameterDirection.Output;



                        string strSQL = @"SELECT TOP 1 @Machine = m.machine,@UUTType=u.uuttype,@Area= a.area FROM tst as t  WITH(NOLOCK) "
                                        + " LEFT JOIN machines as m on m.machineid = t.machineid "
                                        + " LEFT JOIN uuttypes as u on u.uutid = t.uutid "
                                        + " LEFT JOIN areas as a on a.areaid = t.areaid  "
                                        + " WHERE t.sernum = @SerialNumber "
                                        + " ORDER BY t.rectime desc ";
                        //SELECT TOP 1 @MachineID=machineid, @UUTType= uutid , @AreaID= areaid  FROM tst WITH(NOLOCK) where sernum = @SerialNumber order by rectime desc";

                        using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                        {

                            objCMD.CommandType = CommandType.Text;
                            objCMD.Connection = objCon;
                            objCMD.Parameters.AddRange(arParms);

                            // copy select statment
                            string strSQLCopy = strSQL;
                            strSQLCopy = strSQLCopy.Replace("@SerialNumber", Convert.ToString(arParms[0].Value));
                            objEventLogger.WriteEntry(strSQLCopy);
                            objCMD.CommandTimeout = 10000;
                            objCMD.ExecuteNonQuery();
                        }

                        string Machine = string.Empty;
                        string UUTType = string.Empty;
                        string Area = string.Empty;

                        if (arParms[1].Value != null)
                            Machine = Convert.ToString(arParms[1].Value);

                        if (arParms[2].Value != null)
                            UUTType = Convert.ToString(arParms[2].Value);

                        if (arParms[3].Value != null)
                            Area = Convert.ToString(arParms[3].Value);

                        if (string.IsNullOrEmpty(Machine) || string.IsNullOrEmpty(UUTType) || string.IsNullOrEmpty(Area))
                        {
                            flag = false;
                        }

                        SNumAssocation.Rows[i]["Machine"] = Machine;
                        SNumAssocation.Rows[i]["UUTType"] = UUTType;
                        SNumAssocation.Rows[i]["Area"] = Area;


                    }

                }
                catch (Exception ex)
                {

                    throw new Exception("RMASwap:Fail to Get Machineid , uutid and areaid from TST. " + ex.Message);
                }
            }

            return flag;

        }
        #endregion

        #region CHECK TST RECORD
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Snum"></param>
        /// <returns></returns>
        private bool CheckTSTRecord(string Snum)
        {
            bool flag = false;

            try
            {
                using (SqlConnection objCon = new SqlConnection(TSTConnectionString))
                {

                    SqlParameter[] arParms = new SqlParameter[2];

                    arParms[0] = new SqlParameter("@SerialNumber", SqlDbType.VarChar, 40);
                    arParms[0].Value = Snum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@Count", SqlDbType.Int);
                    arParms[1].Direction = ParameterDirection.Output;



                    string strSQL = @"SELECT @Count = count(sernum) 
                                      FROM tst WITH (NOLOCK)
                                      Where sernum = @SerialNumber 
                                      OR parentsernum = @SerialNumber";

                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.Text;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.CommandTimeout = 10000;
                        objCMD.ExecuteNonQuery();

                    }

                    int readCount = arParms[1] == null ? 0 : int.Parse(arParms[1].Value.ToString());
                    if (readCount > 0)
                    {
                        flag = true;
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Checking TST Record Failed. " + ex.Message);
            }
            return flag;
        }
        #endregion

        #region GetAssociationDataTable
        private DataTable GetAssociationDataTable()
        {
            DataTable dt = new DataTable();
            DataColumn dc = new DataColumn("RowID", typeof(System.Int32));
            dc.AutoIncrement = true;
            dc.AutoIncrementSeed = 1;
            dc.AutoIncrementStep = 1;
            // dt.Columns.Add("RowID", typeof(System.Int32));
            dt.Columns.Add(dc);
            dt.Columns.Add("SerialNum", typeof(System.String));
            dt.Columns.Add("ParentSerialNum", typeof(System.String));
            dt.Columns.Add("Machine", typeof(System.String));
            dt.Columns.Add("UUTType", typeof(System.String));
            dt.Columns.Add("Area", typeof(System.String));


            return dt;
        }
        #endregion

        #region CreateHierarchy
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNumAssocation"></param>
        /// <param name="sourceAssociation"></param>
        /// <param name="ChildSNum"></param>
        /// <param name="ParentSNum"></param>
        public void CreateHierarchy(ref DataTable SNumAssocation, SNums[] sourceAssociation, string ChildSNum, string ParentSNum)
        {
            DataRow dr = null;
            SNums[] Childs = sourceAssociation.Where(obj => obj.ParentSNum == ChildSNum).ToArray();

            if (Childs != null && Childs.Length > 0)
            {
                foreach (SNums s in Childs)
                {
                    CreateHierarchy(ref SNumAssocation, sourceAssociation, s.SNum, ChildSNum);
                }
            }

            dr = SNumAssocation.NewRow();
            dr["SerialNum"] = ChildSNum;
            dr["ParentSerialNum"] = ParentSNum;
            SNumAssocation.Rows.Add(dr);
            return;


        }
        #endregion


    }
}
