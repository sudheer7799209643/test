using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PCMTandberg.BusinessEntities
{
  
   

    #region Constants
    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public class Constants
    {
        #region Service Response Status
        /// <summary>
        ///Operation Status response
        /// </summary>
        public enum ResponseStatus
        {
           Success = 0,
           InvalidParameter = 1,
           UnKnownSKU=2,
           AmbigousRequest=3,
           Unauthorized = 4,
           Timeout = 5,
           NotImplemented = 6,
           NoLicSNUMFound =7,
           NoParentChildFound =8,
           InvSrcLicensableSNum =9,
           InvDstLicensableSNum = 10,
           SoftDeletionFailed =11,
           ServiceUnavailable = 1000
        }
        #endregion

        #region SKUType
        /// <summary>
        ///Operation Status response
        /// </summary>
        public enum SKUType
        {
            SW = 1,
            OPTION = 2
        }
        #endregion

        /// <summary>
        /// List of VersionFilter
        /// </summary>
        public static readonly List<string> VersionFilter = new List<string>()
                                                {
                                                 "ALL",
                                                 "LATEST"
                                                };

        #region Service Response Message
        /// <summary>
        /// List of ResponseMessage
        /// </summary>
        public static readonly Dictionary<int, string> ResponseMessage = new Dictionary<int, string>()
                                                        {
                                                            {0,         "Success"},
                                                            {1,         "Invalid Parameter"},
                                                            {2,         "UnKnown SKU / No SKU Found / No SKU Details Found"},
                                                            {3,         "Duplicate Request/Ambigous Request"},
                                                            {4,         "Unauthorized Access"},
                                                            {5,         "Service Timeout"},
                                                            {6,         "Function Not Implemented"},
                                                            {7,         "No Licensable SNum Found for Given SNum"},
                                                            {8,         "No Parent Child Record Found"},
                                                            {9,         "Invalid Source Licensable SNum"},
                                                            {10,         "Invalid Destination Licensable SNum"},
                                                            {11,         "Soft Deletion of Key Failed"},
                                                            {1000,      "Failure Occurred / Service Currently Unavailable / Raise a case with Support Team"}
                                                        };
        #endregion

        #region Requesting System
        /// <summary>
        /// List of Requesting System
        /// </summary>
        public static readonly List<string> RequestingSystem = new List<string>()
                                                {
                                                 "AUTOTEST",
                                                 "SWIFT", 
                                                 "TANDBERG",
                                                 "DEV",
                                                 "QA",
                                                 "STAG",
                                                 "PILOT"
                                                };
        #endregion

        #region Web service Method Description

        public const string GetLicenseWebMethodDescription = "Generate a License for a given Serial number and SKU.";
        public const string GetLicensesWebMethodDescription = "Generate Licenses for multiple Serial numbers.Used for New/Add-on/Upgrade license generation.";
        public const string GetSoftwareVersionsWebMethodDescription = "Get all available version for a given SKU.";
        public const string ValidateSKUWebMethodDescription = "Checks if a SKU or PID  either SW feature or SW Option is avilable in AT or not.";
        public const string GetParentChildAssociationWebMethodDescription = "To Identify the parent-children association from TST.";
        public const string RMASwapWebMethodDescription = "Generate licenses for swapped Serial number and update parent-child in TST.";
        public const string SoftDeleteLicensesWebMethodDescription = "This Method is used to remove unwanted or invalid or not needed or additional License Keys for a given serial number and SKU(s).";
        public const string SetSKUMetadataWebMethodDescription = "This method is uded by SWIFT to update SKU metadata into Autotest.";

        #endregion

        #region Web service Namespace Constant
        public const string TandbergWebServiceNameSpace = "https://cesium.cisco.com/PCMTandbergService";
         public const string DataTypeNameSpace = "Type";
         public const string BindingNameSpace = "ExternalWSDL";
        #endregion

    }
    #endregion

}
