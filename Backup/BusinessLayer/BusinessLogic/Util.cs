using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace PCMTandberg.BusinessLogic
{
    #region Utility Methods
    public class Util
    {
        #region UTF8ByteArrayToString
        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        public static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return (constructedString);
        }
        #endregion

        #region UTF8ByteArrayToPlainString

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        /// <remarks>TO MAKE OUTPUT XML STRING COMPATIBLE WITH SQL SERVER 2005</remarks>
        public static String UTF8ByteArrayToPlainString(Byte[] characters)
        {
            String constructedString = string.Empty;
            UTF8Encoding encoding = new UTF8Encoding();
           
            constructedString = encoding.GetString(characters);
            constructedString = constructedString.Replace("encoding=\"utf-8\"", "");
            
            return (constructedString);
        }
        #endregion

        #region StringValidation
        public bool IsAlpha(string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z]+$");
        }

        public bool IsAlphaNumeric(string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z0-9]+$");
        }

        public bool IsAlphaNumericWithUnderscore(string input)
        {
            return Regex.IsMatch(input, "^[a-zA-Z0-9_]+$");
        }
        #endregion StringValidation

        #region SerializeObject
        public static String SerializeObject(Object pObject)
        {
            string XmlizedString = null;
            MemoryStream memoryStream = new MemoryStream();

            try
            {
                System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(pObject.GetType());
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                xs.Serialize(xmlTextWriter, pObject);
                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
                XmlizedString = UTF8ByteArrayToPlainString(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                memoryStream = null;
            }

            return XmlizedString;


        }
        #endregion

        #region BuildXmlString
        /// <summary>
        /// Build XML String to Pass multiple parameters to Store Procedure
        /// </summary>
        /// <param name="xmlRootName"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string BuildXmlString(string xmlRootName, string[] values)
        {
            StringBuilder xmlString = new StringBuilder();

            xmlString.AppendFormat("<{0}>", xmlRootName);
            for (int i = 0; i < values.Length; i++)
            {
                xmlString.AppendFormat("<value>{0}</value>", values[i]);
            }
            xmlString.AppendFormat("</{0}>", xmlRootName);

            return xmlString.ToString();
        }
        #endregion
    }
    #endregion
}
