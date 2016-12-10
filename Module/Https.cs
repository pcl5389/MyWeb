using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;

namespace MyWeb.Module
{
    public class Https
    {
        public string POST(string url, string reffer, ref CookieCollection cookies, string data)
        {
            try
            {
                HttpWebRequest webReq = (HttpWebRequest)HttpWebRequest.Create(url);
                webReq.Accept = "*/*";
                webReq.Headers["AcceptLanguage"] = "zh-Hans-CN,zh-Hans;q=0.5";
                webReq.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.2; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)";
                webReq.KeepAlive = false;
                webReq.AllowAutoRedirect = false;

                Encoding encoding = Encoding.UTF8;
                //encoding.GetBytes(postData);

                byte[] bs = Encoding.UTF8.GetBytes(data);

                string responseData = String.Empty;
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.CookieContainer = new CookieContainer();
                if (cookies != null && cookies.Count > 0)
                {
                    webReq.CookieContainer.Add(cookies);
                }

                webReq.ContentLength = bs.Length;
                using (Stream reqStream = webReq.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                    reqStream.Close();
                }

                HttpWebResponse response = webReq.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.OK)
                {
                    string domain = string.Empty;
                    if (response.Cookies.Count > 0)
                    {
                        domain = response.Cookies[0].Domain;
                    }



                    //cookies.Add(response.Cookies);

                    if (response.StatusCode == HttpStatusCode.Found && response.Headers[HttpResponseHeader.Location] != null)
                    {
                        url = "jump:" + response.Headers[HttpResponseHeader.Location].Trim();
                        response.Close();
                        webReq.Abort();
                        return url;
                    }

                    Stream stream = response.GetResponseStream();
                    MemoryStream ms = new MemoryStream();
                    byte[] buff = new byte[1024];
                    int count = stream.Read(buff, 0, buff.Length);
                    StringBuilder sb = new StringBuilder();
                    while (count > 0)
                    {
                        ms.Write(buff, 0, count);
                        if (stream.CanRead)
                        {
                            try
                            {
                                count = stream.Read(buff, 0, buff.Length);
                            }
                            catch
                            {
                                count = 0;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                    }
                    response.Close();

                    StreamReader srr = new StreamReader(ms, Encoding.UTF8);
                    ms.Position = 0;
                    string content = srr.ReadToEnd();
                    return content;
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }

        }

        public static string DelDoubleCookie(string cookie)
        {
            if (string.IsNullOrEmpty(cookie))
            {
                return "";
            }
            string[] cookies = cookie.Split(';');
            Hashtable ht = new Hashtable();
            foreach (string cook in cookies)
            {
                int l = cook.IndexOf('=');
                if (l == -1)
                    continue;
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
            ht.Remove("path");
            ht.Remove("Path");
            ht.Remove("expires");
            ht.Remove("Version");
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

        public string GET(string url, ref  CookieCollection cookies)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return "";
                System.Net.HttpWebRequest webReq = System.Net.HttpWebRequest.Create(url) as HttpWebRequest;
                webReq.Accept = "*/*";
                webReq.Headers["AcceptLanguage"] = "zh-Hans-CN,zh-Hans;q=0.5";
                webReq.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.2; WOW64; Trident/7.0; .NET4.0E; .NET4.0C; .NET CLR 3.5.30729; .NET CLR 2.0.50727; .NET CLR 3.0.30729)";

                webReq.KeepAlive = false;
                webReq.AllowAutoRedirect = false;
                webReq.Method = "GET";
                webReq.CookieContainer = new CookieContainer();
                if (cookies.Count > 0)
                    webReq.CookieContainer.Add(cookies);

                string responseData = String.Empty;
                HttpWebResponse response = webReq.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.OK)
                {
                    cookies.Add(response.Cookies);

                    if (response.StatusCode == HttpStatusCode.Found && response.Headers[HttpResponseHeader.Location] != null)
                    {
                        url = "jump:" + response.Headers[HttpResponseHeader.Location].Trim();
                        response.Close();
                        webReq.Abort();
                        return url;
                    }

                    Stream stream = response.GetResponseStream();
                    MemoryStream ms = new MemoryStream();
                    byte[] buff = new byte[1024];
                    int count = stream.Read(buff, 0, buff.Length);
                    StringBuilder sb = new StringBuilder();
                    while (count > 0)
                    {
                        ms.Write(buff, 0, count);
                        if (stream.CanRead)
                        {
                            try
                            {
                                count = stream.Read(buff, 0, buff.Length);
                            }
                            catch
                            {
                                count = 0;
                            }
                        }
                        else
                        {
                            count = 0;
                        }
                    }
                    response.Close();

                    StreamReader srr = new StreamReader(ms, Encoding.UTF8);
                    ms.Position = 0;
                    string content = srr.ReadToEnd();
                    return content;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}