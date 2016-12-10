using System.Text;

namespace MyWeb.Module
{
    public class Pager
    {
        public static string GetString2(string url, int recordnum, int pagesize, int currentpage)
        {
            string url2 = "";
            int l = url.IndexOf("#");

            if (l > -1)
            {
                if (l == 0)
                {
                    url2 = url;
                    url = "";
                }
                else
                {
                    url2 = url.Substring(l);
                    url = url.Substring(0, l);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"AntPager\">");
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            sb.Append("<span>").Append(currentpage).Append("/").Append(pager).Append("</span>");
            if (currentpage > 1)
            {
                sb.Append("<a class=\"btn_prev btn\" href=\"").Append(url + "&page=" + (currentpage - 1).ToString() + url2).Append("\">上一页</a>");
            }
            else
            {
                sb.Append("<a class=\"btn_prev_disable btn\" href=\"javascript:void(0);\">上一页</a>");
            }

            if (currentpage < pager)
            {
                sb.Append("<a class=\"btn_next btn\" href=\"").Append(url + "&page=" + (currentpage + 1).ToString() + url2).Append("\">下一页</a>");
            }
            else
            {
                sb.Append("<a class=\"btn_next_disable btn\" href=\"javascript:void(0);\">下一页</a>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        public static string GetString3(string url, int recordnum, int pagesize, int currentpage)
        {
            string url2 = "";
            int l = url.IndexOf("#");

            if (l > -1)
            {
                if (l == 0)
                {
                    url2 = url;
                    url = "";
                }
                else
                {
                    url2 = url.Substring(l);
                    url = url.Substring(0, l);
                }
            }
            StringBuilder sb = new StringBuilder();
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            sb.Append("<div class=\"pagination\"><ul>");
            if (currentpage < 2)
                sb.Append("<li><a href=\"javascript:void(0)\"> 上一页</a> </li>");
            else
                sb.Append("<li><a href=\"" + url + "&page=" + (currentpage - 1).ToString() + url2 + "\"> 上一页 </a></li>");

            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            int p_start = 0;
            int p_end = 0;

            p_start = (currentpage / 10) * 10;
            p_end = p_start + 10;
            p_start = p_start < 1 ? 1 : p_start;
            p_end = p_end > pager ? pager : p_end;

            if (p_start > 1)
            {
                sb.Append("<li><a href=\"" + url + "&page=1" + url2 + "\">1</a></li>");
            }

            for (int i = p_start; i <= p_end; i++)
            {
                if (i != currentpage)
                    sb.Append("<li><a href=\"" + url + "&page=" + i.ToString() + url2 + "\">" + i.ToString() + "</a></li>");
                else
                    sb.Append("<li><a href=\"javascript:void(0)\"> " + i.ToString() + "</a></li>");
            }

            if (p_end < pager)
            {
                sb.Append("<li><a href=\"" + url + "&page=" + pager.ToString() + url2 + "\">" + pager.ToString() + "</a></li>");
            }

            if (currentpage < pager)
            {
                sb.Append("<li><a href=\"" + url + "&page=" + (currentpage + 1).ToString() + url2 + "\"> 下一页 </a></li>");
            }
            else
            {
                sb.Append("<li><a href=\"javascript:void(0)\">  下一页 </a></li>");
            }
            sb.Append("</ul></div>");
            return sb.ToString();
        }

        public static string GetString4(string url, int recordnum, int pagesize, int currentpage)
        {
            string url2 = "";
            int l = url.IndexOf("#");

            if (l > -1)
            {
                if (l == 0)
                {
                    url2 = url;
                    url = "";
                }
                else
                {
                    url2 = url.Substring(l);
                    url = url.Substring(0, l);
                }
            }
            StringBuilder sb = new StringBuilder();
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            sb.Append("<ul class=\"pagination\">");
            if (currentpage < 2)
                sb.Append("<li><a href=\"javascript:void(0)\"> 上一页</a> </li>");
            else
                sb.Append("<li><a href=\"" + url + "&page=" + (currentpage - 1).ToString() + url2 + "\"> 上一页 </a></li>");

            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            int p_start = 0;
            int p_end = 0;

            p_start = (currentpage / 10) * 10;
            p_end = p_start + 10;
            p_start = p_start < 1 ? 1 : p_start;
            p_end = p_end > pager ? pager : p_end;

            if (p_start > 1)
            {
                sb.Append("<li><a href=\"" + url + "&page=1" + url2 + "\">1</a></li>");
            }

            for (int i = p_start; i <= p_end; i++)
            {
                if (i != currentpage)
                    sb.Append("<li><a href=\"" + url + "&page=" + i.ToString() + url2 + "\">" + i.ToString() + "</a></li>");
                else
                    sb.Append("<li><a href=\"javascript:void(0)\"> " + i.ToString() + "</a></li>");
            }

            if (p_end < pager)
            {
                sb.Append("<li><a href=\"" + url + "&page=" + pager.ToString() + url2 + "\">" + pager.ToString() + "</a></li>");
            }

            if (currentpage < pager)
            {
                sb.Append("<li><a href=\"" + url + "&page=" + (currentpage + 1).ToString() + url2 + "\"> 下一页 </a></li>");
            }
            else
            {
                sb.Append("<li><a href=\"javascript:void(0)\">  下一页 </a></li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        public static string GetString(string url, int recordnum, int pagesize, int currentpage)
        {
            string url2 = "";
            int l = url.IndexOf("#");

            if (l > -1)
            {
                if (l == 0)
                {
                    url2 = url;
                    url = "";
                }
                else
                {
                    url2 = url.Substring(l);
                    url = url.Substring(0, l);
                }
            }
            //<span class="disabled"> < </span><span class="current">1</span><a href="#">2</a><a href="#">3</a><a href="#">4</a><a href="#">5</a><a href="#">6</a><a href="#">7</a>...<a href="#">199</a><a href="#">200</a><a href="#"> > </a></div>
            StringBuilder sb = new StringBuilder();
            url = url.IndexOf('?') > -1 ? url : (url + "?show=1");
            sb.Append("<div class=\"sabrosus\">");
            if (currentpage < 2)
                sb.Append("<span class=\"disabled\"> 上一页 </span>");
            else
                sb.Append("<a href=\"" + url + "&page=" + (currentpage - 1).ToString() + url2 + "\"> 上一页 </a>");

            int pager = recordnum % pagesize == 0 ? recordnum / pagesize : ((recordnum / pagesize) + 1);

            int p_start = 0;
            int p_end = 0;

            p_start = (currentpage / 10) * 10;
            p_end = p_start + 10;
            p_start = p_start < 1 ? 1 : p_start;
            p_end = p_end > pager ? pager : p_end;

            if (p_start > 1)
            {
                sb.Append("<a href=\"" + url + "&page=1" + url2 + "\">1</a>...");
            }

            for (int i = p_start; i <= p_end; i++)
            {
                if (i != currentpage)
                    sb.Append("<a href=\"" + url + "&page=" + i.ToString() + url2 + "\">" + i.ToString() + "</a>");
                else
                    sb.Append("<span class=\"current\">" + i.ToString() + "</span>");
            }

            if (p_end < pager)
            {
                sb.Append("...<a href=\"" + url + "&page=" + pager.ToString() + url2 + "\">" + pager.ToString() + "</a>");
            }

            if (currentpage < pager)
            {
                sb.Append("<a href=\"" + url + "&page=" + (currentpage + 1).ToString() + url2 + "\">▶︎</a>");
            }
            else
            {
                sb.Append("<span class=\"disabled\">▶︎</a>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
