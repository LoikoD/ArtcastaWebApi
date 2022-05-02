using ArtcastaWebApi.Models;
using System.Data.SqlClient;

namespace ArtcastaWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly string sqlDataSource;
        private readonly IConfiguration _config;

        public UserService(IConfiguration config)
        {
            _config = config;
            sqlDataSource = config.GetConnectionString("ArtcastaAppCon");
        }

        public List<User> GetUsers()
        {
            List<User> users = new List<User>();

            string query = "select UserId, Username, RoleId from dbo.Users order by UserId;";

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        User user = new User
                        {
                            UserId = (int)myReader["UserId"],
                            Username = myReader["Username"].ToString(),
                            RoleId = (int)myReader["RoleId"]
                        };
                        users.Add(user);
                    }
                    myReader.Close();
                }

                myConn.Close();
            }

            return users;
        }

        public void CreateUser(User newUser)
        {
            // Check username is not null
            if (string.IsNullOrEmpty(newUser?.Username))
            {
                // Username is null
                throw new Exception("Username is null");
            }
            string checkUserQuery = "select count(*) from dbo.Users where Username = @username;";

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                // Check username not exists
                using (SqlCommand myCommand = new SqlCommand(checkUserQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@username", newUser.Username);
                    int userCount = (int)myCommand.ExecuteScalar();
                    if (userCount != 0)
                    {
                        // Username is already taken
                        throw new Exception("Username is already taken");
                    }
                }

                // Create new user
                string insertQuery = "insert into dbo.Users (Username, Password, RoleId) values (@username, @password, @roleid);";
                using (SqlCommand myCommand = new SqlCommand(insertQuery, myConn))
                {
                    string generalSalt = _config.GetSection("BCrypt")["GeneralSalt"];
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUser.Password + generalSalt);

                    myCommand.Parameters.AddWithValue("@username", newUser.Username);
                    myCommand.Parameters.AddWithValue("@password", hashedPassword);
                    myCommand.Parameters.AddWithValue("@roleid", newUser.RoleId);
                    myCommand.ExecuteNonQuery();
                }

                myConn.Close();
            }
        }
        public void UpdateUserInfo(User newUser)
        {
            // Check username is not null
            if (string.IsNullOrEmpty(newUser?.Username))
            {
                // Username is null
                throw new Exception("Username is null");
            }
            string checkUserQuery = "select count(*) from dbo.Users where Username = @username and UserId != @userId;";

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                // Check username not exists
                using (SqlCommand myCommand = new SqlCommand(checkUserQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@username", newUser.Username);
                    myCommand.Parameters.AddWithValue("@userId", newUser.UserId);
                    int userCount = (int)myCommand.ExecuteScalar();
                    if (userCount != 0)
                    {
                        // Username is already taken
                        throw new Exception("Username is already taken");
                    }
                }

                string updateQuery = "update dbo.Users set Username = @username, RoleId = @roleid where UserId = @userId;";
                using (SqlCommand myCommand = new SqlCommand(updateQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@username", newUser.Username);
                    myCommand.Parameters.AddWithValue("@roleid", newUser.RoleId);
                    myCommand.Parameters.AddWithValue("@userId", newUser.UserId);
                    myCommand.ExecuteNonQuery();
                }

                myConn.Close();
            }
        }

        public void UpdatePassword(int userId, string password)
        {
            // Check password is not null
            if (string.IsNullOrEmpty(password))
            {
                // Password is null
                throw new Exception("Password is null");
            }

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                string updateQuery = "update dbo.Users set Password = @password, RefreshToken = null where UserId = @userId;";
                using (SqlCommand myCommand = new SqlCommand(updateQuery, myConn))
                {
                    string generalSalt = _config.GetSection("BCrypt")["GeneralSalt"];
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password + generalSalt);

                    myCommand.Parameters.AddWithValue("@password", hashedPassword);
                    myCommand.Parameters.AddWithValue("@userId", userId);
                    myCommand.ExecuteNonQuery();
                }

                myConn.Close();
            }
        }

        public void DeleteUser(int userId)
        {
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                string deleteQuery = "delete from dbo.Users where UserId = @userId;";
                using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@userId", userId);
                    myCommand.ExecuteNonQuery();
                }

                myConn.Close();
            }
        }
    }
}
