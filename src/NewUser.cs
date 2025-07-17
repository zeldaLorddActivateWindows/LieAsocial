using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Data.Odbc;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LieAsocial
{
    public class NewUser
    {
        private string? password;
        private string? email;
        public string? Name { get; set; }
        public string? Password
        {
            get => password;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new Exception("Password cannot be empty.");
                if (value.Length < 8) throw new Exception("Password must be at least 8 characters long.");
                if (!Regex.IsMatch(value, @"[a-z]")) throw new Exception("Password must contain at least one lowercase letter.");
                if (!Regex.IsMatch(value, @"\d")) throw new Exception("Password must contain at least one number.");
                if (!Regex.IsMatch(value, @"[!@#$%^&*(),.?""':{}|<>]")) throw new Exception("Password must contain at least one special character.");
                password = value;
            }
        }
        public string? Email { 
            get => email; 
            set => email = IsValid(value!) ? value : throw new Exception("Invalid email"); 
        }
        private readonly string _connectionString;
        public byte[] Salt {  get; } = RandomNumberGenerator.GetBytes(128 / 8); 

        public NewUser(string name, string password, string email, string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }

        private static bool HasFourIntegers(string input)
        {
            var matches = Regex.Matches(input, @"\d");
            return matches.Count >= 4;
        }

        public string HashPassword(string plainPassword)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: plainPassword,
                salt: Salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
        }
        private static bool IsValid(string email)
        {
            var valid = true;

            try
            {
                var emailAddress = new MailAddress(email);
            }
            catch
            {
                valid = false;
            }

            return valid;
        }
        public bool RegisterUser()
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                using var checkCommand = new OdbcCommand("SELECT COUNT(*) FROM users WHERE Email = ? OR Username = ?", connection);
                checkCommand.Parameters.AddWithValue("@email", Email);
                checkCommand.Parameters.AddWithValue("@username", Name);
                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0) throw new Exception("User with this email or username already exists");
                string? hashedPassword = HashPassword(Password!);
                using var insertCommand = new OdbcCommand("INSERT INTO users (Username, PasswordHash, PasswordSalt, Email) VALUES (?, ?, ?, ?)", connection);
                insertCommand.Parameters.AddWithValue("@username", Name);
                insertCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                insertCommand.Parameters.AddWithValue("@PasswordSalt", Convert.ToBase64String(Salt));
                insertCommand.Parameters.AddWithValue("@Email", Email);
                int rowsAffected = insertCommand.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }
    }
}