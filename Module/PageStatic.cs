using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace MyWeb.Module
{
    public abstract class PageStatic
    {
        static string _basePath = string.Empty;
        static DataTable dt_urls = new DataTable();
        static DataTable dt_html = new DataTable();
        static object objURL = new object();
        static bool bInit = false;
        static NoSortHashtable htUrlRegs = new NoSortHashtable();
        static NoSortHashtable htUrlRegObjs = new NoSortHashtable();
        static NoSortHashtable htHtmlRegs = new NoSortHashtable();
        static NoSortHashtable htHtmlRegObjs = new NoSortHashtable();

        static Regex regURL = new Regex("/.+?/\\.\\./", RegexOptions.RightToLeft | RegexOptions.Compiled);

        public static string BasePath
        {
            get { 
                if(!bInit)
                {
                    Init();
                }
                return _basePath;
            }
        }
        static Random rnd = new Random();

        private static void Init()
        {
            if (bInit)
                return;
            lock(objURL)
            {
                if (bInit)
                    return;
                dt_urls.Clear();
                dt_urls.Columns.Add("url");
                dt_urls.Columns.Add("new_url");

                dt_html.Clear();
                dt_html.Columns.Add("url");
                dt_html.Columns.Add("new_url");

                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/Config/url_rewrite.xml"))
                {
                    bInit = true;
                    return;
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + "/Config/url_rewrite.xml");
                XmlNode root = doc.SelectSingleNode("/urls");
                _basePath = root.Attributes["base"].Value;

                XmlNodeList nodes = root.SelectNodes("url");
                if(nodes!=null)
                { 
                    foreach (XmlNode node in nodes)
                    {
                        DataRow dr = dt_urls.NewRow();
                        dr["url"] = node.Attributes["from"].Value;
                        dr["new_url"] = node.Attributes["to"].Value;
                        dt_urls.Rows.Add(dr);
                        string key = rnd.Next(10000, 99999).ToString() + MyWeb.Common.StrEncode(node.Attributes["to"].Value);
                        htUrlRegs.Add(key, node.Attributes["from"].Value);
                        htUrlRegObjs.Add(key, new Regex(node.Attributes["from"].Value, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                    }
                }
                nodes = root.SelectNodes("html");
                if(nodes!=null)
                { 
                    foreach (XmlNode node in nodes)
                    {
                        DataRow dr = dt_html.NewRow();
                        dr["url"] = node.Attributes["from"].Value;
                        dr["new_url"] = node.Attributes["to"].Value;
                        dt_html.Rows.Add(dr);
                        string key = rnd.Next(10000, 99999).ToString() + MyWeb.Common.StrEncode(node.Attributes["to"].Value);
                        htHtmlRegs.Add(key, node.Attributes["from"].Value);
                        htHtmlRegObjs.Add(key, new Regex(node.Attributes["from"].Value, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                    }
                }
                bInit = true;
            }
        }

        public static string DealURL(string content)
        {
            Init();
            if(dt_html != null&& dt_html.Rows.Count>0)
            {
                foreach (string key in htHtmlRegs.Keys)
                {
                    string url = regURL.Replace("/" + BasePath + MyWeb.Common.StrDecode(key.Substring(5)), "/");
                    Regex reg = htHtmlRegObjs[key] as Regex; // new Regex(htHtmlRegs[key].ToString(), RegexOptions.IgnoreCase);
                    content = reg.Replace(content, url);
                }
            }
            return content;
        }

        public static string ToRealURL(string url)
        {
            Init();
            if (url.StartsWith("/"+_basePath))
                url = url.Substring(_basePath.Length);
            if (dt_urls != null && dt_urls.Rows.Count > 0)
            {
                
                foreach (string key in htUrlRegs.Keys)
                {
                    Match m = (htUrlRegObjs[key] as Regex).Match(url);
                    if (m.Success)
                    {
                        Regex reg = htUrlRegObjs[key] as Regex; // new Regex(htUrlRegs[key].ToString(), RegexOptions.IgnoreCase);
                        return reg.Replace(url, MyWeb.Common.StrDecode(key.Substring(5)));
                    }
                }
            }
            return url;
        }
        public static void ToHTML(string content, string path)
        {
            StreamWriter sw=null;
            try
            {
                if(!Directory.Exists(path.Substring(0, path.LastIndexOf("/"))))
                    Directory.CreateDirectory(path.Substring(0, path.LastIndexOf("/")));
                sw = new StreamWriter(path, false, System.Text.Encoding.UTF8);
                sw.Write(content);
                sw.Flush();
            }
            catch(Exception e){
                Scaler.DataBase.Win.WriteLog(e.Message.ToString());
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">绝对路径 或 相对路径</param>
        /// <returns>以根目录为起点的路径</returns>
        public static string GetHTMLPath(string url)
        {
            Init();
            url = url.Replace("?refresh=true", "").Replace("&refresh=true", "");
            if (dt_html != null && dt_html.Rows.Count > 0)
            {
                foreach (string text in PageStatic.htHtmlRegs.Keys)
                {
                    if ((PageStatic.htHtmlRegObjs[text] as Regex).Replace(url, "") == "")
                    {
                        return (PageStatic.htHtmlRegObjs[text] as Regex).Replace(url, Common.StrDecode(text.Substring(5)));
                    }
                    /*
                    Regex reg = new Regex(htHtmlRegs[text].ToString(), RegexOptions.IgnoreCase);
                    if (reg.Replace(url, "") == "")
                    {
                        return reg.Replace(url, Common.StrDecode(text.Substring(5)));
                    }*/
                }
                /*
                foreach (DataRow dr in dt_html.Rows)
                {
                    //throw new Exception(Regex.Replace("/info.aspx?i=1", @"/info\.aspx\?i=(\d*)", ""));
                    if (Regex.Replace(url, dr["url"].ToString(), "", RegexOptions.IgnoreCase) == "")
                    {
                        return Regex.Replace(url, dr["url"].ToString(), dr["new_url"].ToString(), RegexOptions.IgnoreCase);
                        
                        //return dr["new_url"].ToString();
                    }
                    /*
                    Match m = Regex.Match(url, dr["url"].ToString());
                    if (m.Success && m.Value==url)
                    {
                        return Regex.Replace(m.Value, dr["url"].ToString(), dr["new_url"].ToString());
                    }
                    
                }*/
            }
            return "";
        }
    }
}