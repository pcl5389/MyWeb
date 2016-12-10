using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System;
#if Zip
using ICSharpCode.SharpZipLib.Zip;
#endif 
using System.Globalization;
using System.Data;

namespace MyWeb
{
    public abstract class Common
    {
        //private static Hashtable ht = new Hashtable();
        public static Hashtable CacheTypes = new Hashtable();
        public static object objTypes = new object();

        public static Hashtable CacheClass = new Hashtable();
        public static object objClass = new object();

        private static System.Data.DataTable dtCities;
        private static readonly object obj = new object();
        private static System.Reflection.MethodInfo _Invoke = null;
        private static IHttpHandler _DefaultHandler = null;

        //public static object objFactory = new object();
        private static object objRounter = new object();
        public static object objHandler = new object();

        private static Hashtable _htRounting = null;
        public static Hashtable htHandlers = new Hashtable();

        private static Hashtable _htHandlerConfigs = null;
        private static object objHandlerConfigs = new object();
        private static bool bHandlerConfigs = false;

        public static void RedirectBack()
        {
            string reffer= HttpContext.Current.Request["HTTP_REFERER"] == null ? "" : HttpContext.Current.Request["HTTP_REFERER"].ToString();
            HttpContext.Current.Response.Redirect(reffer);
            HttpContext.Current.Response.End();
        }

        [CLSCompliant(false)]
        public static VTemplate.Engine.TemplateDocument tpl(string path)
        {
            MyHandler handler = (MyHandler)HttpContext.Current.Handler;
            path = string.Format(CultureInfo.CurrentCulture, "{0}View/{1}/{2}{3}{4}", HttpContext.Current.Server.MapPath(@"\"), handler.ViewStyle, handler.TemplatePath, path, MyHandler.TMPLEXT).Replace("_index" + MyHandler.TMPLEXT, MyHandler.TMPLEXT);

            return handler.RendTemplate(path);
        }

        public static void init()
        {
            _htRounting = getRounting();
            _htHandlerConfigs = getHandlerConfigs();
        }
#if Zip
        private void Zip(string SrcFile, string DstFile, int BufferSize)
        {
            FileStream fileStreamIn = new FileStream
                (SrcFile, FileMode.Open, FileAccess.Read);
            FileStream fileStreamOut = new FileStream
                (DstFile, FileMode.Create, FileAccess.Write);
            ZipOutputStream zipOutStream = new ZipOutputStream(fileStreamOut);
            byte[] buffer = new byte[BufferSize];
            ZipEntry entry = new ZipEntry(Path.GetFileName(SrcFile));
            zipOutStream.PutNextEntry(entry);
            int size;
            do
            {
                size = fileStreamIn.Read(buffer, 0, buffer.Length);
                zipOutStream.Write(buffer, 0, size);
            } while (size > 0);
            zipOutStream.Close();
            fileStreamOut.Close();
            fileStreamIn.Close();
        }
#endif
        public static string getCurrentURL()
        {
            string querystring = HttpContext.Current.Request.QueryString.ToString();
            if (!string.IsNullOrEmpty(querystring))
            {
                querystring = System.Text.RegularExpressions.Regex.Replace("&" + querystring+"&", "&page=\\d*&", "&").Remove(0, 1);
            }
            if (querystring.EndsWith("&"))
            { 
                querystring = querystring.Remove(querystring.Length - 1); 
            }
            return HttpContext.Current.Request.Url.AbsolutePath + (string.IsNullOrEmpty(querystring) ? "" : ("?" + querystring));
        }
         
        public static string getCountSQL(string sql)
        {
            string sql_tpl = sql = sql.Replace("\r\n", " ").ToLower().Trim();
            if (string.IsNullOrEmpty(sql_tpl))
                return "select 0";
            int i = 0;
#if Debug
            StringBuilder sb = new StringBuilder();
#endif
            while (sql_tpl.IndexOf(")") > -1 && i<10)
            {
                i++;
                MatchCollection mc = Regex.Matches(sql, "\\([^\\(]+?\\)");
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
#if Debug
                        sb.AppendLine(m.Value);
#endif
                        sql_tpl = sql_tpl.Replace(m.Value, Scaler.WinBT.DataBase.space_repeat(m.Value.Length));
                    }
                }
                if (sql_tpl.IndexOf(")") == -1)
                    break;
                mc = Regex.Matches(sql_tpl, "\\([^\\)]+?\\)", RegexOptions.RightToLeft);
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
#if Debug
                        sb.AppendLine(m.Value);
#endif
                        sql_tpl = sql_tpl.Replace(m.Value, Scaler.WinBT.DataBase.space_repeat(m.Value.Length));
                    }
                }
            }
            //MyWeb.debug.var_dump(sb.ToString() + sql_tpl);
            string orderby = Regex.Match(sql_tpl, " order\\s+by ").Value;
            int l = 0;
            if (!string.IsNullOrEmpty(orderby))
            {
                l = sql_tpl.LastIndexOf(orderby);
                if (l > -1)
                {
                    sql = sql.Remove(l);
                }
            }
            if (sql.IndexOf(" union ") > -1)
            {
                return "select count(1) from (" + sql + ") a";
            }
            /*
            while(sql.IndexOf("  ")>-1)
            {
                sql=sql.Replace("  "," ");
            }*/
            l = sql_tpl.IndexOf(" from ");
            if (Regex.Match(sql_tpl, " group\\s+by ").Success)
            {
                return "select count(1) from (" + "select count(1) as cont" + sql.Substring(l) + ") a";
            }
            return "select count(1)" + sql.Substring(l);
        }

        public static Hashtable htHandlerConfigs
        {
            get { return getHandlerConfigs(); }
        }
        public static Hashtable htRounting
        {
            get { return getRounting(); }
        }

        private static Hashtable getHandlerConfigs()
        {
            if (bHandlerConfigs == false)
            {
                lock (MyWeb.Common.objHandlerConfigs)
                {
                    if (bHandlerConfigs)
                        return _htHandlerConfigs;
                    _htHandlerConfigs = new MyWeb.Module.NoSortHashtable();
                    XmlDocument ConfigXml = new XmlDocument();
                    ConfigXml.Load(HttpContext.Current.Server.MapPath("/Config/Handlers.config"));
                    XmlNodeList nodes = ConfigXml.SelectNodes("configuration/webSettings/add");
                    foreach (XmlNode node in nodes)
                    {
                        if (!_htHandlerConfigs.Contains(node.Attributes["name"].Value))
                        {
                            MyWeb.HandlerConfig config = new MyWeb.HandlerConfig();
                            config.CurrentDirectory = node.Attributes["CurrentDirectory"].Value;
                            config.AppName = node.Attributes["AppName"].Value;
                            config.FileCacheConfig = node.Attributes["FileCacheConfig"].Value;
                            config.ViewStyle = node.Attributes["ViewStyle"].Value;
                            config.CacheTime = int.Parse(node.Attributes["CacheTime"].Value);
                            config.TemplatePath = node.Attributes["TemplatePath"].Value;
                            _htHandlerConfigs.Add(node.Attributes["name"].Value, config);
                        }
                    }
                    bHandlerConfigs = true;
                    return _htHandlerConfigs;
                }
            }
            return _htHandlerConfigs;
        }

        private static Hashtable getRounting()
        {
            if (_htRounting == null)
            {
                lock (Common.objRounter)
                {
                    if (_htRounting == null)
                    {
                        _htRounting = new MyWeb.Module.NoSortHashtable();
                        XmlDocument ConfigXml = new XmlDocument();
                        ConfigXml.Load(HttpContext.Current.Server.MapPath("/Config/Routes.config"));
                        XmlNodeList nodes = ConfigXml.SelectNodes("configuration/routes/add");
                        foreach (XmlNode node in nodes)
                        {
                            if (!_htRounting.Contains(node.Attributes["name"].Value.ToLower(MyHandler.Culture)))
                            {
                                string handler = "";
                                if (node.Attributes.Count > 3)
                                {
                                    handler = node.Attributes["type"].Value.ToLower(MyHandler.Culture);
                                }

                                string[] val = new string[]{ "^/" + node.Attributes["url"].Value.ToLower(MyHandler.Culture).Replace("{action}", "{module}").Replace("{module}", "[0-9|a-z|A-z|_|-]*").Replace(".", "\\."), node.Attributes["handler"].Value.ToLower(MyHandler.Culture), handler };
                                _htRounting.Add(node.Attributes["name"].Value.ToLower(MyHandler.Culture), val);
                            }
                        }
                    }
                }
            }
            return _htRounting;
        }
        /*
         public static IHttpHandler DefaultHandler
         { 
             get {
                 if (_DefaultHandler == null)
                 {
                     _DefaultHandler = new System.Web.DefaultHttpHandler();
                 }
                 return _DefaultHandler;
             }

         }

         public static System.Reflection.MethodInfo MyInvoke
         {
             get {
                 if (_Invoke == null)
                 {
                     string frameworkInstallDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                     string path = frameworkInstallDir + "\\System.Web.dll";
                     Assembly web = Assembly.LoadFile(path);
                     Type typ = web.GetType("System.Web.UI.SimpleHandlerFactory");
                     //objFactory = FormatterServices.GetUninitializedObject(typ);
                     _Invoke = typ.GetMethod("GetHandler");
                 }
                 return _Invoke;
             }
         }

        private static void UnZip(string zipfile, string destpath)
        {
            ZipEntry entry;
            string str;
            byte[] b = new byte[1024];
            int length;

            if (destpath == null)
                destpath = zipfile.Replace(Path.GetExtension(zipfile), "");

            using (ZipInputStream s = new ZipInputStream(File.Open(zipfile, FileMode.Open)))
            {
                while ((entry = s.GetNextEntry()) != null)
                {
                    str = Path.Combine(destpath, entry.Name);

                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(str);
                    }
                    else if (entry.IsFile)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(str));
                        using (FileStream fs = File.Create(str))
                        {
                            while ((length = s.Read(b, 0, b.Length)) > 0)
                            {
                                fs.Write(b, 0, length);
                            }
                        }
                    }
                }
            }
        }
   */
        private static string GetVersion()
        {
            string path = HttpContext.Current.Server.MapPath("/version.txt");
            if (File.Exists(path))
            {
                StreamReader sr = new StreamReader(path);
                return sr.ReadToEnd();
            }
            else
            {
                return "0";
            }
        }

        private static void SetVersion(string version)
        {
            string path = HttpContext.Current.Server.MapPath("/version.txt");
            StreamWriter sw = new StreamWriter(path);
            sw.Write(version);
            sw.Close();
        }

        public static bool CheckUpdate()
        {
            string url = "http://www.guangletuan.com/update/version.txt";
            WebClient client = new WebClient();
            string version = client.DownloadString(url);

            string curVersion = GetVersion();
            if (!version.Equals(curVersion, StringComparison.OrdinalIgnoreCase))
            {
                ExecuteUpdate(version);
                return true;
            }
            return false;
        }

        public static bool ExecuteUpdate(string version)
        {
            string url = "http://www.guangletuan.com/update/" + version + ".zip";
            WebClient client = new WebClient();
            string filepath = HttpContext.Current.Server.MapPath("/update/" + version + ".zip");
            if (!Directory.Exists(HttpContext.Current.Server.MapPath("/update")))
            {
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath("/update"));
            }
            client.DownloadFile(url, filepath);
            {
                //解压缩
#if Zip
                UnZip(filepath, HttpContext.Current.Server.MapPath("/"));
#endif
                SetVersion(version);
            }
            return false;
        }

        private static void InitCity()
        {
            if (dtCities != null)
            {
                return;
            }
            lock (obj)
            {
                if (dtCities != null)
                {
                    return;
                }
                dtCities = new System.Data.DataTable();
                dtCities.Columns.Add(new System.Data.DataColumn("city"));
                dtCities.Columns.Add(new System.Data.DataColumn("code"));
                dtCities.Columns.Add(new System.Data.DataColumn("name"));

                XmlDocument doc = new XmlDocument();
                doc.Load(HttpContext.Current.Server.MapPath("/Config/City.xml"));

                XmlNodeList cities = doc.SelectNodes("province/city");
                foreach (XmlNode nod in cities)
                {
                    System.Data.DataRow dr = dtCities.NewRow();
                    dr["city"] = nod.InnerText;
                    dr["code"] = nod.Attributes["code"].Value;
                    dr["name"] = nod.Attributes["name"].Value;
                    dtCities.Rows.Add(dr);
                }
            }
        }

        public static string[] GetCurrentCity()
        {
            return new string[] { "", "", "Default" };
            /*
            if (!MyHandler.OpenCity)
            {
                return new string[] { "", "", "Default" };
            }
            string domain = HttpContext.Current.Request.Url.Host;
            if (ht.ContainsKey(domain))
            {
                return (string[])ht[domain];
            }
            if (dtCities == null)
                InitCity();
            string city = domain.IndexOf(".") > 0 ? domain.Substring(0, domain.IndexOf(".")) : domain;
            System.Data.DataRow[] drs = dtCities.Select("city='" + city + "'");
            if (drs.Length > 0)
            {
                if (!ht.ContainsKey(domain))
                {
                    ht.Add(domain, new string[] { drs[0]["name"].ToString(), drs[0]["code"].ToString(), city });
                }
                return new string[] { drs[0]["name"].ToString(), drs[0]["code"].ToString(), city };
            }
            if (!ht.ContainsKey(domain))
            {
                ht.Add(domain, new string[] { "", "", "Default" });
            }
            return new string[] { "", "", "Default" };
            */
        }

        public static void DeleteFile(string filename)
        {
            File.Delete(filename);
        }

        public static void DeleteFile(string folder, string filename)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);
            foreach (FileInfo fl in dir.GetFiles())
            {
                if (Regex.Match(fl.Name, filename, RegexOptions.Compiled).Success)
                {
                    File.Delete(folder + fl.Name);
                }
            }
        }

        public static void DeleteFile(string folder, string filename, bool subfolder)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);
            try
            {
                File.Delete(folder + filename);
            }
            catch
            { }
            //foreach (FileInfo fl in dir.GetFiles())
            //{
            //    File.Delete(folder + fl.Name);
            //    //if (Regex.Match(fl.Name, filename).Success)
            //    //{
            //    //    File.Delete(folder + fl.Name);
            //    //}
            //}
            if (subfolder)
            {
                foreach (DirectoryInfo dir_tmp in dir.GetDirectories())
                {
                    DeleteFile(dir_tmp.FullName + "\\", filename, true);
                }
            }
        }

        private static object objCache = new object();
        public static string GetCacheTime(string configpath, string module, string action, string defaultvalue)
        {
            DataTable dtCacheTime = null;
            if (HttpContext.Current.Cache.Get(configpath) != null)
            {
                dtCacheTime = (DataTable)HttpContext.Current.Cache.Get(configpath);
            }
            else
            {
                lock(objCache)
                {
                    dtCacheTime = new DataTable();
                    if (HttpContext.Current.Cache.Get(configpath) != null)
                    {
                        dtCacheTime = (DataTable)HttpContext.Current.Cache.Get(configpath);
                    }
                    else
                    {
                        dtCacheTime.Columns.Add("module");
                        dtCacheTime.Columns.Add("action");
                        dtCacheTime.Columns.Add("cachetime");

                        XmlDocument ConfigXml = new XmlDocument();
                        string path = HttpContext.Current.Server.MapPath(configpath);
                        ConfigXml.Load(path);

                        XmlNodeList files = ConfigXml.SelectNodes("configuration/files/file");
                        foreach (XmlNode node in files)
                        {
                            DataRow dr = dtCacheTime.NewRow();
                            dr["module"] = node.Attributes["module"].Value.Trim();
                            dr["action"] = node.Attributes["action"].Value.Trim();
                            dr["cachetime"] = node.Attributes["cachetime"].Value.Trim();
                            dtCacheTime.Rows.Add(dr);
                        }
                        HttpContext.Current.Cache.Insert(configpath, dtCacheTime, new System.Web.Caching.CacheDependency(path));
                    }
                }
            }
            if (dtCacheTime == null)
                return defaultvalue;
            DataRow[] drs = dtCacheTime.Select("module='" + module + "' And action='" + action + "'");
            if (drs == null || drs.Length==0)
            {
                drs = dtCacheTime.Select("module='" + module + "' And action='*'");
            }
            if (drs == null || drs.Length == 0)
            {
                drs = dtCacheTime.Select("module='*' And action='*'");
            }
            if (drs == null || drs.Length == 0)
            {
                return defaultvalue;
            }
            return drs[0]["cachetime"].ToString().Trim();
        }

        public static void Alert(string err)
        {
            HttpContext.Current.Response.Write("<script>alert(\"" + err + "\");</script>");
        }

        public static void PageBack()
        {
            HttpContext.Current.Response.Write("<script>history.back();</script>");
            HttpContext.Current.Response.End();
        }

        public static void GoTo(string url)
        {
            HttpContext.Current.Response.Write("<script>window.location.href='" + url + "';</script>");
            HttpContext.Current.Response.End();
        }

        public static void Redirect(string url)
        {
            HttpContext.Current.Response.Redirect(url);
            HttpContext.Current.Response.End();
        }

        public static object GetParam(string para, string defaultvalue, string type)
        {
            if (HttpContext.Current.Request[para] == null)
            {
                return defaultvalue;
            }
            else
            {
                if (string.IsNullOrEmpty(HttpContext.Current.Request[para].Trim()))
                {
                    return defaultvalue;
                }
                else
                {
                    if (type == "unsafe")
                    {
                        return HttpContext.Current.Request[para];
                    }
                    if (ProcessSqlStr(HttpContext.Current.Request[para].Trim()))
                    {
                        return HttpContext.Current.Request[para];
                    }
                    return defaultvalue;
                }
            }
        }

        public static object GetParam(string para, string defaultvalue)
        {
            return GetParam(para, defaultvalue, "safe");
        }

        /// <summary>
        /// SQl注入
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static bool ProcessSqlStr(string inputString)
        {
            string SqlStr =
                @"and|or|exec|execute|insert|select|delete|update|alter|create|drop|count|\*|chr|char|asc|mid|substring|master|truncate|declare|xp_cmdshell|restore|backup|net +user|net +localgroup +administrators";
            try
            {
                if ((inputString != null) && (inputString != string.Empty))
                {
                    string str_Regex = @"\b(" + SqlStr + @")\b";

                    Regex Regex = new Regex(str_Regex, RegexOptions.IgnoreCase);
                    //string s = Regex.Match(inputString).Value; 
                    if (Regex.IsMatch(inputString))
                        return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string GetAjaxPager(int recordnum, int pagesize, int currentpage, string pagerName)
        {
            StringBuilder sb = new StringBuilder();
            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            sb.Append(currentpage).Append("/").Append(pager).Append("<div style='display:none' class='recordnum'>" + recordnum + "</div><div style='display:none' class='pagenum'>" + pager + "</div>");
            if (currentpage > 1)
            {
                sb.Append("<a class=\"btn_prev\"  href=\"javascript:void(0)\" onclick=\"javascript:").Append(pagerName).Append("('").Append(currentpage - 1).Append("')").Append("\">上一页</a>");
            }
            if (currentpage < pager)
            {
                sb.Append("<a class=\"btn_next\" href=\"javascript:void(0)\" onclick=\"javascript:").Append(pagerName).Append("('").Append(currentpage + 1).Append("')").Append("\">下一页</a>");
            }
            return sb.ToString();
        }

        public static string GetPager2(string url, int recordnum, int pagesize, int currentpage)
        {
            string loc = string.Empty;
            StringBuilder sb = new StringBuilder();
            if (url.IndexOf("#") > -1)
            {
                loc = url.Substring(url.IndexOf("#"));
                url = url.Substring(0, url.IndexOf("#"));
            }
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            //sb.Append(currentpage).Append("/").Append(pager);
            if (currentpage > 1)
            {
                sb.Append("<a class=\"btn_prev previous_page\" href=\"" + url + "&page=" + (currentpage - 1) + loc + "\">◀︎</a>");
            }
            else
            {
                sb.Append("<span class=\"btn_prev previous_page disabled\">◀︎</span>");
            }
            if (currentpage < pager)
            {
                sb.Append("<a class=\"btn_next next_page\" href=\"" + url + "&page=" + (currentpage + 1) + loc + "\">▶︎</a>");
            }
            else
            {
                sb.Append("<span class=\"btn_prev next_page disabled\">▶︎</span>");
            }
            return sb.ToString();
        }

        public static string GetPager(string url, int recordnum, int pagesize, int currentpage)
        {
            string loc = string.Empty;
            StringBuilder sb = new StringBuilder();
            if (url.IndexOf("#") > -1)
            {
                loc = url.Substring(url.IndexOf("#"));
                url = url.Substring(0, url.IndexOf("#"));
            }
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            int p_start = 0;
            int p_end = 0;

            p_start = (currentpage / 10) * 10;
            p_end = p_start + 10;
            p_start = p_start < 1 ? 1 : p_start;
            p_end = p_end > pager ? pager : p_end;

            sb.Append("<a href=\"" + url + "&page=1" + loc + "\">首页</a>&nbsp;&nbsp;");
            if (p_start > 1)
            {
                sb.Append("&nbsp;<a href=\"" + url + "&page=" + (p_start - 1) + loc + "\">...</a>&nbsp;");
            }

            for (int i = p_start; i <= p_end; i++)
            {
                if (i != currentpage)
                    sb.Append("<a href=\"" + url + "&page=" + i + loc + "\">" + i + "</a>&nbsp;&nbsp;");
                else
                    sb.Append("<span class=\"page - now\" style='color:red'>" + i + "</span>&nbsp;&nbsp;");
            }

            //<span class="page-now">1</span>
            if (p_end < pager)
            {
                sb.Append("&nbsp;<a href=\"" + url + "&page=" + (p_end + 1) + loc + "\">...</a>&nbsp;");
            }
            sb.Append("<a href=\"" + url + "&page=" + pager + loc + "\">末页</a>&nbsp;&nbsp;");
            return sb.ToString();
        }

        public static string GetNoHTMLText(string str)
        {
            string html = str;

            html = Regex.Replace(html, @"<!--[\s\S]*?-->", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<[//]*tr[^>]*>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<[//]*p[^>]*>", "\n", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<[//]*br[^>]*>", "\n", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<[//]*div[^>]*>", "\n", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<STYLE[\s\S]*?</STYLE>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            html = Regex.Replace(html, @"<[\?!A-Za-z/][^><]*>", "");
            html = Regex.Replace(html, "\r", "");
            html = Regex.Replace(html, "\n", "\r\n");
            html = Regex.Replace(html, @"[　|\s]*\r\n[　|\s]*\r\n", "\r\n");
            //html = Regex.Replace(html,@"(\r\n)[^ 　]","$1");
            html = formatHtml(html);
            return (html);
        }

        public static string RestoreHTML(string str)
        {
            return str.Replace("&amp", "&").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static string formatHtml(string str)
        {
            //替换<P>
            string html = Regex.Replace(str, " ", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&nbsp;", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&nbsp", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#8226;", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#8226", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#146;", "'", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#147;", "“", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#148;", "”", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#160;", "", RegexOptions.Compiled);
            html = Regex.Replace(html, "&amp;", "&", RegexOptions.Compiled);
            html = Regex.Replace(html, "&copy;", "?", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#150;", "–", RegexOptions.Compiled);
            html = Regex.Replace(html, "&quot;", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&lt;", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&gt;", " ", RegexOptions.Compiled);
            html = Regex.Replace(html, "&#13;&#10;", "", RegexOptions.Compiled);
            return html;
        }

        public static Dictionary<string, string> DataRowToDictionary(System.Data.DataRow dr)
        {
            System.Data.DataColumnCollection cols = dr.Table.Columns;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            for (int i = 0; i < cols.Count; i++)
            {
                dic.Add(cols[i].ColumnName, dr[cols[i].ColumnName].ToString());
            }
            return dic;
        }

        public static System.Data.DataTable GetNewDataTable(System.Data.DataTable dt, string condition, string sortstr)
        {
            System.Data.DataTable newdt = new System.Data.DataTable();
            newdt = dt.Clone();
            System.Data.DataRow[] dr = dt.Select(condition, sortstr);
            for (int i = 0; i < dr.Length; i++)
            {
                newdt.ImportRow(dr[i]);
            }
            return newdt;
        }
        /*
        public static System.Data.DataTable SelectByPage(System.Data.DataTable oDT, int size, int page)
        {
            int from = size * (page - 1);
            int to = from + size;
            System.Data.DataTable NewTable = oDT.Clone();
            if (from > oDT.Rows.Count)
            {
                from = 0;
                to = size;
            }

            for (int i = from; i < to && i < oDT.Rows.Count; i++)
            {
                NewTable.ImportRow(oDT.Rows[i]);
            }
            return NewTable;
        }

        public static System.Data.DataTable SelectTop(int Top, System.Data.DataTable oDT)
        {
            if (oDT.Rows.Count < Top) return oDT;

            System.Data.DataTable NewTable = oDT.Clone();
            System.Data.DataRow[] rows = oDT.Select("1=1");
            for (int i = 0; i < Top; i++)
            {
                NewTable.ImportRow((System.Data.DataRow)rows[i]);
            }
            return NewTable;
        }

        public static System.Data.DataTable SelectLast(int Top, System.Data.DataTable oDT)
        {
            if (oDT.Rows.Count < Top) return oDT;

            System.Data.DataTable NewTable = oDT.Clone();
            System.Data.DataRow[] rows = oDT.Select("1=1");
            for (int i = Top; i < oDT.Rows.Count; i++)
            {
                NewTable.ImportRow(rows[i]);
            }
            return NewTable;
        }
        */
        public static string StrEncode(string str)
        {
            return System.Web.HttpUtility.UrlEncode(str, System.Text.Encoding.UTF8);
        }
        public static string StrDecode(string str)
        {
            return System.Web.HttpUtility.UrlDecode(str, System.Text.Encoding.UTF8);
        }
        /*
        public static string GetWebContentUsePost(string url, string param, string encoding)
        {
            string str = string.Empty;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(param);
            str = PostDataToUrl(buffer, url);
            return str;
        }
        */
        public static string md5(string str)
        {
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str, "MD5");
        }
        /*
        static string PostDataToUrl(byte[] data, string url)
        {
#region 创建httpWebRequest对象
            WebRequest webRequest = WebRequest.Create(url);
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null)
            {
                throw new ApplicationException(
                    string.Format("Invalid url string: {0}", url)
                    );
            }
#endregion

#region 填充httpWebRequest的基本信息
            //httpRequest.UserAgent = sUserAgent;
            //httpRequest.ContentType = sContentType;
            httpRequest.Method = "POST";
#endregion

#region 填充要post的内容
            httpRequest.ContentLength = data.Length;
            Stream requestStream = httpRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
#endregion

#region 发送post请求到服务器并读取服务器返回信息
            Stream responseStream;
            try
            {
                responseStream = httpRequest.GetResponse().GetResponseStream();
            }
            catch (Exception e)
            {
                throw e;
            }
#endregion

#region 读取服务器返回信息
            string stringResponse = string.Empty;
            using (StreamReader responseReader =
                new StreamReader(responseStream, System.Text.Encoding.Default))
            {
                stringResponse = responseReader.ReadToEnd();
            }
            responseStream.Close();
#endregion
            return stringResponse;
        }

        public static string GetUploadCode(string uploadbtnName, string fileField, string imgshowID, string imgurl, string uploadHandler)
        {
            string strupload="<script type=\"text/javascript\"> \n\r" +
"				$(document).ready(function() {\n\r" +
"					$(\"#$buttonUplod$\").click(function () { \n\r" +
"						$.ajaxFileUpload ({\n\r" +
"							url:'$uploadHandler$', //你处理上传文件的服务端 \n\r" +
"							secureuri:false, //与页面处理代码中file相对应的ID值\n\r" +
"							fileElementId:'$field$', \n\r" +
"							dataType: 'json', //返回数据类型:text，xml，json，html,scritp,jsonp五种\n\r" +
"							success: function (data) {\n\r" +
"								if(data.err==\"\")\n\r" +
"								{\n\r" +
"									$(\"#$imgurl$\").val(data.msg.url);\n\r" +
"									$showimg$document.getElementById(\"$imgshow$\").src=data.msg.url;\n\r" +
"								}\n\r" +
"								else\n\r" +
"								{\n\r" +
"									alert(data.err);\n\r" +
"								}\n\r" +
"							}\n\r" +
"						})\n\r" +
"					});\n\r" +
"				});\n\r" +
"			</script>";

            string ret = strupload.Replace("$buttonUplod$", uploadbtnName).Replace("$uploadHandler$", uploadHandler).Replace("$field$", fileField).Replace("$imgshow$", imgshowID).Replace("$imgurl$", imgurl);
            if (string.IsNullOrEmpty(imgshowID))
            {
                return ret.Replace("$showimg$", "//");
            }
            return ret;
        }

        public static string GetCommonUploadField(string buttonName, string fieldName)
        {
            return "<input id=\"" + fieldName + "\" type=\"file\" size=\"0\" name=\"" + fieldName + "\" class=\"AntInfoTextInputFile\" />&nbsp;<input type=\"button\" value=\"上传\" id=\"" + buttonName + "\" style=\"background:#ECE9D8; border:1px #A7A6AA solid; height:18px; line-height:18px; padding:0px; width:60px\"/>";
        }


        public static string GetUploadString(string uploadbtnName, string fileField, string imgshowID, string imgurl, string uploadHandler)
        {
            return GetCommonUploadField(uploadbtnName, fileField) + GetUploadCode(uploadbtnName, fileField, imgshowID, imgurl, uploadHandler);
        }

        public static string GetKE(string fieldName, string uploadHandler)
        {
            string strKE="<script type=\"text/javascript\">\n\r"+
"                            KE.show({\n\r" +
"	                            id : '$id$',\n\r" +
"	                            resizeMode : 1,\n\r" +
"	                            allowPreviewEmoticons : false,\n\r" +
"	                            allowUpload : true,\n\r" +
"	                            imageUploadJson : '$uploadHander$' ,\n\r" +
"	                            items : [\n\r" +
"	                            'source', '|', 'fontsize', 'textcolor', 'bgcolor', 'bold', 'italic', 'underline',\n\r" +
"	                            'removeformat', '|', 'justifyleft', 'justifycenter', 'justifyright', 'insertorderedlist',\n\r" +
"	                            'insertunorderedlist', '|', 'emoticons', 'image', 'link', 'unlink', 'about']\n\r" +
"                           });\n\r" +
"                            var $content = $('#content').val();\n\r" +
"	                            document.getElementById(\"gxform\").onreset = function(){\n\r" +
"	                            KE.html('$id$', $content);\n\r" +
"                            }\n\r" +
"                            </script>";
            return strKE.Replace("$id$", fieldName).Replace("$uploadHander$", uploadHandler);
        }
        */
        static public int trueLength(string str)
        {
            // str 字符串
            // return 字符串的字节长度
            int lenTotal = 0;
            int n = str.Length;
            string strWord = "";
            int asc;
            for (int i = 0; i < n; i++)
            {
                strWord = str.Substring(i, 1);
                asc = Convert.ToChar(strWord);
                if (asc < 0 || asc > 127)
                    lenTotal = lenTotal + 2;
                else
                    lenTotal = lenTotal + 1;
            }

            return lenTotal;
        }


        static public string CutString(string strOriginal, int maxTrueLength)
        {

            // strOriginal 原始字符串
            // maxTrueLength 需要返回的字符串的字节长度
            // chrPad 字符串不够时的填充字符
            // blnCutTail 字符串的字节长度超过maxTrueLength时是否截断多余字符
            // return 返回填充或截断后的字符串
            char chrPad='.';
            bool blnCutTail = true;
            string strNew = strOriginal;

            if (strOriginal == null || maxTrueLength <= 0)
            {
                strNew = "";
                return strNew;
            }

            int trueLen = trueLength(strOriginal);
            if (trueLen > maxTrueLength)//超过maxTrueLength
            {
                if (blnCutTail)//截断
                {
                    for (int i = strOriginal.Length - 1; i > 0; i--)
                    {
                        strNew = strNew.Substring(0, i);
                        if (trueLength(strNew) == maxTrueLength)
                            break;
                        else if (trueLength(strNew) < maxTrueLength)
                        {
                            strNew += chrPad.ToString();
                            break;
                        }
                    }
                }
            }
            else//填充
            {
                for (int i = 0; i < maxTrueLength - trueLen; i++)
                {
                    strNew += chrPad.ToString();
                }
            }

            return strNew;
        }
    }
}