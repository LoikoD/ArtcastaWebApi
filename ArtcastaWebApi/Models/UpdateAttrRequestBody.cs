namespace ArtcastaWebApi.Models
{
    public class UpdateAttrRequestBody
    {
        public bool typeChanged { get; set; }
        public TableAttribute attribute { get; set; } = new TableAttribute();
    }
}
