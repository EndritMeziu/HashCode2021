﻿using HashCode2021.Input;

namespace HashCode2021
{
    public class InputModel
    {
        public InputModel()
        {
            Engineers = new List<Engineers>();
            Services = new List<Service>();
            Binaries = new List<Binary>();
            Features = new List<Features>();
        }
        public int TimeLimitDays { get; set; }
        public int NumEngineers { get; set; }
        public int NumServices { get; set; }
        public int NumBinaries { get; set; }
        public int NumFeatures { get; set; }
        public List<Engineers> Engineers { get; set; }
        public List<Service> Services { get; set; }
        public List<Binary> Binaries { get; set; }
        public List<Features> Features { get; set; }
        public int TimeToCreateBinary { get; set; }

        public InputModel Clone()
        {
            InputModel inputModel = new InputModel();
            inputModel.TimeLimitDays = this.TimeLimitDays;
            inputModel.NumEngineers = this.NumEngineers;
            inputModel.NumBinaries = this.NumBinaries; 
            inputModel.NumFeatures = this.NumFeatures;
            inputModel.NumServices = this.NumServices;
            inputModel.TimeToCreateBinary = this.TimeToCreateBinary;
            this.Engineers.ForEach(x => inputModel.Engineers.Add(x.Clone()));
            this.Services.ForEach(x => inputModel.Services.Add(x.Clone()));
            this.Binaries.ForEach(x => inputModel.Binaries.Add(x.Clone()));
            this.Features.ForEach(x => inputModel.Features.Add(x.Clone()));

            return inputModel;
    }
    }
}
