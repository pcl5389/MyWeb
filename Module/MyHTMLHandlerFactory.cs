using System;
using System.Web;
using System.IO;

namespace MyWeb.Module
{
    sealed class MyHTMLHandlerFactory : IHttpHandlerFactory
    {
        public static IHttpHandler staticHandler = null;

        IHttpHandler IHttpHandlerFactory.GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            if (File.Exists(context.Server.MapPath(url))) //文件存在
            {
                if (staticHandler == null)
                {
                    Type type = typeof(HttpApplication).Assembly.GetType("System.Web.StaticFileHandler", true);
                    staticHandler = (IHttpHandler)Activator.CreateInstance(type, true);
                }
                return staticHandler;
            }
            else
            {
                string newurl = PageStatic.ToRealURL(url);
                if (newurl != url)
                {
                    HttpContext.Current.Response.Redirect(MyHandler.HOST + newurl, true);
                }
            }
            if (staticHandler == null)
            {
                Type type = typeof(HttpApplication).Assembly.GetType("System.Web.StaticFileHandler", true);
                staticHandler = (IHttpHandler)Activator.CreateInstance(type, true);
            }
            return staticHandler;
        }

        void IHttpHandlerFactory.ReleaseHandler(IHttpHandler handler)
        {}
    }
}