using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using System.Net.Mail;

namespace Services.Mail
{
    public class EmailSenderService
    {
        private readonly string _fromEmail;
        private readonly string _name;
        private readonly string _password;
        private readonly SmtpSender _smtpSender;
        private readonly IWebHostEnvironment _environment;

        public EmailSenderService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _name = configuration["EmailSettings:Name"];
            _password = configuration["EmailSettings:Password"];
            _environment = environment;

            _smtpSender = new SmtpSender(() => new SmtpClient("smtp.gmail.com")
            {
                UseDefaultCredentials = false,
                Port = 587,
                Credentials = new System.Net.NetworkCredential(_fromEmail, _password),
                EnableSsl = true,
            });

            Email.DefaultSender = _smtpSender;
            Email.DefaultRenderer = new RazorRenderer();
        }

        public async Task SendEmailAsync<T>(string toEmail, string subject, string templatePath, T model)
        {
            var absoluteTemplatePath = Path.Combine(_environment.ContentRootPath, templatePath);

            var email = await Email
                .From(_fromEmail, _name)
                .To(toEmail)
                .Subject(subject)
                .UsingTemplateFromFile(absoluteTemplatePath, model)
                .SendAsync();
        }
    }
}