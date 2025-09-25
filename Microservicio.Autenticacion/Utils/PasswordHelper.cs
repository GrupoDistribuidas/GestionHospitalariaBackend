using BCrypt.Net;

namespace Microservicio.Autenticacion.Utils
{
    /// <summary>
    /// Utilidades para manejo de contraseñas con BCrypt
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Genera un hash BCrypt para una contraseña
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash BCrypt con factor de trabajo 11</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 11);
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hash">Hash BCrypt almacenado</param>
        /// <returns>True si la contraseña es correcta</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}