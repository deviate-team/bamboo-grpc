using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Authentication;
using System.Text;

namespace bamboo_grpc.Managers;

public static class JwtAuthenticationManager
{
  public const string JWT_TOKEN_KEY = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTY3Nzc1NzY2NywiaWF0IjoxNjc3NzU3NjY3fQ.59MdX9RDSTvr7wrK4RxkQuODdsmFFlTluELYbczPuXs";
  public const int JWT_TOKEN_EXPIRATION = 24;
  public static AuthenticateResponse GenerateToken(string user_id, string role)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(JWT_TOKEN_KEY);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(new Claim[]
      {
        new Claim(ClaimTypes.Role, role),
        new Claim(ClaimTypes.Name, user_id)
      }),
      Expires = System.DateTime.UtcNow.AddHours(JWT_TOKEN_EXPIRATION),
      SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return new AuthenticateResponse
    {
      AccessToken = tokenString
    };
  }

  public static string DecodeToken(string token)
  {
    token = token.Replace("Bearer ", "");
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(JWT_TOKEN_KEY);
    var tokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(key),
      ValidateIssuer = false,
      ValidateAudience = false,
      ValidateLifetime = false,
      ClockSkew = System.TimeSpan.Zero
    };
    SecurityToken securityToken;
    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
    var jwtSecurityToken = securityToken as JwtSecurityToken;
    if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
    {
      throw new SecurityTokenException("Invalid token");
    }
    var userId = principal.FindFirst(ClaimTypes.Name)?.Value;
    return userId;
  }
}