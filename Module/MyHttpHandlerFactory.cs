using System;
using System.Web;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;

namespace MyWeb.Module
{
    sealed class MyHttpHandlerFactory : IHttpHandlerFactory
    {
        public static object objTypes = new object();
        public static Hashtable CacheTypes = new Hashtable();

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            string handlerName = string.Empty;
            url = url.ToLower(MyHandler.Culture);
            StringBuilder sb = new StringBuilder();

            Common.init();
            string[] val = { };
            foreach (object key in MyWeb.Common.htRounting.Keys)
            {
                val = (string[])MyWeb.Common.htRounting[key];
                sb.Append(val[0]).Append("|");
                Match m = Regex.Match(url, val[0], RegexOptions.IgnoreCase);

                if (m.Success && url == m.Value)
                {
                    url = val[1];
                    if (!string.IsNullOrEmpty(val[2]))
                    {
                        handlerName = val[2];
                    }
                    break;
                }
            }
            string assemName = string.Empty;
            if (handlerName.IndexOf(",", StringComparison.CurrentCulture) > -1)
            {
                assemName = handlerName.Split(',')[1].Trim();
                handlerName = handlerName.Split(',')[0].Trim();
            }
            else
            {
                HttpContext.Current.Response.Write("请更新到最新框架.");
                return null;
            }
            handlerName = assemName + "." + handlerName;
            Type typ = null;
            if (Common.CacheTypes.ContainsKey(handlerName))
                typ = Common.CacheTypes[handlerName] as Type;
            if (typ == null)
            {
                lock (objTypes)
                {
                    if (!CacheTypes.ContainsKey(handlerName))
                    {
                        typ = Assembly.Load(assemName).GetType(handlerName, false, true);
                        CacheTypes[handlerName] = typ;
                    }
                    else
                        typ = CacheTypes[handlerName] as Type;
                }
            }

            if (typ == null)
            {
                HttpContext.Current.Response.Write("<p align=center>模块: " + handlerName + " 加载失败！╯_╰ </p>");
                return null;
            }
            object obj = Activator.CreateInstance(typ);
            return (IHttpHandler)obj;
        }

        IHttpHandler IHttpHandlerFactory.GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            return GetHandler(context, requestType, url, pathTranslated);
        }

        void IHttpHandlerFactory.ReleaseHandler(IHttpHandler handler)
        {}
    }
}