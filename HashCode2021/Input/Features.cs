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
        public Features()
        {

        }

        public string Name { get; set; }
        public int Difficulty { get; set; }
        public int NumServices { get; set; }
        public int NumUsersBenefit { get; set; }
        public List<Service> Services { get; set; }

        public Features Clone()
        {
            return new Features()
            {
                Name = this.Name,
                NumServices = this.NumServices,
                Difficulty = this.Difficulty,
                NumUsersBenefit = this.NumUsersBenefit,
                Services = this.Services
            };
        }
    }
}
