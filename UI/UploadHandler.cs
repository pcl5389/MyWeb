using System;
using System.Text.RegularExpressions;
using System.Web;

namespace MyWeb.UI
{
    public class UploadHandler : IHttpHandler
    {
        /// <summary>
        /// 您将需要在网站的 Web.config 文件中配置此处理程序 
        /// 并向 IIS 注册它，然后才能使用它。有关详细信息，
        /// 请参见下面的链接: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members
        //protected HttpRequest Request { get; private set; }
        //protected HttpResponse Response { get; private set; }
        //protected HttpServerUtility Server { get; private set; }

        public bool IsReusable
        {
            // 如果无法为其他请求重用托管处理程序，则返回 false。
            // 如果按请求保留某些状态信息，则通常这将为 false。
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest Request = HttpContext.Current.Request;
            HttpResponse Response = HttpContext.Current.Response;
            HttpServerUtility Server = HttpContext.Current.Server;

            Response.Charset = "UTF-8";
            string attachdir = "/Upload_Images";     // 上传文件保存路径，结尾不要带/
            int dirtype = 1;                 // 1:按天存入目录 2:按月存入目录 3:按扩展名存目录  建议使用按天存
            int maxattachsize = 5242880;     // 最大上传大小，默认是5M
            string upext = "txt,rar,zip,jpg,jpeg,gif,png,swf,wmv,avi,wma,mp3,mp4,mid,doc,docx,xls,xlsx,wps,mov";    // 上传扩展名
            int msgtype = 2;                 //返回上传参数的格式：1，只返回url，2，返回参数数组
            string immediate = Request.QueryString["immediate"];//立即上传模式，仅为演示用
            byte[] file;                     // 统一转换为byte数组处理
            string localname = "";
            string disposition = Request.ServerVariables["HTTP_CONTENT_DISPOSITION"];

            string err = "";
            string msg = "''";

            string uploadedfilepath="";

            if (disposition != null)
            {
                // HTML5上传
                file = Request.BinaryRead(Request.TotalBytes);
                localname = Regex.Match(disposition, "filename=\"(.+?)\"", RegexOptions.Compiled).Groups[1].Value;// 读取原始文件名
            }
            else
            {
                /*
                StreamReader str = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.UTF8);
                string content = str.ReadToEnd();
                str.Close();
                str.Dispose();
                debug.var_dump(content);
                */
                HttpFileCollection filecollection = Request.Files;
                HttpPostedFile postedfile = filecollection[0];
                // 读取原始文件名
                localname = postedfile.FileName;
                // 初始化byte长度.
                file = new Byte[postedfile.ContentLength];

                // 转换为byte类型
                System.IO.Stream stream = postedfile.InputStream;
                stream.Read(file, 0, postedfile.ContentLength);
                stream.Close();

                filecollection = null;
            }

            if (file.Length == 0) err = "请先选择要上传的文件";
            else
            {
                if (file.Length > maxattachsize) err = "文件大小超过" + maxattachsize + "字节";
                else
                {
                    string attach_dir, attach_subdir, filename, extension, target;

                    // 取上载文件后缀名
                    extension = GetFileExt(localname);
                    if (string.IsNullOrEmpty(extension))
                        extension = "jpg";
                    //if (("," + upext + ",").IndexOf("," + extension + ",") < 0) err = "上传文件扩展名必需为：" + upext;

                    if (("," + upext + ",").IndexOf("," + extension + ",") < 0) err = "格式 ." + extension+" 不支持上传";
                    else
                    {
                        switch (dirtype)
                        {
                            case 2:
                                attach_subdir = "month_" + DateTime.Now.ToString("yyMM");
                                break;
                            case 3:
                                attach_subdir = "ext_" + extension;
                                break;
                            default:
                                attach_subdir = "day_" + DateTime.Now.ToString("yyMMdd");
                                break;
                        }
                        attach_dir = attachdir + "/" + attach_subdir + "/";

                        // 生成随机文件名
                        Random random = new Random(DateTime.Now.Millisecond);
                        filename = DateTime.Now.ToString("yyyyMMddhhmmss") + random.Next(10000) + "." + extension;

                        target = attach_dir + filename;
                        try
                        {
                            CreateFolder(Server.MapPath(attach_dir));

                            System.IO.FileStream fs = new System.IO.FileStream(Server.MapPath(target), System.IO.FileMode.Create, System.IO.FileAccess.Write);
                            fs.Write(file, 0, file.Length);
                            fs.Flush();
                            fs.Close();
                        }
                        catch (Exception ex)
                        {
                            err = ex.Message.ToString();
                        }
                        uploadedfilepath=target;
                        // 立即模式判断
                        if (immediate == "1") target = "!" + target;
                        target = jsonString(target);
                        if (msgtype == 1) msg = "'" + target + "'";
                        else msg = "{\"url\":\"" + target + "\",\"localname\":\"" + jsonString(localname) + "\",\"id\":\"1\"}";
                    }
                }
            }

            file = null;

            Response.Write("{\"err\":\"" + jsonString(err) + "\",\"msg\":" + msg + ", \"error\":" + (string.IsNullOrEmpty(err) ? "0" : "-1") + ",\"url\":\"" + MyHandler.HOST + uploadedfilepath + "\",\"filename\":\"" + uploadedfilepath + "\"}");
        }
        #endregion

        string jsonString(string str)
        {
            str = str.Replace("\\", "\\\\");
            str = str.Replace("/", "\\/");
            str = str.Replace("'", "\\'");
            return str;
        }


        string GetFileExt(string FullPath)
        {
            if (FullPath != "") return FullPath.Substring(FullPath.LastIndexOf('.') + 1).ToLower();
            else return "";
        }

        void CreateFolder(string FolderPath)
        {
            if (!System.IO.Directory.Exists(FolderPath)) System.IO.Directory.CreateDirectory(FolderPath);
        }
    }
}
