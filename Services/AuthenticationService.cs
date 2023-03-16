using Grpc.Core;
using Authentication;
using bamboo_grpc.Interfaces;

namespace bamboo_grpc.Services;

public class AuthenticationService : Authentication.Authentication.AuthenticationBase
{
  private readonly IUsersRepository _repository;
  private readonly ILogger<AuthenticationService> _logger;
  public AuthenticationService(IUsersRepository repository, ILogger<AuthenticationService> logger)
  {
    this._repository = repository;
    this._logger = logger;
  }

  public override async Task<AuthenticateResponse> Login(LoginRequest request, ServerCallContext context)
  {
    try
    {
      if (string.IsNullOrEmpty(request.Username))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Username is required"));
      }
      if (string.IsNullOrEmpty(request.Password))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is required"));
      }

      var user = await _repository.GetUserByUsername(request.Username);
      if (user == null)
      {
        throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid username"));
      }
      if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
      {
        throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid password"));
      }

      var reply = JwtAuthentication.GenerateToken(user.Id, "User");
      return reply;
    }
    catch (Exception ex)
    {
      throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
      _logger.LogError($"Failed to login: {ex}");
    }
  }

  public override async Task<AuthenticateResponse> Register(RegisterRequest request, ServerCallContext context)
  {
    try
    {
      if (string.IsNullOrEmpty(request.Username))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Username is required"));
      }
      if (string.IsNullOrEmpty(request.Password))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is required"));
      }
      if (string.IsNullOrEmpty(request.Email))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Email is required"));
      }
      if (!IsValidEmail(request.Email))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid email"));
      }
      if (!IsValidPassword(request.Password))
      {
        throw new RpcException(new Status(StatusCode.InvalidArgument, "Password must be at least 6 characters"));
      }

      if (await _repository.GetUserByUsername(request.Username) != null)
      {
        throw new RpcException(new Status(StatusCode.AlreadyExists, "Username is already taken"));
      }
      if (await _repository.GetUserByEmail(request.Email) != null)
      {
        throw new RpcException(new Status(StatusCode.AlreadyExists, "Email is already taken"));
      }

      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
      await _repository.InsertUser(request.Username, hashedPassword, request.Email, "User");
      var user = await _repository.GetUserByUsername(request.Username);

      var reply = JwtAuthentication.GenerateToken(user.Id, "User");
      return reply;
    }
    catch (Exception ex)
    {
      throw new RpcException(new Status(StatusCode.Unavailable, "Error"));
    }
  }

  private bool IsValidEmail(string email)
  {
    var trimmedEmail = email.Trim();

    if (trimmedEmail.EndsWith("."))
    {
      return false;
    }
    try
    {
      var addr = new System.Net.Mail.MailAddress(email);
      return addr.Address == trimmedEmail;
    }
    catch
    {
      return false;
    }
  }

  private bool IsValidPassword(string password)
  {
    var trimmedPassword = password.Trim();
    if (trimmedPassword.Length < 6)
    {
      return false;
    }
    return true;
  }
}