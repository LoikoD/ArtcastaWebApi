using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using ArtcastaWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ArtcastaWebApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CategoryController(IConfiguration configuration)
        {
            _config = configuration;
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
            string query = "insert into dbo.Categories (CategoryName, Ord) values (@categoryName, (select (coalesce(max(Ord),0) + 1) from dbo.Categories));";

            string sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
            int insertedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryName", category.CategoryName);
                    insertedRows = myCommand.ExecuteNonQuery();

                }
                myConn.Close();
            }
            if (insertedRows == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
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

            // TODO: deleteing all tables, that belongs to this category

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
