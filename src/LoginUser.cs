using System.Data.Odbc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace LieAsocial
{
    public class LoginUser
    {
        public string Username { get; }
        public string PlainPassword { get; }
        private readonly string _connectionString;

        public LoginUser(string username, string password, string connectionString)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            PlainPassword = password ?? throw new ArgumentNullException(nameof(password));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private string HashPassword(string password, byte[] salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
        }

        public bool ValidateLogin()
        {
            try
            {
                var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var getCommand = new OdbcCommand("SELECT PasswordHash, PasswordSalt FROM users WHERE Username = ?", connection);
                getCommand.Parameters.AddWithValue("@username", Username);

                using var reader = getCommand.ExecuteReader();
                if (!reader.Read()) return false;
                string storedHash = reader.GetString(0);
                byte[] storedSalt = Convert.FromBase64String(reader.GetString(1));
                string hashedPassword = HashPassword(PlainPassword, storedSalt);
                return hashedPassword == storedHash;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error during login: {ex.Message}", ex);
            }
        }

        public void UpdateLastLogin()
        {
            try
            {
                var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var updateCommand = new OdbcCommand("UPDATE users SET LastLogin = CURRENT_TIMESTAMP WHERE Username = ?", connection);
                updateCommand.Parameters.AddWithValue("@username", Username);
                updateCommand.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                Console.WriteLine($"Warning: Could not update last login time: {ex.Message}");
            }
        }
    }
}