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
    public partial class GetSNHierarchyProcess
    {
        #region Property & Variables
        EventLogger objEventLogger;
        TransactionLogger objTransactionLogger;
        ParentChildAssociationBL objParentChildAssociationBL;
        #endregion 
        
        #region .ctor
        public GetSNHierarchyProcess()
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
        public GetSNHierarchyResponse GetSNHierarchy(GetSNHierarchyRequest objGetSNHierarchyRequest)
        {
            GetSNHierarchyResponse objGetSNHierarchyResponse = new GetSNHierarchyResponse();
            GetSNHierarchyBL objBL = new GetSNHierarchyBL();

            try
            {
                //do validation here
                if (objBL.ValidateRequest(objGetSNHierarchyRequest, objGetSNHierarchyResponse))
                {
                    objBL.ProcessRequest(objGetSNHierarchyRequest, objGetSNHierarchyResponse);
                }
            }
            catch (Exception ex)
            {
                //SET FAILURE STATUS
                objGetSNHierarchyResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
                objGetSNHierarchyResponse.ResponseMessage = Constants.ResponseMessage[1000].ToString();

                //LOG EXCEPTION
                objEventLogger.WriteLog(ex.Message + Environment.NewLine + ex.StackTrace, objGetSNHierarchyRequest.RequestingSystem, DateTime.UtcNow, objGetSNHierarchyRequest.RequestID);
            }
            finally
            {

                objGetSNHierarchyResponse.ResponseDateTime = DateTime.UtcNow;

                if (objGetSNHierarchyRequest.SNum != null)
                {

                    for (int s = 0; s < objGetSNHierarchyRequest.SNum.Length; s++)
                    {
                        if (objGetSNHierarchyRequest.SNum[s].Length > 0)
                        {

                            //if ((objGetSNHierarchyRequest.Filter != null))
                            //{
                            //    if (objGetSNHierarchyRequest.Filter.LookupFilter == null)
                            //    {
                            //        objGetSNHierarchyResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                            //        objGetSNHierarchyResponse.Association[s].Message = "Please select LookUp Filter (FilterName and FilterValue) to proceed";
                            //        objGetSNHierarchyResponse.Association[s].SNumList = null;
                            //        break;
                            //    }


                            //    if (
                            //                        (
                            //                        (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"]) ||
                            //                        (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"]) ||
                            //                        (objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"])
                            //                        ) &&
                            //                        (objParentChildAssociationResponse.ResponseStatus == 0) &&
                            //                        (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"]))
                            //                        )
                            //    {
                            //        objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                            //        objParentChildAssociationResponse.Association[s].Message = "Invalid FilterName. The valid Filter Name is LOOK_UP.";
                            //        objParentChildAssociationResponse.Association[s].SNumList = null;
                            //    }

                            //    else
                            //    {

                            //        if ((
                            //             (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"])) &&
                            //              (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])) &&
                            //              (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"]))
                            //              ) &&
                            //              (objParentChildAssociationResponse.ResponseStatus == 0) &&
                            //              (objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                            //              )
                            //        {
                            //            objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                            //            objParentChildAssociationResponse.Association[s].Message = "Invalid FilterValue. The valid Filter Values are SN_HIERARCHY_AND_SW_VERSIONS,SN_HIERARCHY_ONLY and ALL.";
                            //            objParentChildAssociationResponse.Association[s].SNumList = null;
                            //        }

                            //        else
                            //        {
                            //            if (
                            //        (
                            //        (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"])) &&
                            //        (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])) &&
                            //        (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"]))
                            //        ) &&
                            //        (objParentChildAssociationResponse.ResponseStatus == 0) &&
                            //        (!(objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"]))
                            //        )
                            //            {
                            //                objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.InvalidParameter;
                            //                objParentChildAssociationResponse.Association[s].Message = "Invalid/blank/null FilterName and FilterValue. The valid Filter Name is LOOK_UP and the valid Filter Values are SN_HIERARCHY_AND_SW_VERSIONS,SN_HIERARCHY_ONLY and ALL.";
                            //                objParentChildAssociationResponse.Association[s].SNumList = null;
                            //            }

                            //            else
                            //            {

                            //                //if ((objParentChildAssociationRequest.Filter != null) && (objParentChildAssociationRequest.Filter.LookupFilter != null)
                            //                //    && (objParentChildAssociationResponse.ResponseStatus == 0))
                            //                //{
                            //                if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_ONLY"])
                            //                    && (objParentChildAssociationResponse.ResponseStatus == 0)
                            //                    && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                            //                {
                            //                    for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                            //                    {
                            //                        SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                            //                        SNums[] snumlist = list1.SNumList;
                            //                        SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                            //                        if (IsSNumList.Length > 0)
                            //                        {
                            //                            list1.LicensableSNum = IsSNumList[0].SNum;
                            //                            list1.SNumList = null;
                            //                        }
                            //                        else
                            //                        {
                            //                            objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                            //                            objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                            //                            objParentChildAssociationResponse.Association[s].SNumList = null;
                            //                        }

                            //                    }
                            //                    break;
                            //                }
                            //                else
                            //                {
                            //                    if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["SN_HIERARCHY_AND_SW_VERSIONS"])
                            //                        && (objParentChildAssociationResponse.ResponseStatus == 0)
                            //                    && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                            //                    {
                            //                        for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                            //                        {

                            //                            SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                            //                            SNums[] snumlist = list1.SNumList;
                            //                            SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                            //                            if (IsSNumList.Length > 0)
                            //                            {
                            //                                list1.LicensableSNum = IsSNumList[0].SNum;
                            //                                SKUDetailInfo[] inputswsku = IsSNumList[0].SKU.ToArray<SKUDetailInfo>();
                            //                                var swsku1 = inputswsku.Where(o => o.SKUType == "SW");
                            //                                IsSNumList[0].SKU = swsku1.ToArray();
                            //                                list1.SNumList = IsSNumList;
                            //                            }
                            //                            else
                            //                            {
                            //                                objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                            //                                objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                            //                                objParentChildAssociationResponse.Association[s].SNumList = null;
                            //                            }
                            //                        }
                            //                        break;

                            //                    }

                            //                    else
                            //                    {
                            //                        if ((objParentChildAssociationRequest.Filter.LookupFilter.FilterValue == ConfigurationSettings.AppSettings["ALL"]) && (objParentChildAssociationResponse.ResponseStatus == 0)
                            //                    && objParentChildAssociationRequest.Filter.LookupFilter.FilterName == ConfigurationSettings.AppSettings["LOOK_UP"])
                            //                        {
                            //                            for (int i = 0; i < objParentChildAssociationRequest.SNum.Length; i++)
                            //                            {

                            //                                SNumsAssociation list1 = objParentChildAssociationResponse.Association[i];
                            //                                SNums[] snumlist = list1.SNumList;
                            //                                SNums[] IsSNumList = snumlist.Where(o => o.IsLicensableSNum == true).ToArray<SNums>();
                            //                                if (IsSNumList.Length > 0)
                            //                                {
                            //                                    list1.LicensableSNum = IsSNumList[0].SNum;
                            //                                    list1.SNumList = IsSNumList;
                            //                                }
                            //                                else
                            //                                {
                            //                                    objParentChildAssociationResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                            //                                    objParentChildAssociationResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                            //                                    objParentChildAssociationResponse.Association[s].SNumList = null;
                            //                                }
                            //                            }
                            //                            break;
                            //                        }
                            //                    }
                            //                }
                            //                //}
                            //            }
                            //        }
                            //    }
                            //}

                            //else
                            //{
                                //fix for defect 2904 - add null check
                                if (objGetSNHierarchyResponse.Association != null)
                                {

                                if ((objGetSNHierarchyResponse.Association[0].SNumList.Length <= 0))
                                {
                                    objGetSNHierarchyResponse.Association[s].Status = (int)Constants.ResponseStatus.NoParentChildFound;
                                    objGetSNHierarchyResponse.Association[s].Message = Constants.ResponseMessage[8].ToString();
                                    objGetSNHierarchyResponse.Association[s].SNumList = null;
                                }
                                }
                            //}


                        }
                    }
                }

                //SERIALIZE REQUEST/RESPONSE
                string request = "GetParentChildAssociationRequest";
                string response = "GetParentChildAssociationResponse";

                try
                {
                    request = Util.SerializeObject(objGetSNHierarchyRequest);
                    response = Util.SerializeObject(objGetSNHierarchyResponse);
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
                objTransactionLogger.LogTransaction(objGetSNHierarchyRequest.RequestID, objGetSNHierarchyRequest.RequestDateTime, objGetSNHierarchyRequest.RequestingSystem,
                    request, objGetSNHierarchyResponse.ResponseID, objGetSNHierarchyResponse.ResponseDateTime, response,
                    objGetSNHierarchyResponse.ResponseStatus, objGetSNHierarchyResponse.ResponseMessage, 0);

                //EVENT LOG ENTRY
                objEventLogger.WriteEntry("End Processing GetParentChildAssociation");
            }
            return objGetSNHierarchyResponse;
        }

        public GetSNHierarchyResponse GetSNHierarchy(GetSNHierarchyRequest objParentChildAssociationRequest, bool ValidateRequest)
        {


            return new GetSNHierarchyResponse();
        }
        #endregion

    }
}
