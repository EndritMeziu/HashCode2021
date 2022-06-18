using HashCode2021.Input;

namespace HashCode2021
{
    public class FeatureModel
    {
        public Features Feature { get; set; }
        public List<Binary> Binaries { get; set; }

        public FeatureModel Clone()
        {
            var binaries = new List<Binary>();
            foreach(var bin in this.Binaries)
                binaries.Add(bin.Clone());
            
            return new FeatureModel
            {
                Feature = this.Feature.Clone(),
                Binaries = binaries,
            };
        }
    }
}
