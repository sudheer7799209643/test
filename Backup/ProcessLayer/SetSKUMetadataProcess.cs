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
    public partial class SetSKUMetadataProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        SetSKUMetadataBL objSetSKUMetadataBL;
        #endregion 

        #region .ctor
        public SetSKUMetadataProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objSetSKUMetadataBL = new SetSKUMetadataBL();
        }
        #endregion

        #region SetSoftwareMetadata
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objSetSoftwareMetadataRequest"></param>
        /// <returns></returns>
        public SetSKUMetadataResponse SetSKUMetadata(SetSKUMetadataRequest objSetSKUMetadataRequest)
        {
            var objSetSKUMetadataResponse = new SetSKUMetadataResponse();

            objSetSKUMetadataResponse.RequestID = objSetSKUMetadataRequest.RequestID;
            objSetSKUMetadataResponse.ResponseID = objSetSKUMetadataRequest.RequestID;
            objSetSKUMetadataResponse.ResponseDateTime = DateTime.UtcNow;
            objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.NotImplemented;
            objSetSKUMetadataResponse.ResponseMessage = Constants.ResponseStatus.NotImplemented.ToString();            

            try
            {
                //ASSIGNING ID'S
                objSetSKUMetadataResponse.RequestID = objSetSKUMetadataRequest.RequestID;
                objSetSKUMetadataResponse.ResponseID = objSetSKUMetadataRequest.RequestID;             

                //INPUT DATA VALIDATION

                if(objSetSKUMetadataBL.ValidateRequest(objSetSKUMetadataRequest,objSetSKUMetadataResponse))               
                {
                    //PROCESS DATA & RETURN RESPONSE
                    objSetSKUMetadataBL.ProcessRequest(objSetSKUMetadataRequest,objSetSKUMetadataResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
               
                objSetSKUMetadataResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objSetSKUMetadataResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objSetSKUMetadataRequest.RequestingSystem, DateTime.UtcNow, objSetSKUMetadataRequest.RequestID);
            }
            finally
            {

                objSetSKUMetadataResponse.ResponseDateTime = DateTime.UtcNow;


                //SERIALIZE REQUEST/RESPONSE
                string request = "SetSKUMetadataRequest";
                string response = "SetSKUMetadataResponse";

                try
                {
                    request = Util.SerializeObject(objSetSKUMetadataRequest);
                    response = Util.SerializeObject(objSetSKUMetadataResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteLog("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace, objSetSKUMetadataRequest.RequestingSystem, DateTime.UtcNow, objSetSKUMetadataRequest.RequestID);
                }

                if (!request.IsValidString())
                    request = "SetSKUMetadataRequest";
                if (!response.IsValidString())
                    response = "SetSKUMetadataResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objSetSKUMetadataRequest.RequestID, objSetSKUMetadataRequest.RequestDateTime, objSetSKUMetadataRequest.RequestingSystem,
                    request, objSetSKUMetadataResponse.ResponseID, objSetSKUMetadataResponse.ResponseDateTime, response,
                    objSetSKUMetadataResponse.ResponseStatus, objSetSKUMetadataResponse.ResponseMessage, 0);

                //End Processing SetSKUMetadata

            }

            //SetSKUMetadata ends
            return objSetSKUMetadataResponse;


            //return resp;
        }
        #endregion
    }
}
