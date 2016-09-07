/*******************************************************
 *  Author: Terry Reese
 *  Project: alma_api
 *  License: http://creativecommons.org/publicdomain/zero/1.0
 *  To the extent possible under law, Terry Reese has waived 
 *  all copyright and related or neighboring rights to koha_api. 
 *  This work is published from:  United States. 
 ********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;


namespace alma_api
{
    internal class Authentications
    {

        private bool pssl = false;

        internal bool Ignore_SSL_Certificates
        {
            set { pssl = value; }
            get { return pssl; }
        }
        /// <summary>
        /// Together with the AcceptAllCertifications method right
        /// below this causes to bypass errors caused by SLL-Errors.
        /// </summary>
        public static void IgnoreBadCertificates()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
        }

        /// <summary>
        /// In Short: the Method solves the Problem of broken Certificates.
        /// Sometime when requesting Data and the sending Webserverconnection
        /// is based on a SSL Connection, an Error is caused by Servers whoes
        /// Certificate(s) have Errors. Like when the Cert is out of date
        /// and much more... So at this point when calling the method,
        /// this behaviour is prevented
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certification"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns>true</returns>
        private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        } 

        internal bool Authorize(string host, string apikey, out string return_status)
        {
            //look up users to test the key
            return_status = "";
            
            string users = ReadUri(host, apikey);
            //System.Windows.Forms.MessageBox.Show(users);
            if (users.IndexOf("users total_record_count") > -1)
            {
                return true;
            } else
            {
                return_status = users;
                return false;
            }
        }

        public string ReadUri(string uri, string apikey = null)
        {

            try
            {
                LeaveDotsAndSlashesEscaped();

                System.Net.WebRequest.DefaultWebProxy = null;
                //if (apikey != null)
                //{
                //    //objRequest.Headers["Authorization"] = "apikey " + apikey;
                //    uri = uri + "?apikey=" + apikey;
                //}

                uri += "&apikey=" + apikey;
                //System.Windows.Forms.MessageBox.Show(uri);

                System.Net.HttpWebRequest objRequest =
                (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
                
                //System.Net.HttpWebRequest objRequest =
                //(System.Net.HttpWebRequest)System.Net.WebRequest.Create(MyUri(uri));
                objRequest.Proxy = null;

                //if (cglobal.PublicProxy != null)
                //{
                //    objRequest.Proxy = cglobal.PublicProxy;
                //}
                objRequest.UserAgent = "MarcEdit 6.2 Alma Plugin";
                objRequest.Proxy = null;
                objRequest.Accept = "*/*";

                //Changing the default timeout from 100 seconds to 30 seconds.
                objRequest.Timeout = 30000;

                System.Net.WebResponse objResponse = objRequest.GetResponse();

                System.IO.StreamReader reader = new System.IO.StreamReader(objResponse.GetResponseStream(), System.Text.Encoding.GetEncoding(1252));
                string tmpVal = reader.ReadToEnd().Trim();
                //System.Windows.Forms.MessageBox.Show(uri + "\n" + tmpVal);
                reader.Close();
                objResponse.Close();

                return tmpVal;
            }
            catch (System.Exception xx)
            {
                //System.Windows.Forms.MessageBox.Show(uri + "\n" + xx.ToString());
                return "";
            }
        }

        private void LeaveDotsAndSlashesEscaped()
        {
            var getSyntaxMethod =
                typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (getSyntaxMethod == null)
            {
                //probably isn't 4.0
                return;
            }


            System.Reflection.MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);
                        // Clear the CanonicalizeAsFilePath attribute
                        if ((flagsValue & 0x1000000) != 0)
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                    }
                }
            }


            var uriParser = getSyntaxMethod.Invoke(null, new object[] { "http" });

            var setUpdatableFlagsMethod =
                uriParser.GetType().GetMethod("SetUpdatableFlags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (setUpdatableFlagsMethod == null)
            {
                //throw new MissingMethodException("UriParser", "SetUpdatableFlags");
                //probably isn't 4.0
                return;
            }

            setUpdatableFlagsMethod.Invoke(uriParser, new object[] { 0 });
        }
    }
}
