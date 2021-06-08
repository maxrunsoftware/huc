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
            help.AddParameter("bodyTemplate", "bt", "Body template file for templating");
            help.AddParameter("template1", "t1", "Replaces {t1} with this value in subject and body");
            help.AddParameter("template2", "t2", "Replaces {t2} with this value in subject and body");
            help.AddParameter("template3", "t3", "Replaces {t3} with this value in subject and body");
            help.AddParameter("template4", "t4", "Replaces {t4} with this value in subject and body");
            help.AddParameter("template5", "t5", "Replaces {t5} with this value in subject and body");
            help.AddParameter("template6", "t6", "Replaces {t6} with this value in subject and body");
            help.AddParameter("template7", "t7", "Replaces {t7} with this value in subject and body");
            help.AddParameter("template8", "t8", "Replaces {t8} with this value in subject and body");
            help.AddParameter("template9", "t9", "Replaces {t9} with this value in subject and body");
            help.AddValue("<attachment1> <attachment2> <attachment3> <etc>");
            help.AddExample("-h=`smtp.somerelay.org` -from=`someone@aol.com` -to=`grandma@aol.com` -s=`Grandpa Birthday` -b=`Tell Grandpa / nHAPPY BIRTHDAY!`");
            help.AddExample("-h=`smtp.somerelay.org` -to=`person1@aol.com; person2 @aol.com` -cc=`person3 @aol.com` -bcc=`person4 @aol.com` -s=`Some subject text` -b=`Some text for body` myAttachedFile1.csv myAttachedFile2.txt");
            help.AddDetail("If both -body and -bodyTemplate are specified the -body value is placed first, then the -bodyTemplate content is placed on a new line");
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
            var subject = GetArgParameterOrConfigRequired("subject", "s");
            var body = GetArgParameterOrConfig("body", "b");
            if (body != null) body = body.TrimEnd();
            if (body.TrimOrNull() == null) body = null;
            if (body != null) body = body.Replace("\\" + "n", Constant.NEWLINE_WINDOWS, StringComparison.OrdinalIgnoreCase);

            var bodyTemplate = GetArgParameterOrConfig("bodyTemplate", "bt").TrimOrNull();
            if (bodyTemplate != null)
            {
                var bodyTemplateFile = ReadFile(bodyTemplate);
                if (body != null) body = body + Constant.NEWLINE_WINDOWS + bodyTemplateFile;
                else body = bodyTemplateFile;
            }

            for (int i = 1; i <= 9; i++)
            {
                var t1 = "template" + i;
                var t2 = "t" + i;
                var replacement = GetArgParameterOrConfig(t1, t2);
                if (replacement.TrimOrNull() == null) replacement = null;
                if (replacement == null) continue;

                t2 = "{" + t2 + "}";
                if (subject != null) subject = subject.Replace(t2, replacement, StringComparison.OrdinalIgnoreCase);
                if (body != null) body = body.Replace(t2, replacement, StringComparison.OrdinalIgnoreCase);
            }

            var attachmentFiles = ParseInputFiles(GetArgValuesTrimmed()).ToArray();
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
                    mail.Subject = subject;

                    mail.BodyEncoding = encoding;
                    mail.IsBodyHtml = false;
                    if (body != null) mail.Body = body;

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
