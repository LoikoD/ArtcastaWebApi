namespace ArtcastaWebApi.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        //public List<int> AccessTableIds { get; set; } = new List<int>();
        public List<AccessCategory> AccessCategories { get; set; } = new List<AccessCategory>();
        public List<int> AccessPointIds { get; set; } = new List<int>();
    }
}
