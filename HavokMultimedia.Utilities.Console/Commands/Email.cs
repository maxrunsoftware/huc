/*
Copyright (c) 2021 Steven Foster (steven.d.foster@gmail.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using HavokMultimedia.Utilities;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Email : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Sends an email");
            help.AddParameter("host", "h", "Hostname or IP of SMTP server");
            help.AddParameter("port", "o", "Port of SMTP server (25)");
            help.AddParameter("username", "u", "Username of SMTP server");
            help.AddParameter("password", "p", "Password of SMTP server");
            help.AddParameter("ssl", "SSL connection to SMTP server (false)");
            help.AddParameter("timeout", "t", "SMTP server timeout seconds (60)");
            help.AddParameter("encoding", "e", "Encoding to use (UTF8)");
            help.AddParameter("to", "TO field seperated by ;");
            help.AddParameter("cc", "CC field seperated by ;");
            help.AddParameter("bcc", "BCC field seperated by ;");
            help.AddParameter("from", "f", "FROM field");
            help.AddParameter("replyTo", "r", "REPLYTO field");
            help.AddParameter("subject", "s", "Subject line");
            help.AddParameter("body", "b", "Body of email");
            help.AddValue("<attachment1> <attachment2> <attachment3> <etc>");
            help.AddExample("-h=`smtp.somerelay.org` -from=`someone@aol.com` -to=`grandma@aol.com` -s=`Grandpa Birthday` -b=`Tell Grandpa / nHAPPY BIRTHDAY!`");
            help.AddExample("-h=`smtp.somerelay.org` -to=`person1@aol.com; person2 @aol.com` -cc=`person3 @aol.com` -bcc=`person4 @aol.com` -s=`Some subject text` -b=`Some text for body` myAttachedFile1.csv myAttachedFile2.txt");
        }


        protected override void ExecuteInternal()
        {
            var smtphost = GetArgParameterOrConfigRequired("host", "h");
            var smtpport = GetArgParameterOrConfigInt("port", "o", 25);
            var smtpusername = GetArgParameterOrConfig("username", "u");
            var smtppassword = GetArgParameterOrConfig("password", "p");
            var smtpenablessl = GetArgParameterOrConfigBool("ssl", null, false);
            var smtptimeout = GetArgParameterOrConfigInt("timeout", "t", 60);
            var encoding = GetArgParameterOrConfigEncoding("encoding", "e");
            var from = GetArgParameterOrConfigRequired("from", "f");
            var to = GetEmailAddresses("to");
            var cc = GetEmailAddresses("cc");
            var bcc = GetEmailAddresses("bcc");
            var replyto = GetArgParameterOrConfig("replyto", "r", from);
            var s = GetArgParameterOrConfigRequired("subject", "s");
            var b = GetArgParameterOrConfig("body", "b");
            if (b != null) b = b.Replace("\\" + "n", Constant.NEWLINE_WINDOWS, StringComparison.OrdinalIgnoreCase);

            var attachmentFiles = Util.ParseInputFiles(GetArgValuesTrimmed()).ToArray();
            log.Debug(attachmentFiles, nameof(attachmentFiles));
            CheckFileExists(attachmentFiles);

            using (var client = new SmtpClient())
            {
                client.Host = smtphost;
                client.Port = smtpport;
                client.EnableSsl = smtpenablessl;
                client.Timeout = smtptimeout * 1000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = smtpusername == null;
                if (smtpusername != null) client.Credentials = new System.Net.NetworkCredential(smtpusername, smtppassword);

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    to.ForEach(o => mail.To.Add(o));
                    cc.ForEach(o => mail.CC.Add(o));
                    bcc.ForEach(o => mail.Bcc.Add(o));
                    mail.SubjectEncoding = encoding;
                    mail.Subject = s;

                    mail.BodyEncoding = encoding;
                    mail.IsBodyHtml = false;
                    if (b != null) mail.Body = b;

                    mail.ReplyToList.Clear();
                    mail.ReplyToList.Add(new MailAddress(replyto ?? from));

                    mail.Sender = mail.From;

                    var attachments = attachmentFiles.Select(o => mail.AddAttachment(o)).ToList();

                    client.Send(mail);

                    foreach (var a in attachments.ToListReversed()) a.Dispose();
                }
            }

        }

        private IEnumerable<MailAddress> ParseEmailAddresses(string emailAddresses)
        {

            var list = new List<MailAddress>();
            emailAddresses = emailAddresses.TrimOrNull();
            if (emailAddresses == null) return list;

            foreach (var ea in emailAddresses.Split(';', ',', '|').TrimOrNull().WhereNotNull())
            {
                var ma = new MailAddress(ea);
                list.Add(ma);
            }

            return list;
        }
        private MailAddress[] GetEmailAddresses(string key)
        {
            var to1 = GetArgParameter(key, null);
            var to11 = ParseEmailAddresses(to1);
            var to2 = GetArgParameterConfig(key);
            var to22 = ParseEmailAddresses(to2);
            var to = to11.Concat(to22).ToArray();
            for (int i = 0; i < to.Length; i++) log.Debug($"{key}[{i}]: {to[i]}");
            return to;
        }
    }
}
