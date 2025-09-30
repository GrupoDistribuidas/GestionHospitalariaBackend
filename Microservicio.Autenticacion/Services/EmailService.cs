using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Microservicio.Autenticacion.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordByEmailAsync(string toEmail, string recipientName, string username, string password);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordByEmailAsync(string toEmail, string recipientName, string username, string password)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    emailSettings["FromName"], 
                    emailSettings["FromEmail"]
                ));
                message.To.Add(new MailboxAddress(recipientName, toEmail));
                message.Subject = "Recuperación de Contraseña - Sistema Hospital Central";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5;'>
                            <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <div style='text-align: center; margin-bottom: 30px;'>
                                    <h1 style='color: #2c5aa0; margin: 0;'>Sistema Hospital Central</h1>
                                    <h2 style='color: #666; font-weight: normal; margin: 10px 0 0 0;'>Recuperación de Contraseña</h2>
                                </div>
                                
                                <div style='margin-bottom: 30px;'>
                                    <p style='font-size: 16px; line-height: 1.5; color: #333; margin-bottom: 20px;'>
                                        Estimado/a <strong>{recipientName}</strong>,
                                    </p>
                                    
                                    <p style='font-size: 16px; line-height: 1.5; color: #333; margin-bottom: 20px;'>
                                        Has solicitado recuperar tu contraseña para el sistema del Hospital Central. 
                                        Se ha generado una nueva contraseña temporal para tu cuenta:
                                    </p>
                                    
                                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; border-left: 4px solid #2c5aa0; margin: 20px 0;'>
                                        <p style='margin: 0; font-size: 16px; color: #333;'><strong>Usuario:</strong> {username}</p>
                                        <p style='margin: 10px 0 0 0; font-size: 16px; color: #333;'><strong>Contraseña Temporal:</strong> {password}</p>
                                    </div>
                                    
                                    <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border: 1px solid #ffeaa7; margin: 20px 0;'>
                                        <p style='margin: 0; font-size: 14px; color: #856404;'>
                                            <strong>⚠️ Importante:</strong> Esta es una contraseña temporal. Por seguridad, 
                                            te recomendamos encarecidamente cambiar esta contraseña después de iniciar sesión en el sistema.
                                        </p>
                                    </div>
                                    
                                    <p style='font-size: 16px; line-height: 1.5; color: #333; margin-bottom: 20px;'>
                                        Si no solicitaste esta recuperación de contraseña, por favor contacta inmediatamente 
                                        al administrador del sistema.
                                    </p>
                                </div>
                                
                                <div style='border-top: 1px solid #eee; padding-top: 20px; text-align: center;'>
                                    <p style='font-size: 14px; color: #666; margin: 0;'>
                                        Este es un mensaje automático, por favor no responder a este correo.
                                    </p>
                                    <p style='font-size: 14px; color: #666; margin: 10px 0 0 0;'>
                                        © {DateTime.Now.Year} Hospital Central - Sistema de Gestión Hospitalaria
                                    </p>
                                </div>
                            </div>
                        </body>
                        </html>",
                    TextBody = $@"
Sistema Hospital Central - Recuperación de Contraseña

Estimado/a {recipientName},

Has solicitado recuperar tu contraseña para el sistema del Hospital Central.
Se ha generado una nueva contraseña temporal para tu cuenta.

Credenciales de acceso:
Usuario: {username}
Contraseña Temporal: {password}

IMPORTANTE: Esta es una contraseña temporal. Por seguridad, te recomendamos encarecidamente cambiar esta contraseña después de iniciar sesión en el sistema.

Si no solicitaste esta recuperación de contraseña, por favor contacta inmediatamente al administrador del sistema.

Este es un mensaje automático, por favor no responder a este correo.

© {DateTime.Now.Year} Hospital Central - Sistema de Gestión Hospitalaria"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                await client.ConnectAsync(
                    emailSettings["SmtpHost"], 
                    int.Parse(emailSettings["SmtpPort"]!), 
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    emailSettings["Username"], 
                    emailSettings["Password"]
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Correo de recuperación de contraseña enviado exitosamente a: {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de recuperación de contraseña a: {Email}", toEmail);
                return false;
            }
        }
    }
}