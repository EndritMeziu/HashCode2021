namespace HashCode2021.Input
{
    public class Service
    {
        public string Name { get; set; }
        public Service(string Name)
        {
            this.Name = Name;
        }

        public Service Clone()
        {
            return new Service(this.Name)
            {
                Name = this.Name
            };
        }
    }
}
