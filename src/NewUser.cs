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
            set => password = HasFourIntegers(value!) && value?.Length >= 8  ? value : throw new Exception("Password must be at least 8 long and contain 4 or more numbers.");
        }
        public string? Email { get; set; }
        private readonly string _connectionString;
        static int uid = 0;
        public NewUser(string name, string password, string email, string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Name = name;
            Password = password;
            Email = email;
            uid++;
        }

        private static bool HasFourIntegers(string input)
        {
            var matches = Regex.Matches(input, @"\d");
            return matches.Count >= 4;
        }

        public bool RegisterUser()
        {
            try
            {
                var connection = new OdbcConnection(_connectionString);
                connection.Open();

                var checkCommand = new OdbcCommand("SELECT COUNT(*) FROM users WHERE Email = ?", connection);
                checkCommand.Parameters.AddWithValue("@email", Email);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0) throw new Exception("User with this email already exists");

                using var insertCommand = new OdbcCommand("INSERT INTO users (username, PasswordHash, Email) VALUES (?, ?, ?)", connection);
                insertCommand.Parameters.AddWithValue("@username", Name);
                insertCommand.Parameters.AddWithValue("@PasswordHash", Password);
                insertCommand.Parameters.AddWithValue("@Email", Email);

                int rowsAffected = insertCommand.ExecuteNonQuery();
                if (rowsAffected == 0) throw new Exception("Failed to register user");
                else return true;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }
    }
}