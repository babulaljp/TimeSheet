using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace UEAW.Pages
{
    public class ContactUsModel : PageModel
    {
        private readonly IConfiguration _config;

        public ContactUsModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public ContactInput Input { get; set; }

        public string SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var smtp = _config.GetSection("Smtp");
            var host = smtp["Host"];
            if (string.IsNullOrEmpty(host))
            {
                ModelState.AddModelError(string.Empty, "SMTP is not configured.");
                return Page();
            }

            int port = 25;
            if (!int.TryParse(smtp["Port"], out port)) port = 25;
            bool enableSsl = false;
            bool.TryParse(smtp["EnableSsl"], out enableSsl);
            var user = smtp["User"];
            var pass = smtp["Password"];
            var from = smtp["From"] ?? user;
            var to = smtp["To"];

            if (string.IsNullOrEmpty(to))
            {
                ModelState.AddModelError(string.Empty, "SMTP recipient (Smtp:To) is not configured.");
                return Page();
            }

            var body = $"Name: {Input.Name}\nEmail: {Input.Email}\n\nMessage:\n{Input.Message}";

            try
            {
                using var msg = new MailMessage(from, to)
                {
                    Subject = Input.Subject ?? "Contact Us message",
                    Body = body
                };

                if (!string.IsNullOrEmpty(Input.Email))
                {
                    msg.ReplyToList.Add(new MailAddress(Input.Email, Input.Name));
                }

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl
                };

                if (!string.IsNullOrEmpty(user))
                {
                    client.Credentials = new NetworkCredential(user, pass);
                }

                await client.SendMailAsync(msg);
                SuccessMessage = "Your message has been sent. Thank you.";

                // Clear the form
                ModelState.Clear();
                Input = new ContactInput();
                return Page();
            }
            catch (SmtpException ex)
            {
                ModelState.AddModelError(string.Empty, "Failed to send message: " + ex.Message);
                return Page();
            }
        }

        public class ContactInput
        {
            [Required]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Subject { get; set; }

            [Required]
            public string Message { get; set; }
        }
    }
}

