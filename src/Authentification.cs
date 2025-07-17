using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components;

namespace LieAsocial
{
    public class AuthService
    {
        private readonly ProtectedLocalStorage _localStorage;
        private readonly UserService _userService;
        private User? _currentUser;
        private string? _sessionId;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public AuthService(ProtectedLocalStorage localStorage, UserService userService)
        {
            _localStorage = localStorage;
            _userService = userService;
        }

        public event Action<User?>? UserChanged;

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public async Task<bool> InitializeAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var sessionResult = await _localStorage.GetAsync<string>("sessionId");
                if (sessionResult.Success && !string.IsNullOrEmpty(sessionResult.Value))
                {
                    _sessionId = sessionResult.Value;
                    var userId = _userService.GetUserIdFromSession(_sessionId);

                    if (userId.HasValue)
                    {
                        _currentUser = _userService.GetUserById(userId.Value);
                        UserChanged?.Invoke(_currentUser);
                        return true;
                    }
                    else await _localStorage.DeleteAsync("sessionId");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _semaphore.Release();
            }
            return false;
        }

        public async Task<bool> LoginAsync(LoginUser loginUser)
        {
            if (loginUser.ValidateLogin())
            {
                var userId = GetUserIdByUsername(loginUser.Username);
                if (userId.HasValue)
                {
                    _currentUser = _userService.GetUserById(userId.Value);
                    _sessionId = _userService.CreateSession(userId.Value);

                    await _localStorage.SetAsync("sessionId", _sessionId);

                    loginUser.UpdateLastLogin();
                    UserChanged?.Invoke(_currentUser);
                    return true;
                }
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(_sessionId)) _userService.InvalidateSession(_sessionId);
            
            await _localStorage.DeleteAsync("sessionId");
            _currentUser = null;
            _sessionId = null;
            UserChanged?.Invoke(null);
        }

        private int? GetUserIdByUsername(string username)
        {
            try
            {
                using var connection = new System.Data.Odbc.OdbcConnection(_userService._connectionString);
                connection.Open();

                var command = new System.Data.Odbc.OdbcCommand("SELECT UserId FROM Users WHERE Username = ?", connection);
                command.Parameters.AddWithValue("@username", username);
                var result = command.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}