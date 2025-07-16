using System.Data;
using System.Data.Odbc;
using System.Security.Cryptography;

namespace LieAsocial
{
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public string CreateSession(int userId)
        {
            try
            {
                var sessionId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var expiresAt = DateTime.UtcNow.AddDays(30);
                var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("INSERT INTO Sessions (SessionId, UserId, ExpiresAt) VALUES (?, ?, ?)", connection);

                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@expiresAt", expiresAt);
                command.ExecuteNonQuery();
                return sessionId;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public int? GetUserIdFromSession(string sessionId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("SELECT UserId FROM Sessions WHERE SessionId = ? AND ExpiresAt > ? AND IsActive = 1", connection);

                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@expiresAt", DateTime.UtcNow);

                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : null;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public void InvalidateSession(string sessionId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("UPDATE Sessions SET IsActive = 0 WHERE SessionId = ?", connection);

                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public User? GetUserById(int userId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("SELECT UserId, Username, Email, DisplayName, Bio, ProfilePictureUrl, DateCreated FROM Users WHERE UserId = ?", connection);
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        DisplayName = reader.IsDBNull("DisplayName") ? reader.GetString("Username") : reader.GetString("DisplayName"),
                        Bio = reader.IsDBNull("Bio") ? "" : reader.GetString("Bio"),
                        ProfilePictureUrl = reader.IsDBNull("ProfilePictureUrl") ? "" : reader.GetString("ProfilePictureUrl"),
                        DateCreated = reader.GetDateTime("DateCreated")
                    };
                }
                return null;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public bool UpdateProfile(int userId, string displayName, string bio)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("UPDATE Users SET DisplayName = ?, Bio = ? WHERE UserId = ?", connection);
                command.Parameters.AddWithValue("@displayName", displayName);
                command.Parameters.AddWithValue("@bio", bio);
                command.Parameters.AddWithValue("@userId", userId);

                return command.ExecuteNonQuery() > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public List<User> SearchUsers(string query, int limit = 10)
        {
            var users = new List<User>();
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("SELECT UserId, Username, DisplayName, ProfilePictureUrl FROM Users WHERE Username LIKE ? OR DisplayName LIKE ? LIMIT ?", connection);
                var searchTerm = $"%{query}%";
                command.Parameters.AddWithValue("@username", searchTerm);
                command.Parameters.AddWithValue("@displayName", searchTerm);
                command.Parameters.AddWithValue("@limit", limit);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        DisplayName = reader.IsDBNull("DisplayName") ? reader.GetString("Username") : reader.GetString("DisplayName"),
                        ProfilePictureUrl = reader.IsDBNull("ProfilePictureUrl") ? "" : reader.GetString("ProfilePictureUrl")
                    });
                }
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }

            return users;
        }
    }

    public class User //maybe convert to struct later
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }
}