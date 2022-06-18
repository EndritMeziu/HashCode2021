using HashCode2021.Input;
using HashCode2021.Validator.Models;

namespace HashCode2021.Validator.Helpers
{
    public static class SolutionHelpers
    {
        public static int GetFeatureTime(Features feature, Binary binary, List<Engineers> engineers, int time)
        {
            return feature.Difficulty + binary.Services.Count + GetNumberOfEnginnersWorkingOnCurrentBinary(binary, engineers, time);
        }
    
        public static int GetNumberOfEnginnersWorkingOnCurrentBinary(Binary binary, List<Engineers> engineers, int time)
        {
            int numEngineers = 0;
            foreach (var engineer in engineers)
            {
                if (engineer.Operations.Count > 0)
                {
                    foreach (var operation in engineer.Operations)
                    {
                        if (binary.Id == operation.BinaryId && (time >= operation.StartTime && time < operation.EndTime))
                        {
                            numEngineers++;
                        }
                    }
                }
            }
            return numEngineers;
        }

        public static bool CheckTaskSchedulingBetweenEngineers(SolutionFile solutionFile)
        {
            //check if tasks are done in time limit
            //check if feature is implemented completely ---> todo
            var engineers = solutionFile.Enginners;
            foreach(var enginner in engineers)
            {
                var currentEngineerOperations = enginner.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                var otherEngineers = engineers.Where(x => x.Id != enginner.Id).ToList();
                foreach(var currentEngineerOperation in currentEngineerOperations)
                {
                    foreach(var otherEngineer in otherEngineers)
                    {
                        var otherEngineerOperations = otherEngineer.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                        foreach(var otherEngineerOperation in otherEngineerOperations)
                        {
                            if(currentEngineerOperation.FeatureName == otherEngineerOperation.FeatureName && 
                                currentEngineerOperation.BinaryId == otherEngineerOperation.BinaryId)
                            {
                                if (currentEngineerOperation.EndTime > otherEngineerOperation.StartTime &&
                                    currentEngineerOperation.EndTime <= otherEngineerOperation.EndTime)
                                    return false;

                                if (otherEngineerOperation.EndTime > currentEngineerOperation.StartTime &&
                                    otherEngineerOperation.EndTime <= currentEngineerOperation.EndTime)
                                    return false;

                                if (currentEngineerOperation.StartTime >= otherEngineerOperation.StartTime &&
                                    currentEngineerOperation.EndTime <= otherEngineerOperation.EndTime)
                                    return false;

                                if (currentEngineerOperation.StartTime >= otherEngineerOperation.StartTime &&
                                    currentEngineerOperation.EndTime >= otherEngineerOperation.EndTime)
                                    return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}
