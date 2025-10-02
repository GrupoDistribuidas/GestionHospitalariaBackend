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
        /// <param name=\"password\">Contraseña en texto plano</param>
        /// <returns>Hash BCrypt con factor de trabajo 11</returns>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 11);
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash BCrypt
        /// </summary>
        /// <param name=\"password\">Contraseña en texto plano</param>
        /// <param name=\"hash\">Hash BCrypt almacenado</param>
        /// <returns>True si la contraseña es correcta</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// Genera una contraseña temporal aleatoria
        /// </summary>
        /// <param name=\"length\">Longitud de la contraseña (por defecto 12)</param>
        /// <returns>Contraseña temporal en texto plano</returns>
        public static string GenerateTemporaryPassword(int length = 12)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%&*";

            var random = new Random();
            var password = new System.Text.StringBuilder();

            // Asegurar que la contraseña tenga al menos un carácter de cada tipo
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Rellenar el resto con caracteres aleatorios
            string allChars = upperCase + lowerCase + numbers + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Mezclar los caracteres para que no sean predecibles
            return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
        }
    }
}
