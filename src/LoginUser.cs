using System.Data.Odbc;

namespace LieAsocial
{
    public class LoginUser
    {
        public string Username { get; }
        public string Password { get; }
        private readonly string _connectionString;

        public LoginUser(string username, string password, string connectionString)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool ValidateLogin()
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                using var command = new OdbcCommand("SELECT COUNT(*) FROM users WHERE Username = ? AND PasswordHash = ?", connection);
                command.Parameters.AddWithValue("@username", Username);
                command.Parameters.AddWithValue("@passwordhash", Password);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error during login: {ex.Message}", ex);
            }
        }
    }
}