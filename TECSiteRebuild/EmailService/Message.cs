using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TECSite.EmailService
{
    public class Message
    {
        public MailboxAddress From { get; set; }
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public IFormFile Attachments { get; set; }

        public Message(string fromName, string fromAdress, Dictionary<string, string> to, string subject, string content, IFormFile attachments)
        {
            From = new MailboxAddress(fromName, fromAdress);
            To = new List<MailboxAddress>();

            foreach (string key in to.Keys)
            {
                To.Add(new MailboxAddress(key, to[key]));
            }
            Subject = subject;
            Content = content;
            Attachments = attachments;
        }
    }
}
