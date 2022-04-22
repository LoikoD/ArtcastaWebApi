namespace ArtcastaWebApi.Models
{
    public class AttributeType
    {
        public int AttrTypeId { get; set; }
        public string SystemAttrTypeName { get; set; } = string.Empty;
        public string AttrTypeName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}