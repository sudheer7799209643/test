using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using PCMTandberg.BusinessEntities;
using PCMTandberg.BusinessLogic;
using PCMTandberg.Logger;

namespace PCMTandberg.ProcessMessage
{
    /// <summary>
    /// LicensesProcess
    /// </summary>
    public partial class LicensesProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        LicensesBL objLicensesBL;
        #endregion 

        #region .ctor
        public LicensesProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objLicensesBL = new LicensesBL();
        }
        #endregion

        #region GetLicenses
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicenseRequest"></param>
        /// <returns></returns>
        public LicenseResponse GetLicenses(LicenseRequest objLicenseRequest)
        {
            var objLicenseResponse = new LicenseResponse();

            //Start GetLicenses;
            if (objLicenseRequest != null)
            {
                #region DUMMY RESPONSE
                /*
                objLicenseResponse.RequestID = objLicenseRequest.RequestID;
                objLicenseResponse.ResponseID = objLicenseRequest.RequestID;
                objLicenseResponse.ResponseDateTime = DateTime.UtcNow;
                objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                objLicenseResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();

                #region Creating Dummy Response
                if (objLicenseRequest.UnitInfo.Length > 0)
                {
                    UnitLicenseInfo[] UnitLicenseInfoList = new UnitLicenseInfo[objLicenseRequest.UnitInfo.Length];
                    for (int i = 0; i < UnitLicenseInfoList.Length; i++)
                    {
                        UnitLicenseInfoList[i] = new UnitLicenseInfo();
                        UnitLicenseInfoList[i].SNum = objLicenseRequest.UnitInfo[i].SNum;
                        UnitLicenseInfoList[i].LicensableSNum = "AAAA-BBBB-CCCC-DDDD-EEEE" + i.ToString();

                        SKUDetailInfo[] SKUInfoList = new SKUDetailInfo[req.UnitInfo[i].SKU.Length];
                        for (int j = 0; j < SKUInfoList.Length; j++)
                        {
                            SKUInfoList[j] = new SKUDetailInfo();
                            SKUInfoList[j].SKUID = objLicenseRequest.UnitInfo[i].SKU[j].SKUID;
                            SKUInfoList[j].SKUName = "Memory Card";
                            SKUInfoList[j].TAAPartNo = "TAA" + SKUInfoList[j].SKUID;
                            SKUInfoList[j].TAAPartName = "TAANO" + SKUInfoList[j].SKUID;
                            SKUInfoList[j].Qty = 1;
                            SKUInfoList[j].OptionLicenseKey = "";
                            SKUInfoList[j].SKUType = "SW";

                            VersionInfoWithLicense[] VersionInfoWithLicenseList = new VersionInfoWithLicense[2];
                            for (int k = 0; k < VersionInfoWithLicenseList.Length; k++)
                            {

                                VersionInfoWithLicenseList[k] = new VersionInfoWithLicense();
                                VersionInfoWithLicenseList[k].VersionPartName = "sdfasf";
                                VersionInfoWithLicenseList[k].VersionPartNo = "123";
                                VersionInfoWithLicenseList[k].ImageUrl = "https://cisco.com/downloads";
                                VersionInfoWithLicenseList[k].IsShippedVersion = (k == 0) ? true : false;
                                VersionInfoWithLicenseList[k].LicenseKey = "AAAA-BBBB-CCCC-DDDD-EEEE" + k.ToString();
                                VersionInfoWithLicenseList[k].ReleaseDate = DateTime.Now.AddDays(-5).ToUniversalTime();
                                VersionInfoWithLicenseList[k].Version = "LatestMajorRelease";
                                VersionInfoWithLicenseList[k].VersionSeqNo = k + 1;
                                VersionInfoWithLicenseList[k].VersionType = "DOT MAJOR";

                            }
                            SKUInfoList[j].VersionList = VersionInfoWithLicenseList;

                        }
                        UnitLicenseInfoList[i].SKU = SKUInfoList;
                    }


                    objLicenseResponse.UnitLicenseInfo = UnitLicenseInfoList;

                }
                else
                {
                    objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objLicenseResponse.ResponseMessage = Constants.ResponseStatus.InvalidParameter.ToString();
                }
                #endregion
                */
                #endregion

                try
                {
                    //ASSIGNING ID'S
                    objLicenseResponse.RequestID = objLicenseRequest.RequestID;
                    objLicenseResponse.ResponseID = objLicenseRequest.RequestID;

                    //Input Validation GetLicenses
                    if (objLicensesBL.ValidateRequest(objLicenseRequest, objLicenseResponse))
                    {
                        //PROCESS DATA & RETURN RESPONSE
                        objLicensesBL.ProcessRequest(objLicenseRequest, objLicenseResponse);
                    }

                }
                catch (Exception ex)
                {
                    //SET FAILURE STATUS
                    objLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                    objLicenseResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                    //LOG EXCEPTION
                    objEventLogger.WriteLog("GetLicenses:"+ ex.Message + Environment.NewLine + ex.StackTrace, objLicenseRequest.RequestingSystem, DateTime.UtcNow, objLicenseRequest.RequestID);
                }
                finally
                {

                    objLicenseResponse.ResponseDateTime = DateTime.UtcNow;


                    //SERIALIZE REQUEST/RESPONSE
                    string request = "GetLicensesRequest";
                    string response = "GetLicensesResponse";

                    try
                    {
                        request = Util.SerializeObject(objLicenseRequest);
                        response = Util.SerializeObject(objLicenseResponse);
                    }
                    catch (Exception ex)
                    {
                        objEventLogger.WriteLog("GetLicenses:Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objLicenseRequest.RequestingSystem, DateTime.UtcNow, objLicenseRequest.RequestID);
                    }

                    if (!request.IsValidString())
                        request = "GetLicensesRequest";
                    if (!response.IsValidString())
                        response = "GetLicensesResponse";


                    //LOG REQUEST/RESPONSE
                    objTransactionLogger.LogTransaction(objLicenseRequest.RequestID, objLicenseRequest.RequestDateTime, objLicenseRequest.RequestingSystem,
                        request, objLicenseResponse.ResponseID, objLicenseResponse.ResponseDateTime, response,
                        objLicenseResponse.ResponseStatus, objLicenseResponse.ResponseMessage, 0);

                    //End Processing GetLicenses

                }
            }
            //GetLicenses ends
            return objLicenseResponse;
        }
        #endregion

    }
}
