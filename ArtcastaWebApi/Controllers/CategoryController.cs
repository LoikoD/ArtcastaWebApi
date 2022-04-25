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
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAccessPointsService _accessPointsService;
        private readonly IRolesService _rolesService;

        public CategoryController(IConfiguration configuration, IAccessPointsService accessPointsService, IRolesService rolesService)
        {
            _config = configuration;
            _accessPointsService = accessPointsService;
            _rolesService = rolesService;
        }

        [HttpGet]
        public JsonResult Get()
        {
            string query = @"select CategoryId, CategoryName, Ord from dbo.Categories order by Ord";

            DataTable table = new DataTable();
            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            SqlDataReader myReader;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    myConn.Close();
                }
            }

            return new JsonResult(table);
        }

        [HttpPost]
        public ActionResult Post(Category category)
        {
            string query = "insert into dbo.Categories (CategoryName, Ord) output INSERTED.CategoryId values (@categoryName, (select (coalesce(max(Ord),0) + 1) from dbo.Categories));";

            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            int categoryId = 0;

            //try
            //{

                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@categoryName", category.CategoryName);
                        categoryId = (int)myCommand.ExecuteScalar();

                    }
                    myConn.Close();
                }
                if (categoryId == 0)
                {
                    return new BadRequestResult();
                }

                // Добавление доступа к новой категории ролям, у которых есть доступ к модулю "Конфигурация"
                int confAccessPointId = _accessPointsService.GetAccessPointIdByName("Конфигурация");
                
                List<int> rolesWithConfAccessIds = _rolesService.GetRoleIdsByAccessPointId(confAccessPointId);

                AccessCategory accessCategory = new AccessCategory
                {
                    Id = categoryId,
                    CanEdit = true
                };
                foreach (int roleId in rolesWithConfAccessIds)
                {
                    _rolesService.AddAccessCategoryToRole(accessCategory, roleId);
                }
            //}
            //catch (Exception ex)
            //{
                
            //    //return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            //}

            return new OkResult();
        }

        [HttpPut]
        public ActionResult Put(List<Category> categoryList)
        {
            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            string query = "update dbo.Categories set CategoryName = @categoryName, Ord = @ord where CategoryId = @categoryId;";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                foreach (Category category in categoryList)
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@categoryName", category.CategoryName);
                        myCommand.Parameters.AddWithValue("@ord", category.Ord);
                        myCommand.Parameters.AddWithValue("@categoryId", category.CategoryId);
                        myCommand.ExecuteNonQuery();

                    }
                }
                myConn.Close();

            }

            return new OkResult();
        }

        [HttpPut("{categoryId}")]
        public ActionResult Put(int categoryId, Category category)
        {
            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            string query = "update dbo.Categories set CategoryName = @categoryName, Ord = @ord where CategoryId = @categoryId;";
            int updatedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryName", category.CategoryName);
                    myCommand.Parameters.AddWithValue("@ord", category.Ord);
                    myCommand.Parameters.AddWithValue("@categoryId", categoryId);
                    updatedRows = myCommand.ExecuteNonQuery();

                }
                myConn.Close();

            }

            if (updatedRows == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
        }

        [HttpDelete("{categoryId}")]
        public ActionResult Delete(int categoryId)
        {
            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");

            // TODO: deleting all tables where CategoryId = @categoryId

            _rolesService.DeleteAccessCategoriesByCategoryId(categoryId);

            string updateOrdQuery = "update dbo.Categories set Ord = Ord - 1 where Ord > (select Ord from dbo.Categories where CategoryId = @categoryId);";
            string deleteQuery = "delete from dbo.Categories where CategoryId = @categoryId;";
            int deletedRows = 0;

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(updateOrdQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryId", categoryId);
                    myCommand.ExecuteNonQuery();

                }
                using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryId", categoryId);
                    deletedRows = myCommand.ExecuteNonQuery();

                }
                myConn.Close();

            }

            if (deletedRows == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
        }

    }
}
