using System.Data;

namespace ArtcastaWebApi.Models
{
    public class Table
    {
        public int TableId { get; set; }
        public string SystemTableName { get; set; } = "";
        public string TableName { get; set; } = "";
        public int CategoryId { get; set; }
        public int Ord { get; set; }
        public DataTable Data { get; set; } = new DataTable();
        public List<TableAttribute> Attributes { get; set; } = new List<TableAttribute>();

    }
}
