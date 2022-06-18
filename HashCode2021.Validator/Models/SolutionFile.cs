using HashCode2021.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCode2021.Validator.Models
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
