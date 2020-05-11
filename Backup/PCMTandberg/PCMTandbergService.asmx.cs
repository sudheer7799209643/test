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
using PCMTandberg.ProcessMessage;



namespace PCMTandberg
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    // [WebService(Namespace = "http://tempuri.org/")]
    //[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    // [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    [WebServiceBinding(ConformsTo = WsiProfiles.None)]
    [WebService(Namespace = Constants.TandbergWebServiceNameSpace)]
    public class PCMTandbergService : PCMTandbergSvc
    {
        #region GetLicense WebMethod
        /// <summary>
        /// To Get DF License
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.GetLicenseWebMethodDescription)]
        public override DFLicenseResponse GetLicense(DFLicenseRequest objDFLicenseRequest)
        {
            LicenseProcess objLicenseProcess = new LicenseProcess();
            return objLicenseProcess.GetLicense(objDFLicenseRequest);
        }
        #endregion

        #region GetLicenses WebMethod
        /// <summary>
        /// Method to get Licenses for Add-On/Product Upgrade related options.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.GetLicensesWebMethodDescription)]
        public override LicenseResponse GetLicenses(LicenseRequest objLicenseRequest)
        {
            LicensesProcess objLicenseProcess = new LicensesProcess();
            return objLicenseProcess.GetLicenses(objLicenseRequest);
        }
        #endregion

        #region GetSoftwareVersions WebMethod
        /// <summary>
        /// Metod to get Available SKU Versions for specified SKU ID
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.GetSoftwareVersionsWebMethodDescription)]
        public override GetSoftwareVersionsResponse GetSoftwareVersions(GetSoftwareVersionsRequest objSoftwareVersionsRequest)
        {
            SoftwareVersionsProcess objSoftwareVersionsProcess = new SoftwareVersionsProcess();
            return objSoftwareVersionsProcess.GetSoftwareVersions(objSoftwareVersionsRequest);
        }

        #endregion

        #region ValidateSKU WebMethod
        /// <summary>
        /// Checks if a SKU or PID  either SW feature or SW Option is avilable in AT or not.
        /// </summary>
        /// <param name="objValidateSKURequest"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.ValidateSKUWebMethodDescription)]
        public override ValidateSKUResponse ValidateSKU(ValidateSKURequest objValidateSKURequest)
        {
            ValidateSKUProcess objValidateSKUProcess = new ValidateSKUProcess();
            return objValidateSKUProcess.ValidateSKUVersions(objValidateSKURequest);
        }

        #endregion

        #region GetParentChildAssocation WebMethod
        /// <summary>
        /// Method to get Parent Child Association
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.GetParentChildAssociationWebMethodDescription)]
        public override GetParentChildAssociationResponse GetParentChildAssociation(GetParentChildAssociationRequest objParentChildAssociationRequest)
        {
            ParentChildAssociationProcess objParentChildAssociationProcess = new ParentChildAssociationProcess();
            return objParentChildAssociationProcess.GetParentChildAssociation(objParentChildAssociationRequest);
        }
        #endregion



        #region GetSNHierarchy   WebMethod
        /// <summary>
        /// Method to get Parent Child Association
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.GetParentChildAssociationWebMethodDescription)]
        public override GetSNHierarchyResponse GetSNHierarchy(GetSNHierarchyRequest objGetSNHierarchyRequest)
        {
            GetSNHierarchyProcess objGetSNHierarchyProcess = new GetSNHierarchyProcess();
            return objGetSNHierarchyProcess.GetSNHierarchy(objGetSNHierarchyRequest);
        }
        #endregion



        #region RMASwap WebMethod
        /// <summary>
        /// Method to perform RMA Swap
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [WebMethod(Description = Constants.RMASwapWebMethodDescription)]
        public override RMASwapResponse RMASwap(RMASwapRequest objRMASwapRequest)
        {

            RMASwapProcess objRMASwapProcess = new RMASwapProcess();
            return objRMASwapProcess.RMASwap(objRMASwapRequest);
            
        }
        #endregion

        #region SoftDeleteLicenses WebMethod
        [WebMethod(Description = Constants.SoftDeleteLicensesWebMethodDescription)]
        public override SoftDeleteLicensesResponse SoftDeleteLicenses(SoftDeleteLicensesRequest objSoftDeleteLicensesRequest)
        {
            SoftDeleteLicensesProcess objSoftDeleteLicensesProcess = new SoftDeleteLicensesProcess();
            return objSoftDeleteLicensesProcess.SoftDeleteLicenses(objSoftDeleteLicensesRequest);
        }

        #endregion

        #region SetSKUMetadataResponse WebMethod
        [WebMethod(Description = Constants.SetSKUMetadataWebMethodDescription)]
        public override SetSKUMetadataResponse SetSKUMetadata(SetSKUMetadataRequest objSetSKUMetadataRequest)
        {

            SetSKUMetadataProcess objSetSKUMetadataProcess = new SetSKUMetadataProcess();
            return objSetSKUMetadataProcess.SetSKUMetadata(objSetSKUMetadataRequest);
          
        }
        #endregion
    }
}
