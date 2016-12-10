//using System.Linq;
using System.Collections.Specialized;

namespace MyWeb
{
    public class MyForm
    {
        public static string GetInsertSQL(string TableName, NameValueCollection FormData)
        {
            string text = "";
            string text2 = "";
            for (int i = 0; i < FormData.Count; i++)
            {
                if (!FormData.Keys[i].ToString().StartsWith("__") && (FormData.Keys[i].ToString().IndexOf('$') <= 0) && !string.IsNullOrEmpty(FormData.Keys[i].ToString().Trim()))
                {
                    if (i == (FormData.Count - 1))
                    {
                        text = text + "[" + FormData.AllKeys[i].ToString() + "]";
                        if (FormData[i].ToString() == "")
                        {
                            text2 = text2 + "''";
                        }
                        else
                        {
                            text2 = text2 + "'" + FormData[i].ToString().Replace("'","''") + "'";
                        }
                    }
                    else
                    {
                        text = text + "[" + FormData.AllKeys[i].ToString() + "],";
                        if (FormData[i].ToString() == "")
                        {
                            text2 = text2 + "'',";
                        }
                        else
                        {
                            text2 = text2 + "'" + FormData[i].ToString().Replace("'", "''") + "',";
                        }
                    }
                }
            }
            if (text.Substring(text.Length - 1, 1) == ",")
            {
                text = text.Remove(text.Length - 1);
                text2 = text2.Remove(text2.Length - 1);
            }
            string cmdText = "insert into " + TableName + "(" + text + ") Values(" + text2 + ")";
            return cmdText;
        }

        public static string GetDeleteSQL(string TableName, string strWhere)
        {
            return "Delete From "+ TableName +" where "+ strWhere;
        }

        public static string GetUpdateSQL(string TableName, NameValueCollection FormData, string strWhere)
        {
            string text = "";
            for (int i = 0; i < FormData.Count; i++)
            {
                if (!FormData.Keys[i].ToString().StartsWith("__") && (FormData.Keys[i].ToString().IndexOf('$') <= 0) && !string.IsNullOrEmpty(FormData.Keys[i].ToString().Trim()))
                {
                    if (i == (FormData.Count - 1))
                    {
                        text = text + "[" + FormData.AllKeys[i].ToString() + "]=";
                        if (FormData[i].ToString() == "")
                        {
                            text = text + "'' ";
                        }
                        else
                        {
                            text = text + "'" + FormData[i].ToString().Replace("'", "''") + "' ";
                        }
                    }
                    else
                    {
                        text = text + "[" + FormData.AllKeys[i].ToString() + "]=";
                        if (FormData[i].ToString() == "")
                        {
                            text = text + "'',";
                        }
                        else
                        {
                            text = text + "'" + FormData[i].ToString().Replace("'", "''") + "',";
                        }
                    }
                }
            }
            if(text.Substring(text.Length-1)==",")
            {
                text=text.Remove(text.Length-1);
            }
            string cmdText = "update " + TableName + " set " + text + " where " + strWhere;
            return cmdText;
        }
    }
}
