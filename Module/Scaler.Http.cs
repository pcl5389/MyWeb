using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO.Compression;
using System.Threading;
using System.Collections.Generic;

namespace Scaler
{
    sealed class TcpConnection {
        public Uri URL { set; get; }
        public WebProxy Proxy { set; get; }
        public TcpClient Client { set; get; }
        public Stream HttpStream {set; get; }

        public int bUsing = 1;
        public TcpConnection() { }

    }

    public class Http
    {
        private static TcpConnection[] conns = new TcpConnection[999];
        private string _proxy = string.Empty;
        private string _proxy_port = string.Empty;
        private string _cert_path = string.Empty;
        private string _cert_pass = string.Empty;
        //private bool _debug = true;
        private bool _keep_alive = true;
        private bool _gzip = true;
        private bool _multi_part_post = false;
        private string _header = string.Empty;
        private Dictionary<string, string> _custom_header = new Dictionary<string, string>();

        public Dictionary<string, string> CustomHeader {
            get { return _custom_header; }
            set { _custom_header = value; }
        }
        public bool Gzip
        {
            get
            {
                return _gzip;
            }
            set
            {
                _gzip = value;
            }
        }
        public bool MultiPartPost {
            get
            {
                return _multi_part_post;
            }
            set
            {
                _multi_part_post = value;
            }
        }
        public bool KeepAlive {
            get
            {
                return _keep_alive;
            }
            set
            {
                _keep_alive = value;
            }
        }

        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
            }
        }

        public string Proxy
        {
            get
            {
                return _proxy;
            }
            set
            {
                _proxy = value;
            }
        }

        public string Cert
        {
            get
            {
                return _cert_path;
            }
            set
            {
                _cert_path = value;
            }
        }

        public string CertPass
        {
            get
            {
                return _cert_pass;
            }
            set
            {
                _cert_pass = value;
            }
        }

        public string ProxyPort
        {
            get
            {
                return _proxy_port;
            }
            set
            {
                _proxy_port = value;
            }
        }
        public Http(){
            System.Net.ServicePointManager.DefaultConnectionLimit = 9999;
        }
        /*
        public string GetWebHTML(string url, ref string cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.r .Headers.Set("Accept", "* /*");
            request.Headers.Add("Accept-Language", "zh-cn");
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; InfoPath.2; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
            //request.Headers.Add("Accept", "gzip, deflate, sdch");
            //Accept-Encoding: gzip, deflate, sdch
            //request.Headers.Add("Accept", "* /*");
            //string cookies = "AspxAutoDetectCookieSupport=1";
            //CookieContainer cookie = new CookieContainer();
            //request.CookieContainer = cookie;
            //cookie.SetCookies(new System.Uri(url), cookies);
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch (Exception e)
            {
                return e.Message;
            }
            Stream stream = response.GetResponseStream();
            cookie = response.Headers.Get("Set-Cookie").Replace("; path=/", "").Trim();
            cookie = Regex.Replace(cookie, "expires=.+?GMT,", "");
            //cookie = Regex.Match(cookie, "ASP.NET_SessionId=.+?;").Value;
            StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            string content = sr.ReadToEnd();
            stream.Close();
            sr.Close();
            response.Close();
            return content;
        }
    */
        public string GetHTML(string URL, string reffer, string mycookie, string data, string method)
        {
            return GetHTML_WithEncode(URL, reffer, mycookie, data, method, Encoding.UTF8, Encoding.UTF8);
        }

        bool ValidateServerCertificate(
                 object sender,
                 X509Certificate certificate,
                 X509Chain chain,
                 SslPolicyErrors sslPolicyErrors)
        {
            return true;
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
#if Debug
            throw new Exception(string.Format("Certificate error: {0}", sslPolicyErrors));
#endif
            return false;
        }

        private static int iCount = 0;
        private static object objLock = new object();

        public int Connect(Uri url, WebProxy proxy)
        {
            int iUse = -1;
            int iRequestCount = 0;

            
            if (iCount > 0)
            {
                for (int i = 0; i < iCount; i++)
                {
                    if (conns[i]!=null && conns[i].URL.Host.Equals(url.Host, StringComparison.OrdinalIgnoreCase) && conns[i].URL.Port.Equals(url.Port) && conns[i].Proxy == proxy)
                    {
                        int bAvalid = Interlocked.CompareExchange(ref conns[i].bUsing, 1, 0);
                        if (bAvalid.Equals(0))
                        {
                            if (KeepAlive && conns[i].Client.Client != null && conns[i].Client.Connected)
                            {
                                return i;
                            }
                            else
                            {
                                iUse = i;
                                break;
                            }
                        }
                    }
                }
            }
            if (iUse == -1)
            {
                if (iCount < conns.Length)
                {
                    iUse = Interlocked.Increment(ref iCount);
                    iUse = iUse - 1;
                    conns[iUse] = new TcpConnection();

                    conns[iUse].Proxy = proxy;
                    conns[iUse].URL = url;
                }
                else
                {
#if Debug
                    throw new Exception("超过最大连接数" + conns.Length.ToString());
#else
                    System.Threading.Thread.Sleep(3000);
                    return Connect(url, proxy);
#endif
                }
            }

            //开始连接
StartRequest:
            conns[iUse].Client = new TcpClient();
            NetworkStream stream = null;

            try
            {
                //int keepAlive = -1744830460;                                                        // SIO_KEEPALIVE_VALS 
                //byte[] inValue = new byte[] { 1, 0, 0, 0, 0x20, 0x4e, 0, 0, 0xd0, 0x07, 0, 0 };     // True, 20 秒, 2 秒 
                if (proxy != null)
                {
                    conns[iUse].Client.Connect(proxy.Address.Host, proxy.Address.Port);
                    if (url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) //ssl
                    {
                        stream = conns[iUse].Client.GetStream();
                        byte[] tunnelRequest = Encoding.UTF8.GetBytes(String.Format("CONNECT {0}:443  HTTP/1.1\r\nHost: {0}\r\n\r\n", proxy.Address.Host));
                        stream.Write(tunnelRequest, 0, tunnelRequest.Length);
                        stream.Flush();

                        byte[] buffer = new byte[1024];
                        int bytes = stream.Read(buffer, 0, buffer.Length);
#if Debug
                        Console.Write(Encoding.UTF8.GetString(buffer, 0, bytes));
#endif
                    }
                    else
                    {
                        stream = conns[iUse].Client.GetStream();
                    }
                }
                else
                {
                    conns[iUse].Client.Connect(url.Host, url.Port);
                    //if (_keep_alive)
                        //conns[iUse].Client.Client.IOControl(keepAlive, inValue, null);
                    stream = conns[iUse].Client.GetStream();
                }

                if (url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) //ssl
                {
                    SslStream ssl = new SslStream(stream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                    if (!string.IsNullOrEmpty(Cert))
                    {
                        string pfxPath = Cert; // Application.StartupPath + "\\Certs\\center01.pfx"; //center01.pfx
                        string pfxPassword = _cert_pass;
                        X509CertificateCollection cers = new X509CertificateCollection();

                        X509Certificate cer;
                        if (String.IsNullOrEmpty(pfxPassword)) //是否证书加载是否需要密码
                            cer = new X509Certificate(pfxPath);
                        else
                            cer = new X509Certificate(pfxPath, pfxPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                        cers.Add(cer);
                        ssl.AuthenticateAsClient(url.Host, cers, SslProtocols.Default, false);
                    }
                    else
                    {
                        ssl.AuthenticateAsClient(url.Host);
                    }
                    if (!ssl.IsAuthenticated)
                    {
                        throw new Exception("认证失败");
                    }
                    conns[iUse].HttpStream = ssl;
                }
                else
                {
                    conns[iUse].HttpStream = conns[iUse].Client.GetStream();
                }
                return iUse;
            }
            catch (Exception e)
            {
                if (iRequestCount > 3)
                {
                    throw new Exception("连接失败:" + e.Message.ToString());
                }
                else
                {
                    iRequestCount++;
                    System.Threading.Thread.Sleep(5000);
                    goto StartRequest;
                }
            }
        }

        public MemoryStream Request(string URL, string reffer, string mycookie, string data, string method, Encoding SendEncode, Encoding RecvEncode)
        {
            Uri URI = new Uri(URL);
            int iconn = 0, iRequest = 0, iSend = 0;

beginRequest:
            if (!string.IsNullOrEmpty(_proxy))
            {
                iconn = Connect(URI, new WebProxy(_proxy, int.Parse(_proxy_port)));
            }
            else
            {
                iconn = Connect(URI, null);
            }
            Console.WriteLine(iconn.ToString());
#if Debug
            Console.WriteLine(iconn.ToString());
#endif
            if (iconn == -1)
                return null;

            var ssl = conns[iconn].HttpStream;
            //生成header
            string requestHeader = null;
            if (_multi_part_post)
                requestHeader = BuildMultiPartPostHeader(URI, reffer, mycookie, data, method, SendEncode, RecvEncode);
            else
                requestHeader = BuildHeader(URI, reffer, mycookie, data, method, SendEncode, RecvEncode);
            //if(URL.StartsWith("https://oa.sdbcn.com/compass/j_acegi_security_check"))
            //    debug.var_dump(requestHeader);
            byte[] request = SendEncode.GetBytes(requestHeader);
beginsend:
            if (conns[iconn].Client.Connected)
            {
                byte[] buffer = new byte[2048];
                int count = 0;
#if Debug
                DateTime dt0;
#endif
                try
                {
#if Debug
                    Console.WriteLine("线程" + iconn.ToString() + "开始请求....");
                    dt0 = DateTime.Now;
#endif
                    ssl.Write(request, 0, request.Length);
                    ssl.Flush();
#if Debug
                    Console.WriteLine("线程" + iconn.ToString() + "请求结束....耗时：" + (DateTime.Now - dt0).TotalMilliseconds.ToString() + "毫秒");
                    Console.WriteLine("线程" + iconn.ToString() + "开始接收....");
                    dt0 = DateTime.Now;
#endif
                    count = ssl.Read(buffer, 0, buffer.Length);
#if Debug
                    Console.WriteLine("线程" + iconn.ToString() + "接受结束....耗时：" + (DateTime.Now - dt0).TotalMilliseconds.ToString() + "毫秒");
#endif
                }
                catch (Exception ee)
                {
                    iRequest++;
                    if (iRequest > 3)
                    {
                        throw new Exception("发送接收失败次数超过3次:" + ee.Message.ToString());
                    }
                    conns[iconn].Client.Close();
                    Interlocked.Exchange(ref conns[iconn].bUsing, 0);
                    //连接被中断
                    goto beginRequest;
                }
                //读取
                int weblen = 0;
                int iStart = 0;
                bool bChunked = false;
                bool bGzipEncode = false;

                MemoryStream ms = new MemoryStream();
                if (count == 0)
                {
                    if (iSend > 3)
                    {
#if Debug
                        Console.WriteLine("远程服务器无响应,重新连接");
#endif
                        conns[iconn].Client.Close();
                        Interlocked.Exchange(ref conns[iconn].bUsing, 0);
                        //连接被中断
                        goto beginRequest;
                    }
                    iSend++;
                    goto beginsend;
                }
                string header = RecvEncode.GetString(buffer, 0, count);
                int l = header.IndexOf("\r\n\r\n");
                int iRecved = 0;
                iStart = 0;

                while (header.StartsWith("HTTP"))
                {
                    l = header.IndexOf("\r\n\r\n");
                    if (l > -1)
                    {
                        iStart = l + 4;
                        _header = header.Substring(0, iStart - 4);
                        break;
                    }
                    iRecved = iRecved + count;
                    count = ssl.Read(buffer, 0, buffer.Length);
                    header = header + RecvEncode.GetString(buffer, 0, count);
                }
                ms.Write(buffer, iStart - iRecved, count - iStart + iRecved);

                if (Regex.Match(_header, "Transfer-Encoding:\\s*chunked", RegexOptions.IgnoreCase).Success)
                    bChunked = true;
                if (Regex.Match(_header, "Content-Encoding:\\s*gzip", RegexOptions.IgnoreCase).Success)
                    bGzipEncode = true;
                int rec = iRecved + count;
                if (header.StartsWith("HTTP/1.1 100 Continue"))
                {
                    ssl.Write(RecvEncode.GetBytes("\r\n"), 0, RecvEncode.GetBytes("\r\n").Length);
                    count = ssl.Read(buffer, 0, buffer.Length);
                    rec = count;
                    string tmp = RecvEncode.GetString(buffer, 0, count);
                    if (tmp.IndexOf("Content-Length:") > -1)
                    {
                        weblen = int.Parse(Regex.Match(tmp, "Content-Length: \\d*").Value.Replace("Content-Length:", "").Trim());
                        weblen = weblen + tmp.IndexOf("\r\n\r\n") + 4;
                    }
                    ms.Position = 0;
                    ms.Write(buffer, 0, count);
                }
                else
                {
                    if (header.IndexOf("Content-Length:") > -1 && header.IndexOf("\r\n\r\n") > -1)
                    {
                        string content_length = Regex.Match(header, "Content-Length: \\d*").Value.Replace("Content-Length:", "").Trim();
                        weblen = int.Parse(content_length);
                        weblen = weblen + header.IndexOf("\r\n\r\n") + 4;
                    }
                }
#if Debug
                dt0 = DateTime.Now;
                Console.WriteLine("线程" + iconn + "开始接收正文....");
#endif
                while (count > 0)
                {
#if Debug
                    Console.WriteLine("线程" + iconn + "正文接收" + count + "个字节");
#endif
                    if (weblen > 0 && rec == weblen)
                    {
                        break;
                    }
                    if (ssl.CanRead)
                    {
                        if (weblen > 0)
                        {
                            if ((weblen - rec) > buffer.Length)
                            {
                                count = ssl.Read(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                if (weblen - rec > 0)
                                {
                                    count = ssl.Read(buffer, 0, weblen - rec);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            rec = rec + count;
                            ms.Write(buffer, 0, count);
                        }
                        else
                        {
                            count = ssl.Read(buffer, 0, buffer.Length);
                            if (bChunked)
                            {
                                if (count == 5
                                    && buffer[0].Equals(48)
                                        && buffer[1].Equals(13)
                                        && buffer[2].Equals(10)
                                        && buffer[3].Equals(13)
                                        && buffer[4].Equals(10))
                                {
                                    break;
                                }
                                if (count >= 7
                                    && buffer[count - 7].Equals(13)
                                        && buffer[count - 6].Equals(10)
                                        && buffer[count - 5].Equals(48)
                                        && buffer[count - 4].Equals(13)
                                        && buffer[count - 3].Equals(10)
                                        && buffer[count - 2].Equals(13)
                                        && buffer[count - 1].Equals(10))
                                {
                                    ms.Write(buffer, 0, count - 7);
                                    break;
                                }
                            }
                            ms.Write(buffer, 0, count);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
#if Debug
                Console.WriteLine("线程" + iconn.ToString() + "正文接受结束....耗时：" + (DateTime.Now - dt0).TotalMilliseconds.ToString() + "毫秒");
#endif
                if (!_keep_alive)
                {
                    ssl.Close();
                    conns[iconn].Client.Close();
                }
                Interlocked.Exchange(ref conns[iconn].bUsing, 0);
#if DebugZip
                FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/debug.gzip", FileMode.OpenOrCreate);
                BinaryWriter w = new BinaryWriter(fs);
                w.Write(ms.ToArray());
                w.Close();
                fs.Close();
#endif
                MemoryStream ms2 = new MemoryStream();
                if (bChunked)
                {
                    int iCur = 0;
                    DealMemoryStream(ref ms, ref ms2, iCur);
#if DebugZip
                    FileStream fs2 = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/debug2.gzip", FileMode.OpenOrCreate);
                    BinaryWriter w2 = new BinaryWriter(fs2);
                    w2.Write(ms2.ToArray());
                    w2.Close();
                    fs2.Close();
#endif
                }
                else if (bGzipEncode)
                {
                    ms.Position = 0;
                    while (ms.CanRead)
                    {
                        int itmp = ms.Read(buffer, 0, buffer.Length);
                        if (itmp > 0)
                            ms2.Write(buffer, 0, itmp);
                        else
                            break;
                    }
                }

                if (bGzipEncode)
                {
#if Debug
                    dt0 = DateTime.Now;
#endif
                    ms2.Position = 0;
                    using (GZipStream zipStream = new GZipStream(ms2, CompressionMode.Decompress))
                    {
                        ms = new MemoryStream();
                        while (zipStream.CanRead)
                        {
                            int itmp = zipStream.Read(buffer, 0, buffer.Length);
                            if (itmp > 0)
                                ms.Write(buffer, 0, itmp);
                            else
                                break;
                        }
                        zipStream.Close();
                    }
#if Debug
                    Console.WriteLine("解压缩耗时：" + (DateTime.Now - dt0).TotalMilliseconds.ToString() + "毫秒");
#endif
                }
                else if (bChunked) //
                {
                    ms = new MemoryStream();
                    ms2.Position = 0;
                    while (ms2.CanRead)
                    {
                        int itmp = ms2.Read(buffer, 0, buffer.Length);
                        if (itmp > 0)
                            ms.Write(buffer, 0, itmp);
                        else
                            break;
                    }
                }
                return ms;
            }
            else
            {
                iRequest++;
                if (iRequest > 5)
                {
                    throw new Exception("网络连接失败");
                }
                Interlocked.Exchange(ref conns[iconn].bUsing, 0);

                System.Threading.Thread.Sleep(3000);
                goto beginRequest;
            }
        }
        public string GetHTML_WithEncode(string URL, string reffer, string mycookie, string data, string method, Encoding SendEncode, Encoding RecvEncode)
        {
            _multi_part_post = false;
            MemoryStream ms = Request(URL, reffer, mycookie, data, method, SendEncode, RecvEncode);
            if (ms != null)
            {

                Match m = Regex.Match(_header, "Content-Type:.+?\r\n");
                if (m.Success)
                {
                    m = Regex.Match(m.Value.ToLower(), "charset=.+?(\\s|;|\r\n)");
                    if (m.Success)
                    {
                        string encoding = m.Value.Substring(8).Replace(";", "").Trim();
                        switch (encoding)
                        {
                            case "utf-8":
                                if (RecvEncode != Encoding.UTF8)
                                    RecvEncode = Encoding.UTF8;
                                break;
                            case "gb2312":
                                if (RecvEncode != Encoding.Default)
                                    RecvEncode = Encoding.Default;
                                break;
                            case "gb18030":
                                if (RecvEncode != Encoding.Default)
                                    RecvEncode = Encoding.Default;
                                break;
                        }
                    }
                }

                ms.Position = 0;
                StreamReader srr = new StreamReader(ms, RecvEncode);
                return srr.ReadToEnd();
            }
            return String.Empty;
        }
        public void DealMemoryStream(ref MemoryStream ms, ref MemoryStream ms2, int iStart)
        {
            if (iStart >= ms.Length)
                return;

            int iLoc = 0;
            char charLen;
            ms.Position = iStart;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                charLen = (char)ms.ReadByte();
                if (charLen.Equals('\r') && ms.ReadByte().Equals(10))
                {
                    iLoc = i + 2;
                    break;
                }
                sb.Append(charLen);
            }
            if (iLoc == 0)
                throw new Exception("chunked编码错误");
            int iLen = Convert.ToInt32(sb.ToString(), 16);
            iStart = iStart + iLoc;

            if (iLen > 0)
            {
                //复制内容
                byte[] buffer = new byte[iLen];
                ms.Position = iStart;
                int count = ms.Read(buffer, 0, iLen);
                ms2.Write(buffer, 0, count);
                iStart = iStart + iLen;
                DealMemoryStream(ref ms, ref ms2, iStart + 2);
            }
        }
        public string BuildHeader(Uri URI, string reffer, string cookie, string data, string method, Encoding sendEncode, Encoding recvEncode)
        {
            StringBuilder RequestHeaders = new StringBuilder();//用来保存HTML协议头部信息
            //RequestHeaders.AppendLine(string.Format("{0} {1} HTTP/1.1", method/*此处可填写GET或POST*/, URI.PathAndQuery));
            RequestHeaders.AppendLine(string.Format("{0} {1} HTTP/1.1", method, string.IsNullOrEmpty(_proxy) ? URI.PathAndQuery : URI.AbsoluteUri));
            //
            //Accept-Encoding: gzip, deflate, sdch
            RequestHeaders.AppendLine("Accept: text/html, application/xhtml+xml, */*");
            if (_gzip)
            {
                RequestHeaders.AppendLine("Accept-Encoding: gzip");
            }
            if (reffer.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Referer: {0}", reffer));
            }
            RequestHeaders.AppendLine("Accept-Language: zh-Hans-CN,zh-Hans;q=0.5");
            RequestHeaders.AppendLine("User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/38.0.2125.104 Safari/537.36");
            if (_custom_header.Count > 0)
            {
                foreach (string key in _custom_header.Keys)
                {
                    RequestHeaders.AppendLine(string.Format("{0}: {1}", key, _custom_header[key]));
                }
            }
            
            //User-Agent: Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko

            if (data.Length > 0)
            {
                RequestHeaders.AppendLine("Content-Type: application/x-www-form-urlencoded");
            }
            if (_keep_alive) //Connection: keep-alive
            {
                RequestHeaders.AppendLine("Connection: keep-alive");
            }
            else
            {
                RequestHeaders.AppendLine("Connection: close");
            }

            if (data.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Content-Length: {0}", sendEncode.GetBytes(data).Length));
            }
            //RequestHeaders.AppendLine("DNT: 1");
            RequestHeaders.AppendLine(string.Format("Host: {0}", URI.Host));
            if (cookie.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Cookie: {0}", cookie));
            }

            RequestHeaders.Append("\r\n");
            if (data.Length > 0)
            {
                RequestHeaders.AppendLine(data);
            }
            RequestHeaders.Append("\r\n");
            return RequestHeaders.ToString();
        }
        public string BuildMultiPartPostHeader(Uri URI, string reffer, string cookie, string data, string method, Encoding sendEncode, Encoding recvEncode)
        {
            int datalength = sendEncode.GetBytes(data).Length;
            StringBuilder RequestHeaders = new StringBuilder();//用来保存HTML协议头部信息
            RequestHeaders.AppendLine(string.Format("{0} {1} HTTP/1.1", method/*此处可填写GET或POST*/, string.IsNullOrEmpty(_proxy) ? URI.PathAndQuery : URI.AbsoluteUri));
            RequestHeaders.AppendLine(string.Format("Host: {0}", URI.Host));
            RequestHeaders.AppendLine("Accept: text/html, application/xhtml+xml, */*");
            if (_gzip)
            {
                RequestHeaders.AppendLine("Accept-Encoding: gzip");
            }
            if (reffer.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Referer: {0}", reffer));
            }
            RequestHeaders.AppendLine("Accept-Language: zh-Hans-CN,zh-Hans;q=0.5");
            if (data.Length > 0)
            {
                RequestHeaders.AppendLine("Content-Type: multipart/form-data; boundary=---------------------------7db1861030472");
            }
            RequestHeaders.AppendLine("User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/38.0.2125.104 Safari/537.36");

            if (_keep_alive)
            {
                RequestHeaders.AppendLine("Connection: keep-alive"); //Keep-Alive | Close
            }
            else
            {
                RequestHeaders.AppendLine("Connection: close"); //Keep-Alive | Close
            }

            if (data.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Content-Length: {0}", datalength));
            }
            if (_custom_header.Count > 0)
            {
                foreach (string key in _custom_header.Keys)
                {
                    RequestHeaders.AppendLine(string.Format("{0}: {1}", key, _custom_header[key]));
                }
            }
            //RequestHeaders.AppendLine("Cache-Control: no-cache");
            if (cookie.Length > 0)
            {
                RequestHeaders.AppendLine(string.Format("Cookie: {0}", cookie));
            }
            RequestHeaders.AppendLine("\r\n");
            RequestHeaders.AppendLine(data);
            return RequestHeaders.ToString();
        }

        public string PostData_MultiPart(string URL, string reffer, string mycookie, string data, string method, Encoding SendEncode, Encoding RecvEncode)
        {
            _multi_part_post = true;
            MemoryStream ms = Request(URL, reffer, mycookie, data, method, SendEncode, RecvEncode);
            if (ms != null)
            {
                ms.Position = 0;
                StreamReader srr = new StreamReader(ms, RecvEncode);
                return srr.ReadToEnd();
            }
            return String.Empty;
        }

        public void GetCookies(string content, ref string cookies)
        {
            MatchCollection mc = Regex.Matches(content, "Set-Cookie:.*");
            if (mc != null && mc.Count > 0)
            {
                foreach (Match m in mc)
                {
                    string tmp = m.Value.Trim().Replace("Set-Cookie:", "").Trim();
                    tmp = Regex.Replace(tmp, "; expires=.+?;", ";");
                    tmp = tmp.Replace("; HttpOnly", "").Replace("; path=/", "").Trim();
                    if (cookies.Length == 0)
                        cookies = tmp;
                    else
                    {
                        if (cookies.Substring(cookies.Length - 1, 1) == ";")
                        {
                            cookies = cookies + " " + tmp;
                        }
                        else
                        {
                            cookies = cookies + "; " + tmp;
                        }
                    }
                }
                if (cookies.Length > 1 && cookies.Substring(cookies.Length - 1, 1) == ";")
                {
                    cookies = cookies.Remove(cookies.Length - 1);
                }
            }
        }

        public static Dictionary<string, string> HiddenFields(string content)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            MatchCollection mc = Regex.Matches(content, "<input\\s+type=('|\")?hidden('|\")?.+?>", RegexOptions.IgnoreCase);
            foreach (Match m in mc)
            {
                string name = Regex.Match(m.Value, "name=('|\")?.+?('|\"|\\s)").Value.Replace("name=", "").Replace("\"", "").Replace("'", "").Trim();
                string value = Regex.Match(m.Value, "value=('|\")?.+?('|\"|\\s)").Value.Replace("value=", "").Replace("\"", "").Replace("'", "").Trim();
                fields[name] = value;
            }
            return fields;
        }

        public static string GetHidenFields(string content)
        {
            Dictionary<string, string> fields = HiddenFields(content);
            StringBuilder sb = new StringBuilder();
            if (fields.Count > 0)
            {
                foreach (string key in fields.Keys)
                {
                    if (sb.Length > 0)
                        sb.Append("&");
                    sb.Append(key).Append("=").Append(System.Web.HttpUtility.UrlEncode(fields[key], Encoding.Default));
                }
            }
            return sb.ToString();
        }

        public static string GetCookies(string cookie, string content)
        {
            MatchCollection mc = Regex.Matches(content, "Set-Cookie:.*");
            foreach (Match m in mc)
            {
                string expire = Regex.Match(m.Value, "Expires=.+?GMT", RegexOptions.IgnoreCase).Value.Replace("Expires=", "");
                if (!string.IsNullOrEmpty(expire))
                {
                    DateTime dt = DateTime.Now;
                    if (DateTime.TryParse(expire, out dt))
                    {
                        if (dt < DateTime.Now)
                        {
                            continue;
                        }
                    }
                }
                string tmp = m.Value.Replace("Set-Cookie:", "").Trim();
                if (cookie.Length > 0)
                {
                    cookie = cookie + "; " + tmp;
                }
                else
                    cookie = tmp;
            }
            return DelDoubleCookie(cookie);
        }

        public static string DelDoubleCookie(string cookie)
        {
            string[] cookies = cookie.Split(';');
            Hashtable ht = new Hashtable();
            foreach (string cook in cookies)
            {
                int l = cook.IndexOf('=');
                if (l > -1)
                {
                    string[] item = { cook.Substring(0, l), cook.Substring(l + 1) };
                    if (ht.Contains(item[0].Trim()))
                    {
                        ht[item[0].Trim()] = item[1].Trim();
                    }
                    else
                    {
                        ht.Add(item[0].Trim(), item[1].Trim());
                    }
                }

            }
            ht.Remove("path");
            ht.Remove("Path");
            ht.Remove("expires");
            ht.Remove("Expires");
            ht.Remove("domain");
            ht.Remove("Domain");



            StringBuilder sb = new StringBuilder();
            foreach (string key in ht.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("; ");
                }
                sb.Append(key).Append("=").Append(ht[key]);
            }
            return sb.ToString();
        }

        public static string HtmlToUBB(string _Html)
        {
            _Html = Regex.Replace(_Html, "<br[^>]*>", "\n\r");
            _Html = Regex.Replace(_Html, @"<p[^>\/]*\/>", "\n\r");
            _Html = Regex.Replace(_Html, @"<p[^>\/]*>", "\n\r");
            _Html = Regex.Replace(_Html, "\\son[\\w]{3,16}\\s?=\\s*([\'\"]).+?\\1", "");
            _Html = Regex.Replace(_Html, "<hr[^>]*>", "[hr]");

            _Html = Regex.Replace(_Html, "<(\\/)?blockquote([^>]*)>", "[$1blockquote]");
            _Html = Regex.Replace(_Html, "<img[^>]*smile=\"(\\d+)\"[^>]*>", "'[s:$1]");
            _Html = Regex.Replace(_Html, "<img[^>]*src=[\'\"\\s]*([^\\s\'\"]+)[^>]*>", "[IMG]$1[/IMG]");
            _Html = Regex.Replace(_Html, "<a[^>]*href=[\'\"\\s]*([^\\s\'\"]*)[^>]*>(.+?)<\\/a>", "[url=$1]$2[/url]");
            _Html = Regex.Replace(_Html, "<b>(.+?)</b>", @"\[b\]$1\[/b\]");
            _Html = Regex.Replace(_Html, "<[^>]*?>", "");
            _Html = Regex.Replace(_Html, "&amp;", "&");
            _Html = Regex.Replace(_Html, "&nbsp;", " ");
            _Html = Regex.Replace(_Html, "&lt;", "<");
            _Html = Regex.Replace(_Html, "&gt;", ">");

            return _Html;
        }
    }
}
