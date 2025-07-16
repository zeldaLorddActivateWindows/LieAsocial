using System.Data;
using System.Data.Odbc;

namespace LieAsocial
{
    public class Post
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool IsDeleted { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

    public class PostService
    {
        private readonly string _connectionString;

        public PostService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public bool CreatePost(int userId, string content)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();

                var command = new OdbcCommand(
                    "INSERT INTO Posts (UserId, Content) VALUES (?, ?)",
                    connection);

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@content", content);

                return command.ExecuteNonQuery() > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public List<Post> GetPosts(int? currentUserId = null, int page = 1, int pageSize = 20)
        {
            var posts = new List<Post>();

            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();

                var sql = @"
                    SELECT p.PostId, p.UserId, p.Content, p.DateCreated, p.DateUpdated,
                           u.Username, u.DisplayName, u.ProfilePictureUrl,
                           COUNT(DISTINCT l.LikeId) as LikeCount,
                           COUNT(DISTINCT c.CommentId) as CommentCount" +
                    (currentUserId.HasValue ? ",\n                           CASE WHEN ul.UserId IS NOT NULL THEN 1 ELSE 0 END as IsLikedByCurrentUser" : "") +
                    @"
                    FROM Posts p
                    JOIN Users u ON p.UserId = u.UserId
                    LEFT JOIN Likes l ON p.PostId = l.PostId
                    LEFT JOIN Comments c ON p.PostId = c.PostId AND c.IsDeleted = 0" +
                    (currentUserId.HasValue ? "\n                    LEFT JOIN Likes ul ON p.PostId = ul.PostId AND ul.UserId = ?" : "") +
                    @"
                    WHERE p.IsDeleted = 0
                    GROUP BY p.PostId, p.UserId, p.Content, p.DateCreated, p.DateUpdated,
                             u.Username, u.DisplayName, u.ProfilePictureUrl" +
                    (currentUserId.HasValue ? ", ul.UserId" : "") +
                    @"
                    ORDER BY p.DateCreated DESC
                    LIMIT ? OFFSET ?";

                var command = new OdbcCommand(sql, connection);

                int paramIndex = 0;
                if (currentUserId.HasValue)
                {
                    command.Parameters.AddWithValue($"@currentUserId", currentUserId.Value);
                    paramIndex++;
                }

                command.Parameters.AddWithValue($"@limit", pageSize);
                command.Parameters.AddWithValue($"@offset", (page - 1) * pageSize);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    posts.Add(new Post
                    {
                        PostId = reader.GetInt32("PostId"),
                        UserId = reader.GetInt32("UserId"),
                        Content = reader.GetString("Content"),
                        DateCreated = reader.GetDateTime("DateCreated"),
                        DateUpdated = reader.GetDateTime("DateUpdated"),
                        Username = reader.GetString("Username"),
                        DisplayName = reader.IsDBNull("DisplayName") ? reader.GetString("Username") : reader.GetString("DisplayName"),
                        ProfilePictureUrl = reader.IsDBNull("ProfilePictureUrl") ? "" : reader.GetString("ProfilePictureUrl"),
                        LikeCount = reader.GetInt32("LikeCount"),
                        CommentCount = reader.GetInt32("CommentCount"),
                        IsLikedByCurrentUser = currentUserId.HasValue && reader.GetInt32("IsLikedByCurrentUser") == 1
                    });
                }
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
            return posts;
        }

        public bool LikePost(int postId, int userId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("INSERT INTO Likes (PostId, UserId) VALUES (?, ?)", connection);

                command.Parameters.AddWithValue("@postId", postId);
                command.Parameters.AddWithValue("@userId", userId);

                return command.ExecuteNonQuery() > 0;
            }
            catch (OdbcException)
            {
                return false;
            }
        }

        public bool UnlikePost(int postId, int userId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();

                var command = new OdbcCommand("DELETE FROM Likes WHERE PostId = ? AND UserId = ?", connection);
                command.Parameters.AddWithValue("@postId", postId);
                command.Parameters.AddWithValue("@userId", userId);

                return command.ExecuteNonQuery() > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        public bool DeletePost(int postId, int userId)
        {
            try
            {
                using var connection = new OdbcConnection(_connectionString);
                connection.Open();
                var command = new OdbcCommand("UPDATE Posts SET IsDeleted = 1 WHERE PostId = ? AND UserId = ?", connection);
                command.Parameters.AddWithValue("@postId", postId);
                command.Parameters.AddWithValue("@userId", userId);
                return command.ExecuteNonQuery() > 0;
            }
            catch (OdbcException ex)
            {
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }
    }
}