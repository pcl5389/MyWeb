namespace Scaler.DataBase
{
    using MyWeb;
    using System;
    using System.Data;
    using System.Data.OleDb;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Web;

    public static class Win
    {
        public static void WriteLog(string strs)
        {
            StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\Log.txt", true);
            sw.Write(DateTime.Now.ToString()+","+ strs);
            sw.WriteLine();
            sw.Close();
        }
    }

    public class MyConnection {
        public OleDbConnection Conn = null;  
        public int bUsing = 1;

		public MyConnection()
		{}
    }

    public abstract class DataBase
    {
        private const int connMax=50;
        public static bool init = false;
        public static MyConnection[] Conns = new MyConnection[connMax];
        public static int intconn = 0;
        /// <summary>
        /// Microsoft.Jet.OLEDB.4.0
        /// Microsoft.ACE.OLEDB.12.0
        /// </summary>
        //private static string connstring = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "\\App_Data\\data.mdb;Persist Security Info=False";
        private static string connstring = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "\\App_Data\\data.mdb;Persist Security Info=False";
        private static object dblock=new object();

        static DataBase() { }

        public static int GetConn()
        {
            int iUse = -1;

            if (intconn > 0)
            {
                for (int i = 0; i < intconn; i++)
                {
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
                                Conns[i].Conn = new OleDbConnection(connstring);
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
            Conns[iUse].Conn = new OleDbConnection(connstring);
            Conns[iUse].Conn.Open();
            return iUse;
        }

        public static int ExecuteSql(string sql)
        {
            int ret = 0;
			int ii=GetConn();
            MyConnection conn = Conns[ii];
            OleDbCommand cmd = new OleDbCommand(sql, conn.Conn);

            try
            {
                ret = cmd.ExecuteNonQuery();
                return ret;
            }
            catch (Exception e)
            {
                HttpContext.Current.Response.Cookies["LastErr"].Value = MyWeb.Common.StrEncode(e.Message.ToString());
                Win.WriteLog(e.Message.ToString() + "," + sql);
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
                string err = HttpContext.Current.Request.Cookies["LastErr"].Value;
                HttpContext.Current.Response.Cookies["LastErr"].Expires = DateTime.Now.AddDays(-1);
                if (!string.IsNullOrEmpty(err))
                {
                    return err;
                }
            }
            return "";
        }
        public static DataTable ExecuteSP(OleDbParameter[] paras, string spName)
        { 
            DataSet ret = new DataSet();
            int ii = GetConn();
            MyConnection conn = Conns[ii];

            OleDbCommand cmd = new OleDbCommand(spName, conn.Conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(paras);

            OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
            try
            {
                adapter.Fill(ret, "tb");
                return ret.Tables["tb"];
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message.ToString() + "," + spName);
                return null;
            }
            finally
            {
                Interlocked.Exchange(ref Conns[ii].bUsing, 0);
                cmd.Dispose();
                adapter.Dispose();
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

        public static DataTable GetRecords(string sql, int pagesize, int page)
        {
            DataSet ret=new DataSet();
            int ii = GetConn();
            MyConnection conn = Conns[ii];

            OleDbCommand cmd = new OleDbCommand(sql, conn.Conn);
            OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
            try
            {
                if (pagesize == 0)
                    adapter.Fill(ret, "tb");
                else
                    adapter.Fill(ret, pagesize * page, pagesize, "tb");
                return ret.Tables["tb"];
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message.ToString() + "," + sql);
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
            object ret=new object();
            int ii = GetConn();
            MyConnection conn = Conns[ii];
            
            OleDbCommand cmd = new OleDbCommand(sql, conn.Conn);

            try
            {
                ret = cmd.ExecuteScalar();
                return ret == null ? "" : ret;
            }
            catch (Exception e)
            {
                Win.WriteLog(e.Message.ToString() + "," + sql);
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
