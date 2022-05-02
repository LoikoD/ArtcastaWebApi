using ArtcastaWebApi.Models;
using ArtcastaWebApi.Services;
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
        private readonly IUserService _userService;
        public UserController(IConfiguration configuration, IUserService userService)
        {
            _config = configuration;
            _userService = userService;
        }

        // GET: api/<UserController>
        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                List<User> users = _userService.GetUsers();
                return new JsonResult(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // POST api/<UserController>
        [HttpPost]
        public ActionResult Post([FromBody] User user)
        {
            try
            {
                _userService.CreateUser(user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }



        // PUT api/<UserController>/5
        [HttpPut("{userId}")]
        public ActionResult Put(int userId, [FromBody] User user)
        {
            if (userId != user.UserId)
            {
                return BadRequest();
            }
            try
            {
                _userService.UpdateUserInfo(user);
                if (!string.IsNullOrEmpty(user.Password))
                {
                    _userService.UpdatePassword(userId, user.Password);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();

        }

        // DELETE api/<UserController>/5
        [HttpDelete("{userId}")]
        public ActionResult Delete(int userId)
        {
            try
            {
                _userService.DeleteUser(userId);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }
    }
}
