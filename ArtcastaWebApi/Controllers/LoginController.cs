using ArtcastaWebApi.Models;
using ArtcastaWebApi.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtcastaWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;

        public LoginController(IConfiguration configuration, ITokenService tokenService)
        {
            _config = configuration;
            _tokenService = tokenService;
        }

        [HttpPost]
        public ActionResult Post([FromBody] UserLogin creds)
        {
            if (string.IsNullOrEmpty(creds?.Username) || string.IsNullOrEmpty(creds?.Password))
            {
                return new BadRequestResult();
            }

            string query = "select top 1 UserId, Username, Password, u.RoleId, r.RoleName" +
                    " from dbo.Users u inner join dbo.Roles r on r.RoleId = u.RoleId where Username = @username;";

            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            SqlDataReader myReader;
            string hashedPassword = "";
            User user = new User();
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@username", creds.Username);
                        myReader = myCommand.ExecuteReader();
                        while (myReader.Read())
                        {
                            user = new User
                            {
                                UserId = (int)myReader["UserId"],
                                Username = myReader["Username"].ToString(),
                                RoleId = (int)myReader["RoleId"],
                                RoleName = myReader["RoleName"].ToString()
                            };
                            hashedPassword = myReader["Password"].ToString();
                        }
                        myReader.Close();
                    }
                    myConn.Close();
                }
            } catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            string generalSalt = _config.GetSection("BCrypt")["GeneralSalt"];
            if (user is null || string.IsNullOrEmpty(hashedPassword) || !BCrypt.Net.BCrypt.Verify(creds.Password + generalSalt, hashedPassword))
            {
                // Username or password is incorrect
                return new UnauthorizedResult();
            }

            string accessPointsQuery = "select AccessPointName from dbo.AccessPoints ap inner join dbo.RolesAccessPoints rap on ap.AccessPointId = rap.AccessPointId where RoleId = @roleId;";
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(accessPointsQuery, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", user.RoleId);
                        myReader = myCommand.ExecuteReader();
                        while (myReader.Read())
                        {
                            user.AccessPoints.Add(myReader.GetString(myReader.GetOrdinal("AccessPointName")));
                        }
                        myReader.Close();
                    }
                    myConn.Close();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleName)
            };

            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenEpxiryTime = DateTime.Now.AddDays(double.Parse(_config.GetSection("Jwt")["RefreshTokenDaysLife"]));

            // Update RefreshToken
            string insertQuery = "update dbo.Users set RefreshToken = @refreshToken, RefreshTokenExpiryTime = @refreshTokenExpiryTime where UserId = @userId;";
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    using (SqlCommand myCommand = new SqlCommand(insertQuery, myConn))
                    {
                        myConn.Open();
                        myCommand.Parameters.AddWithValue("@refreshToken", refreshToken);
                        myCommand.Parameters.AddWithValue("@refreshTokenExpiryTime", refreshTokenEpxiryTime);
                        myCommand.Parameters.AddWithValue("@userId", user.UserId);
                        int insertedRows = myCommand.ExecuteNonQuery();
                        myConn.Close();

                        if (insertedRows != 1)
                        {
                            // RefreshToken Not Updated
                            return StatusCode(StatusCodes.Status500InternalServerError, "RefreshToken Not Updated");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }


            var requestResult = new
            {
                User = user,
                AccessToken = accessToken
            };

            //Response.Cookies.Append("access_token", accessToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = true });
            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
            Response.Cookies.Append("username", user.Username, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });

            return new JsonResult(requestResult);
        }
    }
}
