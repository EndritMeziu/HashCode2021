namespace HashCode2021.Input
{
    public class Features
    {
        public Features(string Name,int Difficulty,int NumServices,int NumUserBenefit)
        {
            this.Name = Name;
            this.Difficulty = Difficulty;
            this.NumServices = NumServices;
            this.NumUsersBenefit = NumUserBenefit;
            this.Services = new List<Service>();
        }
        public string Name { get; set; }
        public int Difficulty { get; set; }
        public int NumServices { get; set; }
        public int NumUsersBenefit { get; set; }
        public List<Service> Services { get; set; }
    }
}
