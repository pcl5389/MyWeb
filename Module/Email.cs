using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Xml;

namespace MyWeb.Module
{
    public class Email
    {
        public string recv = string.Empty;
        public string content = string.Empty;
        public string subject = string.Empty;

        public string mailServer = string.Empty;
        public string sendUser = string.Empty;
        public string sendPwd = string.Empty;
        public string mailFrom = string.Empty;
        public string realName = string.Empty;
        public int sendSpan = 0;

        private bool bInit = false;

        public Email()
        {
            Init();
        }

        private string Init()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + "config.xml");

                sendUser = doc.SelectSingleNode("/mailbox/username").Attributes["value"].Value;
                sendPwd = doc.SelectSingleNode("/mailbox/password").Attributes["value"].Value;
                mailFrom = doc.SelectSingleNode("/mailbox/mailfrom").Attributes["value"].Value;
                realName = doc.SelectSingleNode("/mailbox/realname").Attributes["value"].Value;
                mailServer = doc.SelectSingleNode("/mailbox/mailserver").Attributes["value"].Value;
                sendSpan = int.Parse(doc.SelectSingleNode("/mailbox/sendspan").Attributes["value"].Value);
                bInit = true;
                return "ok";
            }
            catch (Exception e)
            {
                return e.Message.ToString();
            }
        }

        public string SendNow()
        {
            if (!bInit)
            {
                string ret = Init();
                if (ret != "ok")
                {
                    return ret;
                }
            }

            string result;
            SmtpClient mail = new SmtpClient();
            //发送方式
            mail.DeliveryMethod = SmtpDeliveryMethod.Network;
            //smtp服务器
            mail.Host = mailServer; // "smtp.sina.com";
            //用户名凭证               
            mail.Credentials = new System.Net.NetworkCredential(sendUser, sendPwd);
            mail.UseDefaultCredentials = true;
            //邮件信息
            MailMessage message = new MailMessage();
            //发件人
            message.From = new MailAddress(mailFrom, realName);
            //收件人
            try
            {
                message.To.Add(recv);
            }
            catch
            {
                return "邮箱格式错误";
            }
            //主题
            message.Subject = subject;
            //内容
            message.Body = content;
            //正文编码
            message.BodyEncoding = System.Text.Encoding.UTF8;
            //设置为HTML格式
            message.IsBodyHtml = true;
            //优先级
            message.Priority = MailPriority.High;

            try
            {
                mail.Send(message);
                result = "发送成功";
                return result;
            }
            catch (Exception e)
            {
                result = e.Message.ToString();
            }
            return result;
        }


    }
}
