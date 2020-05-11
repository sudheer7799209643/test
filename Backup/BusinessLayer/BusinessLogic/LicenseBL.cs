using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCMTandberg.BusinessEntities;
using PCMTandberg.DataAccess;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace PCMTandberg.BusinessLogic
{
    public class LicenseBL
    {
        #region Property & Variables
        string connectionString = "";
        string TandbergCAPrimary;
        string TandbergCASecondary;
        X509Certificate2 objX509Certificate2;
        string tslCertPath = "";
        CommonValidationBL objCommonValidationBL;
        string expiryDateAlg17 = "";
        //NEW CR - Added to call the new methods written in CommonBL
        CommonBL objCommonBL;

        private string newSKUIDField;
        public string NewSKUID
        {
            get { return newSKUIDField; }
            set { newSKUIDField = value; }
        }

        private string algSeedField;
        public string AlgSeed
        {
            get { return algSeedField; }
            set { algSeedField = value; }
        }

        private string algIDField;
        public string AlgID
        {
            get { return algIDField; }
            set { algIDField = value; }
        }

        private int algQtyField;
        public int AlgQty
        {
            get { return algQtyField; }
            set { algQtyField = value; }
        }

        private int allowManyField;
        public int AllowMany
        {
            get { return allowManyField; }
            set { allowManyField = value; }
        }

        private int sKUTypeField;
        public int SKUType
        {
            get { return sKUTypeField; }
            set { sKUTypeField = value; }
        }

        private string sKUVersionField;
        public string SKUVersion
        {
            get { return sKUVersionField; }
            set { sKUVersionField = value; }
        }

        private int historicQtyField;
        public int HistoricQty
        {
            get { return historicQtyField; }
            set { historicQtyField = value; }
        }

        private int currentIndexField;
        public int CurrentIndex
        {
            get { return currentIndexField; }
            set { currentIndexField = value; }
        }


        #endregion

        #region .ctor
        public LicenseBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
            TandbergCAPrimary = ConfigurationSettings.AppSettings["TandbergCAPrimary"];
            TandbergCASecondary = ConfigurationSettings.AppSettings["TandbergCASecondary"];
            tslCertPath = ConfigurationSettings.AppSettings["tslCertPath"];
            expiryDateAlg17 = ConfigurationSettings.AppSettings["expiryDateAlg17"];
            objX509Certificate2 = new X509Certificate2(tslCertPath, "");
            objCommonValidationBL = new CommonValidationBL();
            //NEW CR - Added to call the new methods written in CommonBL
            objCommonBL = new CommonBL();
        }
        #endregion

        #region ValidateRequest
        public bool ValidateRequest(DFLicenseRequest objDFLicenseRequest, DFLicenseResponse objDFLicenseResponse)
        {
            bool ValidationStatus = true;

            if (!objDFLicenseRequest.RequestID.IsValidString() && ValidationStatus)
            {
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objDFLicenseResponse.ResponseMessage = "Invalid Request ID.";
                ValidationStatus = false;
            }
            if (!objDFLicenseRequest.RequestingSystem.IsValidString() && ValidationStatus)
            {
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objDFLicenseResponse.ResponseMessage = "Requesting System Value is Empty.";
                ValidationStatus = false;
            }
            if (objCommonValidationBL.DupCheckRequestId(objDFLicenseRequest.RequestID, objDFLicenseRequest.RequestingSystem) && ValidationStatus)
            {
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.AmbigousRequest;
                objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[3].ToString();
                ValidationStatus = false;
            }
            if (!objDFLicenseRequest.SNum.IsValidSNumFormat() && ValidationStatus)  //VALIDATE SNUM FORMAT
            {
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                objDFLicenseResponse.ResponseMessage = "Invalid SNum Format";
                ValidationStatus = false;
            }

            if ((objDFLicenseRequest.MacAddress != null) && ValidationStatus)  //VALIDATE MAC address
            {
                if (objDFLicenseRequest.MacAddress.Length != 12)
                {
                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objDFLicenseResponse.ResponseMessage = "MAC address entered is less than 12 characters. Please preceed the MAC address with necessary zeroes";
                    ValidationStatus = false;
                }
            }

            if ((String.IsNullOrEmpty(objDFLicenseRequest.Seed) || String.IsNullOrEmpty(objDFLicenseRequest.AlgType)) && ValidationStatus)
            {
                if (String.IsNullOrEmpty(objDFLicenseRequest.SKUID))
                {
                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objDFLicenseResponse.ResponseMessage = "Insufficient Inputs";
                    ValidationStatus = false;
                }

            }

            if (objDFLicenseRequest.AlgType == "17" && ValidationStatus)
            {
                if (String.IsNullOrEmpty(objDFLicenseRequest.ExpDate.ToString()))
                {
                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objDFLicenseResponse.ResponseMessage = "Expiry date is mandatory for alg 17 SKU";
                    ValidationStatus = false;
                }
                else if (objDFLicenseRequest.ExpDate < DateTime.Now)
                {
                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                    objDFLicenseResponse.ResponseMessage = "Expiry date should be greater then today's date for alg 17 SKU";
                    ValidationStatus = false;
                }

            }

            return ValidationStatus;
        }
        #endregion

        #region GetLicense
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objDFLicenseRequest"></param>
        /// <param name="objDFDFLicenseResponse"></param>
        /// <returns></returns>
        public bool GetLicense(DFLicenseRequest objDFLicenseRequest, DFLicenseResponse objDFLicenseResponse)
        {
            return GetLicense(objDFLicenseRequest, objDFLicenseResponse, true, false);
        }
        #endregion

        #region GetLicense
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objDFLicenseRequest"></param>
        /// <param name="objDFDFLicenseResponse"></param>
        /// <returns></returns>
        public bool GetLicense(DFLicenseRequest objDFLicenseRequest, DFLicenseResponse objDFLicenseResponse, bool IsDFRequest, bool IsRMARequest)
        {
            //START PROCESSING  
            bool successflag = false;

            //OPERATIONAL FLAGS
            bool CMRequest = false;
            bool NewKeyGenerated = false;
            bool RequireNewKey = true;
            int IsShippedVersion = IsDFRequest || IsRMARequest ? 1 : 0;
            string ErrorMessage = string.Empty;

            var objLicRequest = new LicRequest();
            var objLicResponse = new LicResponse();
            var objCAProxy = new SigServTandbergService();
            bool dataflag = false;

            string format = "yyyyMMdd";    // Use this format
            string display = objDFLicenseRequest.ExpDate.ToString(format);


            try
            {

                //CHECK IF ITS CM REQUEST OR NOT
                if (objDFLicenseRequest.Seed.IsValidString() && objDFLicenseRequest.AlgType.IsValidString())
                    CMRequest = true;

                //CALL TO GET LICENSE PARAMETERS FROM DATABASE
                dataflag = GetSKULicenseMetadata(objDFLicenseRequest.SKUID, objDFLicenseRequest.Version, out ErrorMessage);

                //IF NOT QUANTITY SEPCIFIED IN REQUEST DEFAULT IS 1
                if (!objDFLicenseRequest.QtySpecified || objDFLicenseRequest.Qty <= 0)
                {
                    objDFLicenseRequest.Qty = 1;
                    objDFLicenseRequest.QtySpecified = true;
                }


                if (!CMRequest && dataflag)
                {
                    objDFLicenseRequest.SKUID = string.IsNullOrEmpty(objDFLicenseRequest.SKUID) ? "" : (String.IsNullOrEmpty(this.NewSKUID) ? objDFLicenseRequest.SKUID : this.NewSKUID);
                    objDFLicenseRequest.AlgType = !string.IsNullOrEmpty(this.AlgID) ? this.AlgID : objDFLicenseRequest.AlgType;


                    //Changes for bug 4806,4807 resolution 
                    if (objDFLicenseRequest.AlgType == "17")
                    {
                        //changes done for the mark tovey request - time based RMS keys for alg 17
                        objDFLicenseRequest.ExpDate = DateTime.Parse(expiryDateAlg17);
                        //CR for TimeBases RMA keys Requested By Mark Tovey. Dated 07/10/2014
                        if (objDFLicenseRequest.SKUID.EndsWith("Y"))
                        {
                            string SKUID = objDFLicenseRequest.SKUID;
                            string years = SKUID.Substring(SKUID.LastIndexOf('-') + 1);
                            string noOfYears = years.Substring(0, years.LastIndexOf('Y'));
                            int yearscount = 0;
                            if (int.TryParse(noOfYears, out yearscount))
                            {
                                if(yearscount==0)
                                {
                                    throw new Exception("RMA SKU Expiry years cannot be 0.Please enter valid no of years");
                                }
                                //else
                                //{
                                //    //int days = (yearscount * 365) + 29;
                                //    //objDFLicenseRequest.ExpDate = DateTime.Now.AddDays(days);
                                //    objDFLicenseRequest.ExpDate = DateTime.Parse(expiryDateAlg17);
                                //}
                            }
                            else
                            {
                                throw new Exception("RMA SKU is not in correct format.Please enter valid no of years");
                            }
                        }                      

                        else if (objDFLicenseRequest.ExpDate.ToShortDateString()=="1/1/0001")
                        {
                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            throw new Exception("Expiry date is mandatory for alg 17 SKU");
                        }
                        else if (objDFLicenseRequest.ExpDate < DateTime.Now)
                        {
                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            throw new Exception("Expiry date should be greater then today's date for alg 17 SKU");
                        }
                    }
                    if (objDFLicenseRequest.AlgType == "21")
                    {
                        //changes done for the mark tovey request - time based RMS keys for alg 17
                        //objDFLicenseRequest.ExpDate = objDFLicenseRequest.ExpDate;
                        //CR for TimeBases RMA keys Requested By Mark Tovey. Dated 07/10/2014
                        if (objDFLicenseRequest.ExpDate.ToShortDateString() == "1/1/0001")
                        {
                            objDFLicenseRequest.AlgType = "19";
                            //objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            //throw new Exception("Expiry date is mandatory for alg 21 SKU");
                        }
                        else if (objDFLicenseRequest.ExpDate < DateTime.Now)
                        {                            
                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            throw new Exception("Expiry date should be greater then today's date for alg 21 SKU");
                        }
                    }

                    // New Alg 20 - CR for generating licenses similar to old alg 17
                    if (objDFLicenseRequest.AlgType == "20")
                    {
                       
                        if (objDFLicenseRequest.SKUID.EndsWith("Y"))
                        {
                            string SKUID = objDFLicenseRequest.SKUID;
                            string years = SKUID.Substring(SKUID.LastIndexOf('-') + 1);
                            string noOfYears = years.Substring(0, years.LastIndexOf('Y'));
                            int yearscount = 0;
                            if (int.TryParse(noOfYears, out yearscount))
                            {
                                if (yearscount == 0)
                                {
                                    throw new Exception("RMA SKU Expiry years cannot be 0.Please enter valid no of years");
                                }
                                else
                                {
                                    int days = (yearscount * 365) + 29;
                                    objDFLicenseRequest.ExpDate = DateTime.Now.AddDays(days);
                                }

                            }
                            else
                            {
                                throw new Exception("RMA SKU is not in correct format.Please enter valid no of years");
                            }
                        }


                        else if (objDFLicenseRequest.ExpDate.ToShortDateString() == "1/1/0001")
                        {
                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            throw new Exception("Expiry date is mandatory for alg 20 SKU");
                        }
                        else if (objDFLicenseRequest.ExpDate < DateTime.Now)
                        {
                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
                            throw new Exception("Expiry date should be greater then today's date for alg 20 SKU");
                        }
                    }

                    if (objDFLicenseRequest.AlgType == "51" || objDFLicenseRequest.AlgType == "53")
                    {
                        long MinMacAdr = GetMinMacAddress_Alg51_53(objDFLicenseRequest.SNum);
                        long tempLong = long.Parse(MinMacAdr.ToString());
                        string MacAddress = tempLong.ToString("X");
                        MacAddress = (MacAddress.PadLeft(12, '0'));
                        if (objDFLicenseRequest.AlgType == "51")
                        {
                            objDFLicenseRequest.AlgType = "11";
                            objDFLicenseRequest.MacAddress = MacAddress;
                        }
                        else
                        {
                            if (objDFLicenseRequest.AlgType == "53")
                            {
                                objDFLicenseRequest.AlgType = "13";
                                objDFLicenseRequest.MacAddress = MacAddress;
                            }
                        }
                    }


                    objDFLicenseRequest.Seed = !string.IsNullOrEmpty(this.AlgSeed) ? this.AlgSeed : objDFLicenseRequest.Seed;
                    objDFLicenseRequest.Version = string.IsNullOrEmpty(this.SKUVersion) ? objDFLicenseRequest.Version : this.SKUVersion;

                    //UPDATE DF QUANTITY BY MULTIPLYING QTY FROM METADATA
                    if (this.AlgQty > 0)
                    {
                        if (!IsRMARequest) //UPDATED QTY BY MULTIPLYING METADATA QTY
                        {
                            if (this.AllowMany == 1 && this.SKUType == 2)
                            {
                                objDFLicenseRequest.Qty = objDFLicenseRequest.Qty * this.AlgQty;
                            }
                            else
                            {
                                if (this.AllowMany == 0 && this.SKUType == 2 && (this.AlgID == "13" || this.AlgID == "53"))
                                {
                                    objDFLicenseRequest.Qty = objDFLicenseRequest.Qty * this.AlgQty;
                                }
                                //objDFLicenseRequest.Qty = this.AlgQty > 0 ? this.AlgQty : objDFLicenseRequest.Qty;
                            }
                        }
                    }
                }
                if (!CMRequest)
                {

                    if (this.AlgID != "9" && (string.IsNullOrEmpty(this.AlgID) || string.IsNullOrEmpty(this.AlgSeed) || this.SKUType == 0 || ErrorMessage != "SUCCESS"))
                    {
                        objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.UnKnownSKU;
                        objDFLicenseResponse.ResponseMessage = string.IsNullOrEmpty(ErrorMessage) ? Constants.ResponseMessage[2].ToString() : ErrorMessage;
                    }
                    else
                    {
                        if (this.AlgID == "12") // CASE ALG12
                        {
                            // ManageAlgType12

                            //CREATE CA REQUEST
                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);

                            //HANDLE ALG12
                            /*new CR user logging 08/2013*/
                            objLicResponse = ManageAlgType12(objLicRequest, IsShippedVersion,objDFLicenseRequest.RequestingSystem);

                            //GET PCM RESPONSE
                            CATOPCMResponse(objLicResponse, objDFLicenseResponse);

                            //DO NOT LOG KEY AS ITS BEEN ALREADY LOGGED
                            RequireNewKey = false;


                        }
                        else
                        {

                            DataTable licenseData = null;
                            DataSet ds = FindLicenseHistory(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, this.SKUType, this.SKUVersion);
                            if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                                licenseData = ds.Tables[0];

                            this.HistoricQty = 0;
                            this.CurrentIndex = 0;
                            string LicenseKey = string.Empty;

                            if (licenseData != null && licenseData.Rows.Count > 0)
                            {
                                licenseData.DefaultView.Sort = "CreatedOn desc";
                                this.HistoricQty = (licenseData.Rows[0]["Qty"] != DBNull.Value && Convert.ToInt32(licenseData.Rows[0]["Qty"]) > 0) ? Convert.ToInt32(licenseData.Rows[0]["Qty"]) : 0;
                                this.CurrentIndex = (licenseData.Rows[0]["Index"] != DBNull.Value && Convert.ToInt32(licenseData.Rows[0]["Index"]) > 0) ? Convert.ToInt32(licenseData.Rows[0]["Index"]) : 0;
                                LicenseKey = (licenseData.Rows[0]["LicenseKey"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(licenseData.Rows[0]["LicenseKey"]))) ? Convert.ToString(licenseData.Rows[0]["LicenseKey"]) : string.Empty;
                            }

                            if (this.AlgID == "13" || this.AlgID == "53") // CASE ALG13
                            {
                                if (!IsRMARequest)
                                {
                                    objDFLicenseRequest.Qty = objDFLicenseRequest.Qty + this.HistoricQty;
                                }

                                if (!objDFLicenseRequest.IndexSpecified)
                                {
                                    objDFLicenseRequest.Index = 1 + this.CurrentIndex;
                                    objDFLicenseRequest.IndexSpecified = true;
                                }
                                //NEW CR- removed to avoid the duplicate insertion
                                //RequireNewKey=true;
                            }
                            else if (this.AllowMany == 1)  // CASE ALLOWMANY
                            {
                                if (!objDFLicenseRequest.IndexSpecified)
                                {
                                    if (objDFLicenseRequest.Index > 0)
                                    {
                                        objDFLicenseRequest.Index = this.CurrentIndex;
                                    }
                                    else
                                    {
                                        objDFLicenseRequest.Index = 1 + this.CurrentIndex;
                                    }
                                    //objDFLicenseRequest.Index = this.CurrentIndex;
                                    objDFLicenseRequest.IndexSpecified = true;
                                    RequireNewKey = true;
                                }
                                else
                                {
                                    DataRow[] dr = null;

                                    dr = licenseData.Select("Index = " + objDFLicenseRequest.Index);


                                    if (dr != null && dr.Length > 0)
                                    {
                                        bool flag = false;
                                        for (int i = 0; i < dr.Length; i++)
                                        {
                                            if (Convert.ToInt32(dr[i]["Index"]) == objDFLicenseRequest.Index)
                                            {
                                                string key = Convert.ToString(dr[i]["LicenseKey"]);

                                                if (!string.IsNullOrEmpty(key))
                                                {
                                                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                                    objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();
                                                    objDFLicenseResponse.LicenseKey = key;
                                                    RequireNewKey = false;
                                                    flag = true;
                                                }
                                            }
                                        }
                                        if (!flag)
                                            RequireNewKey = true;
                                    }
                                    else
                                    {
                                        RequireNewKey = true;
                                    }
                                }

                            }
                            else
                            {
                                // LOGIC TO CHECK NEED TO GENERATE KEY OR NOT
                                if (this.SKUType == 1)
                                {
                                    if (!string.IsNullOrEmpty(LicenseKey))
                                    {
                                        if ((this.AlgID == "9") || (this.AlgID == "0"))
                                            LicenseKey = "No RK";

                                        objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                        objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();
                                        objDFLicenseResponse.LicenseKey = LicenseKey;
                                        RequireNewKey = false;
                                    }
                                    else
                                    {
                                        RequireNewKey = true;
                                        if ((this.AlgID == "9") || (this.AlgID == "0"))
                                        {
                                            LicenseKey = "No RK";
                                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                            objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();
                                            objDFLicenseResponse.LicenseKey = LicenseKey;


                                        }

                                    }
                                }
                                else
                                {
                                    //Allowmany = 0 and SKUTYPE =2 and ALGID <> 13 or 53 or 12 (New CR)
                                    if (!string.IsNullOrEmpty(LicenseKey))
                                    {
                                        if ((this.AlgID == "9") || (this.AlgID == "0"))
                                            LicenseKey = "No RK";
                                        #region Previous Logic
                                        //if (this.AlgID == "5" || this.AlgID == "6" || objDFLicenseRequest.AlgType == "56" || this.AlgID == "8")
                                        //{
                                        //    if (!objDFLicenseRequest.IndexSpecified)
                                        //    {
                                        //        objDFLicenseRequest.Index = 1 + this.CurrentIndex;
                                        //        objDFLicenseRequest.IndexSpecified = true;
                                            
                                        //    }
                                        //    RequireNewKey = true;
                                        //}
                                        #endregion
                                        //NEW CR- for alg id !=12 or 13 or 53 and Allowmany = 0 and producttype=2
                                        if (this.AlgID != "12" || this.AlgID != "13" || objDFLicenseRequest.AlgType != "53")
                                        {

                                            objDFLicenseRequest.Index = 1;
                                            objDFLicenseRequest.IndexSpecified = true;
                                            objDFLicenseRequest.Qty = 1;
                                            objDFLicenseRequest.QtySpecified = true;

                                            RequireNewKey = true;
                                        }
                                        else
                                        {
                                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                            objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();
                                            objDFLicenseResponse.LicenseKey = LicenseKey;
                                            RequireNewKey = false;
                                        }
                                    }
                                    else
                                    {
                                        RequireNewKey = true;
                                        if ((this.AlgID == "9") || (this.AlgID == "0"))
                                        {
                                            LicenseKey = "No RK";
                                            objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                            objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();
                                            objDFLicenseResponse.LicenseKey = LicenseKey;
                                        }
                                        #region Previous logic
                                        //if (this.AlgID == "5" || this.AlgID == "6" || objDFLicenseRequest.AlgType == "56" || this.AlgID == "8")
                                        //{
                                        //    if (!objDFLicenseRequest.IndexSpecified)
                                        //    {
                                        //        objDFLicenseRequest.Index = 1 + this.CurrentIndex;
                                        //        objDFLicenseRequest.IndexSpecified = true;

                                        //    }
                                        //}
                                        #endregion

                                        #region Algorithm 19 changes
                                        //NEW CR- for alg id !=12 or 13 or 53 and Allowmany = 0 and producttype=2
                                        //New CR--To  Generate Key for Alg 19 productType2 and allow many=0 and Producttype 2
                                        //Previuos condition
                                        //if (this.AlgID != "12" || this.AlgID != "13" || objDFLicenseRequest.AlgType != "53")
                                        if (objDFLicenseRequest.AlgType != "19" && objDFLicenseRequest.AlgType != "17" && objDFLicenseRequest.AlgType != "21")
                                        {
                                            if (this.AlgID != "12" || this.AlgID != "13" || objDFLicenseRequest.AlgType != "53" )
                                            {
                                                objDFLicenseRequest.Index = 1;
                                                objDFLicenseRequest.IndexSpecified = true;
                                                objDFLicenseRequest.Qty = 1;
                                                objDFLicenseRequest.QtySpecified = true;
                                            }
                                        }
                                            //Checking existing Key Values as New request for Index change
                                        else if (objDFLicenseRequest.AlgType == "19" || objDFLicenseRequest.AlgType == "17" ||  objDFLicenseRequest.AlgType == "21")
                                        {
                                            if (objDFLicenseRequest.AlgType == "19" && SKUType == 2)
                                            {
                                                DataTable dtAlg19History = FindIndexAlg19(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Qty);
                                                if (dtAlg19History.Rows.Count > 0)
                                                {
                                                    var index = dtAlg19History.AsEnumerable().Max(r => r.Field<int>("Index"));
                                                    objDFLicenseRequest.Index = index + 1;
                                                    objDFLicenseRequest.IndexSpecified = true;
                                                }
                                            }
                                            else
                                            {
                                                DataTable dtAlg19History = FindLicenseHistoryAlg19(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, this.SKUType, objDFLicenseRequest.Version, objDFLicenseRequest.Qty);
                                                if (dtAlg19History.Rows.Count > 0)
                                                {
                                                    var index = dtAlg19History.AsEnumerable().Max(r => r.Field<int>("Index"));
                                                    objDFLicenseRequest.Index = index + 1;
                                                    objDFLicenseRequest.IndexSpecified = true;
                                                }
                                            }
                                        }
                                        #endregion

                                    }
                                }


                            }

                            if (RequireNewKey && this.AlgID != "9" && this.AllowMany == 1) 
                            {
                                //CREATE CA REQUEST
                                string TempReleaseKey = null;
                                for (int i = 0; i < objDFLicenseRequest.Qty; i++, objDFLicenseRequest.Index++)
                                {
                                    if (objDFLicenseRequest.AlgType == "54")
                                    {
                                        objDFLicenseRequest.AlgType = "4";
                                        objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                        objLicResponse = CallCAPrimaryService(objLicRequest);
                                        objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                                    }
                                    else
                                    {
                                        if (objDFLicenseRequest.AlgType == "56")
                                        {
                                            objDFLicenseRequest.AlgType = "6";
                                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                            objLicResponse = CallCAPrimaryService(objLicRequest);
                                            objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                                        }
                                        else if (objDFLicenseRequest.AlgType == "17" || objDFLicenseRequest.AlgType == "21")
                                        {
                                            DataTable dtAlg19History = FindLicenseHistoryAlg19(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, this.SKUType, objDFLicenseRequest.Version, objDFLicenseRequest.Qty);
                                            if (dtAlg19History.Rows.Count > 0)
                                            {
                                                var index = dtAlg19History.AsEnumerable().Max(r => r.Field<int>("Index"));
                                                objDFLicenseRequest.Index = index + 1;
                                                objDFLicenseRequest.IndexSpecified = true;
                                                objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                                objLicResponse = CallCAPrimaryService(objLicRequest);
                                            }
                                        }
                                        else
                                        {
                                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                            objLicResponse = CallCAPrimaryService(objLicRequest);
                                        }
                                    }

                                    //CALL CA
                                    //NEW CR  - Code to check the duplicate License History for option(ProductType=2)
                                    DataTable dtChkLicenseData = null;
                                    DataSet dsChkLicenseData = objCommonBL.FindDuplicateLicenseHistory(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Index, objDFLicenseRequest.Qty);
                                    if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                                        dtChkLicenseData = dsChkLicenseData.Tables[0];

                                    if (dtChkLicenseData.Rows.Count > 0)
                                        RequireNewKey = false;
                                    else
                                        RequireNewKey = true;

                                    //objLicResponse = CallCAPrimaryService(objLicRequest);
                                    //GET PCM RESPONSE
                                    CATOPCMResponse(objLicResponse, objDFLicenseResponse);
                                    //CHECK FLAGS AND RESPONSE STATUS FOR LOGGING
                                    if (RequireNewKey && objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                                        NewKeyGenerated = true;



                                    //RECORD KEY IF NEW KEY GENERATED AND ITS NOT CM REQUEST
                                    if (!CMRequest && NewKeyGenerated)
                                    {
                                        //LOG KEY IF NEW KEY GENERATED
                                        RecordReleaseKey(objDFLicenseRequest, objDFLicenseResponse, IsShippedVersion);
                                    }
                                    if (objDFLicenseRequest.Qty > 1)
                                    {
                                        objLicResponse.ReleaseKey = TempReleaseKey + objLicResponse.ReleaseKey;
                                        TempReleaseKey = objLicResponse.ReleaseKey + ",";
                                        objDFLicenseResponse.LicenseKey = objLicResponse.ReleaseKey;
                                    }
                                    else
                                    {
                                        objDFLicenseResponse.LicenseKey = objLicResponse.ReleaseKey;

                                    }

                                }
                                //objDFLicenseRequest.Index = objDFLicenseRequest.Index - 1;

                            }
                            else
                            {

                                if ((this.AlgID == "9") || (this.AlgID == "0"))
                                {
                                    LicenseKey = "No RK";
                                    objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                                    objDFLicenseResponse.ResponseMessage = Constants.ResponseMessage[0].ToString();

                                    //NEW CR - Code to check the duplicate License History for option(ProductType=2)
                                    DataTable dtChkLicenseData = null;
                                    DataSet dsChkLicenseData = objCommonBL.FindDuplicateLicenseHistory(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Index, objDFLicenseRequest.Qty);
                                    if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                                        dtChkLicenseData = dsChkLicenseData.Tables[0];

                                    if (dtChkLicenseData.Rows.Count > 0)
                                        RequireNewKey = false;
                                    else
                                        RequireNewKey = true;


                                    //CHECK FLAGS AND RESPONSE STATUS FOR LOGGING
                                    if (RequireNewKey && objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                                        NewKeyGenerated = true;

                                    //RECORD KEY IF NEW KEY GENERATED AND ITS NOT CM REQUEST
                                    if (!CMRequest && NewKeyGenerated)
                                    {
                                        //LOG KEY IF NEW KEY GENERATED
                                        RecordReleaseKey(objDFLicenseRequest, objDFLicenseResponse, IsShippedVersion);
                                    }

                                    objDFLicenseResponse.LicenseKey = LicenseKey;


                                }
                                else
                                {
                                    if (objDFLicenseRequest.AlgType == "54")
                                    {
                                        objDFLicenseRequest.AlgType = "4";
                                        objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                        objLicResponse = CallCAPrimaryService(objLicRequest);
                                        objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                                    }
                                    else
                                    {
                                        if (objDFLicenseRequest.AlgType == "56")
                                        {
                                            objDFLicenseRequest.AlgType = "6";
                                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                            objLicResponse = CallCAPrimaryService(objLicRequest);
                                            objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                                        }
                                        else
                                        {
                                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                                            objLicResponse = CallCAPrimaryService(objLicRequest);
                                        }
                                    }
                                    //NEW CR - Code to check the duplicate License History for option(ProductType=2)
                                    DataTable dtChkLicenseData = null;
                                    DataSet dsChkLicenseData = objCommonBL.FindDuplicateLicenseHistory(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Index, objDFLicenseRequest.Qty);
                                    if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                                        dtChkLicenseData = dsChkLicenseData.Tables[0];

                                    if (dtChkLicenseData.Rows.Count > 0)
                                        RequireNewKey = false;
                                    else
                                        RequireNewKey = true;


                                    //CALL CA

                                    //objLicResponse = CallCAPrimaryService(objLicRequest);
                                    //GET PCM RESPONSE
                                    CATOPCMResponse(objLicResponse, objDFLicenseResponse);
                                    //CHECK FLAGS AND RESPONSE STATUS FOR LOGGING
                                    if (RequireNewKey && objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                                        NewKeyGenerated = true;

                                    //RECORD KEY IF NEW KEY GENERATED AND ITS NOT CM REQUEST
                                    if (!CMRequest && NewKeyGenerated)
                                    {
                                        //LOG KEY IF NEW KEY GENERATED
                                        RecordReleaseKey(objDFLicenseRequest, objDFLicenseResponse, IsShippedVersion);
                                    }
                                    objDFLicenseResponse.LicenseKey = objLicResponse.ReleaseKey;
                                }
                            }
                        }
                    }
                }
                else 
                {
                    //CREATE CA REQUEST
                    if (objDFLicenseRequest.AlgType == "54")
                    {
                        objDFLicenseRequest.AlgType = "4";
                        objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                        objLicResponse = CallCAPrimaryService(objLicRequest);
                        objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                    }
                    else
                    {
                        if (objDFLicenseRequest.AlgType == "56")
                        {
                            objDFLicenseRequest.AlgType = "6";
                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                            objLicResponse = CallCAPrimaryService(objLicRequest);
                            objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                        }
                        else
                        {
                            objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                            objLicResponse = CallCAPrimaryService(objLicRequest);
                        }
                    }
                    //CALL CA
                    //objLicResponse = CallCAPrimaryService(objLicRequest);

                    //NEW CR - Code to check the duplicate License History for option(ProductType=2)
                    DataTable dtChkLicenseData = null;
                    DataSet dsChkLicenseData = objCommonBL.FindDuplicateLicenseHistory(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Index, objDFLicenseRequest.Qty);
                    if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                        dtChkLicenseData = dsChkLicenseData.Tables[0];

                    if (dtChkLicenseData.Rows.Count > 0)
                        RequireNewKey = false;
                    else
                        RequireNewKey = true;


                    //GET PCM RESPONSE
                    CATOPCMResponse(objLicResponse, objDFLicenseResponse);

                    //CHECK FLAGS AND RESPONSE STATUS FOR LOGGING
                    if (RequireNewKey && objDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success)
                        NewKeyGenerated = true;

                    //RECORD KEY IF NEW KEY GENERATED AND ITS NOT CM REQUEST
                    if (!CMRequest && NewKeyGenerated)
                    {
                        //LOG KEY IF NEW KEY GENERATED
                        RecordReleaseKey(objDFLicenseRequest, objDFLicenseResponse, IsShippedVersion);
                    }

                    objDFLicenseResponse.LicenseKey = objDFLicenseResponse.LicenseKey;


                }



                successflag = true;
            }
            catch (Exception ex)
            {
                if (ex.Message.EndsWith("alg 17 SKU") || ex.Message.EndsWith("alg 20 SKU") || ex.Message.EndsWith("Please enter valid no of years"))
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    throw new Exception("GetLicense:Error Occured While Processing GetLicense." + ex.Message + Environment.NewLine + ex.StackTrace);
                }
                
            }

            return successflag;
        }
        #endregion

        //NEW CR - method for RMASwap, which swaps  the License key destination Snum
        #region
        public DFLicenseResponse GetRMALicense(DFLicenseRequest objDFLicenseRequest, DFLicenseResponse objDFLicenseResponse)
        {
            var objLicRequest = new LicRequest();
            var objLicResponse = new LicResponse();
            var objCAProxy = new SigServTandbergService();
            string format = "yyyyMMdd";
            string display = objDFLicenseRequest.ExpDate.ToString(format);
            string ErrorMessage = string.Empty;
            bool dataflag = GetSKULicenseMetadata(objDFLicenseRequest.SKUID, objDFLicenseRequest.Version, out ErrorMessage);
            objDFLicenseRequest.AlgType = !string.IsNullOrEmpty(this.AlgID) ? this.AlgID : objDFLicenseRequest.AlgType;
            objDFLicenseRequest.Seed = !string.IsNullOrEmpty(this.AlgSeed) ? this.AlgSeed : objDFLicenseRequest.Seed;
            objDFLicenseRequest.Version = !string.IsNullOrEmpty(this.SKUVersion) ? this.SKUVersion : objDFLicenseRequest.Version;
            bool RequireNewKey = false;
            //Check if the record is already existing in the database.If existing then do not create a key else create a key 
            DataTable dtChkLicenseData = null;
            DataSet dsChkLicenseData = objCommonBL.FindDuplicateLicenseHistoryForRMASWAP(objDFLicenseRequest.SNum, objDFLicenseRequest.SKUID, objDFLicenseRequest.Index, objDFLicenseRequest.Qty, objDFLicenseRequest.Version);
            if (dsChkLicenseData != null && dsChkLicenseData.Tables != null && dsChkLicenseData.Tables.Count > 0)
                dtChkLicenseData = dsChkLicenseData.Tables[0];
            if (dtChkLicenseData.Rows.Count > 0)
                RequireNewKey = false;
            else
                RequireNewKey = true;
            if (objDFLicenseRequest.AlgType == "54")
            {
                objDFLicenseRequest.AlgType = "4";
                objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                objLicResponse = CallCAPrimaryService(objLicRequest);
                objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
            }
            else
            {
                if (objDFLicenseRequest.AlgType == "56")
                {
                    objDFLicenseRequest.AlgType = "6";
                    objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                    objLicResponse = CallCAPrimaryService(objLicRequest);
                    objLicResponse.ReleaseKey = objLicResponse.ReleaseKey + '-' + display;
                }
                else
                {
                    objLicRequest = PCMTOCARequest(objDFLicenseRequest);
                    objLicResponse = CallCAPrimaryService(objLicRequest);
                }
            }
            CATOPCMResponse(objLicResponse, objDFLicenseResponse);
            //if the record is not existing already in the database then create a key.
            if (RequireNewKey.Equals(true))
            {
                RecordReleaseKey(objDFLicenseRequest, objDFLicenseResponse, 1);
            }
            return objDFLicenseResponse;
        }
        #endregion


        #region GetSKULicenseMetadata
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objDFLicenseRequest"></param>
        /// <param name="objLicRequest"></param>
        public bool GetSKULicenseMetadata(string SKUID, string Version, out string ErrorMessage)
        {
            bool flag = false;
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[10];

                    arParms[0] = new SqlParameter("@SKUID", SqlDbType.VarChar, 100);
                    arParms[0].Value = (string.IsNullOrEmpty(SKUID) ? "-1" : SKUID);
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@VersionName", SqlDbType.VarChar, 50);
                    arParms[1].Value = (string.IsNullOrEmpty(Version) ? string.Empty : Version);
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@NewSKUID", SqlDbType.VarChar, 100);
                    arParms[2].Direction = ParameterDirection.Output;

                    arParms[3] = new SqlParameter("@Seed", SqlDbType.VarChar, 50);
                    arParms[3].Direction = ParameterDirection.Output;

                    arParms[4] = new SqlParameter("@AlgID", SqlDbType.VarChar, 50);
                    arParms[4].Direction = ParameterDirection.Output;

                    arParms[5] = new SqlParameter("@AlgQty", SqlDbType.Int);
                    arParms[5].Direction = ParameterDirection.Output;

                    arParms[6] = new SqlParameter("@AllowMany", SqlDbType.Int);
                    arParms[6].Direction = ParameterDirection.Output;

                    arParms[7] = new SqlParameter("@SKUType", SqlDbType.Int);
                    arParms[7].Direction = ParameterDirection.Output;

                    arParms[8] = new SqlParameter("@SKUVersion", SqlDbType.VarChar, 50);
                    arParms[8].Direction = ParameterDirection.Output;

                    arParms[9] = new SqlParameter("@ErrorMessage", SqlDbType.VarChar, 200);
                    arParms[9].Direction = ParameterDirection.Output;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_GetSKULicenseMetadata", objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                    int ProcStatus = Convert.ToString(arParms[9].Value) == "SUCCESS" ? 0 : -1;
                    if (ProcStatus == 0)
                    {
                        this.NewSKUID = arParms[2].Value == null ? "" : Convert.ToString(arParms[2].Value).Trim();
                        this.AlgSeed = arParms[3].Value == null ? "" : Convert.ToString(arParms[3].Value).Trim();
                        this.AlgID = arParms[4].Value == null ? "" : Convert.ToString(arParms[4].Value).Trim();
                        this.AlgQty = arParms[5].Value == null ? 1 : Convert.ToInt32(arParms[5].Value);
                        this.AllowMany = arParms[6].Value == null ? 0 : Convert.ToInt32(arParms[6].Value);
                        this.SKUType = arParms[7].Value == null ? 1 : Convert.ToInt32(arParms[7].Value);
                        this.SKUVersion = arParms[8].Value == null ? "" : Convert.ToString(arParms[8].Value);

                        flag = true;
                    }
                    ErrorMessage = Convert.ToString(arParms[9].Value);

                }
            }
            catch (Exception ex)
            {
                //read details in to message and throw exception to Process layer
                throw new Exception("Error Occured While Processing GetSKULicenseMetadata. " + ex.Message + Environment.NewLine + ex.StackTrace);

            }
            return flag;
        }
        #endregion

        #region ManageAlgType12
        /*new CR user logging 08/2013*/
        public LicResponse ManageAlgType12(LicRequest objLicRequest, int IsShippedVersion, string requestingSystem)
        {

            LicResponse objLicResponse = new LicResponse();
            SigServTandbergService objCAProxy = new SigServTandbergService();
            string Algtype = "";
            string Seed = "";
            int Index = 1;

            #region comments
            //alg, seed, partNum,
            //check licensekey table
            //get count
            //processed_count = (Qty - CountFromDB) 
            //loop call to CA (Qty - CountFromDB) Times
            //record keys to licensekey table
            //log transaction
            //return last release key in response
            //final object returned will get converted to DFResponse
            #endregion comments
            //Qty count should be atleast 1 if empty then it is treated as 1
            if (objLicRequest.Ports < 1)
            {
                objLicResponse.ResponseStatus = "1";//invalidparameter
                objLicResponse.ResponseMessage = "Request with SKU: " + objLicRequest.SKU + ". SKU Count less than 1 not allowed.";
            }
            //if input qty count should not exceed 4 if exceeds then display a  warning message
            else if (objLicRequest.Ports > 4)
            {
                objLicResponse.ResponseStatus = "1";//invalidparameter
                objLicResponse.ResponseMessage = "Request with SKU: " + objLicRequest.SKU + " .SKU Count has exceeded Key Limit.";
            }
            else
            {
                //get the Alg 12 License keys from the database
                DataSet ds = GetLicenseHistoryForAlgID12(objLicRequest.SNum);
                string strPartName = string.Empty;
                int intQty = 0;
                //if Alg12 keys exist then fetch the Qty and the PartName of that key
                if (ds.Tables[0].Rows.Count > 0)
                {
                    intQty = Int32.Parse(ds.Tables[0].Rows[0]["Qty"].ToString());
                    strPartName = ds.Tables[0].Rows[0]["PartName"].ToString();
                }
                //add the input qty and historic qty.Check if the total qty exceeds 4,if exceeds then display a warning message.            
                if ((intQty + objLicRequest.Ports) > 4)
                {
                    objLicResponse.ResponseStatus = "1";//invalidparameter
                    objLicResponse.ResponseMessage = "Request with SKU: " + objLicRequest.SKU + " .SKU Count has exceeded Key Limit.";
                }
                else
                {
                    //delete the existing key from the database
                    /*new CR user logging 08/2013*/
                    if (ds.Tables[0].Rows.Count > 0)
                        objCommonBL.DeleteExistingAlg12PortLicenseKey(objLicRequest.SNum, strPartName, requestingSystem);


                    int seedPointer = intQty + objLicRequest.Ports;
                    //NEW CR - Duplicate issue
                    bool indexSpecified = objLicRequest.IndexSpecified;
                    //set the seed based on the total qty calculated.
                    switch (seedPointer)
                    {
                        case 1:
                            Algtype = "12";
                            Seed = "0xe17";
                            Index = 1;
                            break;
                        case 2:
                            Algtype = "12";
                            Seed = "0xe16";
                            Index = 1;
                            break;
                        case 3:
                            Algtype = "12";
                            Seed = "0xe15";
                            Index = 1;
                            break;
                        case 4:
                            Algtype = "12";
                            Seed = "0xe14";
                            Index = 1;
                            break;
                        default:
                            Algtype = "Error";
                            Seed = "";
                            break;
                    }
                    //set the response object 
                    LicResponse tempLicResponse = new LicResponse();
                    objLicRequest.Seed = Seed;
                    objLicRequest.AlgType = Algtype;
                    objLicRequest.Index = Index;
                    objLicRequest.IndexSpecified = true;
                    objLicRequest.Ports = intQty + objLicRequest.Ports;
                    objCAProxy.Url = TandbergCAPrimary;
                    //call the CA server to generate the key
                    tempLicResponse = CallCAPrimaryService(objLicRequest);
                    //if the key generation is successfull then insert the key to the database.
                    if (tempLicResponse.ResponseStatus == "SUCCESS" && !string.IsNullOrEmpty(objLicRequest.SKU))
                        InsertKey(objLicRequest, tempLicResponse, 1);
                    if (Algtype != "Error")
                        objLicResponse = tempLicResponse;

                }
            }

            return objLicResponse;
        }
        #endregion

        #region PCMTOCARequest
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objDFLicenseRequest"></param>
        /// <returns></returns>
        public LicRequest PCMTOCARequest(DFLicenseRequest objDFLicenseRequest)
        {
            LicRequest objLicRequest = new LicRequest();
            objLicRequest.AlgType = objDFLicenseRequest.AlgType;
            objLicRequest.ExpDate = objDFLicenseRequest.ExpDate;
            if (objLicRequest.ExpDate != DateTime.Parse("1/1/0001"))
                objLicRequest.ExpDateSpecified = true;
            objLicRequest.Index = objDFLicenseRequest.Index;
            if (objLicRequest.Index > 1 || objLicRequest.Index == 1)
                objLicRequest.IndexSpecified = true;
            objLicRequest.MAC = objDFLicenseRequest.MacAddress;
            objLicRequest.Ports = objDFLicenseRequest.Qty;
            if (objLicRequest.Ports > 1)
                objLicRequest.PortsSpecified = true;
            objLicRequest.RequestDateTime = objDFLicenseRequest.RequestDateTime;
            objLicRequest.Seed = objDFLicenseRequest.Seed;
            objLicRequest.SKU = objDFLicenseRequest.SKUID;
            objLicRequest.SNum = objDFLicenseRequest.SNum;
            objLicRequest.Version = objDFLicenseRequest.Version;

            return objLicRequest;

        }
        #endregion

        #region CATOPCMResponse
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicResponse"></param>
        /// <returns></returns>
        public void CATOPCMResponse(LicResponse objLicResponse, DFLicenseResponse objDFLicenseResponse)
        {
            if (objLicResponse != null && objLicResponse.ResponseStatus.ToUpper() == "1")
            {
                objDFLicenseResponse.LicenseKey = "";
                objDFLicenseResponse.ResponseDateTime = DateTime.Now.ToUniversalTime();
                objDFLicenseResponse.ResponseMessage = string.IsNullOrEmpty(objLicResponse.ResponseMessage) ? "Invalid Parameter" : objLicResponse.ResponseMessage;
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.InvalidParameter;
            }
            else if (objLicResponse != null && (string.IsNullOrEmpty(objLicResponse.ResponseMessage) || objLicResponse.ResponseStatus.ToUpper() == "FAILURE" || string.IsNullOrEmpty(objLicResponse.ReleaseKey)))
            {
                objDFLicenseResponse.LicenseKey = "";
                objDFLicenseResponse.ResponseDateTime = DateTime.Now.ToUniversalTime();
                objDFLicenseResponse.ResponseMessage = string.IsNullOrEmpty(objLicResponse.ResponseMessage) ? "Signature Server Failure" : objLicResponse.ResponseMessage;
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.ServiceUnavailable;
            }
            else
            {
                objDFLicenseResponse.LicenseKey = objLicResponse.ReleaseKey;
                objDFLicenseResponse.ResponseStatus = (int)Constants.ResponseStatus.Success;
                objDFLicenseResponse.ResponseDateTime = objLicResponse.ResponseDateTime;
                objDFLicenseResponse.ResponseMessage = objLicResponse.ResponseMessage;
            }

        }
        #endregion

        #region RecordReleaseKey
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicRequest"></param>
        /// <param name="objLicResponse"></param>
        public void RecordReleaseKey(DFLicenseRequest objDFLicenseRequest, DFLicenseResponse objDFDFLicenseResponse, int IsShippedVersion)
        {
            try
            {
                if (objDFDFLicenseResponse != null && objDFDFLicenseResponse.ResponseStatus == (int)Constants.ResponseStatus.Success && objDFDFLicenseResponse.LicenseKey.IsValidString() && objDFLicenseRequest.SKUID.IsValidString())
                {
                    using (SqlConnection objCon = new SqlConnection(connectionString))
                    {
                        SqlParameter[] arParms = new SqlParameter[14];

                        arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                        arParms[0].Value = objDFLicenseRequest.SNum;
                        arParms[0].Direction = ParameterDirection.Input;

                        arParms[1] = new SqlParameter("@LicenseKey", SqlDbType.VarChar, 100);
                        arParms[1].Value = objDFDFLicenseResponse.LicenseKey;
                        arParms[1].Direction = ParameterDirection.Input;

                        arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                        arParms[2].Value = (objDFLicenseRequest.IndexSpecified) ? objDFLicenseRequest.Index : 1;
                        arParms[2].Direction = ParameterDirection.Input;

                        arParms[3] = new SqlParameter("@Qty", SqlDbType.Int);
                        arParms[3].Value = (objDFLicenseRequest.QtySpecified) ? objDFLicenseRequest.Qty : 1;
                        arParms[3].Direction = ParameterDirection.Input;

                        arParms[4] = new SqlParameter("@PartName", SqlDbType.VarChar, 50);
                        arParms[4].Value = (string.IsNullOrEmpty(objDFLicenseRequest.SKUID)) ? "" : objDFLicenseRequest.SKUID;
                        arParms[4].Direction = ParameterDirection.Input;

                        arParms[5] = new SqlParameter("@ProductType", SqlDbType.Int);
                        arParms[5].Value = SKUType != 0 ? SKUType : 1;
                        arParms[5].Direction = ParameterDirection.Input;

                        arParms[6] = new SqlParameter("@VersionName", SqlDbType.VarChar, 50);
                        arParms[6].Value = (string.IsNullOrEmpty(objDFLicenseRequest.Version)) ? "" : objDFLicenseRequest.Version;
                        arParms[6].Direction = ParameterDirection.Input;

                        arParms[7] = new SqlParameter("@IsShippedVersion", SqlDbType.Bit);
                        arParms[7].Value = IsShippedVersion;
                        arParms[7].Direction = ParameterDirection.Input;

                        arParms[8] = new SqlParameter("@SigSrvName", SqlDbType.VarChar, 50);
                        arParms[8].Value = "TandbergCA";
                        arParms[8].Direction = ParameterDirection.Input;

                        arParms[9] = new SqlParameter("@AlgID", SqlDbType.VarChar, 50);
                        arParms[9].Value = this.AlgID.IsValidString() ? this.AlgID : "0";
                        arParms[9].Direction = ParameterDirection.Input;

                        arParms[10] = new SqlParameter("@AllowMany", SqlDbType.Int);
                        arParms[10].Value = this.AllowMany;
                        arParms[10].Direction = ParameterDirection.Input;

                        /*new CR user logging 08/2013*/
                        arParms[11] = new SqlParameter("@CreatedBy", SqlDbType.VarChar, 50);
                        arParms[11].Value = objDFLicenseRequest.RequestingSystem;
                        arParms[11].Direction = ParameterDirection.Input;

                        arParms[12] = new SqlParameter("@UpdatedBy", SqlDbType.VarChar, 50);
                        arParms[12].Value = objDFLicenseRequest.RequestingSystem;
                        arParms[12].Direction = ParameterDirection.Input;

                        arParms[13] = new SqlParameter("@ErrorMessage", SqlDbType.VarChar, 200);
                        arParms[13].Direction = ParameterDirection.Output;

                        using (SqlCommand objCMD = new SqlCommand("pTAA_RecordReleaseKey", objCon))
                        {
                            objCon.Open();
                            objCMD.CommandType = CommandType.StoredProcedure;
                            objCMD.Connection = objCon;
                            objCMD.Parameters.AddRange(arParms);
                            objCMD.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:Error Occured While Processing RecordReleaseKey:" + ex.Message + Environment.NewLine + ex.StackTrace);
                throw ex;
                //read details in to message and throw exception to Process layer
            }
        }
        #endregion

        #region GetLicenseHistoryCountForAlgID12
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNUM"></param>
        /// <returns></returns>
        public DataSet GetLicenseHistoryForAlgID12(string SNUM)
        {

            DataSet ds = null;

            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[1];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNUM;
                    arParms[0].Direction = ParameterDirection.Input;

                    string strSQLCommand = "pTAA_GetAlg12LicenseKeys";
                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);

                    /*string strSQL = @"select @Count = count(snum) From [dbo].[TAA_LicenseKeys]
                    WHERE [SNum] = @SNum and (PartName = '193241PL' or PartName = 'LIC-3241-PL1' or PartName = 'LIC-3241-1PRI') and  deleted = 0 ";*/

                    //                    string strSQL = @"SELECT * FROM [dbo].[TAA_LicenseKeys] WITH (NOLOCK)
                    //                                     WHERE [SNum] = @SNum AND PartName IN ( SELECT DISTINCT PM.TAAPartNo AS SKUID
                    //                                                                            FROM TAA_ProductMetadata  AS PM WITH (NOLOCK)
                    //                                                                            INNER JOIN TAA_LicenseMetadata  AS LM WITH (NOLOCK) ON LM.ProductMetadataID = PM.ProductMetadataID 
                    //                                                                            WHERE LM.AlgID = '12' 
                    //                                                                            UNION 
                    //                                                                            SELECT DISTINCT PM.SKUID AS SKUID
                    //                                                                            FROM TAA_ProductMetadata AS PM WITH (NOLOCK)
                    //                                                                            INNER JOIN TAA_LicenseMetadata AS LM WITH (NOLOCK) ON LM.ProductMetadataID = PM.ProductMetadataID 
                    //                                                                            WHERE LM.AlgID = '12') and deleted =0";




                    //                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                    //                    {
                    //                        objCon.Open();
                    //                        objCMD.CommandType = CommandType.Text;
                    //                        objCMD.Connection = objCon;
                    //                        objCMD.Parameters.AddRange(arParms);
                    //                        objCMD.ExecuteNonQuery();
                    //                    }
                    //                    count = arParms[1] == null ? 0 : int.Parse(arParms[1].Value.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:AlgType 12 Failed. " + ex.Message);
            }
            return ds;
        }
        #endregion


        //        public int GetLicenseHistoryCountForAlgID12(string SNUM, string PartName)
        //        {
        //            int count = 0;

        //            try
        //            {
        //                using (SqlConnection objCon = new SqlConnection(connectionString))
        //                {
        //                    SqlParameter[] arParms = new SqlParameter[3];

        //                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
        //                    arParms[0].Value = SNUM;
        //                    arParms[0].Direction = ParameterDirection.Input;

        //                    arParms[1] = new SqlParameter("@PartName", SqlDbType.VarChar, 40);
        //                    arParms[1].Value = PartName;
        //                    arParms[1].Direction = ParameterDirection.Input;

        //                    arParms[2] = new SqlParameter("@Count", SqlDbType.Int);
        //                    arParms[2].Direction = ParameterDirection.Output;                 


        //                    string strSQL = @"SELECT  @Count = Qty FROM [dbo].[TAA_LicenseKeys] WITH (NOLOCK)
        //                                     WHERE [SNum] = @SNum and PartName IN (SELECT DISTINCT PM.TAAPartNo FROM TAA_ProductMetadata PM INNER JOIN TAA_LicenseMetadata  AS LM WITH (NOLOCK) ON 
        //																							LM.ProductMetadataID = PM.ProductMetadataID WHERE LM.AlgID = '12' 
        //																							and ( PM.TAAPartNo=@PartName or PM.SKUID = @PartName) 
        //																						    UNION                                                          
        //                                                                           				    SELECT DISTINCT PM.SKUID FROM TAA_ProductMetadata PM INNER JOIN TAA_LicenseMetadata  AS LM WITH (NOLOCK) ON 
        //                                                                           				    LM.ProductMetadataID = PM.ProductMetadataID WHERE LM.AlgID = '12'
        //                                                                           				    and ( PM.TAAPartNo=@PartName or PM.SKUID = @PartName)) and deleted =0";


        //                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
        //                    {
        //                        objCon.Open();
        //                        objCMD.CommandType = CommandType.Text;
        //                        objCMD.Connection = objCon;
        //                        objCMD.Parameters.AddRange(arParms);
        //                        objCMD.ExecuteNonQuery();
        //                    }                   
        //                        count = arParms[2] == null ? 0 : int.Parse(arParms[2].Value.ToString());

        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new Exception("GetLicense:AlgType 12 Failed. " + ex.Message);
        //            }
        //            return count;
        //        }

        public int DeleteExistingPortLicenseKeyForAlgID12(string SNUM, string PartName)
        {
            int count = 0;

            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[3];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNUM;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@PartName", SqlDbType.VarChar, 40);
                    arParms[1].Value = PartName;
                    arParms[1].Direction = ParameterDirection.Input;


                    string strSQL = @"Delete from [dbo].[TAA_LicenseKeys] WITH (NOLOCK)
                                     WHERE [SNum] = @SNum and PartName = @PartName and deleted =0";


                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.Text;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                    //count = arParms[2] == null ? 0 : int.Parse(arParms[2].Value.ToString());

                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:AlgType 12 Failed. " + ex.Message);
            }
            return count;
        }


        #region FindLicenseHistory
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SNum"></param>
        /// <param name="SKU"></param>
        public DataSet FindLicenseHistory(string SNum, string SKUID, int SKUType, string VersionName)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    string strSQLCommand = "pTAA_FindLicenseHistory";
                    SqlParameter[] arParms = new SqlParameter[4];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@SKUID", SqlDbType.VarChar, 100);
                    arParms[1].Value = SKUID;
                    arParms[1].Direction = ParameterDirection.Input;



                    arParms[2] = new SqlParameter("@SKUType", SqlDbType.Int);
                    arParms[2].Value = SKUType;
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@VersionName", SqlDbType.VarChar, 100);
                    arParms[3].Value = VersionName;
                    arParms[3].Direction = ParameterDirection.Input;



                    ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, strSQLCommand, arParms);

                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetLicense:Find License History Failed. " + ex.Message);
            }
            return ds;
        }

        //Alg19 History
        public DataTable FindLicenseHistoryAlg19(String Snum, String SKUID, int SKUType, string Version, int Quantity)
        {
            DataSet ds = new DataSet();
            string query = "";
            if (SKUType == 2)
            {
                query = @"Select * from Cryptologger.dbo.TAA_LicenseKeys(nolock) where Snum=@Snum and PartName=@PartName and Qty=@Quantity";
            }
            else
            {
                query = @"Select * from Cryptologger.dbo.TAA_LicenseKeys(nolock) where Snum=@Snum and PartName=@PartName and Qty=@Quantity and VersionName=@Vname";
            }

            SqlParameter[] sqlPamam = new SqlParameter[4];
            sqlPamam[0] = new SqlParameter("@Snum", Snum);
            sqlPamam[1] = new SqlParameter("@PartName", SKUID);
            sqlPamam[2] = new SqlParameter("@Quantity", Quantity);
            sqlPamam[3] = new SqlParameter("@Vname", Version);
            using (SqlConnection objCon = new SqlConnection(connectionString))
            {
                ds = SqlHelper.ExecuteDataset(objCon, CommandType.Text, query, sqlPamam);
            }
            return ds.Tables[0];
        }
        #endregion

        //Alg19 History
        public DataTable FindIndexAlg19(String Snum, String SKUID,  int Quantity)
        {
            DataSet ds = new DataSet();
            string query = "";

            query = "[dbo].[pTAA_GetIndexAlg19]";
            
            SqlParameter[] sqlPamam = new SqlParameter[4];
            sqlPamam[0] = new SqlParameter("@Snum", Snum);
            sqlPamam[1] = new SqlParameter("@PartName", SKUID);
            sqlPamam[2] = new SqlParameter("@Quantity", Quantity);
            
            using (SqlConnection objCon = new SqlConnection(connectionString))
            {
                ds = SqlHelper.ExecuteDataset(objCon, CommandType.StoredProcedure, query, sqlPamam);
            }
            return ds.Tables[0];
        }
        

        #region CallCAPrimaryService
        /// <summary>
        ///   
        /// </summary>
        /// <param name="objLicRequest"></param>
        /// <returns></returns>
        /// <remarks>Comment this Method before Checkin</remarks>
        //public LicResponse CallCAPrimaryService(LicRequest objLicRequest)
        //{
        //    LicResponse objLicResponse = new LicResponse();
        //    SigServTandbergService objCAProxy = new SigServTandbergService();
        //    try
        //    {

        //        //objCAProxy.ClientCertificates.Add(objX509Certificate2);
        //        //System.Net.ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(MyServerCertValidationCb);

        //        objCAProxy.Url = TandbergCAPrimary;
        //        objLicResponse = objCAProxy.GetLicense(objLicRequest);

        //        return objLicResponse;
        //    }
        //    catch (Exception ex)
        //    {
        //        //objLicResponse = CallCASecondaryService(objLicRequest, ex.Message);
        //        return objLicResponse;
        //    }

        //}


        /// <summary>
        ///   
        /// </summary>
        /// <param name="objLicRequest"></param>
        /// <returns></returns>
        /// <remarks>Uncomment this Method before Checkin</remarks>
        public LicResponse CallCAPrimaryService(LicRequest objLicRequest)
        {
            LicResponse objLicResponse = new LicResponse();
            SigServTandbergService objCAProxy = new SigServTandbergService();
            try
            {

                objCAProxy.ClientCertificates.Add(objX509Certificate2);
                System.Net.ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(MyServerCertValidationCb);

                objCAProxy.Url = TandbergCAPrimary;
                objLicResponse = objCAProxy.GetLicense(objLicRequest);

                return objLicResponse;
            }
            catch (Exception ex)
            {
                objLicResponse = CallCASecondaryService(objLicRequest, ex.Message);
                return objLicResponse;
            }

        }

        #endregion

        #region CallCASecondaryService
        private LicResponse CallCASecondaryService(LicRequest objLicRequest, string ErrorMessage)
        {
            LicResponse objLicResponse = new LicResponse();
            SigServTandbergService objCAProxy = new SigServTandbergService();
            try
            {
                objCAProxy.ClientCertificates.Add(objX509Certificate2);
                System.Net.ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(MyServerCertValidationCb);

                objCAProxy.Url = TandbergCASecondary;
                objLicResponse = objCAProxy.GetLicense(objLicRequest);

                return objLicResponse;
            }
            catch (Exception ex)
            {
                throw new Exception("Primary CA Failed: " + ErrorMessage + ". Secondary CA Failed: " + ex.Message);
            }
        }
        #endregion

        #region MyServerCertValidationCb
        public bool MyServerCertValidationCb(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        #endregion

        #region InsertKey
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objLicRequest"></param>
        /// <param name="objLicResponse"></param>
        public void InsertKey(LicRequest objLicRequest, LicResponse objLicResponse, int IsShippedVersion)
        {
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[12];

                    arParms[0] = new SqlParameter("@SNum", SqlDbType.VarChar, 40);
                    arParms[0].Value = objLicRequest.SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@LicenseKey", SqlDbType.VarChar, 100);
                    arParms[1].Value = objLicResponse.ReleaseKey;
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Index", SqlDbType.Int);
                    arParms[2].Value = (objLicRequest.IndexSpecified) ? objLicRequest.Index : 1;
                    arParms[2].Direction = ParameterDirection.Input;

                    arParms[3] = new SqlParameter("@Qty", SqlDbType.Int);
                    arParms[3].Value = objLicRequest.Ports;
                    arParms[3].Direction = ParameterDirection.Input;

                    arParms[4] = new SqlParameter("@PartName", SqlDbType.VarChar, 50);
                    arParms[4].Value = (string.IsNullOrEmpty(objLicRequest.SKU)) ? "" : objLicRequest.SKU;
                    arParms[4].Direction = ParameterDirection.Input;

                    arParms[5] = new SqlParameter("@ProductType", SqlDbType.Int);
                    arParms[5].Value = 1;
                    arParms[5].Direction = ParameterDirection.Input;

                    arParms[6] = new SqlParameter("@VersionName", SqlDbType.VarChar, 50);
                    arParms[6].Value = (string.IsNullOrEmpty(objLicRequest.Version)) ? "" : objLicRequest.Version;
                    arParms[6].Direction = ParameterDirection.Input;

                    arParms[7] = new SqlParameter("@IsShippedVersion", SqlDbType.Bit);
                    arParms[7].Value = IsShippedVersion;
                    arParms[7].Direction = ParameterDirection.Input;

                    arParms[8] = new SqlParameter("@SigSrvName", SqlDbType.VarChar, 50);
                    arParms[8].Value = "TandbergCA";
                    arParms[8].Direction = ParameterDirection.Input;

                    arParms[9] = new SqlParameter("@AlgID", SqlDbType.VarChar, 50);
                    arParms[9].Value = "12";
                    arParms[9].Direction = ParameterDirection.Input;

                    arParms[10] = new SqlParameter("@AllowMany", SqlDbType.Int);
                    arParms[10].Value = 0;
                    arParms[10].Direction = ParameterDirection.Input;

                    arParms[11] = new SqlParameter("@ErrorMessage", SqlDbType.VarChar, 200);
                    arParms[11].Direction = ParameterDirection.Output;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_RecordReleaseKey", objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //read details in to message and throw exception to Process layer
            }
        }
        #endregion

        #region GetMinMacAddress_Alg51_53
        public Int64 GetMinMacAddress_Alg51_53(string SNum)
        {
            //DataTable snumMacData = new DataTable();
            //DataSet objDS = null;
            string connectionStringMfgProd = "";
            long MinMACAdr = 0;
            connectionStringMfgProd = ConfigurationSettings.AppSettings["sqlconnMfg"];
            try
            {
                using (SqlConnection objMFGCon = new SqlConnection(connectionStringMfgProd))
                {
                    SqlParameter[] arParms = new SqlParameter[2];
                    arParms[0] = new SqlParameter("@sernum", SqlDbType.VarChar, 40);
                    arParms[0].Value = SNum;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@MinMac", SqlDbType.BigInt);
                    arParms[1].Direction = ParameterDirection.Output;

                    using (SqlCommand objCMD = new SqlCommand("pTAA_GetMinValueMacadr_Alg5153", objMFGCon))
                    {
                        objMFGCon.Open();
                        objCMD.CommandType = CommandType.StoredProcedure;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                        var outputparam = objCMD.Parameters["@MinMac"].Value;
                        if (outputparam is DBNull)
                        {
                            MinMACAdr = 0;
                        }
                        else
                        {
                            MinMACAdr = Convert.ToInt64(objCMD.Parameters["@MinMac"].Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading mfg data. " + ex.Message + ". ");
            }
            return MinMACAdr;
        }
        #endregion
    }



}
