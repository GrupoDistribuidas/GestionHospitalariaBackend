using BCrypt.Net;

namespace Microservicio.Autenticacion.Utils
{
    /// <summary>
    /// Utilidades para manejo de contrase�as con BCrypt
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Genera un hash BCrypt para una contrase�a
        /// </summary>
        /// <param name="password">Contrase�a en texto plano</param>
        /// <returns>Hash BCrypt con factor de trabajo 11</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 11);
        }

        /// <summary>
        /// Verifica si una contrase�a coincide con su hash BCrypt
        /// </summary>
        /// <param name="password">Contrase�a en texto plano</param>
        /// <param name="hash">Hash BCrypt almacenado</param>
        /// <returns>True si la contrase�a es correcta</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}