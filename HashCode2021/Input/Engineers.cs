namespace HashCode2021.Input
{
    public class Engineers
    {
        public int Id { get; set; }
        public Engineers(int Id)
        {
            this.Id = Id;
            this.Operations = new List<EnginnerOperation>();
        }

        public int AvailableDays { get; set; }
        public List<EnginnerOperation> Operations { get; set; }
    }
}
