using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace TECSite.EmailService
{
    public class EmailConfiguration
    {
        public string From { get; }
        public string SmtpServer { get; } 
        public int Port { get; } 
        public string UserName { get; } 
        public string Password { get; }

        public EmailConfiguration()
        {
            From = "theenergeticconvention@gmail.com";
            SmtpServer = "smtp.gmail.com";
            Port = 465;
            UserName = "theenergeticconvention@gmail.com";
            Debug.WriteLine(Environment.GetEnvironmentVariable("SMTPPASS"));
            Password = Environment.GetEnvironmentVariable("SMTPPASS")!;
            Debug.WriteLine(Password);
        }
    }
}
