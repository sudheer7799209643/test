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
    /// SoftwareVersionsProcess
    /// </summary>
    public partial class SoftwareVersionsProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        SoftwareVersionsBL objSoftwareVersionsBL;
        #endregion

        #region .ctor
        public SoftwareVersionsProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objSoftwareVersionsBL = new SoftwareVersionsBL();
        }
        #endregion

        #region GetSoftwareVersions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftwareVersionsRequest"></param>
        /// <returns></returns>
        public GetSoftwareVersionsResponse GetSoftwareVersions(GetSoftwareVersionsRequest objSoftwareVersionsRequest)
        {
            var objSoftwareVersionsResponse = new GetSoftwareVersionsResponse();

            objSoftwareVersionsResponse.RequestID = objSoftwareVersionsRequest.RequestID;
            objSoftwareVersionsResponse.ResponseID = objSoftwareVersionsRequest.RequestID;
            objSoftwareVersionsResponse.ResponseDateTime = DateTime.UtcNow;
            objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();
            objSoftwareVersionsResponse.SKUID = objSoftwareVersionsRequest.SKUID;








            //Start GetSoftwareVersions

            try
            {
                //ASSIGNING ID'S
                objSoftwareVersionsResponse.RequestID = objSoftwareVersionsRequest.RequestID;
                objSoftwareVersionsResponse.ResponseID = objSoftwareVersionsRequest.RequestID;

                //INPUT DATA VALIDATION
                if (objSoftwareVersionsBL.ValidateRequest(objSoftwareVersionsRequest, objSoftwareVersionsResponse))
                {
                    //PROCESS DATA & RETURN RESPONSE
                    objSoftwareVersionsBL.ProcessRequest(objSoftwareVersionsRequest, objSoftwareVersionsResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objSoftwareVersionsResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objSoftwareVersionsResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objSoftwareVersionsRequest.RequestingSystem, DateTime.UtcNow, objSoftwareVersionsRequest.RequestID);
            }
            finally
            {

                objSoftwareVersionsResponse.ResponseDateTime = DateTime.UtcNow;


                //SERIALIZE REQUEST/RESPONSE
                string request = "GetSoftwareVersionsRequest";
                string response = "GetSoftwareVersionsResponse";

                try
                {
                    request = Util.SerializeObject(objSoftwareVersionsRequest);
                    response = Util.SerializeObject(objSoftwareVersionsResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objSoftwareVersionsRequest.RequestingSystem, DateTime.UtcNow, objSoftwareVersionsRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "GetSoftwareVersionsRequest";
                if (!response.IsValidString())
                    response = "GetSoftwareVersionsResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objSoftwareVersionsRequest.RequestID, objSoftwareVersionsRequest.RequestDateTime, objSoftwareVersionsRequest.RequestingSystem,
                    request, objSoftwareVersionsResponse.ResponseID, objSoftwareVersionsResponse.ResponseDateTime, response,
                    objSoftwareVersionsResponse.ResponseStatus, objSoftwareVersionsResponse.ResponseMessage, 0);

                //End Processing GetSoftwareVersions

            }

            //GetSoftwareVersions ends
            return objSoftwareVersionsResponse;
        }
        #endregion

















    }
}

