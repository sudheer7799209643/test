using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using PCMTandberg.BusinessEntities;
using PCMTandberg.BusinessLogic;
using System.Configuration;
using PCMTandberg.Logger;

namespace PCMTandberg.ProcessMessage
{
    public partial class LicenseProcess
    {
        #region Property & Variables
        string TandbergCAPrimary;
        string TandbergCASecondary;
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        LicenseBL objBL;
        #endregion

        #region .ctor
        public LicenseProcess()
        {
            TandbergCAPrimary = ConfigurationSettings.AppSettings["TandbergCAPrimary"];
            TandbergCASecondary = ConfigurationSettings.AppSettings["TandbergCASecondary"];
            objBL = new LicenseBL();
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
        }
        #endregion

        #region GetLicense
        public DFLicenseResponse GetLicense(DFLicenseRequest objDFLicenseRequest)
        {
            var objDFLicenseResponse = new DFLicenseResponse();
            var objLicRequest = new LicRequest();
            var objLicResponse = new LicResponse();
            var objCAProxy = new SigServTandbergService();

            //Start GetLicense;
            objEventLogger.WriteEntry("Start GetLicense");

            if (objDFLicenseRequest != null)
            {
                try
                {
                    //ASSIGNING ID'S
                    objDFLicenseResponse.RequestID = objDFLicenseRequest.RequestID;
                    objDFLicenseResponse.ResponseID = objDFLicenseRequest.RequestID;

                    //Input Validation GetLicenses
                    if (objBL.ValidateRequest(objDFLicenseRequest, objDFLicenseResponse))
                    {
                        objBL.GetLicense(objDFLicenseRequest, objDFLicenseResponse);
                    }
                }
                catch(Exception ex)
                {
                    //Message for alg 17

                    if (ex.Message.EndsWith("alg 17 SKU") || ex.Message.EndsWith("alg 20 SKU") || ex.Message.EndsWith("Please enter valid no of years"))
                    {
                        //SET FAILURE STATUS
                        objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                        objDFLicenseResponse.ResponseMessage = ex.Message;
                        objDFLicenseResponse.LicenseKey = "";

                    }
                    else
                    {

                        //SET FAILURE STATUS
                        objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                        objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();
                        objDFLicenseResponse.LicenseKey = "";
                    }

                    //LOG EXCEPTION
                    objEventLogger.WriteLog("GetLicense:"+ex.Message + Environment.NewLine + ex.StackTrace, objDFLicenseRequest.RequestingSystem, DateTime.UtcNow, objDFLicenseRequest.RequestID);
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
            }

            return objDFLicenseResponse;
        }
        #endregion

    }
}
