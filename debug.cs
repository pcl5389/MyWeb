using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Web;

public class debug
{
    public static void var_dump(object obj)
    {
        StringBuilder sb = new StringBuilder();
        switch (obj.GetType().ToString())
        {
            case "System.Diagnostics.Process":
                sb.Append("<table border=1><tr>");
                Process ps = obj as Process;
                sb.Append("<tr><th>").Append("Handle").Append("</th><td>").Append(ps.Handle.ToString()).Append("</td></tr>");
                sb.Append("<tr><th>").Append("ID").Append("</th><td>").Append(ps.Id).Append("</td></tr>");
                sb.Append("<tr><th>").Append("MachineName").Append("</th><td>").Append(ps.MachineName).Append("</td></tr>");
                sb.Append("<tr><th>").Append("MainModule").Append("</th><td>").Append(ps.MainModule.ModuleName).Append("</td></tr>");
                sb.Append("<tr><th>").Append("ProcessorAffinity").Append("</th><td>").Append(ps.ProcessorAffinity.ToInt64().ToString()).Append("</td></tr>");

                sb.Append("</tr>");
                sb.AppendLine();
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                //HttpContext.Current.Response.End();
                break;
            case "System.Diagnostics.ProcessStartInfo":
                sb.Append("<table border=1><tr>");
                ProcessStartInfo p = obj as ProcessStartInfo;
                foreach (string key in p.EnvironmentVariables.Keys)
                {
                    sb.Append("<tr><th>").Append(key).Append("</th><td>").Append(p.EnvironmentVariables[key]).Append("</td></tr>");
                }
                foreach (string key in p.Verbs)
                {
                    sb.Append("<tr><th>").Append(key).Append("</th><td>").Append(key).Append("</td></tr>");
                }
                sb.Append("<tr><th>").Append("LoadUserProfile").Append("</th><td>").Append(p.LoadUserProfile.ToString()).Append("</td></tr>");
                sb.Append("<tr><th>").Append("WorkingDirectory").Append("</th><td>").Append(p.WorkingDirectory).Append("</td></tr>");
                sb.Append("<tr><th>").Append("UserName").Append("</th><td>").Append(p.UserName).Append("</td></tr>");
                sb.Append("<tr><th>").Append("Password").Append("</th><td>").Append(p.Password).Append("</td></tr>");
                sb.Append("<tr><th>").Append("Domain").Append("</th><td>").Append(p.Domain).Append("</td></tr>");
                sb.Append("<tr><th>").Append("FileName").Append("</th><td>").Append(p.FileName).Append("</td></tr>");
   
                sb.Append("</tr>");
                sb.AppendLine();
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                //HttpContext.Current.Response.End();
                break;
            case "System.Data.DataRow":
                DataRow dr1 = obj as DataRow;
                sb.Append("<table border=1><tr>");
                foreach (object item in dr1.ItemArray)
                {
                    sb.Append("<th>").Append(item.ToString()).Append("</th>");
                }
                sb.Append("</tr>");
                sb.AppendLine();
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                HttpContext.Current.Response.End();
                break;
            case "System.Collections.Hashtable":
                Hashtable htTable = obj as Hashtable;
                sb.Append("<table border=1>");
                foreach (string key in htTable.Keys)
                {
                    sb.Append("<tr><th>").Append(key).Append("</th><td>").Append(htTable[key]).Append("</td></tr>");
                }
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                HttpContext.Current.Response.End();
                break;
            case "System.Collections.Specialized.NameValueCollection":
                NameValueCollection nv = obj as NameValueCollection;
                sb.Append("<table border=1>");
                foreach (string key in nv.Keys)
                {
                    sb.Append("<tr><th>").Append(key).Append("</th><td>").Append(nv[key]).Append("</td></tr>");
                }
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                HttpContext.Current.Response.End();
                break;
            case "System.Data.DataTable":
                DataTable dt = obj as DataTable;

                sb.Append("<table border=1><tr>");
                foreach (DataColumn dc in dt.Columns)
                {
                    sb.Append("<th>").Append(dc.ColumnName).Append("</th>");
                }
                sb.Append("</tr>");
                sb.AppendLine();
                foreach (DataRow dr in dt.Rows)
                {
                       

                    sb.Append("<tr>");
                    for (int i = 0; i < dt.Columns.Count; i++)
                        sb.Append("<td>").Append(dr[i]).Append("</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
                HttpContext.Current.Response.Write(sb.ToString());
                HttpContext.Current.Response.End();
                break;
            default:
                HttpContext.Current.Response.Write(obj.GetType().ToString() + ":" + obj.ToString());
                HttpContext.Current.Response.End();
                break;
        }
    }

    public static void d(object obj)
    {
        StringBuilder sb = new StringBuilder();
        switch (obj.GetType().ToString())
        {
            case "System.Data.DataRow":
                DataRow dr1 = obj as DataRow;
                sb.Append("<table border=1><tr>");
                foreach (object item in dr1.ItemArray)
                {
                    sb.Append("<th>").Append(item.ToString()).Append("</th>");
                }
                sb.Append("</tr>");
                sb.AppendLine();
                sb.Append("</table>");
                throw new Exception(sb.ToString());
            case "System.Data.DataTable":
                DataTable dt = obj as DataTable;

                foreach (DataColumn dc in dt.Columns)
                {
                    sb.Append(dc.ColumnName).Append("\t").Append("|");
                }
                sb.AppendLine();
                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                        sb.Append(dr[i]).Append("\t").Append("|");
                    sb.AppendLine();
                }
                throw new Exception(sb.ToString());
            default:
                throw new Exception(obj.GetType().ToString() + ":" + obj.ToString());
        }
    }

    public static void log(string log)
    {

    }
}