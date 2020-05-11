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
    /// ValidateSKUProcess
    /// </summary>
    public partial class ValidateSKUProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        ValidateSKUBL objValidateSKUBL;
        #endregion

        #region .ctor
        public ValidateSKUProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objValidateSKUBL = new ValidateSKUBL();
        }
        #endregion

        #region ValidateSKUVersions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objValidateSKURequest"></param>
        /// <returns></returns>
        public ValidateSKUResponse ValidateSKUVersions(ValidateSKURequest objValidateSKURequest)
        {
            var objValidateSKUResponse = new ValidateSKUResponse();

            objValidateSKUResponse.RequestID = objValidateSKURequest.RequestID;
            objValidateSKUResponse.ResponseID = objValidateSKURequest.RequestID;
            objValidateSKUResponse.ResponseDateTime = DateTime.UtcNow;
            objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
            objValidateSKUResponse.ResponseMessage = Constants.ResponseStatus.Success.ToString();
            objValidateSKUResponse.SKUID = new Response_SKUVersionInfo[objValidateSKURequest.SKUID.Length];

            for (int i = 0; i < objValidateSKURequest.SKUID.Length; i++)
            {
                objValidateSKUResponse.SKUID[i] = CreateResponseInfo(objValidateSKURequest.SKUID[i].SKUID);
            }
            //Start ValidateSKU Process

            try
            {
                //ASSIGNING ID'S
                objValidateSKUResponse.RequestID = objValidateSKURequest.RequestID;
                objValidateSKUResponse.ResponseID = objValidateSKURequest.RequestID;

                //INPUT DATA VALIDATION
                if (objValidateSKUBL.ValidateRequest(objValidateSKURequest, objValidateSKUResponse))
                {
                    //PROCESS DATA & RETURN RESPONSE
                    objValidateSKUBL.ProcessRequest(objValidateSKURequest, objValidateSKUResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objValidateSKUResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objValidateSKUResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objValidateSKURequest.RequestingSystem, DateTime.UtcNow, objValidateSKURequest.RequestID);
            }
            finally
            {

                objValidateSKUResponse.ResponseDateTime = DateTime.UtcNow;


                //SERIALIZE REQUEST/RESPONSE
                string request = "GetValidateSKURequest";
                string response = "GetValidateSKUResponse";

                try
                {
                    request = Util.SerializeObject(objValidateSKURequest);
                    response = Util.SerializeObject(objValidateSKUResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objValidateSKURequest.RequestingSystem, DateTime.UtcNow, objValidateSKURequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "GetValidateSKURequest";
                if (!response.IsValidString())
                    response = "GetValidateSKUResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objValidateSKURequest.RequestID, objValidateSKURequest.RequestDateTime, objValidateSKURequest.RequestingSystem,
                    request, objValidateSKUResponse.ResponseID, objValidateSKUResponse.ResponseDateTime, response,
                    objValidateSKUResponse.ResponseStatus, objValidateSKUResponse.ResponseMessage, 0);

                //End Processing GetValidateSKU

            }

            //GetValidateSKU ends
            return objValidateSKUResponse;
        }
        #endregion

        public Response_SKUVersionInfo CreateResponseInfo(string Sku)
        {
            Response_SKUVersionInfo obj = new Response_SKUVersionInfo();
            //obj.SNum = SNum;
            //obj.Status = (int)Constants.ResponseStatus.Success;
            //obj.Message = Constants.ResponseStatus.Success.ToString();
            //obj.LicensableSNum = "";
            //obj.SKU = null;
            obj.AvailableVersion = null;
            obj.ResponseStatus = (int)Constants.ResponseStatus.Success;
            obj.ResponseMessage = Constants.ResponseStatus.Success.ToString();
            obj.SKUID = Sku;
            return obj;


        }
    }
}

