using HashCode2021.Input;

namespace HashCode2021
{
    public class FeatureModel
    {
        public Features Feature { get; set; }
        public List<Binary> Binaries { get; set; }
        public List<string> Services { get; set; }
        public Dictionary<int,int> FeatureBinaryTime { get; set; }
        public int FeatureTimeBinary { get; set; }
        public FeatureModel Clone()
        {
            var binaries = new List<Binary>();
            foreach(var bin in this.Binaries)
                binaries.Add(bin.Clone());

            var services = new List<string>();
            foreach (var service in this.Services)
                services.Add(service);

            return new FeatureModel
            {
                Feature = this.Feature.Clone(),
                Binaries = binaries,
                Services = services,
            };
        }
    }
}
