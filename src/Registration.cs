using System.Text.RegularExpressions;
using System.Data.Odbc;

namespace LieAsocial
{
    public class NewUser
    {
        private string? password;
        public string? Name { get; set; }
        public string? Password
        {
            get => password;
            set => password = value?.Length >= 8 && HasFourIntegers(value) ? value : throw new Exception("Password must contain at least 4 numbers and be 8+ characters");
        }
        public string? Email { get; set; }
        private readonly string _connectionString;

        public NewUser(string name, string password, string email, string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Name = name;
            Password = password;
            Email = email;
        }

        private static bool HasFourIntegers(string input)
        {
            var matches = Regex.Matches(input, @"\d");
            return matches.Count >= 4;
        }

        public void RegisterUser()
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();

                var checkCommand = new OdbcCommand("SELECT COUNT(*) FROM Users WHERE Email = ?", connection);
                checkCommand.Parameters.AddWithValue("@email", Email);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0) throw new Exception("User with this email already exists");

                var insertCommand = new OdbcCommand("INSERT INTO Users (Name, Password, Email) VALUES (?, ?, ?)", connection);
                insertCommand.Parameters.AddWithValue("@name", Name);
                insertCommand.Parameters.AddWithValue("@password", Password);
                insertCommand.Parameters.AddWithValue("@email", Email);

                int rowsAffected = insertCommand.ExecuteNonQuery();
                if (rowsAffected == 0) throw new Exception("Failed to register user");
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }
    }
}