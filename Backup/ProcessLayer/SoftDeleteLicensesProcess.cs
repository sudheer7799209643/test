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
    /// SoftDeleteLicensesProcess
    /// </summary>
    public partial class SoftDeleteLicensesProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        SoftDeleteLicensesBL objSoftDeleteLicensesBL;
        #endregion

        #region .ctor
        public SoftDeleteLicensesProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objSoftDeleteLicensesBL = new SoftDeleteLicensesBL();
        }
        #endregion

        #region SoftDeleteLicensesProcess
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSoftDeleteLicensesRequest"></param>
        /// <returns></returns>
        public SoftDeleteLicensesResponse SoftDeleteLicenses(SoftDeleteLicensesRequest objSoftDeleteLicensesRequest)
        {
            var objSoftDeleteLicensesResponse = new SoftDeleteLicensesResponse();

            objSoftDeleteLicensesResponse.RequestID = objSoftDeleteLicensesRequest.RequestID;
            objSoftDeleteLicensesResponse.ResponseID = objSoftDeleteLicensesRequest.RequestID;
            objSoftDeleteLicensesResponse.ResponseDateTime = DateTime.UtcNow;
            objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();
            objSoftDeleteLicensesResponse.SNum = objSoftDeleteLicensesRequest.SNum;
            if (objSoftDeleteLicensesRequest.SKU != null)
            {
                objSoftDeleteLicensesResponse.SKU = objSoftDeleteLicensesRequest.SKU;
            }
               

            //Start GetSoftDeleteLicenses

            try
            {
                //ASSIGNING ID'S
                objSoftDeleteLicensesResponse.RequestID = objSoftDeleteLicensesRequest.RequestID;
                objSoftDeleteLicensesResponse.ResponseID = objSoftDeleteLicensesRequest.RequestID;

                //INPUT DATA VALIDATION
                if (objSoftDeleteLicensesBL.ValidateRequest(objSoftDeleteLicensesRequest, objSoftDeleteLicensesResponse))
                {
                    //PROCESS DATA & RETURN RESPONSE
                    objSoftDeleteLicensesBL.ProcessRequest(objSoftDeleteLicensesRequest, objSoftDeleteLicensesResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objSoftDeleteLicensesResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objSoftDeleteLicensesResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objSoftDeleteLicensesRequest.RequestingSystem, DateTime.UtcNow, objSoftDeleteLicensesRequest.RequestID);
            }
            finally
            {

                objSoftDeleteLicensesResponse.ResponseDateTime = DateTime.UtcNow;


                //SERIALIZE REQUEST/RESPONSE
                string request = "GetSoftDeleteLicensesRequest";
                string response = "GetSoftDeleteLicensesResponse";

                try
                {
                    request = Util.SerializeObject(objSoftDeleteLicensesRequest);
                    response = Util.SerializeObject(objSoftDeleteLicensesResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objSoftDeleteLicensesRequest.RequestingSystem, DateTime.UtcNow, objSoftDeleteLicensesRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "GetSoftDeleteLicensesRequest";
                if (!response.IsValidString())
                    response = "GetSoftDeleteLicensesResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objSoftDeleteLicensesRequest.RequestID, objSoftDeleteLicensesRequest.RequestDateTime, objSoftDeleteLicensesRequest.RequestingSystem,
                    request, objSoftDeleteLicensesResponse.ResponseID, objSoftDeleteLicensesResponse.ResponseDateTime, response,
                    objSoftDeleteLicensesResponse.ResponseStatus, objSoftDeleteLicensesResponse.ResponseMessage, 0);

                //End Processing GetSoftDeleteLicenses

            }

            //GetSoftDeleteLicenses ends
            return objSoftDeleteLicensesResponse;
        }
        #endregion

















    }
}

