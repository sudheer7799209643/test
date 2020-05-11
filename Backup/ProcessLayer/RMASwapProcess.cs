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
    public partial class RMASwapProcess
    {
        #region Properties & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        RMASwapBL objRMASwapBL;
        #endregion

        #region .ctor
        public RMASwapProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objRMASwapBL = new RMASwapBL();
        }
        #endregion

        #region RMASwap
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicenseRequest"></param>
        /// <returns></returns>
        public RMASwapResponse RMASwap(RMASwapRequest objRMASwapRequest)
        {
            var objRMASwapResponse = new RMASwapResponse();
            
            #region DUMMY RESPONSE
                /*
            objRMASwapResponse.RequestID = objRMASwapRequest.RequestID;
            objRMASwapResponse.ResponseID = objRMASwapRequest.RequestID;
            objRMASwapResponse.ResponseDateTime = DateTime.UtcNow;
            objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objRMASwapResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();

            #region Creating Dummy Response
           
            LicenseInfo[] licenseList = new LicenseInfo[2];

            for (int i = 0; i < licenseList.Length; i++)
            {
                licenseList[i] = new LicenseInfo();
                licenseList[i].ImageUrl = "https://cisco.com/downloads";
                licenseList[i].IsShippedVersion = (i == 0) ? true : false; ;
                licenseList[i].Qty = i + 1;
                licenseList[i].ReleaseDate = DateTime.Now.AddDays(-2).ToUniversalTime();
                licenseList[i].SKUType = "SOFTWARE";
                licenseList[i].Version = "LatestMajorRelease";
                licenseList[i].VersionSeqNo = 0;
                licenseList[i].VersionType = "DOT MAJOR";
                licenseList[i].SKUID = "11233" + i.ToString();

            }

            objRMASwapResponse.Licenses = licenseList;  
            #endregion

                  */
            #endregion

            //Start RMASwap

            try
            {
                //ASSIGNING ID'S
                objRMASwapResponse.RequestID = objRMASwapRequest.RequestID;
                objRMASwapResponse.ResponseID = objRMASwapRequest.RequestID;

                //INPUT DATA VALIDATION
                if (objRMASwapBL.ValidateRequest(objRMASwapRequest, objRMASwapResponse))
                {
                    //PROCESS DATA & RETURN RESPONSE
                    objRMASwapBL.ProcessRequest(objRMASwapRequest, objRMASwapResponse);

                }

            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objRMASwapResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objRMASwapResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objRMASwapRequest.RequestingSystem, DateTime.UtcNow, objRMASwapRequest.RequestID);
            }
            finally
            {

                objRMASwapResponse.ResponseDateTime = DateTime.UtcNow;


                //SERIALIZE REQUEST/RESPONSE
                string request = "RMASwapRequest";
                string response = "RMASwapResponse";

                try
                {
                    request = Util.SerializeObject(objRMASwapRequest);
                    response = Util.SerializeObject(objRMASwapResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objRMASwapRequest.RequestingSystem, DateTime.UtcNow, objRMASwapRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "RMASwapRequest";
                if (!response.IsValidString())
                    response = "RMASwapResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objRMASwapRequest.RequestID, objRMASwapRequest.RequestDateTime, objRMASwapRequest.RequestingSystem,
                    request, objRMASwapResponse.ResponseID, objRMASwapResponse.ResponseDateTime, response,
                    objRMASwapResponse.ResponseStatus, objRMASwapResponse.ResponseMessage, 0);

                //End Processing RMASwap

            }

            //RMASwap ends
           

            return objRMASwapResponse;
        }
        #endregion

    }
}
