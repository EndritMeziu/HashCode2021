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
            var engineers = solutionFile.Enginners;
            foreach (var enginner in engineers)
            {
                var currentEngineerOperations = enginner.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                var otherEngineers = engineers.Where(x => x.Id != enginner.Id).ToList();
                foreach (var currentEngineerOperation in currentEngineerOperations)
                {
                    foreach (var otherEngineer in otherEngineers)
                    {
                        var otherEngineerOperations = otherEngineer.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                        foreach (var otherEngineerOperation in otherEngineerOperations)
                        {
                            if (currentEngineerOperation.FeatureName == otherEngineerOperation.FeatureName &&
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


            //check if one feature is done multiple times
            foreach (var engineer in engineers)
            {
                var currentEngineerOperations = engineer.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                var otherEngineers = engineers.Where(x => x.Id != engineer.Id).ToList();
                foreach (var currentEngineerOperation in currentEngineerOperations)
                {
                    foreach (var otherEngineer in otherEngineers)
                    {
                        var otherEngineerOperations = otherEngineer.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList();
                        foreach (var otherEngineerOperation in otherEngineerOperations)
                        {
                            if (currentEngineerOperation.FeatureName == otherEngineerOperation.FeatureName &&
                               currentEngineerOperation.BinaryId == otherEngineerOperation.BinaryId)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            //

            return true;
        }

        public static int CalculateScore(List<Engineers> engineers, InputModel inputModel)
        {
            int score = 0;
            List<EnginnerOperation> operations = new List<EnginnerOperation>();
            foreach(var engineer in engineers)
            {
                operations.AddRange(engineer.Operations.Where(x => !x.Operation.Contains("wait")).ToList()); 
            }

            var groupedOperations = operations.GroupBy(x => x.FeatureName).ToList();
            foreach(var operation in groupedOperations)
            {
                var lastEndTime = operation.OrderByDescending(x => x.EndTime).FirstOrDefault().EndTime;
                if (lastEndTime >= inputModel.TimeLimitDays)
                    continue;

                //not all services are implemented
                if (operation.Count() != inputModel.Features.Where(x => x.Name == operation.Key).FirstOrDefault().Services.Count())
                    continue;

                var feature = inputModel.Features.Where(x => x.Name == operation.Key).FirstOrDefault();
                if(feature != null)
                {
                    int daysUp = inputModel.TimeLimitDays - lastEndTime;
                    score += (daysUp * feature.NumUsersBenefit);
                }
            }

            return score;
        }
    }
}
