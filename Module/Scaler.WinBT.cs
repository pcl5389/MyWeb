using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace Scaler.WinBT
{
    public class Win
    {
        static object objWriter = new object();
        public static void WriteLog(string strs)
        {
            lock(objWriter)
            { 
                var sw = new StreamWriter(HttpRuntime.AppDomainAppPath + "Log.txt", true);
                sw.Write(DateTime.Now + "," + strs);
                sw.WriteLine();
                sw.Close();
                sw.Dispose();
            }
        }
    }

    public class MyConnection
    {
        public SqlConnection Conn = null;
        public int bUsing = 1;
    }

    public abstract class DataBase
    {
        private const int connMax = 100;
        public static bool init;
        public static MyConnection[] Conns = new MyConnection[connMax];
        public static int intconn = 0;
        private static readonly bool bDebug = ConfigurationManager.AppSettings["Debug"] == null ? false : bool.Parse(ConfigurationManager.AppSettings["Debug"]);

        private static readonly string connstring = ConfigurationManager.ConnectionStrings["winbt"].ConnectionString;
        private static readonly object dblock = new object();

        static DataBase() { }

        public static int GetConn()
        {
            int iUse = -1;

            if (intconn > 0)
            {
                for (int i = 0; i < intconn; i++)
                {
                    if (Conns[i] == null)
                        continue;
                    int iAvalid = Interlocked.CompareExchange(ref Conns[i].bUsing, 1, 0);
                    if (iAvalid.Equals(0))
                    {
                        switch (Conns[i].Conn.State)
                        {
                            case ConnectionState.Closed:
                                Conns[i].Conn.Open();
                                return i;
                            case ConnectionState.Open:
                                return i;
                            default:
                                Conns[i].Conn.Dispose();
                                Conns[i].Conn = new SqlConnection(connstring);
                                Conns[i].Conn.Open();
                                return i;
                        }
                    }
                }
            }
            if (intconn >= connMax)
            {
                Thread.Sleep(3000);
                return GetConn();
            }
            iUse = Interlocked.Increment(ref intconn);
            iUse = iUse - 1;
            Conns[iUse] = new MyConnection();
            Conns[iUse].Conn = new SqlConnection(connstring);
            Conns[iUse].Conn.Open();
            return iUse;
        }


        public static int ExecuteSql(string sql)
        {
            if(bDebug)
                Win.WriteLog(sql);
            int ret = 0;
            int ii = GetConn();
            MyConnection conn = Conns[ii];
            var cmd = new SqlCommand(sql, conn.Conn);

            try
            {
                ret = cmd.ExecuteNonQuery();
                return ret;
            }
            catch (Exception e)
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.Cookies["LastErr"].Value = MyWeb.Common.StrEncode(e.Message.ToString());
                }
                Win.WriteLog(e.Message + "," + sql);
                return ret;
            }
            finally
            {
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
                cmd.Dispose();
            }
        }

        public static string getLastErr()
        {
            if (HttpContext.Current.Request.Cookies["LastErr"] != null)
            {
                if (HttpContext.Current != null)
                {
                    string err = HttpContext.Current.Request.Cookies["LastErr"].Value;
                    HttpContext.Current.Response.Cookies["LastErr"].Expires = DateTime.Now.AddDays(-1);
                    if (!string.IsNullOrEmpty(err))
                    {
                        return err;
                    }
                }
            }
            return "";
        }

        public static ArrayList ExecuteSP(SqlParameter[] paras, string spName)
        {
            ArrayList al = new ArrayList();
            int ii = GetConn();
            MyConnection conn = Conns[ii];

            var cmd = new SqlCommand(spName, conn.Conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;

            cmd.Parameters.AddRange(paras);
            SqlDataReader sr = null;

            try
            {
                sr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                DataTable dt = new DataTable();
                dt.Load(sr);
                sr.Close();
                foreach (SqlParameter sp in paras)
                {
                    if (sp.Direction == ParameterDirection.InputOutput || sp.Direction == ParameterDirection.Output)
                    {
                        al.Add(sp.Value);
                    }
                }
                al.Add(dt);
                return al;
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message + ";" + spName);
                return null;
            }
            finally
            {
                if (sr != null && !sr.IsClosed)
                {
                    sr.Close();
                }
                if (conn.Conn.State != ConnectionState.Open)
                {
                    conn.Conn.Open();
                }
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
            }
        }

        public static DataTable ShowState()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("state", typeof(string));
            dt.Columns.Add("using", typeof(string));

            for (int i = 0; i < intconn; i++)
            {
                DataRow dr = dt.NewRow();
                dr["id"] = i;
                dr["state"] = Conns[i].Conn.State.ToString();
                dr["using"] = Conns[i].bUsing.ToString();
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public static DataTable GetRecords(string sql, int pagesize, int page)
        {
            if (bDebug)
                Win.WriteLog(sql);
            var ret = new DataSet();
            int ii = GetConn();
            MyConnection conn = Conns[ii];

            var cmd = new SqlCommand(sql, conn.Conn);
            var adapter = new SqlDataAdapter(cmd);
            try
            {
                if (pagesize == 0)
                    adapter.Fill(ret, "tb");
                else
                    adapter.Fill(ret, pagesize*page, pagesize, "tb");
                return ret.Tables["tb"];
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message + "," + sql);
                return null;
            }
            finally
            {
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
                cmd.Dispose();
                adapter.Dispose();
            }
        }

        public static string space_repeat(int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private static string del_repeat_space(string src)
        {
            while (src.IndexOf("  ") > -1)
            {
                src = src.Replace("  ", " ");
            }
            return src;
        }

        public static DataTable GetNewRecords(string sql, string pk, int pagesize, int page)
        {
            sql = sql.ToLower().Trim().Replace("\r", "").Replace("\n", " ");
            Match m = Regex.Match(sql, "^select (distinct| )*top\\s+\\d+\\s+");
            if (!m.Success)
            {
                if (pagesize > 0)
                {
                    if (page == 0)
                    {
                        sql = sql.Insert(7, "top " + (pagesize * (page + 1)).ToString() + " ");
                    }
                    else
                    {
                        string SqlTpl = sql, SqlNew = sql, SqlExisits = string.Empty;
                        string _pk = pk.IndexOf('.') > 0 ? pk.Substring(pk.IndexOf('0') + 1) : pk;

                        int i = 0;
                        while (SqlTpl.IndexOf(")") > -1 && i < 10)
                        {
                            i++;
                            MatchCollection mc = Regex.Matches(sql, "\\([^\\(]+?\\)");
                            if (mc.Count > 0)
                            {
                                foreach (Match mm in mc)
                                {
                                    SqlTpl = SqlTpl.Replace(mm.Value, Scaler.WinBT.DataBase.space_repeat(mm.Value.Length));
                                }
                            }
                            if (SqlTpl.IndexOf(")") == -1)
                                break;
                            mc = Regex.Matches(SqlTpl, "\\([^\\)]+?\\)", RegexOptions.RightToLeft);
                            if (mc.Count > 0)
                            {
                                foreach (Match mm in mc)
                                {
                                    SqlTpl = SqlTpl.Replace(mm.Value, Scaler.WinBT.DataBase.space_repeat(mm.Value.Length));
                                }
                            }
                        }

                        string SqlSel = Regex.Match(sql, "^select (distinct )?.+? from ").Value.Trim();
      
                        SqlExisits = "not exists (select 1 from (" + "select top " + (pagesize * page).ToString() + " " + _pk + " from "+ sql.Substring(SqlSel.Length) + ") as zz where " + pk + "=zz.id)";
                        if (SqlTpl.IndexOf(" where ") > -1)
                        {
                            if (SqlTpl.IndexOf(" order by ") > -1)
                            {
                                SqlNew = sql.Insert(SqlTpl.IndexOf(" order by "), " And " + SqlExisits);
                            }
                            else
                            {
                                SqlNew = sql + " And " + SqlExisits;
                            }
                        }
                        else
                        {
                            if (SqlTpl.IndexOf(" order by ") > -1)
                            {
                                SqlNew = sql.Insert(SqlTpl.IndexOf(" order by "), " where " + SqlExisits);
                            }
                            else
                            {
                                SqlNew = sql + " where " + SqlExisits;
                            }
                        }
                        sql = SqlNew.Insert(7, "top " + pagesize + " ");
                    }
                }
            }
#if Debug
            Win.WriteLog("real£º" + sql);
#endif
            if (bDebug)
                Win.WriteLog(sql);
            var ret = new DataSet();
            int ii = GetConn();
            MyConnection conn = Conns[ii];

            var cmd = new SqlCommand(sql, conn.Conn);
            var adapter = new SqlDataAdapter(cmd);
            try
            {
                if (pagesize == 0 || !string.IsNullOrEmpty(pk))
                    adapter.Fill(ret, "tb");
                else
                    adapter.Fill(ret, pagesize * page, pagesize, "tb");
                return ret.Tables["tb"];
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message + "," + sql);
                return null;
            }
            finally
            {
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
                cmd.Dispose();
                adapter.Dispose();
            }
        }

        public static object GetValue(string sql)
        {
            if (bDebug)
                Win.WriteLog(sql);
            var ret = new object();
            int ii = GetConn();
            MyConnection conn = Conns[ii];
            var cmd = new SqlCommand(sql, conn.Conn);

            try
            {
                ret = cmd.ExecuteScalar();
                return ret == null ? "" : ret;
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message + "," + sql);
                return "";
            }
            finally
            {
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
                cmd.Dispose();
            }
        }
    }
}