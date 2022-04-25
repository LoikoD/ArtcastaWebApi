using ArtcastaWebApi.Models;
using System.Data.SqlClient;

namespace ArtcastaWebApi.Services
{
    public class AccessPointsService : IAccessPointsService
    {
        private readonly string sqlDataSource;

        public AccessPointsService(IConfiguration config)
        {
            sqlDataSource = config.GetConnectionString("ArtcastaAppCon");
        }
        public List<AccessPoint> GetAllAccessPoints()
        {
            List<AccessPoint> accessPoints = new List<AccessPoint>();
            string query = "select AccessPointId, AccessPointName from dbo.AccessPoints;";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        AccessPoint ap = new AccessPoint
                        {
                            AccessPointId = myReader.GetInt32(myReader.GetOrdinal("AccessPointId")),
                            AccessPointName = myReader.GetString(myReader.GetOrdinal("AccessPointName"))
                        };
                        accessPoints.Add(ap);
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return accessPoints;
        }

        public int GetAccessPointIdByName(string name)
        {
            string query = "select AccessPointId from dbo.AccessPoints where AccessPointName = @accessPointName;";
            int accessPointId = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@accessPointName", name);
                    accessPointId = (int)myCommand.ExecuteScalar();
                }
                myConn.Close();
            }
            return accessPointId;
        }
    }
}
