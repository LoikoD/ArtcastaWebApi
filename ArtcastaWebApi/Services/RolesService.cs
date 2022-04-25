using ArtcastaWebApi.Models;
using System.Data.SqlClient;

namespace ArtcastaWebApi.Services
{
    public class RolesService : IRolesService
    {
        private readonly string sqlDataSource;

        public RolesService(IConfiguration config)
        {
            sqlDataSource = config.GetConnectionString("ArtcastaAppCon");
        }

        public List<AccessCategory> GetAccessCategoriesByRoleId(int roleId)
        {
            string query = "select r.RoleId, rac.CategoryId, rac.CanEdit from dbo.Roles r inner join dbo.RolesAccessCategories rac on r.RoleId = rac.RoleId where r.RoleId = @roleId;";
            List<AccessCategory> accessCategories = new List<AccessCategory>();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleId", roleId);
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        AccessCategory accessCategory = new AccessCategory
                        {
                            Id = myReader.GetInt32(myReader.GetOrdinal("CategoryId")),
                            CanEdit = myReader.GetBoolean(myReader.GetOrdinal("CanEdit"))
                        };
                        accessCategories.Add(accessCategory);
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return accessCategories;
        }

        public List<int> GetAccessPointsByRoleId(int roleId)
        {
            string query = "select r.RoleId, rap.AccessPointId from dbo.Roles r inner join dbo.RolesAccessPoints rap on r.RoleId = rap.RoleId where r.RoleId = @roleId;";
            List<int> accessPoints = new List<int>();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleId", roleId);
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        accessPoints.Add(myReader.GetInt32(myReader.GetOrdinal("AccessPointId")));
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return accessPoints;
        }

        /*public List<int> GetAccessTablesByRoleId(int roleId)
        {
            string query = "select r.RoleId, rat.TableId from dbo.Roles r inner join dbo.RolesAccessTables rat on r.RoleId = rat.RoleId where r.RoleId = @roleId;";
            List<int> accessTables = new List<int>();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleId", roleId);
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        accessTables.Add(myReader.GetInt32(myReader.GetOrdinal("TableId")));
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return accessTables;
        }*/

        public List<Role> GetRoles()
        {
            string query = "select RoleId, RoleName from dbo.Roles;";
            List<Role> roles = new List<Role>();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        Role role = new Role
                        {
                            RoleId = myReader.GetInt32(myReader.GetOrdinal("RoleId")),
                            RoleName = myReader.GetString(myReader.GetOrdinal("RoleName"))
                        };
                        role.AccessCategories = GetAccessCategoriesByRoleId(role.RoleId);
                        //role.AccessTableIds = GetAccessTablesByRoleId(role.RoleId);
                        role.AccessPointIds = GetAccessPointsByRoleId(role.RoleId);
                        roles.Add(role);
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return roles;
        }

        private void UpdateAccessCategoriesByRoleId(List<AccessCategory> newList, int roleId)
        {
            if (newList.Count == 0)
            {
                string deleteQuery = "delete from dbo.RolesAccessCategories where RoleId = @roleId;";
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
            else
            {
                string query = "merge RolesAccessCategories as target using ( select * from ( values ";
                foreach (AccessCategory ac in newList)
                {
                    query += $"(@roleId, @categoryId{ac.Id}, @canEdit{ac.Id}), ";
                }
                query = query.Remove(query.Length - 2);
                query += ") as s (RoleId, CategoryId, CanEdit) ) as source on target.RoleId = source.RoleId and target.CategoryId = source.CategoryId " +
                    "WHEN NOT MATCHED By target THEN INSERT (RoleId, CategoryId, CanEdit) VALUES (source.RoleId, source.CategoryId, source.CanEdit) " +
                    "WHEN MATCHED THEN UPDATE SET target.CanEdit = source.CanEdit " +
                    "WHEN NOT MATCHED By source AND target.RoleId = @roleId THEN DELETE;";
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        foreach (AccessCategory ac in newList)
                        {
                            myCommand.Parameters.AddWithValue($"@categoryId{ac.Id}", ac.Id);
                            myCommand.Parameters.AddWithValue($"@canEdit{ac.Id}", ac.CanEdit);
                        }
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
        }
        private void UpdateAccessPointsByRoleId(List<int> newList, int roleId)
        {
            if (newList.Count == 0)
            {
                string deleteQuery = "delete from dbo.RolesAccessPoints where RoleId = @roleId;";
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
            else
            {
                string query = "merge RolesAccessPoints as target using ( select * from ( values ";
                foreach (int id in newList)
                {
                    query += $"(@roleId, @accessPointId{id}), ";
                }
                query = query.Remove(query.Length - 2);
                query += ") as s (RoleId, AccessPointId) ) as source on target.RoleId = source.RoleId and target.AccessPointId = source.AccessPointId " +
                    "WHEN NOT MATCHED By target THEN INSERT (RoleId, AccessPointId) VALUES (source.RoleId, source.AccessPointId) " +
                    "WHEN NOT MATCHED By source AND target.RoleId = @roleId THEN DELETE;";
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        foreach (int id in newList)
                        {
                            myCommand.Parameters.AddWithValue($"@accessPointId{id}", id);
                        }
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
        }

        private void UpdateRoleName(string newName, int roleId)
        {
            string query = "update dbo.Roles set RoleName = @roleName where RoleId = @roleId;";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleName", newName);
                    myCommand.Parameters.AddWithValue("@roleId", roleId);
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }

        private int InsertRole(string newName)
        {
            string query = "insert into dbo.Roles (RoleName) output INSERTED.RoleId values (@roleName);";
            int newId = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleName", newName);
                    newId = (int)myCommand.ExecuteScalar();
                }
                myConn.Close();
            }
            return newId;
        }
        private void InsertAccessCategoriesByRoleId(List<AccessCategory> newList, int roleId)
        {
            if (newList.Count == 0)
            {
                return;
            }
            else
            {
                string query = "insert into dbo.RolesAccessCategories (RoleId, CategoryId, CanEdit) values ";
                foreach (AccessCategory ac in newList)
                {
                    query += $"(@roleId, @categoryId{ac.Id}, @canEdit{ac.Id}), ";
                }
                query = query.Remove(query.Length - 2);
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        foreach (AccessCategory ac in newList)
                        {
                            myCommand.Parameters.AddWithValue($"@categoryId{ac.Id}", ac.Id);
                            myCommand.Parameters.AddWithValue($"@canEdit{ac.Id}", ac.CanEdit);
                        }
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
        }

        private void InsertAccessPointsByRoleId(List<int> newList, int roleId)
        {
            if (newList.Count == 0)
            {
                return;
            }
            else
            {
                string query = "insert into dbo.RolesAccessPoints (RoleId, AccessPointId) values ";
                foreach (int id in newList)
                {
                    query += $"(@roleId, @accessPointId{id}), ";
                }
                query = query.Remove(query.Length - 2);
                using (SqlConnection myConn = new SqlConnection(sqlDataSource))
                {
                    myConn.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@roleId", roleId);
                        foreach (int id in newList)
                        {
                            myCommand.Parameters.AddWithValue($"@accessPointId{id}", id);
                        }
                        myCommand.ExecuteNonQuery();
                    }
                    myConn.Close();
                }
            }
        }

        public void UpdateRole(Role role)
        {
            UpdateRoleName(role.RoleName, role.RoleId);
            UpdateAccessCategoriesByRoleId(role.AccessCategories, role.RoleId);
            UpdateAccessPointsByRoleId(role.AccessPointIds, role.RoleId);
        }

        public void CreateRole(Role role)
        {
            int roleId = InsertRole(role.RoleName);
            if (roleId == 0)
            {
                throw new Exception("не удалось вставить строку в Roles");
            }
            InsertAccessCategoriesByRoleId(role.AccessCategories, roleId);
            InsertAccessPointsByRoleId(role.AccessPointIds, roleId);
        }

        public void AddAccessCategoryToRole(AccessCategory accessCategory, int roleId)
        {
            string query = "insert into dbo.RolesAccessCategories (RoleId, CategoryId, CanEdit) values (@roleId, @categoryId, @canEdit);";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@roleId", roleId);
                    myCommand.Parameters.AddWithValue("@categoryId", accessCategory.Id);
                    myCommand.Parameters.AddWithValue("@canEdit", accessCategory.CanEdit);
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }

        //private int GetRoleIdByName(string name)
        //{
        //    string query = "select RoleId from dbo.Roles where RoleName = @roleName;";
        //    int roleId = 0;
        //    using (SqlConnection myConn = new SqlConnection(sqlDataSource))
        //    {
        //        myConn.Open();
        //        using (SqlCommand myCommand = new SqlCommand(query, myConn))
        //        {
        //            myCommand.Parameters.AddWithValue("@roleName", name);
        //            roleId = (int)myCommand.ExecuteScalar();
        //        }
        //        myConn.Close();
        //    }
        //    return roleId;
        //}

        public List<int> GetRoleIdsByAccessPointId(int accessPointId)
        {
            List<int> roleIdList = new List<int>();
            string query = "select distinct RoleId from dbo.RolesAccessPoints where AccessPointId = @accessPointId;";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@accessPointId", accessPointId);
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        roleIdList.Add(myReader.GetInt32(myReader.GetOrdinal("RoleId")));
                    }
                }
                myConn.Close();
            }
            return roleIdList;
        }

    }
}
