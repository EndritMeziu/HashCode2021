namespace HashCode2021.Input
{
    public class Binary
    {
        public int Id { get; set; }
        public List<Service> Services { get; set; }
        public Binary(int Id)
        {
            this.Id = Id;
            Services = new List<Service>();
        }
    }
}
