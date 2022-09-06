using HashCode2021.Input;

namespace HashCode2021.Models
{
    public class SolutionFile
    {
        public SolutionFile(int workingEngineers)
        {
            Enginners = new List<Engineers>();
            WorkingEngineers = workingEngineers;
        }
        public int WorkingEngineers { get; set; }
        public List<Engineers> Enginners { get; set; }
    }
}
