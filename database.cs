using System.Collections.Specialized;
using System.Data;

namespace MyWeb
{

    public class database
    {
        private string TABLENAME = "";

        public database(string tablename)
        {
            TABLENAME = tablename;
        }

        public void dealSearch(string strField, string option, string strInput, ref string strWhere, ref string urlParam)
        {
            string strFieldValue = MyWeb.Common.GetParam(strInput, "").ToString();
            if (!string.IsNullOrEmpty(strFieldValue))
            {
                if (!string.IsNullOrEmpty(strWhere))
                {
                    strWhere += " And ";
                }
                if (strInput.StartsWith("int_"))
                {
                    strWhere = strField + "=" + strInput;
                }
                else
                {
                    if (option == "like")
                    {
                        strWhere = strField + " like '%" + strInput + "%'";
                    }
                    else
                    {
                        strWhere = strField + " = '" + strInput + "'";
                    }
                }
            }
        }

        public DataRow gets(string strWhere)
        {
            string Sql = "select top 1 * from " + TABLENAME + " where " + strWhere;
            DataTable dt = Scaler.DataBase.DataBase.GetRecords(Sql, 0, 0);
            if (dt!=null && dt.Rows.Count>0)
            {
                return dt.Rows[0];
            }
            return null;
        }

        public DataRow get(string id)
        {
            return gets("id=" + id);
        }

        public bool save(NameValueCollection nv, string primarykey)
        {
            if (string.IsNullOrEmpty(nv[primarykey].Trim())) //添加
            {
                nv.Remove(primarykey);
                string Sql = MyWeb.MyForm.GetInsertSQL(TABLENAME, nv);
                if (Scaler.DataBase.DataBase.ExecuteSql(Sql) > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                string primaryvalue = nv[primarykey];
                nv.Remove(primarykey);
                string Sql = MyWeb.MyForm.GetUpdateSQL(TABLENAME, nv, primarykey + "=" + primaryvalue);
                if (Scaler.DataBase.DataBase.ExecuteSql(Sql) > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool dels(string strWhere)
        {
            string Sql = "delete from " + TABLENAME + " where " + strWhere;
            if (Scaler.DataBase.DataBase.ExecuteSql(Sql) > 0)
                return true;
            return false;
        }

        public bool del(string id)
        {
            return dels("id=" + id);
        }


        public Module.Records list(string Sql, int PageSize)
        {
            Module.Records record = new Module.Records();
            if (PageSize == 0) //显示全部
            {
                record.list = Scaler.DataBase.DataBase.GetRecords(Sql, 0, 0);
                return record;
            }
            int page = int.Parse(MyWeb.Common.GetParam("page", "1").ToString());

            page = page < 1 ? 1 : page;

            record.list = Scaler.DataBase.DataBase.GetRecords(Sql, PageSize, page - 1);
            int iCount = int.Parse(Scaler.DataBase.DataBase.GetValue(MyWeb.Common.getCountSQL(Sql)).ToString());
            record.iCount = iCount;
            record.iPage = page;
            return record;
        }

        public Module.Records list(int PageSize, string strOrder)
        {
            string sql = "select * from " + TABLENAME + strOrder;
            return list(sql, PageSize);
        }

        public Module.Records list(int PageSize)
        {
            string sql = "select * from " + TABLENAME;
            return list(sql, PageSize);
        }

        public Module.Records list()
        {
            return list(0, "");
        }
    }
}