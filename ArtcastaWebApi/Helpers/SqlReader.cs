using System.Data.SqlClient;

namespace ArtcastaWebApi.Helpers
{
    public static class SqlReader
    {
        public static string SafeGetString(SqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetString(colIndex);
            return string.Empty;
        }
        public static int? GetNullableInt(SqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetInt32(colIndex);
            return null;
        }
    }
}
