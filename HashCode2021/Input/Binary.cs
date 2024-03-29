﻿namespace HashCode2021.Input
{
    public class Binary : ICloneable
    {
        public int Id { get; set; }
        public List<Service> Services { get; set; }
        public bool Done { get; set; }
        public int NotAvailableUntil { get; set; } //handle not selected while moving services
        public int EngineerWorkingUntil { get; set; }
        public Binary(int Id)
        {
            this.Id = Id;
            Services = new List<Service>();
            this.Done = false;
            this.NotAvailableUntil = 0;
            this.EngineerWorkingUntil = 0;
        }

        public Binary()
        {

        }

        public Binary Clone()
        {
            var binary = new Binary(this.Id)
            {
                Id = this.Id,
                NotAvailableUntil = this.NotAvailableUntil,
                EngineerWorkingUntil = this.EngineerWorkingUntil
            };
            this.Services.ForEach(x => binary.Services.Add(new Service(x.Name) { Name = x.Name }));
            return binary;
        }
        object ICloneable.Clone()
        {
            return new Binary()
            {
                Services = this.Services,
                Id = this.Id,
                NotAvailableUntil = this.NotAvailableUntil,
                EngineerWorkingUntil = this.EngineerWorkingUntil
            };
        }
    }
}
