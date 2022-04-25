using ArtcastaWebApi.Models;
using ArtcastaWebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArtcastaWebApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessPointsController : ControllerBase
    {
        private readonly IAccessPointsService _accessPointsService;
        public AccessPointsController(IAccessPointsService accessPointsService)
        {
            _accessPointsService = accessPointsService;
        }

        [HttpGet]
        public ActionResult Get()
        {
            List<AccessPoint> accessPoints;
            try
            {
                accessPoints = _accessPointsService.GetAllAccessPoints();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new JsonResult(accessPoints);
        }

    }
}
