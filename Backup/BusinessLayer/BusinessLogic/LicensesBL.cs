using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Net.Security;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using PCMTandberg.BusinessEntities;
using PCMTandberg.DataAccess;
using PCMTandberg.Logger;


namespace PCMTandberg.BusinessLogic
{
    public class LicensesBL
    {
        #region Property & Variables

        string connectionString = string.Empty;
        CommonValidationBL objCommonValidationBL;
        CommonBL objCommonBL;
        ParentChildAssociationBL objParentChildAssociationBL;
        PerformanceTraceLogger objTraceLogger;
        #endregion

        #region .ctor
        public LicensesBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            objCommonValidationBL = new CommonValidationBL();
            objCommonBL = new CommonBL();
            objParentChildAssociationBL = new ParentChildAssociationBL();
        }
        #endregion

        #region ValidateRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicenseRequest"></param>
        /// <returns></returns>
        public bool ValidateRequest(LicenseRequest objLicenseRequest, LicenseResponse objLicenseResponse)
        {
            bool ValidationStatus = true;
            Int64 countSN = Convert.ToInt64((ConfigurationSettings.AppSettings["countSN"]));

            if (!objLicenseRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objLicenseResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (!objLicenseRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objLicenseResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objLicenseRequest.RequestID, objLicenseRequest.RequestingSystem) && ValidationStatus)
            {
                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objLicenseResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if ((objLicenseRequest.UnitInfo == null || objLicenseRequest.UnitInfo.Length < 1 || objLicenseRequest.UnitInfo.Length > countSN) && ValidationStatus)  //VALIDATE SNUM UNIT (MAX 100)
            {
                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objLicenseResponse.ResponseMessage = "Invalid UnitInfo";
                ValidationStatus = false;
            }
            if (objLicenseRequest.UnitInfo != null && ValidationStatus)
            {
                for (int i = 0; i < objLicenseRequest.UnitInfo.Length; i++)
                {
                    if (!objLicenseRequest.UnitInfo[i].SNum.IsValidSNumFormat() && ValidationStatus)  //VALIDATE SNUM FORMAT
                    {
                        objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objLicenseResponse.ResponseMessage = "Invalid SNum Format";
                        ValidationStatus = false;
                    }
                    if (objLicenseRequest.UnitInfo[i].SKU != null && objLicenseRequest.UnitInfo[i].SKU.Length > 0 && ValidationStatus) //VALIDATE SKU FORMAT
                    {
                        if (objLicenseRequest.UnitInfo[i].SKU.Length == 1 && string.IsNullOrEmpty(objLicenseRequest.UnitInfo[i].SKU[0].SKUID) && !string.IsNullOrEmpty(objLicenseRequest.UnitInfo[i].SKU[0].VersionFilter))
                        {
                            ValidationStatus = true;
                        }
                        else
                        {
                            for (int j = 0; j < objLicenseRequest.UnitInfo[i].SKU.Length; j++)
                            {
                                if (!objLicenseRequest.UnitInfo[i].SKU[j].SKUID.IsValidString() && ValidationStatus)
                                {
                                    objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                                    objLicenseResponse.ResponseMessage = "Invalid SKU Format";
                                    ValidationStatus = false;
                                }
                            }
                        }
                    }
                    if (objLicenseRequest.UnitInfo[i].MacAddress != null && objLicenseRequest.UnitInfo[i].MacAddress.Length > 0 && ValidationStatus) //VALIDATE MacAddress FORMAT
                    {
                        for (int k = 0; k < objLicenseRequest.UnitInfo[i].MacAddress.Length; k++)
                        {
                            if (!objLicenseRequest.UnitInfo[i].MacAddress[k].IsValidMacAddressFormat() && ValidationStatus)
                            {
                                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                                objLicenseResponse.ResponseMessage = "Invalid MacAddress Format";
                                ValidationStatus = false;
                            }
                            if (objLicenseRequest.UnitInfo[i].MacAddress[k].Length != 12 && ValidationStatus) //Validation for Lenght of MAX address
                            {
                                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                                objLicenseResponse.ResponseMessage = "MAC address entered is less than 12 characters. Please preceed the MAC address with necessary zeroes";
                                ValidationStatus = false;
                            }
                            
                        }
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
        /// <param name="objLicenseRequest"></param>
        /// <param name="objLicenseResponse"></param>
        /// <returns></returns>
        public bool ProcessRequest(LicenseRequest objLicenseRequest, LicenseResponse objLicenseResponse)
        {
            // START PROCESSING  
            bool flag = false;
            bool AddOnFlag = false;
            string UpgradeVersionFilter = "LATEST";
            DateTime ExpiryDate = DateTime.MinValue;
            if (objLicenseRequest.CheckLicensableSNUMSpecified == false)
            {
                objLicenseRequest.CheckLicensableSNUM = true;
            }
            LicenseResponse tempObjLicenseResponse;
            try
            {
                if (objLicenseRequest.UnitInfo != null && objLicenseRequest.UnitInfo.Length > 0)
                {
                    //JUST MAKING COPY
                    tempObjLicenseResponse = objLicenseResponse;

                    //check the CheckLicensableSNUM field and process the below section only if true
                    string[] macAddress = null;
                    if (objLicenseRequest.CheckLicensableSNUM == true)
                    {
                        //FIRST GET PARENTCHILD RELATION FROM GETPARENTCHILD METHOD
                        #region CALL GET PARENTCHILD RELATION
                        List<UnitLicenseInfo> objUnitLicenseInfo = GetParentChildRelationShip(objLicenseRequest,out macAddress);
                        #endregion

                        if (objUnitLicenseInfo != null && objUnitLicenseInfo.Count > 0)
                        {

                            tempObjLicenseResponse.UnitLicenseInfo = objUnitLicenseInfo.ToArray<UnitLicenseInfo>();


                            // START PROCESSING EACH SNUM & Licensable SNum OF UNITLICENSEINFO OF RESPONSE
                            for (int i = 0; i < objLicenseResponse.UnitLicenseInfo.Length; i++)
                            {
                                //INITALIZE RESPONSE UNITLICENSEINFO OBJECTS
                                if (objLicenseRequest.UnitInfo[i].ExpiryDateSpecified == true)
                                {
                                    ExpiryDate = objLicenseRequest.UnitInfo[i].ExpiryDate;
                                }
                                else
                                {
                                    ExpiryDate = DateTime.MinValue;
                                }
                                SKUInfo[] skuList;
                                SKUDetailInfo[] skuDetailList = null;
                                SKUDetailInfo[] outputSKUDetailList = null;

                                UnitInfo reqUnitInfo;
                                AddOnFlag = false;


                                if (objLicenseResponse.UnitLicenseInfo[i] != null && objLicenseResponse.UnitLicenseInfo[i].Status == (int)Constants.ResponseStatus.Success && !String.IsNullOrEmpty(objLicenseResponse.UnitLicenseInfo[i].LicensableSNum))
                                {
                                    #region CHECK ITS UPGRADE OR ADD-ON

                                    //FINDOUT REQUESTED SNUM
                                    reqUnitInfo = objLicenseRequest.UnitInfo.Where(obj => obj.SNum == objLicenseResponse.UnitLicenseInfo[i].SNum).FirstOrDefault<UnitInfo>();

                                    //IF UNIT OBJECT CONTAINS SKU LIST ITS ADD-ON REQUEST (TODO:CHECK HOW TO USE QTY)
                                    if (reqUnitInfo != null && reqUnitInfo.SKU != null && reqUnitInfo.SKU.Length > 0)
                                    {
                                        if (reqUnitInfo.SKU.Length == 1 && string.IsNullOrEmpty(reqUnitInfo.SKU[0].SKUID) && !string.IsNullOrEmpty(reqUnitInfo.SKU[0].VersionFilter))
                                        {
                                            #region Tracing Start Code
                                            objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUListForGivenSNum IN Software Upgrade Request", 1);

                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.StartTrace();
                                            #endregion

                                            skuList = objCommonBL.GetSKUListForGivenSNum(objLicenseResponse.UnitLicenseInfo[i].LicensableSNum, reqUnitInfo.SKU[0].VersionFilter);

                                            #region Tracing End Code
                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.EndTrace();
                                            #endregion

                                            UpgradeVersionFilter = reqUnitInfo.SKU[0].VersionFilter;
                                        }
                                        else
                                        {
                                            skuList = reqUnitInfo.SKU;
                                            AddOnFlag = true;
                                        }
                                    }
                                    else //IF UNIT OBJECT DOESNT CONTAIN ANY SKU DETAILS (ITS SOFTWARE UPGRADE REQUEST FOR EXISTING UNIT) 
                                    {
                                        #region Tracing Start Code
                                        objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUListForGivenSNum IN Software Upgrade Request", 1);

                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.StartTrace();
                                        #endregion

                                        skuList = objCommonBL.GetSKUListForGivenSNum(objLicenseResponse.UnitLicenseInfo[i].LicensableSNum, string.Empty);

                                        #region Tracing End Code
                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.EndTrace();
                                        #endregion

                                        UpgradeVersionFilter = "LATEST";
                                    }
                                    if (skuList == null || skuList.Length == 0)
                                    {
                                        objLicenseResponse.UnitLicenseInfo[i].Status = (int)Constants.ResponseStatus.UnKnownSKU;
                                        objLicenseResponse.UnitLicenseInfo[i].Message = Constants.ResponseMessage[2].ToString();
                                        continue;
                                    }
                                    #endregion

                                    #region DO ALL PROCESSING.
                                    if (skuList != null && skuList.Length > 0)
                                    {
                                        string[] arr = skuList.Select(o => o.SKUID).ToArray<string>();

                                        #region Tracing Start Code
                                        objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUDetailInfo for SKU", 1);

                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.StartTrace();
                                        #endregion

                                        //THIS WILL RETURN ALL SKU DETAILS WITH CURRENT LICENSE
                                        skuDetailList = objCommonBL.GetSKUDetailInfo(arr, objLicenseResponse.UnitLicenseInfo[i].LicensableSNum);

                                        #region Tracing End Code
                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.EndTrace();
                                        #endregion

                                        if (AddOnFlag)
                                        {
                                            if (skuDetailList != null && skuDetailList.Length > 0)
                                            {
                                                //ADD ON REQUEST

                                                #region Tracing Start Code
                                                objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "AddOnRequest Processing for 1 Unit", 1);

                                                if (objTraceLogger.IsEnable)
                                                    objTraceLogger.StartTrace();
                                                #endregion

                                                outputSKUDetailList = AddOnRequest(objLicenseResponse.UnitLicenseInfo[i].LicensableSNum, macAddress, skuList, skuDetailList,ExpiryDate,objLicenseRequest.RequestingSystem);

                                                #region Tracing End Code
                                                if (objTraceLogger.IsEnable)
                                                    objTraceLogger.EndTrace();
                                                #endregion

                                                objLicenseResponse.UnitLicenseInfo[i].SKU = outputSKUDetailList;

                                                //Changes made for Defect 2848
                                                if (objLicenseResponse.UnitLicenseInfo[i].SKU[0].OptionLicenseKey.StartsWith("Request with SKU:"))
                                                {
                                                    objLicenseResponse.UnitLicenseInfo[i].Status = 1;
                                                    objLicenseResponse.UnitLicenseInfo[i].Message = objLicenseResponse.UnitLicenseInfo[i].SKU[0].OptionLicenseKey;
                                                    objLicenseResponse.UnitLicenseInfo[i].SKU[0].OptionLicenseKey = string.Empty;
                                                }
                                                //End of changes for Defect 2848
                                            }
                                            else
                                            {
                                                objLicenseResponse.UnitLicenseInfo[i].Status = (int)Constants.ResponseStatus.UnKnownSKU;
                                                objLicenseResponse.UnitLicenseInfo[i].Message = Constants.ResponseMessage[2].ToString();
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //SOFTWARE UPGRADE REQUEST
                                            if (skuDetailList != null && skuDetailList.Length > 0)
                                            {
                                                #region Tracing Start Code
                                                objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "SoftwareUpgradeRequest Processing for 1 Unit", 1);

                                                if (objTraceLogger.IsEnable)
                                                    objTraceLogger.StartTrace();
                                                #endregion
                                                /*new CR user logging 08/2013*/
                                                SoftwareUpgradeRequest(ref skuDetailList, objLicenseResponse.UnitLicenseInfo[i].LicensableSNum, UpgradeVersionFilter,ExpiryDate,objLicenseRequest.RequestingSystem);

                                                #region Tracing End Code
                                                if (objTraceLogger.IsEnable)
                                                    objTraceLogger.EndTrace();
                                                #endregion


                                                objLicenseResponse.UnitLicenseInfo[i].SKU = skuDetailList;
                                            }
                                            else
                                            {
                                                objLicenseResponse.UnitLicenseInfo[i].Status = (int)Constants.ResponseStatus.UnKnownSKU;
                                                objLicenseResponse.UnitLicenseInfo[i].Message = Constants.ResponseMessage[2].ToString();
                                                continue;
                                            }

                                        }

                                    }
                                    #endregion
                                }

                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < objLicenseRequest.UnitInfo.Length; i++)
                        {
                            //INITALIZE RESPONSE UNITLICENSEINFO OBJECTS

                            SKUInfo[] skuList;
                            SKUDetailInfo[] skuDetailList = null;
                            SKUDetailInfo[] outputSKUDetailList = null;
                            string[] NlmacAddress = null;
                            UnitInfo reqUnitInfo;
                            AddOnFlag = false;
                            if (objLicenseRequest.UnitInfo[i].ExpiryDateSpecified == true)
                            {
                                ExpiryDate = objLicenseRequest.UnitInfo[i].ExpiryDate;
                            }
                            else
                            {
                                ExpiryDate = DateTime.MinValue;
                            }
                            #region CALL GET PARENTCHILD RELATION
                            List<UnitLicenseInfo> objNLUnitLicenseInfo = GetParentChildRelationShip(objLicenseRequest, out NlmacAddress);
                            #endregion

                            if (objLicenseRequest.UnitInfo[i] != null)
                            {
                                #region CHECK ITS UPGRADE OR ADD-ON

                                //FINDOUT REQUESTED SNUM
                                //Defect 2858*********
                                reqUnitInfo = objLicenseRequest.UnitInfo[i];
                                //end changes *******8
                               //reqUnitInfo = objLicenseRequest.UnitInfo.Where(obj => obj.SNum == objLicenseRequest.UnitInfo[i].SNum).FirstOrDefault<UnitInfo>();

                                //IF UNIT OBJECT CONTAINS SKU LIST ITS ADD-ON REQUEST (TODO:CHECK HOW TO USE QTY)
                                if (reqUnitInfo != null && reqUnitInfo.SKU != null && reqUnitInfo.SKU.Length > 0)
                                {
                                    if (reqUnitInfo.SKU.Length == 1 && string.IsNullOrEmpty(reqUnitInfo.SKU[0].SKUID) && !string.IsNullOrEmpty(reqUnitInfo.SKU[0].VersionFilter))
                                    {
                                        #region Tracing Start Code
                                        objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUListForGivenSNum IN Software Upgrade Request", 1);

                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.StartTrace();
                                        #endregion

                                        skuList = objCommonBL.GetSKUListForGivenSNum(objLicenseRequest.UnitInfo[i].SNum, reqUnitInfo.SKU[0].VersionFilter);

                                        #region Tracing End Code
                                        if (objTraceLogger.IsEnable)
                                            objTraceLogger.EndTrace();
                                        #endregion

                                        UpgradeVersionFilter = reqUnitInfo.SKU[0].VersionFilter;
                                    }
                                    else
                                    {
                                        skuList = reqUnitInfo.SKU;
                                        AddOnFlag = true;
                                    }
                                }
                                else //IF UNIT OBJECT DOESNT CONTAIN ANY SKU DETAILS (ITS SOFTWARE UPGRADE REQUEST FOR EXISTING UNIT) 
                                {
                                    #region Tracing Start Code
                                    objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUListForGivenSNum IN Software Upgrade Request", 1);

                                    if (objTraceLogger.IsEnable)
                                        objTraceLogger.StartTrace();
                                    #endregion

                                    skuList = objCommonBL.GetSKUListForGivenSNum(objLicenseRequest.UnitInfo[i].SNum, string.Empty);

                                    #region Tracing End Code
                                    if (objTraceLogger.IsEnable)
                                        objTraceLogger.EndTrace();
                                    #endregion

                                    UpgradeVersionFilter = "LATEST";
                                }
                                if (skuList == null || skuList.Length == 0)
                                {
                                    objLicenseResponse.UnitLicenseInfo[i].Status = (int)Constants.ResponseStatus.UnKnownSKU;
                                    objLicenseResponse.UnitLicenseInfo[i].Message = Constants.ResponseMessage[2].ToString();
                                    continue;
                                }
                                #endregion

                                #region DO ALL PROCESSING.
                                if (skuList != null && skuList.Length > 0)
                                {
                                    string[] arr = skuList.Select(o => o.SKUID).ToArray<string>();

                                    #region Tracing Start Code
                                    objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "GetSKUDetailInfo for SKU", 1);

                                    if (objTraceLogger.IsEnable)
                                        objTraceLogger.StartTrace();
                                    #endregion

                                    //THIS WILL RETURN ALL SKU DETAILS WITH CURRENT LICENSE
                                    skuDetailList = objCommonBL.GetSKUDetailInfo(arr, objLicenseRequest.UnitInfo[i].SNum);

                                    #region Tracing End Code
                                    if (objTraceLogger.IsEnable)
                                        objTraceLogger.EndTrace();
                                    #endregion

                                    if (AddOnFlag)
                                    {
                                        if (skuDetailList != null && skuDetailList.Length > 0)
                                        {
                                            //ADD ON REQUEST

                                            #region Tracing Start Code
                                            objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "AddOnRequest Processing for 1 Unit", 1);

                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.StartTrace();
                                            #endregion
                                            /*new CR user logging 08/2013*/
                                            outputSKUDetailList = AddOnRequest(objLicenseRequest.UnitInfo[i].SNum,NlmacAddress, skuList, skuDetailList,ExpiryDate,objLicenseRequest.RequestingSystem);

                                            #region Tracing End Code
                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.EndTrace();
                                            #endregion

                                            //GET PARENTCHILD RELATION FROM GETPARENTCHILD METHOD
                                            #region CALL GET PARENTCHILD RELATION
                                            List<UnitLicenseInfo> objUnitLicenseInfo = GetParentChildRelationShip(objLicenseRequest,out macAddress);
                                            #endregion
                                            if (objUnitLicenseInfo != null && objUnitLicenseInfo.Count > 0)
                                            {
                                                //Defect 2858*********
                                                if (tempObjLicenseResponse.UnitLicenseInfo == null)
                                                {
                                                    tempObjLicenseResponse.UnitLicenseInfo = objUnitLicenseInfo.ToArray<UnitLicenseInfo>();
                                                }
                                                //Defect 2858*********
                                                objLicenseResponse.UnitLicenseInfo[i].SKU = outputSKUDetailList;
                                            }
                                        }
                                        else
                                        {
                                            objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                            objLicenseResponse.ResponseMessage = Constants.ResponseMessage[2].ToString();
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        //SOFTWARE UPGRADE REQUEST
                                        if (skuDetailList != null && skuDetailList.Length > 0)
                                        {
                                            #region Tracing Start Code
                                            objTraceLogger = new PerformanceTraceLogger(objLicenseRequest.RequestID, "GetLicenses", "SoftwareUpgradeRequest Processing for 1 Unit", 1);

                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.StartTrace();
                                            #endregion
                                            /*new CR user logging 08/2013*/
                                            SoftwareUpgradeRequest(ref skuDetailList, objLicenseRequest.UnitInfo[i].SNum, UpgradeVersionFilter,ExpiryDate,objLicenseRequest.RequestingSystem);

                                            #region Tracing End Code
                                            if (objTraceLogger.IsEnable)
                                                objTraceLogger.EndTrace();
                                            #endregion

                                            //GET PARENTCHILD RELATION FROM GETPARENTCHILD METHOD
                                            #region CALL GET PARENTCHILD RELATION
                                            List<UnitLicenseInfo> objUnitLicenseInfo = GetParentChildRelationShip(objLicenseRequest,out macAddress);
                                            #endregion
                                            if (objUnitLicenseInfo != null && objUnitLicenseInfo.Count > 0)
                                            {
                                                tempObjLicenseResponse.UnitLicenseInfo = objUnitLicenseInfo.ToArray<UnitLicenseInfo>();
                                                objLicenseResponse.UnitLicenseInfo[i].SKU = skuDetailList;
                                            }
                                            
                                        }
                                        else
                                        {
                                            objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                                            objLicenseResponse.ResponseMessage = Constants.ResponseMessage[2].ToString();
                                            continue;
                                        }

                                    }

                                }
                                #endregion
                            }

                        }
                                      

                    }
                }

                flag = true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error Occured While Processing GetLicenses. " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return flag;
        }
        #endregion

        #region VerifyMacAddressAgainstSNum
        /// <summary>
        ///  
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        public bool VerifyMacAddress(string[] ReqMacAddress, string[] ResMacAddress)
        {

            for (int i = 0; i < ReqMacAddress.Length; i++)
            {
                if (ResMacAddress.Contains(ReqMacAddress[i]))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

       #region GetParentChildRelationShip
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicenseRequest"></param>
        /// <returns></returns>
        private List<UnitLicenseInfo> GetParentChildRelationShip(LicenseRequest objLicenseRequest, out string[] resMacAddress)
        {
            //CREATE REQUEST/RESPONSE OBJECT FOR PARENT CHILD
            GetParentChildAssociationRequest objGetParentChildAssociationRequest = new GetParentChildAssociationRequest();
            GetParentChildAssociationResponse objGetParentChildAssociationResponse = new GetParentChildAssociationResponse();

            //INITIALZE RETURN UNIT LICENSE INFO OBJECT
            List<UnitLicenseInfo> UnitLicenseInfoList = new List<UnitLicenseInfo>();
            
            //FINDOUT ALL SERIAL NO FOR WHICH WE NEED TO SEARCH LICENSABLE SNUM
            string[] sNum = objLicenseRequest.UnitInfo.Select(o => o.SNum).ToArray<string>();
           

            if (sNum != null && sNum.Length > 0)
            {
                #region REQUEST TO PARENT CHILD METHOD
                objGetParentChildAssociationRequest.RequestID = objLicenseRequest.RequestID;
                objGetParentChildAssociationRequest.RequestDateTime = DateTime.UtcNow;
                objGetParentChildAssociationRequest.RequestingSystem = objLicenseRequest.RequestingSystem;
                objGetParentChildAssociationRequest.SNum = sNum;    

                bool flag1 = objParentChildAssociationBL.ProcessRequest(objGetParentChildAssociationRequest, objGetParentChildAssociationResponse);
                #endregion

                
                if (objGetParentChildAssociationResponse != null && objGetParentChildAssociationResponse.Association != null && objGetParentChildAssociationResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                {
                    
                    UnitLicenseInfo objUnitLicenseInfo = new UnitLicenseInfo();
                    bool matchFlag;
                   
                    //LOOP THROUGH ALL REQUEST SNUM AND CREATE RESPONSE UNIT LICENSE OBJECT
                    for (int i = 0; i < sNum.Length; i++)
                    {
                        matchFlag = true;
                        string[] reqMacAddress = null;

                        //GET ONE UNIT AND EXTRACT MACADDRESS IF ITS THERE
                        var reqUnitInfo = objLicenseRequest.UnitInfo.Where(obj => obj.SNum == sNum[i]).FirstOrDefault<UnitInfo>();
                        if (reqUnitInfo != null && reqUnitInfo.MacAddress !=null)
                        {
                            if (reqUnitInfo.MacAddress.FirstOrDefault().Length != 12)
                            {
                                matchFlag = false;
                            }
                            else
                            {
                                reqMacAddress = reqUnitInfo.MacAddress;
                            }
                        }

                        // FIND RESPONSE OBJECT FOR GIVEN SNUM
                            var objSNumsAssociation = objGetParentChildAssociationResponse.Association.Where(obj => obj.SNum == sNum[i] && obj.Status == (int)Constants.ResponseStatus.Success).FirstOrDefault<SNumsAssociation>();


                            if (objSNumsAssociation != null)
                            {
                                #region PROCESS RESPONSE SNUM ASSOCATION FOR LICENSABLE SNUM

                                var objSNumList = objSNumsAssociation.SNumList;
                                if (objSNumList != null && objSNumList.Length > 0)
                                {

                                    #region DO MAC ADDRESS VALIDATION IF MAC ADDRESS PROVIDED
                                    resMacAddress = objSNumList.Where(obj => obj.MacAddress.IsValidString() == true && obj.MacAddress != "0").Select(obj => obj.MacAddress).Distinct().ToArray<string>();

                                    if (reqMacAddress != null && resMacAddress != null && reqMacAddress.Length > 0 && resMacAddress.Length > 0)
                                    {
                                        matchFlag = VerifyMacAddress(resMacAddress, reqMacAddress);

                                    }
                                    #endregion

                                    #region SEARCH LICENSABLE SNUM
                                    var licensableSNumList = objSNumList.Where(obj => obj.IsLicensableSNum == true).Select(obj => obj.SNum).ToArray<string>();
                                    if (licensableSNumList == null || licensableSNumList.Length < 1)
                                    {
                                        licensableSNumList = (from obj in objSNumList
                                                              where obj.SKU != null && obj.SKU.Length > 0
                                                              select obj.SNum).ToArray<string>();


                                    }

                                    if (licensableSNumList != null && licensableSNumList.Length > 0 && matchFlag)
                                    {
                                        if (licensableSNumList.Contains(sNum[i]))
                                        {
                                            objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                            objUnitLicenseInfo.LicensableSNum = sNum[i];
                                            UnitLicenseInfoList.Add(objUnitLicenseInfo);
                                        }
                                        else
                                        {
                                            //CREATE RESPONSE OBJECT WITH SNUM AND LICENSABLE SNUM
                                            for (int j = 0; j < licensableSNumList.Length; j++)
                                            {
                                                objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                                objUnitLicenseInfo.LicensableSNum = licensableSNumList[j];
                                                UnitLicenseInfoList.Add(objUnitLicenseInfo);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!matchFlag)
                                        {
                                            if (reqUnitInfo.MacAddress.FirstOrDefault().Length != 12)
                                            {
                                                objUnitLicenseInfo.Status = (int)Constants.ResponseStatus.InvalidParameter;
                                                objUnitLicenseInfo.Message = "MAC address entered is less than 12 characters. Please preceed the MAC address with necessary zeroes";
                                                UnitLicenseInfoList.Add(objUnitLicenseInfo);

                                            }
                                            else
                                            {
                                                objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                                objUnitLicenseInfo.Status = (int)Constants.ResponseStatus.InvalidParameter;
                                                objUnitLicenseInfo.Message = "No Matching MacAddress Found for Given SNum.";
                                                UnitLicenseInfoList.Add(objUnitLicenseInfo);
                                            }
                                        }
                                        else
                                        {
                                            objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                            objUnitLicenseInfo.Status = (int)Constants.ResponseStatus.NoLicSNUMFound;
                                            objUnitLicenseInfo.Message = Constants.ResponseMessage[7].ToString();
                                            UnitLicenseInfoList.Add(objUnitLicenseInfo);
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                    objUnitLicenseInfo.Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                    objUnitLicenseInfo.Message = Constants.ResponseMessage[8].ToString(); 
                                    UnitLicenseInfoList.Add(objUnitLicenseInfo);
                                }
                                #endregion

                            }
                            else
                            {
                                //IF NO RESPONSE OBJECT FOUND ERROR OUT THAT NO PARENT CHILD RECORD FOUND
                                objUnitLicenseInfo = CreateUnitLicenseInfo(sNum[i]);
                                objUnitLicenseInfo.Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                objUnitLicenseInfo.Message = Constants.ResponseMessage[8].ToString();
                                UnitLicenseInfoList.Add(objUnitLicenseInfo);
                            }

                        
                    }
                    
                }
            }
            var objSNumsA = objGetParentChildAssociationResponse.Association.Where(obj => obj.SNum == sNum[0] && obj.Status == (int)Constants.ResponseStatus.Success).FirstOrDefault<SNumsAssociation>();
            var objSL = objSNumsA.SNumList;
            resMacAddress = objSL.Where(obj => obj.MacAddress.IsValidString() == true && obj.MacAddress != "0").Select(obj => obj.MacAddress).Distinct().ToArray<string>();
            return UnitLicenseInfoList;
           
        }
        #endregion

        #region SoftwareUpgradeRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skuDetailList"></param>
        /// <param name="LicensableSNum"></param>
        /// <param name="VersionFilter"></param>
        /// /*new CR user logging 08/2013*/
        public void SoftwareUpgradeRequest(ref SKUDetailInfo[] skuDetailList,string LicensableSNum,string VersionFilter,DateTime ExpiryDate,string requestingSystem)
        {
            for (int j = 0; j < skuDetailList.Length; j++)
            {
                
                string releaseKey = string.Empty;

                #region FOR SOFTWARE
                //FOR SOFTWARE
                if (skuDetailList[j].SKUType == Constants.SKUType.SW.ToString())
                {
                    if (!VersionFilter.IsValidString() || VersionFilter == "LATEST") //IF EMPTY OR LATEST MEANS LATEST VERSION ONLY
                    {
                        VersionInfoWithLicense[] versions = skuDetailList[j].VersionList;

                        if (versions != null && versions.Length > 0)
                        {
                            VersionInfoWithLicense latestVersion = versions.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();

                            //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                            if ((latestVersion != null && string.IsNullOrEmpty(latestVersion.LicenseKey)))
                            {
                                releaseKey = string.Empty;
                                /*new CR user logging 08/2013*/
                                releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, latestVersion.Version, ExpiryDate,requestingSystem);
                                latestVersion.LicenseKey = releaseKey;
                            }
                        }
                    }
                    if (VersionFilter.IsValidString() && VersionFilter == "ALL") //ALL VERSION (I DONT ITS GOING TO BE USED OR NOT)
                    {
                        VersionInfoWithLicense[] versions = skuDetailList[j].VersionList;
                        if (versions != null && versions.Length > 0)
                        {
                            for (int k = 0; k < versions.Length; k++)
                            {
                                //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                if (string.IsNullOrEmpty(versions[k].LicenseKey))
                                {
                                    releaseKey = string.Empty;
                                    /*new CR user logging 08/2013*/
                                    releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, versions[k].Version, ExpiryDate,requestingSystem);
                                    versions[k].LicenseKey = releaseKey;
                                }
                            }
                        }
                    }
                    if (VersionFilter.IsValidString() && !(VersionFilter == "LATEST" || VersionFilter == "ALL" || VersionFilter.Contains("+"))) //SPECIFIC VERSION
                    {
                        VersionInfoWithLicense[] versions = skuDetailList[j].VersionList;
                        VersionInfoWithLicense specificVersion = versions.Where(o => o.Version == VersionFilter).FirstOrDefault<VersionInfoWithLicense>();

                        //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                        if ((specificVersion != null && string.IsNullOrEmpty(specificVersion.LicenseKey)) )
                        {
                            releaseKey = string.Empty;
                            /*new CR user logging 08/2013*/
                            releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, specificVersion.Version, ExpiryDate,requestingSystem);
                            specificVersion.LicenseKey = releaseKey;
                        }

                    }
                    //NEW CR
                    #region WITH ADDITION VERSION KEY
                    if (VersionFilter.IsValidString() && !(VersionFilter == "LATEST" || VersionFilter == "ALL") && VersionFilter.Contains("+")) //Addition VERSION
                    {

                        VersionInfoWithLicense[] versions = skuDetailList[j].VersionList;
                        string strSKUVersion = string.Empty;
                        if (VersionFilter.Contains('+'))
                        {
                            int intindex = VersionFilter.LastIndexOf('+');
                            strSKUVersion = VersionFilter.Substring(0, intindex);
                        }   
                        if (versions != null && versions.Length > 0)
                        {
                            for (int k = 0; k < versions.Length; k++)
                            {
                                string strVersion = versions[k].Version;
                                int intResult;
                                intResult = strVersion.CompareTo(strSKUVersion);
                                if (intResult == 0 || intResult == 1)
                                {
                                    //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                    if (string.IsNullOrEmpty(versions[k].LicenseKey))
                                    {
                                        releaseKey = string.Empty;
                                        /*new CR user logging 08/2013*/
                                        releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, skuDetailList[j].SKUID, skuDetailList[j].SKUType, skuDetailList[j].Qty, 1, versions[k].Version, ExpiryDate,requestingSystem);
                                        versions[k].LicenseKey = releaseKey;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                
                #endregion

            }
        }
        #endregion

        #region AddOnRequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LicensableSNum"></param>
        /// <param name="skuList"></param>
        /// <param name="skuDetailList"></param>
        /// <returns></returns>
        ///  /*new CR user logging 08/2013*/
        public SKUDetailInfo[] AddOnRequest(string LicensableSNum, string[]MacAddress, SKUInfo[] skuList, SKUDetailInfo[] skuDetailList,DateTime ExpiryDate,string requestingSystem)
        {
            List<SKUDetailInfo> inputSKUDetailList = skuDetailList.ToList<SKUDetailInfo>();
            List<SKUDetailInfo> outputSKUDetailList = new List<SKUDetailInfo>();
            
            DataTable SKUAlgTable = CreateSKUAlgTable();
            LicenseBL objBL;
            string ErrorMessage = string.Empty;
            bool outflag = false;
            DataRow dr;
            DataRow newdr;

            #region GetAllAlgorithmData
            for (int i = 0; i < skuList.Length; i++)
            {
                string VersionFilter = !(skuList[i].VersionFilter == "ALL" || skuList[i].VersionFilter == "LATEST") ? skuList[i].VersionFilter : "";
                objBL = new LicenseBL();
                outflag = objBL.GetSKULicenseMetadata(skuList[i].SKUID, VersionFilter, out ErrorMessage);
                if (outflag)
                {
                    dr = SKUAlgTable.NewRow();

                    dr["SKUID"] = !string.IsNullOrEmpty(objBL.NewSKUID) ? objBL.NewSKUID : skuList[i].SKUID;
                    dr["SKUType"] = objBL.SKUType;
                    dr["SKUQty"] = (skuList[i].QtySpecified && skuList[i].Qty > 0) ? skuList[i].Qty : 1;
                    dr["AlgID"] = objBL.AlgID;
                    dr["AlgQty"] = objBL.AlgQty;
                    dr["Seed"] = objBL.AlgSeed;
                    dr["AllowMany"] = objBL.AllowMany;
                    dr["SKUVersion"] = skuList[i].VersionFilter;
                    dr["NewKey"] = (objBL.AllowMany == 1 || objBL.AlgID == "13" || objBL.AlgID == "53") ? 1 : 0;
                    dr["Updated"] = 0;
                    SKUAlgTable.Rows.Add(dr);
                }
            }
            #endregion

            #region UPDATE QTY
            //CHECK EACH SKU IN REQUEST AND UPDATE QTY IF REQUIRE
            for (int i = 0; i < SKUAlgTable.Rows.Count; i++)
            {
                string skuid = Convert.ToString(SKUAlgTable.Rows[i]["SKUID"]);
                DataRow[] sumDR = SKUAlgTable.Select("SKUID ='" + skuid + "' AND Updated = 0 AND NewKey <> 1 AND SKUType = 2");
                if (sumDR != null && sumDR.Length > 1)
                {
                    int UpdateQty = 0;
                    for (int j = 0; j < sumDR.Length; j++)
                    {
                        UpdateQty += Convert.ToInt32(sumDR[j]["SKUQty"]);
                    }
                    for (int j = 0; j < sumDR.Length; j++)
                    {
                        sumDR[j]["SKUQty"] = UpdateQty;
                        sumDR[j]["Updated"] = 1;
                    }
                }
            }
            #endregion

            for (int i = 0; i < SKUAlgTable.Rows.Count; i++)
            {
                newdr = SKUAlgTable.Rows[i];
                if (newdr != null)
                {
                    string SKUID = Convert.ToString(newdr["SKUID"]);
                    int SKUType = Convert.ToInt32(newdr["SKUType"]);
                    int SKUQty = Convert.ToInt32(newdr["SKUQty"]);
                    int HistoricQty = 0;
                    int AlgQty = Convert.ToInt32(newdr["AlgQty"]);
                    string AlgID = Convert.ToString(newdr["AlgID"]);
                    string SKUVersion = Convert.ToString(newdr["SKUVersion"]);
                    string releaseKey = string.Empty;

                    
                    if (SKUType == 2 && Convert.ToInt32(newdr["NewKey"]) == 1)  //ALOWMANY AND ALG13
                    {
                        #region AllowMany & Alg13
                        if (AlgID == "51" || AlgID == "53")
                        {
                            /*new CR user logging 08/2013*/
                           releaseKey = objCommonBL.GetLicenseKey5153(LicensableSNum, SKUID, Constants.SKUType.OPTION.ToString(), SKUQty, 1, "", ExpiryDate, 0, MacAddress, out HistoricQty,requestingSystem);
                        }
                        else
                        {
                            /*new CR user logging 08/2013*/
                        releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.OPTION.ToString(), SKUQty, 1, "", ExpiryDate,0, out HistoricQty,requestingSystem);
                        }
                        var newlist = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).ToArray<SKUDetailInfo>();
                        
                        
                        if (newlist != null && newlist.Length > 0)
                        {
                            for (int k = 0; k < newlist.Length; k++)
                            {
                                if (!outputSKUDetailList.Contains(newlist[k]))
                                {
                                    //newlist[k].OptionLicenseKey = releaseKey;
                                    outputSKUDetailList.Add(newlist[k]);
                                }
                            }
                           
                            
                            //ADD NEW RECORD WITH NEW KEY
                            SKUDetailInfo sd = newlist[0].Clone<SKUDetailInfo>();

                            if (AlgID == "13" || AlgID == "53")
                                sd.Qty = SKUQty * AlgQty + HistoricQty;
                            else
                                sd.Qty = SKUQty * AlgQty;

                            sd.OptionLicenseKey = releaseKey;
                            outputSKUDetailList.Add(sd);

                            
                        }
                        #endregion
                    }
                    else if (SKUType == 1)   //SOFTWARE 
                    {
                        #region WITH LATEST VERSION KEY
                        if (!SKUVersion.IsValidString() || SKUVersion == "LATEST") //IF EMPTY OR LATEST MEANS LATEST VERSION ONLY
                        {
                            var latestsoftware = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).FirstOrDefault<SKUDetailInfo>();
                            if (latestsoftware != null)
                            {
                                VersionInfoWithLicense[] versions = latestsoftware.VersionList;

                                if (versions != null && versions.Length > 0)
                                {
                                    VersionInfoWithLicense latestVersion = versions.OrderByDescending(o => o.VersionSeqNo).FirstOrDefault<VersionInfoWithLicense>();

                                    //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                    if ((latestVersion != null && string.IsNullOrEmpty(latestVersion.LicenseKey)))
                                    {
                                        releaseKey = string.Empty;
                                        /*new CR user logging 08/2013*/
                                        releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.SW.ToString(), SKUQty, 1, latestVersion.Version, ExpiryDate,requestingSystem);
                                        latestVersion.LicenseKey = releaseKey;
                                    }
                                }
                                outputSKUDetailList.Add(latestsoftware);
                            }
                        }
                        #endregion

                        #region WITH ALL VERSION KEY
                        if (SKUVersion.IsValidString() && SKUVersion == "ALL") //ALL VERSION (I DONT ITS GOING TO BE USED OR NOT)
                        {
                            var allsoftware = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).FirstOrDefault<SKUDetailInfo>();
                            if (allsoftware != null)
                            {
                                VersionInfoWithLicense[] versions = allsoftware.VersionList;
                                if (versions != null && versions.Length > 0)
                                {
                                    for (int k = 0; k < versions.Length; k++)
                                    {
                                        //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                        if (string.IsNullOrEmpty(versions[k].LicenseKey))
                                        {
                                            releaseKey = string.Empty;
                                            /*new CR user logging 08/2013*/
                                            releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.SW.ToString(), SKUQty, 1, versions[k].Version, ExpiryDate,requestingSystem);
                                            versions[k].LicenseKey = releaseKey;
                                        }
                                    }
                                }
                                outputSKUDetailList.Add(allsoftware);
                            }
                        }
                        
                        #endregion
                        
                        #region WITH SPECIFIC VERSION KEY
                        if (SKUVersion.IsValidString() && !(SKUVersion == "LATEST" || SKUVersion == "ALL" || SKUVersion.Contains("+"))) //SPECIFIC VERSION
                        {
                            var specificsoftware = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).FirstOrDefault<SKUDetailInfo>();
                            if (specificsoftware != null)
                            {
                                VersionInfoWithLicense[] versions = specificsoftware.VersionList;
                                VersionInfoWithLicense specificVersion = versions.Where(o => o.Version == SKUVersion).FirstOrDefault<VersionInfoWithLicense>();
                                //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                if ((specificVersion != null) && (string.IsNullOrEmpty(specificVersion.LicenseKey)))
                                {
                                    releaseKey = string.Empty;
                                    /*new CR user logging 08/2013*/
                                    releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.SW.ToString(), SKUQty, 1, specificVersion.Version, ExpiryDate,requestingSystem);
                                    specificVersion.LicenseKey = releaseKey;
                                }
                                specificsoftware.VersionList = versions.Where(o => o.Version == SKUVersion).ToArray();
                               outputSKUDetailList.Add(specificsoftware);                                
                            }

                        }
                        #endregion
                        //NEW CR
                        #region WITH ADDITION VERSION KEY
                        if (SKUVersion.IsValidString() && !(SKUVersion == "LATEST" || SKUVersion == "ALL") && SKUVersion.Contains("+")) //Addition VERSION
                        {
                            
                            var additionsoftware = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).FirstOrDefault<SKUDetailInfo>();
                            string strSKUVersion = string.Empty;
                            if (SKUVersion.Contains('+'))
                            {
                                int intindex = SKUVersion.LastIndexOf('+');
                                strSKUVersion = SKUVersion.Substring(0, intindex);
                            }   
                            if (additionsoftware != null)
                            {
                                VersionInfoWithLicense[] versions = additionsoftware.VersionList;
                                if (versions != null && versions.Length > 0)
                                {
                                    VersionInfoWithLicense[] additionversion = new VersionInfoWithLicense[versions.Length];
                                    int count = 0;
                                    for (int k = 0; k < versions.Length; k++)
                                    {
                                        VersionInfoWithImageUrl objVersionInfoWithImageUrl = new VersionInfoWithImageUrl();
                                        objVersionInfoWithImageUrl.Version = versions[k].Version;
                                        
                                        int intResult = objVersionInfoWithImageUrl.Version.CompareTo(strSKUVersion);
                                        if (intResult==0 || intResult==1)
                                        {
                                            count = count + 1;
                                            //IF NO LICENSEKEY THEN CALL GETLICENSE TO GENERATE
                                            if (string.IsNullOrEmpty(versions[k].LicenseKey))
                                            {
                                                releaseKey = string.Empty;
                                                /*new CR user logging 08/2013*/
                                                releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.SW.ToString(), SKUQty, 1, versions[k].Version, ExpiryDate,requestingSystem);
                                                versions[k].LicenseKey = releaseKey;
                                            }
                                            additionversion[count - 1] = versions[k];
                                        }
                                    }
                                    additionsoftware.VersionList = additionversion;
                                    outputSKUDetailList.Add(additionsoftware);

                                }
                                
                                
                            }

                           

                        }
                        #endregion
                    }
                    else   //OTHER OPTIONS 
                    {
                        #region OTHER OPTIONS
                        var list = inputSKUDetailList.Where(obj => obj.SKUID == SKUID || obj.TAAPartNo == SKUID).ToList<SKUDetailInfo>();
                        //releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.OPTION.ToString(), SKUQty, 1, "", DateTime.MinValue);
                        /*new CR user logging 08/2013*/
                        if (AlgID == "51" || AlgID == "53")
                        {
                            /*new CR user logging 08/2013*/
                            releaseKey = objCommonBL.GetLicenseKey5153(LicensableSNum, SKUID, Constants.SKUType.OPTION.ToString(), SKUQty, 1, "", ExpiryDate, 0, MacAddress, out HistoricQty,requestingSystem);
                        }
                        else
                        {
                            /*new CR user logging 08/2013*/
                            releaseKey = objCommonBL.GetLicenseKey(LicensableSNum, SKUID, Constants.SKUType.OPTION.ToString(), SKUQty, 1, "", ExpiryDate, 0, out HistoricQty,requestingSystem);
                        }

                        if (list != null && list.Count > 0)
                        {
                            //UPDATE RECORD WITH NEW KEY
                            for(int j=0;j<list.Count;j++)
                            {
                                list[j].OptionLicenseKey = releaseKey;
                                list[j].Qty = SKUQty * AlgQty;
                            }

                            outputSKUDetailList.AddRange(list);
                        }
                        #endregion
                    }

                }
            }
           
            return outputSKUDetailList.ToArray();

        }
        #endregion

        #region CreateSKUAlgTable
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable CreateSKUAlgTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("SKUID", typeof(System.String));
            dt.Columns.Add("SKUQty", typeof(System.Int32));
            dt.Columns.Add("AlgID", typeof(System.String));
            dt.Columns.Add("AlgQty", typeof(System.Int32));
            dt.Columns.Add("Seed", typeof(System.String));
            dt.Columns.Add("AllowMany", typeof(System.Int32));
            dt.Columns.Add("SKUType", typeof(System.Int32));
            dt.Columns.Add("SKUVersion", typeof(System.String));
            dt.Columns.Add("NewKey", typeof(System.Int32));
            dt.Columns.Add("Updated", typeof(System.Int32));

            return dt;
        }
        #endregion

        #region CreateUnitLicenseInfo
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <returns></returns>
        public UnitLicenseInfo CreateUnitLicenseInfo(string SNum)
        {
            UnitLicenseInfo obj = new UnitLicenseInfo();
            obj.SNum = SNum;
            obj.Status = (int)Constants.ResponseStatus.Success;
            obj.Message = Constants.ResponseStatus.Success.ToString();
            obj.LicensableSNum = "";
            obj.SKU = null;
            return obj;
        }
        #endregion


    }
}
