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
    public class RolesController : ControllerBase
    {
        private readonly IRolesService _rolesService;
        public RolesController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public ActionResult Get()
        {
            List<Role> roles;
            try
            {
                roles = _rolesService.GetRoles();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new JsonResult(roles);
        }

        [HttpPut("{roleId}")]
        public ActionResult Put(int roleId, Role role)
        {
            if (roleId != role.RoleId)
            {
                return new BadRequestResult();
            }

            try
            {
                _rolesService.UpdateRole(role);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }

        [HttpPost]
        public ActionResult Post(Role role)
        {
            if (string.IsNullOrEmpty(role.RoleName))
            {
                return new BadRequestResult();
            }

            try
            {
                _rolesService.CreateRole(role);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }

    }
}
