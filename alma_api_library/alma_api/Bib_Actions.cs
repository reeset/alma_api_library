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


namespace alma_api
{
    public class Bib_Actions
    {
        
        private string perror_message = "";
        private string pusername = "";
        private string ppassword = "";
        private string p_host = "";
        private bool pssl = false;
        private string papikey = "";
        private string pResponse = "";

        public string Alma_Response
        {
            get { return pResponse; }
            set { pResponse = value; }
        }

        public string Error_Message
        {
            set { perror_message = value; }
            get { return perror_message; }
        }

        public bool Ignore_SSL_Errors
        {
            set { pssl = value; }
            get { return pssl; }
        }

        public string Debug_Info {
            get
            {
                return "";
            }
        }

        

        public string Host
        {
            set {
                if (value.EndsWith("/"))
                {
                    p_host = value.TrimEnd("/".ToCharArray());
                }
                else
                {
                    p_host = value;
                }
            }
            get { return p_host; }
        }

        public string API_KEY
        {
            set { papikey = value; }
            get { return papikey; }
        }

        public bool Authorize(string host, string apikey)
        {
            Authentications objA = new Authentications();
            papikey = apikey;
            bool is_authorized = false;
            objA.Ignore_SSL_Certificates = Ignore_SSL_Errors;
            try
            {
                string uri = host + "/almaws/v1/users?limit=10";
                is_authorized = objA.Authorize(uri, apikey, out perror_message);
                
            }
            catch
            {
                Error_Message = "Undefined authorization error";
                return false;
            }

            return is_authorized;
        }
        //data is retrieved in MARCXML
        public string GetRecord(string id)
        {
            string uri = Host + "/almaws/v1/bibs/" + id + "?view=MARCXML&expand=None&apikey=" + API_KEY;         
            if (Ignore_SSL_Errors == true)
            {
                Authentications.IgnoreBadCertificates();
            }
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            //request.Headers["Authorization"] = "apikey " + API_KEY;
            

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string tmp = reader.ReadToEnd();

                reader.Close();
                response.Close();
                return tmp;
            }
            catch (WebException e) 
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    Error_Message = e.Message;
                    return e.Message;
                }
                else
                {
                    Error_Message = e.Message;
                    return e.Message;
                }
            }
            
        }

        public bool CreateRecord(string rec)
        {
            return UpdateRecord(rec, "");
        }


        //Data must be passed in MARCXML
        public bool UpdateRecord(string rec, string id)
        {
            return UpdateRecord(rec, id, false);
        }

        public bool UpdateRecord(string rec, string id, bool holdings = false) 
        {
            string uri = "";
            HttpWebRequest request = null;

            if (id == "")
            {
                //this is for new records
                //uri = Host + "/almaws/v1/bibs/" + id;
               uri = Host + "/almaws/v1/bibs?apikey=" + API_KEY;
                if (Ignore_SSL_Errors == true)
                {
                    Authentications.IgnoreBadCertificates();
                }
                request = (HttpWebRequest)HttpWebRequest.Create(uri);
                //request.Headers["Authorization"] = "apikey " + API_KEY;

                if (Ignore_SSL_Errors == true)
                {
                    Authentications.IgnoreBadCertificates();
                }
        
                //Read the Editor, generate records, turn them into an array to pass this back
                rec = GenerateAlmaRecord(rec);
                //System.Windows.Forms.MessageBox.Show(rec);
                request.Method = "POST";
                request.ContentType = @"application/xml";
                System.IO.StreamWriter writer = new System.IO.StreamWriter(request.GetRequestStream(), System.Text.Encoding.UTF8);
                //this is for the network zone
                //writer.Write("from_nz_mms_id=" + rec);
                writer.Write(rec);
                writer.Flush();
                writer.Close();
            }
            else
            {

                //this is for new records
                //uri = Host + "/almaws/v1/bibs/" + id;
                uri = Host + "/almaws/v1/bibs/" + id + "?apikey=" + API_KEY;
                if (Ignore_SSL_Errors == true)
                {
                    Authentications.IgnoreBadCertificates();
                }
                request = (HttpWebRequest)HttpWebRequest.Create(uri);
                //request.Headers["Authorization"] = "apikey " + API_KEY;

                //Read the Editor, generate records, turn them into an array to pass this back
                rec = GenerateAlmaRecord(rec, id);
                request.Method = "PUT";
                request.ContentType = @"application/xml";
                System.IO.StreamWriter writer = new System.IO.StreamWriter(request.GetRequestStream(), System.Text.Encoding.UTF8);
                writer.Write(rec);
                writer.Flush();
                writer.Close();
            }

         

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1252));
                string tmp = reader.ReadToEnd();

                if (tmp.IndexOf("ERROR") > -1)
                {
                    Error_Message = tmp;
                    return false;
                }
                reader.Close();
                response.Close();
                Alma_Response = tmp;
                //System.Windows.Forms.MessageBox.Show(tmp);
                return true;
            } catch (WebException e ){
                //System.Windows.Forms.MessageBox.Show(e.ToString());
                Error_Message = e.ToString();
                return false;
            }
            catch (Exception ee)
            {
                //System.Windows.Forms.MessageBox.Show(ee.ToString());
                Error_Message = ee.ToString();
                return false;
            }
        }

        //pass in a marcxml record and return 
        //the record id if present
        public string GetRecordId(string xml, string field, string subfield)
        {

            string xpath = "";
            try
            {

                if (xml.IndexOf("marc:collection") > -1)
                {
                    if (System.Convert.ToInt32(field) > 10)
                    {
                        xpath = "/marc:collection/marc:record/marc:datafield[@tag='" + field + "']";
                    }
                    else
                    {
                        xpath = "/marc:collection/marc:record/marc:controlfield[@tag='" + field + "']";
                    }
                }
                else
                {
                    if (System.Convert.ToInt32(field) > 10)
                    {
                        xpath = "/marc:record/marc:datafield[@tag='" + field + "']";
                    }
                    else
                    {
                        xpath = "/marc:record/marc:controlfield[@tag='" + field + "']";
                    }
                }


                //System.Windows.Forms.MessageBox.Show(xpath);
                System.Xml.XmlDocument objXML = new System.Xml.XmlDocument();
                System.Xml.XmlNamespaceManager ns = new System.Xml.XmlNamespaceManager(objXML.NameTable);
                ns.AddNamespace("marc", "http://www.loc.gov/MARC21/slim");

                System.Xml.XmlNode objNode;
                System.Xml.XmlNode objSubfield;
                objXML.LoadXml(xml);
                objNode = objXML.SelectSingleNode(xpath, ns);
                //objNode = objXML.SelectSingleNode(xpath, ns);
                if (!string.IsNullOrEmpty(subfield))
                {
                    objSubfield = objNode.SelectSingleNode("marc:subfield[@code='" + subfield + "']", ns);
                }
                else
                {
                    objSubfield = objNode;
                }
                return objSubfield.InnerText;

            }
            catch (System.Exception xe)
            {
                perror_message += xe.ToString();
                return "";
            }

        }

        private string GenerateAlmaRecord(string s, string id = "")
        {
            //We need to extract the record part (not the collection part)
            System.Xml.XmlTextReader rd;
            string sRec = "";

			try 
			{
                rd = new System.Xml.XmlTextReader(s, System.Xml.XmlNodeType.Document, null);
				//rd = new System.Xml.XmlTextReader(prawXML, System.Xml.XmlNodeType.Document, null);	
			} catch (System.Exception pp){
                //System.Windows.Forms.MessageBox.Show(pp.ToString());
                return "";
            }

            try
            {
                //System.Windows.Forms.MessageBox.Show("here");
                while (rd.Read())
                {
                    //This is where we find the head of the record, 
                    //then process the values within the record.
                    //We also need to do character encoding here if necessary.

                    if (rd.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        //System.Windows.Forms.MessageBox.Show(rd.LocalName);
                        if (rd.LocalName == "record")
                        {
                            sRec = rd.ReadOuterXml();
                            break;
                        }
                    }
                }
                rd.Close();
                sRec = sRec.Replace("marc:", "");
                string sidtag = "";
                if (!string.IsNullOrEmpty(id))
                {
                    sidtag = "<mms_id>" + id + "</mms_id>";
                }
                sRec = "<bib>" + sidtag + "<record_format>marc21</record_format><suppress_from_publishing>false</suppress_from_publishing>" + sRec + "</bib>";
                return sRec;
            }
            catch (System.Exception ppp){
                //System.Windows.Forms.MessageBox.Show(ppp.ToString());
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
