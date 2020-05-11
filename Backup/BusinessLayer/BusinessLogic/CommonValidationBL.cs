using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PCMTandberg.BusinessEntities;
using PCMTandberg.DataAccess;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace PCMTandberg.BusinessLogic
{
    public class CommonValidationBL
    {
        #region Property & Variables
        string connectionString = string.Empty;
        #endregion

        #region .ctor
        public CommonValidationBL()
        {
            connectionString = ConfigurationSettings.AppSettings["sqlconn"];
        }
        #endregion

        #region DupCheckRequestId
        public bool DupCheckRequestId(string requestId, string requestingSystem)
        {
            bool flag = false;
            try
            {
                using (SqlConnection objCon = new SqlConnection(connectionString))
                {
                    SqlParameter[] arParms = new SqlParameter[3];

                    arParms[0] = new SqlParameter("@RequestId", SqlDbType.VarChar, 40);
                    arParms[0].Value = requestId;
                    arParms[0].Direction = ParameterDirection.Input;

                    arParms[1] = new SqlParameter("@RequestingSystem", SqlDbType.VarChar, 40);
                    arParms[1].Value = requestingSystem;
                    arParms[1].Direction = ParameterDirection.Input;

                    arParms[2] = new SqlParameter("@Count", SqlDbType.Int);
                    arParms[2].Direction = ParameterDirection.Output;

                    string strSQL = @"SELECT @Count = COUNT(RequestId) "
                                   + "FROM TAA_TransactionLog WITH (NOLOCK) "
                                   + "WHERE RequestId = @RequestId "
                                   + "AND RequestingSystem = @RequestingSystem";

                    using (SqlCommand objCMD = new SqlCommand(strSQL, objCon))
                    {
                        objCon.Open();
                        objCMD.CommandType = CommandType.Text;
                        objCMD.Connection = objCon;
                        objCMD.Parameters.AddRange(arParms);
                        objCMD.ExecuteNonQuery();
                    }
                    int readCount = arParms[2] == null ? 0 : int.Parse(arParms[2].Value.ToString());
                    if (readCount > 0)
                    {
                        flag = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Validation of Duplicate Request ID Failed. " + ex.Message);
            }
            return flag;
        }
        #endregion

    }

    public static class StringExtensions
    {
        #region IsValidString
        /// <summary>
        /// extension method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsValidString(this string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(str.Trim()))
                return false;
            else
                return true;
        }
        #endregion

        #region IsAlpha
        public static bool IsAlpha(this string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z]+$");
        }
        #endregion

        #region IsNumeric
        public static bool IsNumeric(this string input)
        {
            return Regex.IsMatch(input, @"^\d+$");
        }
        #endregion

        #region IsAlphaNumeric
        public static bool IsAlphaNumeric(this string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z0-9]+$");
        }
        #endregion

        #region IsAlphaNumericWithUnderscore
        public static bool IsAlphaNumericWithUnderscore(this string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z0-9_]+$");
        }
        #endregion

        #region IsValidSNumFormat
        /// <summary>
        /// extension method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsValidSNumFormat(this string str)
        {
            if (!str.IsValidString())
                return false;

            //if (!str.IsAlphaNumeric())
            //    return false;

            //TODO: FORMAT VALIDATION

            return true;
        }
        #endregion

        #region IsValidMacAddressFormat
        /// <summary>
        /// extension method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsValidMacAddressFormat(this string macAddress)
        {
            if (!macAddress.IsValidString())
                return false;

            //TODO: FORMAT VALIDATION
                             
             /*


            Regex rx;
            if (macAddress.Length == 17)
            {    *
                rx = new Regex("([0-9a-fA-F][0-9a-fA-F]:){5}([0-9a-fA-F][0-9a-fA-F])", RegexOptions.IgnoreCase);
                if (rx.IsMatch(macAddress))
                    return true;
            }
            else
            {
                rx = new Regex("([0-9a-fA-F][0-9a-fA-F]){5}([0-9a-fA-F][0-9a-fA-F])", RegexOptions.IgnoreCase);

                if (!rx.IsMatch(macAddress))
                    return false;
            }
              */


            return true;
        }
       

        #endregion
    }

    public static class ObjectCopier
    {
        /// <summary> 
        /// Perform a deep Copy of the object. 
        /// </summary> 
        /// <typeparam name="T">The type of object being copied.</typeparam> 
        /// <param name="source">The object instance to copy.</param> 
        /// <returns>The copied object.</returns> 
        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object 
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
   
}
