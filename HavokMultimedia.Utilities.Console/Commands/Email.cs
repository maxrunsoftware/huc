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
using System.Text;
using HavokMultimedia.Utilities;

namespace HavokMultimedia.Utilities.Console.Commands
{
    public class Email : Command
    {
        protected override void CreateHelp(CommandHelpBuilder help)
        {
            help.AddSummary("Sends an email");
            help.AddParameter(nameof(host), "h", "Hostname or IP of SMTP server");
            help.AddParameter(nameof(port), "o", "Port of SMTP server (25)");
            help.AddParameter(nameof(username), "u", "Username of SMTP server");
            help.AddParameter(nameof(password), "p", "Password of SMTP server");
            help.AddParameter(nameof(ssl), null, "SSL connection to SMTP server (false)");
            help.AddParameter(nameof(timeout), "t", "SMTP server timeout seconds (60)");
            help.AddParameter(nameof(encoding), "e", "Encoding to use (UTF8)");
            help.AddParameter(nameof(to), null, "TO field seperated by ;");
            help.AddParameter(nameof(cc), null, "CC field seperated by ;");
            help.AddParameter(nameof(bcc), null, "BCC field seperated by ;");
            help.AddParameter(nameof(from), "f", "FROM field");
            help.AddParameter(nameof(replyto), "r", "REPLYTO field");
            help.AddParameter(nameof(subject), "s", "Subject line");
            help.AddParameter(nameof(body), "b", "Body of email");
            help.AddParameter(nameof(bodyTemplate), "bt", "Body template file for templating");
            help.AddParameter(nameof(template1), "t1", "Replaces {t1} with this value in subject and body");
            help.AddParameter(nameof(template2), "t2", "Replaces {t2} with this value in subject and body");
            help.AddParameter(nameof(template3), "t3", "Replaces {t3} with this value in subject and body");
            help.AddParameter(nameof(template4), "t4", "Replaces {t4} with this value in subject and body");
            help.AddParameter(nameof(template5), "t5", "Replaces {t5} with this value in subject and body");
            help.AddParameter(nameof(template6), "t6", "Replaces {t6} with this value in subject and body");
            help.AddParameter(nameof(template7), "t7", "Replaces {t7} with this value in subject and body");
            help.AddParameter(nameof(template8), "t8", "Replaces {t8} with this value in subject and body");
            help.AddParameter(nameof(template9), "t9", "Replaces {t9} with this value in subject and body");
            help.AddValue("<attachment1> <attachment2> <attachment3> <etc>");
            help.AddExample("-h=`smtp.somerelay.org` -from=`someone@aol.com` -to=`grandma@aol.com` -s=`Grandpa Birthday` -b=`Tell Grandpa / nHAPPY BIRTHDAY!`");
            help.AddExample("-h=`smtp.somerelay.org` -to=`person1@aol.com; person2 @aol.com` -cc=`person3 @aol.com` -bcc=`person4 @aol.com` -s=`Some subject text` -b=`Some text for body` myAttachedFile1.csv myAttachedFile2.txt");
            help.AddDetail("If both -body and -bodyTemplate are specified the -body value is placed first, then the -bodyTemplate content is placed on a new line");
        }

        private string host;
        private int port;
        private string username;
        private string password;
        private bool ssl;
        private int timeout;
        private Encoding encoding;
        private string from;
        private MailAddress[] to;
        private MailAddress[] cc;
        private MailAddress[] bcc;
        private string replyto;
        private string subject;
        private string body;
        private string bodyTemplate;
        private string template1;
        private string template2;
        private string template3;
        private string template4;
        private string template5;
        private string template6;
        private string template7;
        private string template8;
        private string template9;

        protected override void ExecuteInternal()
        {
            host = GetArgParameterOrConfigRequired(nameof(host), "h");
            port = GetArgParameterOrConfigInt(nameof(port), "o", 25);
            username = GetArgParameterOrConfig(nameof(username), "u");
            password = GetArgParameterOrConfig(nameof(password), "p");
            ssl = GetArgParameterOrConfigBool(nameof(ssl), null, false);
            timeout = GetArgParameterOrConfigInt(nameof(timeout), "t", 60);
            encoding = GetArgParameterOrConfigEncoding(nameof(encoding), "e");
            from = GetArgParameterOrConfigRequired(nameof(from), "f");
            to = GetEmailAddresses(nameof(to));
            cc = GetEmailAddresses(nameof(cc));
            bcc = GetEmailAddresses(nameof(bcc));
            replyto = GetArgParameterOrConfig(nameof(replyto), "r", from);
            subject = GetArgParameterOrConfigRequired(nameof(subject), "s");
            body = GetArgParameterOrConfig(nameof(body), "b");
            if (body != null) body = body.TrimEnd();
            if (body.TrimOrNull() == null) body = null;
            if (body != null) body = body.Replace("\\" + "n", Constant.NEWLINE_WINDOWS, StringComparison.OrdinalIgnoreCase);

            bodyTemplate = GetArgParameterOrConfig(nameof(bodyTemplate), "bt").TrimOrNull();
            if (bodyTemplate != null)
            {
                var bodyTemplateFile = ReadFile(bodyTemplate);
                if (body != null) body = body + Constant.NEWLINE_WINDOWS + bodyTemplateFile;
                else body = bodyTemplateFile;
            }

            template1 = GetArgParameterOrConfig(nameof(template1), "t1");
            template2 = GetArgParameterOrConfig(nameof(template2), "t2");
            template3 = GetArgParameterOrConfig(nameof(template3), "t3");
            template4 = GetArgParameterOrConfig(nameof(template4), "t4");
            template5 = GetArgParameterOrConfig(nameof(template5), "t5");
            template6 = GetArgParameterOrConfig(nameof(template6), "t6");
            template7 = GetArgParameterOrConfig(nameof(template7), "t7");
            template8 = GetArgParameterOrConfig(nameof(template8), "t8");
            template9 = GetArgParameterOrConfig(nameof(template9), "t9");

            var templates = new string[] { template1, template2, template3, template4, template5, template6, template7, template8, template9 };
            for (int i = 1; i <= 9; i++)
            {
                var replacement = templates[i - 1];
                if (replacement.TrimOrNull() == null) replacement = null;
                if (replacement == null) continue;
                var t2 = "{t" + i + "}";
                if (subject != null) subject = subject.Replace(t2, replacement, StringComparison.OrdinalIgnoreCase);
                if (body != null) body = body.Replace(t2, replacement, StringComparison.OrdinalIgnoreCase);
            }

            var attachmentFiles = ParseInputFiles(GetArgValuesTrimmed()).ToArray();
            log.Debug(attachmentFiles, nameof(attachmentFiles));
            CheckFileExists(attachmentFiles);

            using (var client = new SmtpClient())
            {
                client.Host = host;
                client.Port = port;
                client.EnableSsl = ssl;
                client.Timeout = timeout * 1000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = username == null;
                if (username != null) client.Credentials = new System.Net.NetworkCredential(username, password);

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
