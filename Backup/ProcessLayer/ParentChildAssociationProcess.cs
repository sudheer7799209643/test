using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using PCMTandberg.BusinessEntities;
using PCMTandberg.BusinessLogic;
using PCMTandberg.Logger;
using System.Data;

namespace PCMTandberg.ProcessMessage
{
    public partial class ParentChildAssociationProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        ParentChildAssociationBL objParentChildAssociationBL;
        #endregion 
        
        #region .ctor
        public ParentChildAssociationProcess()
        {
            objEventLogger = new EventLogger();
            objTransactionLogger = new TransactionLogger();
            objParentChildAssociationBL = new ParentChildAssociationBL();
        }
        #endregion

        #region GetParentChildAssociation
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objGetParentChildAssociationRequest"></param>
        /// <returns></returns>
        public GetParentChildAssociationResponse GetParentChildAssociation(GetParentChildAssociationRequest objParentChildAssociationRequest)
        {
            GetParentChildAssociationResponse objParentChildAssociationResponse = new GetParentChildAssociationResponse();
            ParentChildAssociationBL objBL = new ParentChildAssociationBL();

            try
            {
                //do validation here
                if (objBL.ValidateRequest(objParentChildAssociationRequest, objParentChildAssociationResponse)) {
                    objBL.ProcessRequest(objParentChildAssociationRequest, objParentChildAssociationResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objParentChildAssociationResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objParentChildAssociationResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objParentChildAssociationRequest.RequestingSystem, DateTime.UtcNow, objParentChildAssociationRequest.RequestID);
            }
            finally
            {

                objParentChildAssociationResponse.ResponseDateTime = DateTime.UtcNow;

                if (objParentChildAssociationRequest.SNum != null)
                {

                    for (int s = 0; s < objParentChildAssociationRequest.SNum.Length; s++)
                    {
                        if (objParentChildAssociationRequest.SNum[s].Length > 0)
                        {

                            if ((objParentChildAssociationRequest.Filter != null))
                            {
                                if (objParentChildAssociationRequest.Filter.LookupFilter == null)
                                {
                                    objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                                    objParentChildAssociationResponse.Association[s].Message = "Please select LookUp Filter (FilterName and FilterValue) to proceed";
                                    objParentChildAssociationResponse.Association[s].SNumList = null;
                                    break;
                                }


                                if (
                                                    (
                                                    (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"]) ||
                                                    (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"]) ||
                                                    (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"])
                                                    ) &&
                                                    (objParentChildAssociationResponse.ResponseStatus == 0) &&
                                                    (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"]))
                                                    )
                                {
                                    objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                                    objParentChildAssociationResponse.Association[s].Message = "Invalid FilterName. The valid Filter Name is LOOK_UP.";
                                    objParentChildAssociationResponse.Association[s].SNumList = null;
                                }

                                else
                                {

                                    if ((
                                         (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"])) &&
                                          (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])) &&
                                          (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"]))
                                          ) &&
                                          (objParentChildAssociationResponse.ResponseStatus == 0) &&
                                          (objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                                          )
                                    {
                                        objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                                        objParentChildAssociationResponse.Association[s].Message = "Invalid FilterValue. The valid Filter Values are SN_HIERARCHY_AND_SW_VERSIONS,SN_HIERARCHY_ONLY and ALL.";
                                        objParentChildAssociationResponse.Association[s].SNumList = null;
                                    }

                                    else
                                    {
                                        if (
                                    (
                                    (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"])) &&
                                    (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])) &&
                                    (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"]))
                                    ) &&
                                    (objParentChildAssociationResponse.ResponseStatus == 0) &&
                                    (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"]))
                                    )
                                        {
                                            objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                                            objParentChildAssociationResponse.Association[s].Message = "Invalid/blank/null FilterName and FilterValue. The valid Filter Name is LOOK_UP and the valid Filter Values are SN_HIERARCHY_AND_SW_VERSIONS,SN_HIERARCHY_ONLY and ALL.";
                                            objParentChildAssociationResponse.Association[s].SNumList = null;
                                        }

                                        else
                                        {

                                            //if ((objParentChildAssociationRequest.Filter != null) && (objParentChildAssociationRequest.Filter.LookupFilter != null)
                                            //    && (objParentChildAssociationResponse.ResponseStatus == 0))
                                            //{
                                            if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"])
                                                && (objParentChildAssociationResponse.ResponseStatus == 0)
                                                && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                                            {
                                                for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                                                {
                                                    SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                                                    SNums[] snumlist = list1.SNumList;
                                                    SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                                                    if (IsSNumList.Length > 0)
                                                    {
                                                        list1.LicensableSNum = IsSNumList[0].SNum;
                                                        list1.SNumList = null;
                                                    }
                                                    else
                                                    {
                                                        objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                                        objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                                                        objParentChildAssociationResponse.Association[s].SNumList = null;
                                                    }

                                                }
                                                break;
                                            }
                                            else
                                            {
                                                if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])
                                                    && (objParentChildAssociationResponse.ResponseStatus == 0)
                                                && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                                                {
                                                    for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                                                    {

                                                        SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                                                        SNums[] snumlist = list1.SNumList;
                                                        SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                                                        if (IsSNumList.Length > 0)
                                                        {
                                                            list1.LicensableSNum = IsSNumList[0].SNum;
                                                            SKUDetailInfo[] inputswsku = IsSNumList[0].SKU.ToArray<SKUDetailInfo>();
                                                            var swsku1 = inputswsku.Where(o => o.SKUType == "SW");
                                                            IsSNumList[0].SKU = swsku1.ToArray();
                                                            list1.SNumList = IsSNumList;
                                                        }
                                                        else
                                                        {
                                                            objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                                            objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                                                            objParentChildAssociationResponse.Association[s].SNumList = null;
                                                        }
                                                    }
                                                    break;

                                                }

                                                else
                                                {
                                                    if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"]) && (objParentChildAssociationResponse.ResponseStatus == 0)
                                                && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                                                    {
                                                        for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                                                        {

                                                            SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                                                            SNums[] snumlist = list1.SNumList;
                                                            SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                                                            if (IsSNumList.Length > 0)
                                                            {
                                                                list1.LicensableSNum = IsSNumList[0].SNum;
                                                                list1.SNumList = IsSNumList;
                                                            }
                                                            else
                                                            {
                                                                objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                                                objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                                                                objParentChildAssociationResponse.Association[s].SNumList = null;
                                                            }
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            //}
                                        }
                                    }
                                }
                            }

                            else
                            {
                                //fix for defect 2904 - add null check
                                if (objParentChildAssociationResponse.Association!=null)
                                {

                                if ((objParentChildAssociationResponse.Association[0].SNumList.Length <= 0))
                                {
                                    objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                    objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                                    objParentChildAssociationResponse.Association[s].SNumList = null;
                                }
                                }
                            }


                        }
                    }
                }

                //SERIALIZE REQUEST/RESPONSE
                string request = "GetParentChildAssociationRequest";
                string response = "GetParentChildAssociationResponse";

                try
                {
                    request = Util.SerializeObject(objParentChildAssociationRequest);
                    response = Util.SerializeObject(objParentChildAssociationResponse);
                }
                catch (Exception ex)
                {
                    objEventLogger.WriteEntry("Request/Response Object Serialization Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }

                if (!request.IsValidString())
                    request = "GetParentChildAssociationRequest";
                if (!response.IsValidString())
                    response = "GetParentChildAssociationResponse";


                //LOG REQUEST/RESPONSE
                objTransactionLogger.LogTransaction(objParentChildAssociationRequest.RequestID, objParentChildAssociationRequest.RequestDateTime, objParentChildAssociationRequest.RequestingSystem,
                    request, objParentChildAssociationResponse.ResponseID, objParentChildAssociationResponse.ResponseDateTime, response,
                    objParentChildAssociationResponse.ResponseStatus, objParentChildAssociationResponse.ResponseMessage, 0);

                //EVENT LOG ENTRY
                objEventLogger.WriteEntry("End Processing GetParentChildAssociation");
            }
            return objParentChildAssociationResponse;
        }

        public GetParentChildAssociationResponse GetParentChildAssociation(GetParentChildAssociationRequest objParentChildAssociationRequest, bool ValidateRequest)
        {


            return new GetParentChildAssociationResponse();
        }
        #endregion

    }
}
