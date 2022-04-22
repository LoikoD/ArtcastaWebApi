namespace ArtcastaWebApi.Models
{
    public class TableAttribute
    {
        public int AttrId { get; set; }
        public int TableId { get; set; }
        public string SystemAttrName { get; set; } = "";
        public string AttrName { get; set; } = "";
        public int PkFlag { get; set; }
        public int AttrTypeId { get; set; }
        public int? AttrTypeProp1 { get; set; }
        public int? AttrTypeProp2 { get; set; }
        public int Ord { get; set; }
    }
}
