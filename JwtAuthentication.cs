using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Authentication;
using System.Text;

public static class JwtAuthentication
{

  public static string JWT_TOKEN_KEY =  Environment.GetEnvironmentVariable("JWT_TOKEN_KEY");
  public static int JWT_TOKEN_EXPIRE = int.Parse(Environment.GetEnvironmentVariable("JWT_TOKEN_EXPIRE"));
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
      Expires = System.DateTime.UtcNow.AddHours(JWT_TOKEN_EXPIRE),
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