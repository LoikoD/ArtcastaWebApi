using ArtcastaWebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;


namespace ArtcastaWebApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        public UserController(IConfiguration configuration)
        {
            _config = configuration;
        }

        // GET: api/<UserController>
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/<UserController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST api/<UserController>
        [HttpPost]
        public ActionResult Post([FromBody] User user)
        {

            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");

            // Check username
            if (user?.Username is not null)
            {
                string checkUserQuery = "select count(*) from dbo.Users where Username = @username;";

                try
                {
                    using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                    {
                        using (SqlCommand myCommand = new SqlCommand(checkUserQuery, myConn))
                        {
                            myConn.Open();
                            myCommand.Parameters.AddWithValue("@username", user.Username);
                            int userCount = (int)myCommand.ExecuteScalar();
                            myConn.Close();
                            if (userCount != 0)
                            {
                                // Username is already taken
                                return new ConflictResult();
                            }
                        }
                    }
                } catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                }
            } else
            {
                // Username is null
                return new BadRequestResult();
            }

            // Create new user
            string insertQuery = "insert into dbo.Users values (@username, @password, @roleid);";
            try
            {
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    using (SqlCommand myCommand = new SqlCommand(insertQuery, myConn))
                    {
                        string generalSalt = _config.GetSection("BCrypt")["GeneralSalt"];
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password + generalSalt);

                        myConn.Open();
                        myCommand.Parameters.AddWithValue("@username", user.Username);
                        myCommand.Parameters.AddWithValue("@password", hashedPassword);
                        myCommand.Parameters.AddWithValue("@roleid", user.RoleId);
                        int insertedRows = myCommand.ExecuteNonQuery();
                        myConn.Close();

                        if (insertedRows == 1)
                        {
                            // User created
                            return new OkResult();
                        }

                    }
                }
            } catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "uknown error");
        }

        // PUT api/<UserController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<UserController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
