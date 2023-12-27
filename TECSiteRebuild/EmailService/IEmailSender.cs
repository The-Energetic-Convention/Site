using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TECSite.EmailService
{
    public interface IEmailSender
    {
        void SendEmail(Message message);
        Task SendEmailAsync(Message message);
    }
}
