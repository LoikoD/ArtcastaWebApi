using ArtcastaWebApi.Models;
using ArtcastaWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Claims;

namespace ArtcastaWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public TokenController(IConfiguration config, ITokenService tokenService)
        {
            _config = config;
            _tokenService = tokenService;
        }

        [HttpGet]
        [Route("refresh")]
        public ActionResult Refresh()
        {
            if (string.IsNullOrEmpty(HttpContext.Request.Cookies["refresh_token"]))
            {
                return new BadRequestResult();
            }

            string refreshToken = HttpContext.Request.Cookies["refresh_token"];
            string username = HttpContext.Request.Cookies["username"];


            string query = "select top 1 UserId, Username, RoleId, RefreshToken, RefreshTokenExpiryTime from dbo.Users where Username = @username;";

            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            SqlDataReader myReader;
            User user = new User();
            string oldRefreshToken = string.Empty;
            DateTime refreshTokenExpiryTime = DateTime.MinValue;
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myConn.Open();
                        myCommand.Parameters.AddWithValue("@username", username);
                        myReader = myCommand.ExecuteReader();
                        while (myReader.Read())
                        {
                            user = new User
                            {
                                UserId = (int)myReader["UserId"],
                                Username = myReader["Username"].ToString(),
                                RoleId = (int)myReader["RoleId"]
                            };
                            oldRefreshToken = myReader.GetString(myReader.GetOrdinal("RefreshToken"));
                            refreshTokenExpiryTime = myReader.GetDateTime(myReader.GetOrdinal("RefreshTokenExpiryTime"));
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

            if (user is null || oldRefreshToken != refreshToken || refreshTokenExpiryTime <= DateTime.Now)
            {
                return new BadRequestResult();
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
                new Claim(ClaimTypes.Name, user.Username)
            };

            var newAccessToken = _tokenService.GenerateAccessToken(claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenEpxiryTime = DateTime.Now.AddDays(double.Parse(_config.GetSection("Jwt")["RefreshTokenDaysLife"]));

            // Update RefreshToken
            string insertQuery = "update dbo.Users set RefreshToken = @refreshToken, RefreshTokenExpiryTime = @refreshTokenEpxiryTime where UserId = @userId;";
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    using (SqlCommand myCommand = new SqlCommand(insertQuery, myConn))
                    {
                        myConn.Open();
                        myCommand.Parameters.AddWithValue("@refreshToken", newRefreshToken);
                        myCommand.Parameters.AddWithValue("@refreshTokenEpxiryTime", refreshTokenEpxiryTime);
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
                AccessToken = newAccessToken
            };

            Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });
            Response.Cookies.Append("username", user.Username, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.None, Secure = true });

            return new JsonResult(requestResult);

        }

        [HttpDelete]
        public void Delete()
        {
            if (Request.Cookies["refresh_token"] != null)
            {
                Response.Cookies.Delete("refresh_token");
            }
            if (Request.Cookies["username"] != null)
            {
                Response.Cookies.Delete("username");
            }
        }

    }
}
