namespace HashCode2021.Input
{
    public class Engineers
    {
        public int Id { get; set; }
        public Engineers(int Id)
        {
            this.Id = Id;
            this.Operations = new List<EnginnerOperation>();
            this.BusyUntil = 0;
        }

        public int AvailableDays { get; set; }
        public int BusyUntil { get; set; }
        public List<EnginnerOperation> Operations { get; set; }

        public Engineers Clone()
        {
            var engineer = new Engineers(this.Id)
            {
                AvailableDays = this.AvailableDays,
                BusyUntil = this.BusyUntil,
                Id = this.Id
            };
            this.Operations.ForEach(x => engineer.Operations.Add(x.Clone()));

            return engineer;
        }
    }
}
