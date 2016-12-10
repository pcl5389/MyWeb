using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using VTemplate.Engine;
using System.Globalization;
using System.IO.Compression;
using System.Net.Sockets;

namespace MyWeb
{
    public class MyHandler : IHttpHandler
    {
        private string CD = @"\";
        private string _AppName = @"";
        private string _fileCacheConfig = string.Empty;
        private Encoding _templateEncoding = Encoding.UTF8;
        private string _templatepath = "";
        private string _viewStyle = "";
		private int _cacheTime = -2;
        private bool hasRegAjaxScript;
        private string _handlerExt = string.Empty;
        private string _templatefile = string.Empty;
        public static CultureInfo Culture = new CultureInfo("zh-CN");
        public TemplateDocument doc = null;

        public void InitConfig(string configname)
        {
            System.Collections.Hashtable hTable = MyWeb.Common.htHandlerConfigs;
            HandlerConfig config = (MyWeb.HandlerConfig)hTable[configname];
            CurrentDirectory = config.CurrentDirectory;                       // 当前路径名
            AppName = config.AppName;                                  // 同上
            FileCacheConfig = config.FileCacheConfig;           //文件缓存规则
            ViewStyle = config.ViewStyle;
            CacheTime = config.CacheTime;
            TemplatePath = config.TemplatePath;
        }

        public MyHandler()
        {}

        public static bool Anti_Virus {
            get
            {
                if (HttpContext.Current.Cache.Get("AntiVirus") == null)
                {
                    if (ConfigurationManager.AppSettings["AntiVirus"] == null)
                    {
                        HttpContext.Current.Cache.Insert("AntiVirus", true);
                    }
                    else
                        HttpContext.Current.Cache.Insert("AntiVirus", bool.Parse(ConfigurationManager.AppSettings["AntiVirus"]));
                }
                return bool.Parse(HttpContext.Current.Cache.Get("AntiVirus").ToString());
            }
        }

        public static string CacheType
        {
            get
            {
                if (HttpContext.Current.Cache.Get("CacheType") == null)
                {
                    if (ConfigurationManager.AppSettings["CacheType"] == null)
                    {
                        HttpContext.Current.Cache.Insert("CacheType", "File");
                    }
                    else
                        HttpContext.Current.Cache.Insert("CacheType", ConfigurationManager.AppSettings["CacheType"].ToString());
                }
                return HttpContext.Current.Cache.Get("CacheType").ToString();
            }
        }

        public static bool bGZip {
            get
            {
                if (HttpContext.Current.Cache.Get("GZip") == null)
                {
                    if (ConfigurationManager.AppSettings["GZip"] == null)
                    {
                        HttpContext.Current.Cache.Insert("GZip", true);
                    }
                    else
                        HttpContext.Current.Cache.Insert("GZip", bool.Parse(ConfigurationManager.AppSettings["GZip"]));
                }
                return bool.Parse(HttpContext.Current.Cache.Get("GZip").ToString());
            }
        }

        public static string HOST {
            get {
                if (HttpContext.Current.Cache.Get("HOST") == null)
                {
                    if (ConfigurationManager.AppSettings["HOST"] == null)
                    {
                        HttpContext.Current.Cache.Insert("HOST", HttpContext.Current.Request.Url.GetLeftPart(System.UriPartial.Authority));
                    }
                    else
                        HttpContext.Current.Cache.Insert("HOST", "http://" + ConfigurationManager.AppSettings["host"].ToString());
                }
                return HttpContext.Current.Cache.Get("HOST").ToString();
            }
        }


        public static bool ToHTML
        {
            get
            {
                if (HttpContext.Current.Cache.Get("ToHTML") == null)
                {
                    if (ConfigurationManager.AppSettings["ToHTML"] == null)
                    {
                        HttpContext.Current.Cache.Insert("ToHTML", false);
                    }
                    else
                        HttpContext.Current.Cache.Insert("ToHTML", bool.Parse(ConfigurationManager.AppSettings["ToHTML"]));
                }
                return bool.Parse(HttpContext.Current.Cache.Get("ToHTML").ToString());
            }
        }

        public static string HandlerExt
        {
            get
            {
                /*
                if (HttpContext.Current.Cache.Get("HandlerExt") == null)
                {
                    if (ConfigurationManager.AppSettings["HandlerExt"] == null)
                    {
                        HttpContext.Current.Cache.Insert("HandlerExt", AjaxPro.Utility.HandlerExtension);
                    }
                    else
                    {
                        AjaxPro.Utility.HandlerExtension = ConfigurationManager.AppSettings["HandlerExt"].Trim();
                        HttpContext.Current.Cache.Insert("HandlerExt", ConfigurationManager.AppSettings["HandlerExt"].Trim());
                    }
                }
                */
                return AjaxPro.Utility.HandlerExtension;
            }
        }

        public static string TMPLEXT
        {
            get
            {
                if (HttpContext.Current.Cache.Get("TmplExt") == null)
                {
                    if (ConfigurationManager.AppSettings["TmplExt"] == null)
                    {
                        HttpContext.Current.Cache.Insert("TmplExt", ".html");
                    }
                    else
                        HttpContext.Current.Cache.Insert("TmplExt", ConfigurationManager.AppSettings["TmplExt"]);
                }
                return HttpContext.Current.Cache.Get("TmplExt").ToString();
            }
        }

        public static bool OpenCity {
            get
            {
                if (HttpContext.Current.Cache.Get("OpenCity") == null)
                {
                    if (ConfigurationManager.AppSettings["OpenCity"] == null)
                    {
                        HttpContext.Current.Cache.Insert("OpenCity", "no");
                    }
                    else
                        HttpContext.Current.Cache.Insert("OpenCity", ConfigurationManager.AppSettings["OpenCity"]);
                }
                return HttpContext.Current.Cache.Get("OpenCity").Equals("yes");
            }
        }

        public static string CurAssembly
        {
            get
            {
                if (HttpContext.Current.Cache.Get("AssemblyName") == null)
                {
                    HttpContext.Current.Cache.Insert("AssemblyName", ConfigurationManager.AppSettings["AssemblyName"]);
                }
                return HttpContext.Current.Cache.Get("AssemblyName").ToString();
            }
        }

        public string CurrentDirectory
        {
            get { return CD; }
            set { CD = value; }
        }

        public int CacheTime
        {
			set
            {
                _cacheTime = value;
            }

            get
            {
				if(_cacheTime==-2)
                {
                    if (HttpContext.Current.Cache.Get("cachetime") == null)
                    {
                        HttpContext.Current.Cache.Insert("cachetime", ConfigurationManager.AppSettings["cacheTime"]);
                    }
                    _cacheTime= int.Parse(HttpContext.Current.Cache.Get("cachetime").ToString());
                }
                return _cacheTime;
            }
        }

        public string AppName
        {
            get { return _AppName == "" ? "" : _AppName + "."; }
            set { _AppName = value; }
        }

        public string FileCacheConfig
        {
            get { return _fileCacheConfig; }
            set { _fileCacheConfig = value; }
        }

        public Encoding TemplateEncoding
        {
            get { return _templateEncoding; }
            set { _templateEncoding = value; }
        }

        public string TemplateFile {
            get { return _templatefile; }
            set { _templatefile = value; }
        }

        public string ViewStyle
        {
            get{
                if(string.IsNullOrEmpty(_viewStyle))
                {
                    if (HttpContext.Current.Cache.Get("viewStyle") == null)
                    {
                        HttpContext.Current.Cache.Insert("viewStyle", ConfigurationManager.AppSettings["viewStyle"]);
                    }
                    _viewStyle= HttpContext.Current.Cache.Get("viewStyle").ToString();
                }
                return _viewStyle;
            }
            set
            {
                _viewStyle = value;
            }
        }

        public static string FrameWork
        {
            get
            {
                if (HttpContext.Current.Cache.Get("framework") == null)
                {
                    HttpContext.Current.Cache.Insert("framework", ConfigurationManager.AppSettings["framework"]);
                }
                return HttpContext.Current.Cache.Get("framework").ToString();
            }
        }

        public static bool bSubFolder
        {
            get
            {
                if (HttpContext.Current.Cache.Get("SubFolder") == null)
                {
                    if (ConfigurationManager.AppSettings["SubFolder"] == null || ConfigurationManager.AppSettings["SubFolder"]=="0")
                    {
                        HttpContext.Current.Cache.Insert("SubFolder", false);
                    }
                    else
                    {
                        if (ConfigurationManager.AppSettings["SubFolder"] == "1")
                            HttpContext.Current.Cache.Insert("SubFolder", true);
                        else
                            HttpContext.Current.Cache.Insert("SubFolder", bool.Parse(ConfigurationManager.AppSettings["SubFolder"]));
                    }
                }
                return (bool) HttpContext.Current.Cache.Get("SubFolder");
            }
        }

        public string TemplatePath
        {
            get { return string.IsNullOrEmpty(_templatepath) ? "" : _templatepath + "/"; }
            set { _templatepath = value; }
        }

        public virtual void ProcessRequest(HttpContext context)
        {
#if Debug
            DateTime dt1 = DateTime.Now;
#endif
            Page_Load();
#if Debug
            HttpContext.Current.Response.Write("页面执行时间:" + (DateTime.Now - dt1).TotalMilliseconds.ToString());
#endif
        }

        public virtual bool IsReusable
        {
            get
            {
                return true;
            }
        }
        private delegate void deleInvoke(TemplateDocument doc);

        /*[CLSCompliant(false)]*/
        public TemplateDocument RendTemplate(string path)
        {
            doc = TemplateDocument.FromFileCache(path, TemplateEncoding);
            return doc;         
        }

        protected void Page_Load()
        {
            string RCD = HttpContext.Current.Server.MapPath(CD);
            string[] city = Common.GetCurrentCity();
            string CacheFilePath = string.Empty;
            string CacheFileName = string.Empty;
            string Url_Absolute_Path = HttpContext.Current.Request.Url.AbsolutePath;
            #region 生成静态页
            if (Url_Absolute_Path.Equals("/RealUrl.aspx") || Url_Absolute_Path.Equals("/AutoMakeHTML.aspx"))
            {
                if (Url_Absolute_Path.Equals("/RealUrl.aspx"))
                {
                    string url = MyWeb.Common.GetParam("url", "").ToString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        HttpContext.Current.Response.Write(Module.PageStatic.ToRealURL(url));
                        HttpContext.Current.Response.End();
                    }
                }
                else
                {
                    string url = MyWeb.Common.StrDecode(MyWeb.Common.GetParam("url", "").ToString()), URL = string.Empty;
                    if (!string.IsNullOrEmpty(url) && !url.Equals(URL = MyWeb.Module.PageStatic.ToRealURL(url)))
                    {
                        TcpClient clientSocket = new TcpClient();
                        Uri URI = null;
                        try
                        {
                            URI = new Uri(MyWeb.MyHandler.HOST + URL);
                            clientSocket.Connect(URI.Host, URI.Port);
                        }
                        catch
                        {
                            return;
                        }

                        StringBuilder RequestHeaders = new StringBuilder();//用来保存HTML协议头部信息

                        RequestHeaders.AppendLine(string.Format("{0} {1} HTTP/1.1", "HEAD", URI.PathAndQuery));
                        RequestHeaders.AppendLine("Accept: text/html, application/xhtml+xml, */*");
                        RequestHeaders.AppendLine("Accept-Language: zh-Hans-CN,zh-Hans;q=0.5");
                        RequestHeaders.AppendLine("User-Agent: Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko");
                        RequestHeaders.AppendLine("Connection: close"); //Keep-Alive | Close
                        RequestHeaders.AppendLine("DNT: 1");
                        RequestHeaders.AppendLine(string.Format("Host: {0}", URI.Host + (URI.Port == 80 ? "" : (":" + URI.Port.ToString()))));

                        RequestHeaders.Append("\r\n");
                        RequestHeaders.Append("\r\n");

                        try
                        {
                            byte[] request = Encoding.UTF8.GetBytes(RequestHeaders.ToString());
                            clientSocket.Client.Send(request);
                            byte[] buffer = new byte[1000];
                            int icount = clientSocket.Client.Receive(buffer, 1000, SocketFlags.None);
                            if (icount > 0)
                            {
                                string content = Encoding.UTF8.GetString(buffer, 0, icount);
                                if (!content.StartsWith("HTTP/1.1 200 OK") || content.IndexOf("Content-Length: 76\r\n") > 0)
                                {
                                    HttpContext.Current.Response.Write("err");
                                }
                                else
                                    HttpContext.Current.Response.Write("ok");
                            }
                            clientSocket.Close();
                        }
                        catch (Exception e)
                        {
                            HttpContext.Current.Response.Write(e.Message.ToString());
                        }
                        finally
                        {
                            if (clientSocket.Client != null)
                            {
                                clientSocket.Client.Close();
                            }
                            if (clientSocket != null)
                            {
                                clientSocket.Close();
                            }
                            HttpContext.Current.Response.End();
                        }
                    }
                }
            }
            #endregion

            bool bRefresh = HttpContext.Current.Request.QueryString["refresh"] == null ? false : bool.Parse(HttpContext.Current.Request.QueryString["refresh"].ToString());
            if (bGZip)
            {
                ResponseCompressionType compressionType = this.GetCompressionMode(HttpContext.Current.Request);
                if (compressionType != ResponseCompressionType.None)
                {
                    HttpContext.Current.Response.AppendHeader("Content-Encoding", compressionType.ToString().ToLower());
                    if (compressionType == ResponseCompressionType.GZip)
                    {
                        HttpContext.Current.Response.Filter = new GZipStream(HttpContext.Current.Response.Filter, CompressionMode.Compress);
                    }
                    else
                    {
                        HttpContext.Current.Response.Filter = new DeflateStream(HttpContext.Current.Response.Filter, CompressionMode.Compress);
                    }
                }
            }

            if (HttpContext.Current.Request.QueryString["tpl"] != null && !string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["tpl"]))
            {
                TemplateFile = HttpContext.Current.Request.QueryString["tpl"].Trim();
            }
#if Debug
            HttpContext.Current.Response.Write("Refresh:" + bRefresh.ToString() + "\n");
#endif
            //string webbase = HttpContext.Current.Request.Url.GetLeftPart(System.UriPartial.Authority) + CurrentDirectory.Replace(@"\", "/") + "View/" + ViewStyle + (string.IsNullOrEmpty(TemplatePath) ? "" : ("/" + TemplatePath.Remove(TemplatePath.Length-1)));
            string webbase = HOST + CurrentDirectory.Replace(@"\", "/") + "View/" + ViewStyle + (string.IsNullOrEmpty(TemplatePath) ? "" : ("/" + TemplatePath.Remove(TemplatePath.Length - 1)));
            string oldWebBase = webbase;

            //
            string path =string.Empty;
            if (Url_Absolute_Path.IndexOf(_AppName, StringComparison.OrdinalIgnoreCase) > -1)
            {
                if (Url_Absolute_Path.Length <= (AppName.Length+1))
                {
                    path = "";
                }
                else
                {
                    path = Url_Absolute_Path.Substring(AppName.Length + 1).ToLower();
                }
            }
            else
            {
                path = Url_Absolute_Path.Substring(1);
            }
            //string path = HttpContext.Current.Request.Url.AbsolutePath.IndexOf(_AppName, StringComparison.OrdinalIgnoreCase) > -1 ? HttpContext.Current.Request.Url.AbsolutePath.Substring(AppName.Length+1).ToLower() : HttpContext.Current.Request.Url.AbsolutePath.Substring(1);
#if Debug
            HttpContext.Current.Response.Write("appname:" + AppName + "\n");
            HttpContext.Current.Response.Write("path:" + path + "\n");
#endif
            string M = string.Empty;
            string A = string.Empty;
            string P = HttpContext.Current.Request.Url.Query.Replace("?", "_").Replace("\\", "_").Replace("/", "_");

            //TemplateDocument doc = null;
            string[] ms = path.Split('/');

            if (ms.Length >= 1)
            {
                M = ms[0].Replace(".aspx", "").Replace("default", "index").Trim();
                M = M == "" ? "index" : M;
                if (ms.Length > 1)
                {
                    A = ms[1].Replace(".aspx", "").ToLower().Replace("default", "index").Trim();
                    A = A == "" ? "index" : A;
                }
                else
                {
                    A = "index";
                }
            }
            else
            {
                M = "index";
                A = "index";
            }
            M = M.Replace("-", "_");
            A = A.Replace("-", "_");
            if (M.Length > 1)
                M = M.Substring(0, 1).ToUpper() + M.Substring(1);
            else
                M = M.ToUpper();

            if (M.Equals("undefined") || A.Equals("undefined") || A.Equals("images"))
            {
                HttpContext.Current.Response.End();
            }
            string C = string.Format("{2}.{0}Control.{1}", AppName, M, CurAssembly);
#if Debug
            HttpContext.Current.Response.Write("M:" + M + " - A:" + A + " - P:" + P + " - C:" + C + "\n");
#endif
            if (FileCacheConfig != "" && File.Exists(HttpContext.Current.Server.MapPath(FileCacheConfig)))
            {
                CacheTime = int.Parse(Common.GetCacheTime(FileCacheConfig, M, A, CacheTime.ToString()));
            }

            if(ToHTML)
            {
                CacheFilePath = string.Format("{0}{1}", RCD, MyWeb.Module.PageStatic.BasePath);
                CacheFileName = MyWeb.Module.PageStatic.GetHTMLPath(HttpContext.Current.Request.RawUrl).Trim();
                if (string.IsNullOrEmpty(CacheFileName))
                {
                    CacheFilePath = string.Format("{0}temp/{1}{2}/", RCD, TemplatePath, city[2]).Replace("/Default/", "/");
                    CacheFileName = string.Format("{0}_{1}{2}.html", M, A, P);
                 }
            }
            else
            { 
                CacheFilePath = string.Format("{0}temp/{1}{2}/", RCD, TemplatePath, city[2]).Replace("/Default/", "/");
                CacheFileName = string.Format("{0}_{1}{2}.html", M, A, P);
            }
#if Debug
            HttpContext.Current.Response.Write("缓存时间: " + CacheTime.ToString() + "\n");
            HttpContext.Current.Response.Write("缓存文件: " + CacheFilePath + CacheFileName + "\n");
#endif
            if (CacheTime != 0)
            {
                if (CacheType == "File")
                {
                    if (File.Exists(CacheFilePath + CacheFileName))
                    {
#if Debug
                        HttpContext.Current.Response.Write("缓存文件已存在！\n");
#endif
                        FileInfo fi = new FileInfo(CacheFilePath + CacheFileName);
#if Debug
                        HttpContext.Current.Response.Write("距上次修改：" + (DateTime.Now - fi.LastWriteTime).TotalSeconds.ToString() + "秒 \n");
#endif
                        if (!bRefresh && (CacheTime == -1 || (DateTime.Now - fi.LastWriteTime).TotalSeconds < CacheTime))
                        {
                            //HttpContext.Current.Response.Write("&&&&&&&&！" + "\n");

                            const long ChunkSize = 102400;//100K 每次读取文件，只读取100K，这样可以缓解服务器的压力 
                            byte[] buffer = new byte[ChunkSize];
                            HttpContext.Current.Response.Clear();
                            FileStream iStream = File.OpenRead(CacheFilePath + CacheFileName);
                            long dataLengthToRead = iStream.Length;//获取下载的文件总大小 
                            while (dataLengthToRead > 0 && HttpContext.Current.Response.IsClientConnected)
                            {
                                int lengthRead = iStream.Read(buffer, 0, Convert.ToInt32(ChunkSize));//读取的大小 
                                HttpContext.Current.Response.OutputStream.Write(buffer, 0, lengthRead);
                                HttpContext.Current.Response.Flush();
                                dataLengthToRead = dataLengthToRead - lengthRead;
                            }
                            iStream.Close();
                            iStream.Dispose();
                            //HttpContext.Current.Response.WriteFile(CacheFilePath + CacheFileName);
                            HttpContext.Current.Response.End();
                        }
                        else
                        {
                            File.Delete(CacheFilePath + CacheFileName);
                        }
                    }
                }
                else
                {
                    object objWebContent = HttpContext.Current.Cache.Get(CacheFilePath + CacheFileName);
                    if (objWebContent != null)
                    {
                        HttpContext.Current.Response.Write(objWebContent);
                        HttpContext.Current.Response.End();
                    }
                }
            }
            string tplpath = string.Empty;
            if (bSubFolder)
            {
                if (!string.IsNullOrEmpty(TemplateFile))
                {
                    tplpath = string.Format("{0}View/{1}/{2}{3}/{4}{5}", RCD, ViewStyle, TemplatePath, M, TemplateFile, TMPLEXT);
                }
                else
                {
                    tplpath = string.Format("{0}View/{1}/{2}{3}/{4}{5}", RCD, ViewStyle, TemplatePath, M, A, TMPLEXT);
                }
                
                webbase = webbase + "/" + M;
            }
            else
            {
                if (!string.IsNullOrEmpty(TemplateFile))
                {
                    tplpath = string.Format("{0}View/{1}/{2}{3}_{4}{5}", RCD, ViewStyle, TemplatePath, M, TemplateFile, TMPLEXT).Replace("_index" + TMPLEXT, TMPLEXT);
                }
                else
                {
                    tplpath = string.Format("{0}View/{1}/{2}{3}_{4}{5}", RCD, ViewStyle, TemplatePath, M, A, TMPLEXT).Replace("_index" + TMPLEXT, TMPLEXT);
                }
            }
#if Debug
            HttpContext.Current.Response.Write("模板文件: " + tplpath + "\n");
            HttpContext.Current.Response.Write("自定义模板文件: " + TemplateFile + "\n");
#endif
            if (!File.Exists(tplpath))
            {
                if (bSubFolder) //兼容老版本
                {
                    if (!string.IsNullOrEmpty(TemplateFile))
                    {
                        tplpath = string.Format("{0}View/{1}/{2}{3}_{4}{5}", RCD, ViewStyle, TemplatePath, M, TemplateFile, TMPLEXT).Replace("_index" + TMPLEXT, TMPLEXT);
                    }
                    else
                    {
                        tplpath = string.Format("{0}View/{1}/{2}{3}_{4}{5}", RCD, ViewStyle, TemplatePath, M, A, TMPLEXT).Replace("_index" + TMPLEXT, TMPLEXT);
                    }
                    if (File.Exists(tplpath))
                    {
                        doc = TemplateDocument.FromFileCache(tplpath, TemplateEncoding);
                        webbase = oldWebBase;
                    }
                }
#if Debug
                HttpContext.Current.Response.Write("模板 " + tplpath + " 不存在！" + "\n");
#endif
            }
            else
            {
                doc = TemplateDocument.FromFileCache(tplpath, TemplateEncoding);
            }
            
            DateTime dt_start = DateTime.Now;

            string typename=string.Format("{2}.{0}Control.{1}", AppName, M, CurAssembly);
            Type typ =null;
            if (Common.CacheTypes.ContainsKey(typename))
                typ = Common.CacheTypes[typename] as Type;

            if (typ == null)
            {
                lock (Common.objTypes)
                {
                    if (!Common.CacheTypes.ContainsKey(typename))
                    {
                        typ = Assembly.Load(CurAssembly).GetType(typename, false);
                        //typ = Type.GetType(typename);
                        Common.CacheTypes[typename] = typ;
                    }
                    else
                        typ = Common.CacheTypes[typename] as Type;
                }
            }
            
            if (typ != null)
            {
                object obj = Activator.CreateInstance(typ);
                /*
                object obj = null;
                if (Common.CacheClass.ContainsKey(typename))
                    obj = Common.CacheClass[typename];
                else
                {
                    lock (Common.objClass)
                    {
                        if (!Common.CacheClass.ContainsKey(typename))
                        {
                            //obj = typ.InvokeMember("", BindingFlags.CreateInstance, null, null, null);
                            obj = Activator.CreateInstance(typ);
                            Common.CacheClass[typename] = obj;
                        }
                        else
                        {
                            obj = Common.CacheClass[typename];
                        }
                    }
                 }
                 */
                if (obj == null)
                {
                    HttpContext.Current.Response.Write("实例化失败！");
                    HttpContext.Current.Response.End();
                }
                deleInvoke d = (deleInvoke)Delegate.CreateDelegate(typeof(deleInvoke), obj, A);
                if (d == null || obj==null) //类中的方法不存在
                {
                    HttpContext.Current.Response.Write("<center>要执行的网页不存在！<br />返回<a href='/'><b>首页</b></a></center>");
                    HttpContext.Current.Response.End();
                }
                else
                {
#if Debug
                    DateTime dt1 = DateTime.Now;
#endif
                    d.Invoke(doc);
#if Debug
                    HttpContext.Current.Response.Write("Action执行时间:" + (DateTime.Now - dt1).TotalMilliseconds.ToString());
#endif
                    //methodInfo.Invoke(obj, new object[] { doc });
                    //IMethodInvoker invoker = FastReflectionCaches.MethodInvokerCache.Get(methodInfo);
                    //invoker.Invoke(obj, new object[] { doc });
                    /*
                    if (FrameWork == "2.0") // 快速反射 需.NET 3.5 
                    {
                        methodInfo.Invoke(obj, new object[] { doc });
                    }
                    else  
                    {
                        IMethodInvoker invoker = FastReflectionCaches.MethodInvokerCache.Get(methodInfo);
                        invoker.Invoke(obj, new object[] { doc });
                    }
                     */
                }



                // 快速反射 需.NET 3.5 
                //typ.InvokeMember(A, BindingFlags.InvokeMethod, null, obj, new object[] { doc });
                //IMethodInvoker invoker = FastReflectionCaches.MethodInvokerCache.Get(methodInfo);
                //invoker.Invoke(obj, new object[] {doc});
                if (doc != null)
                {
#if Debug
                    HttpContext.Current.Response.Write("真实模板路径：" + doc.File + "\n");
#endif
                    hasRegAjaxScript = false;
                    if (doc.Variables.Contains("ajaxjs"))
                    {
                        doc.SetValue("ajaxjs", RegAjaxScript(typ));
                    }
                    doc.SetValue("null", null);
                    doc.SetValue("webbase", webbase);
                }
                else
                {
                    return;
                }
            }
            else   //类不存在
            {
#if Debug
                HttpContext.Current.Response.Write("类: " + CurAssembly + "." + AppName + "Control." + M + " 未找到！" + "\n");
#endif
                if (ConfigurationManager.AppSettings["NoPage"] != null)
                {
                    HttpContext.Current.Response.Redirect(ConfigurationManager.AppSettings["NoPage"].ToString()+M);
                    HttpContext.Current.Response.End();
                }
                else
                {
                    HttpContext.Current.Response.Write("<center>网页不存在！<br />返回<a href='/'><b>首页</b></a></center>");
                    HttpContext.Current.Response.End();
                }
            }
            //HttpContext.Current.Response.Buffer = true;
            string webcontent = doc.GetRenderText(); 

            if (ToHTML && CacheType == "File")
            {
                webcontent = MyWeb.Module.PageStatic.DealURL(webcontent);
            }

            if (CacheTime != 0 && !File.Exists(CacheFilePath + CacheFileName)) //生成静态
            {
                if (CacheType == "File")
                {
                    MyWeb.Module.PageStatic.ToHTML(webcontent, CacheFilePath + CacheFileName);
                }
                else
                {
                    if (CacheTime == -1)
                        HttpContext.Current.Cache.Insert(CacheFilePath + CacheFileName, webcontent);
                    else
                        HttpContext.Current.Cache.Insert(CacheFilePath + CacheFileName, webcontent, null, DateTime.Now.AddSeconds(CacheTime), System.Web.Caching.Cache.NoSlidingExpiration);
                }
            }
            HttpContext.Current.Response.Write(webcontent);
        }

        private ResponseCompressionType GetCompressionMode(HttpRequest request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(acceptEncoding))
            {
                return ResponseCompressionType.None;
            }
            acceptEncoding = acceptEncoding.ToUpperInvariant();
            if (acceptEncoding.Contains("GZIP"))
            {
                return ResponseCompressionType.GZip;
            }
            else if (acceptEncoding.Contains("DEFLATE"))
            {
                return ResponseCompressionType.Deflate;
            }
            else
            {
                return ResponseCompressionType.None;
            }
        }
        private enum ResponseCompressionType { None, GZip, Deflate }

        public string RegAjaxScript(Type type)
        {
            StringBuilder sb = new StringBuilder();
            if (!hasRegAjaxScript)
            {
                sb.AppendLine("<script type=\"text/javascript\" src=\"/public/ajaxPro2.js\"></script>");
            }
            sb.Append("<script type=\"text/javascript\" src=\"/ajaxpro/");
            string jsName = type.AssemblyQualifiedName;
            int e = jsName.IndexOf(',', jsName.IndexOf(',') + 1);
            sb.Append(jsName.Substring(0, e) + MyHandler.HandlerExt);
            sb.Append("\"></script>");
            hasRegAjaxScript = true;
            return sb.ToString();
        }
    }
}