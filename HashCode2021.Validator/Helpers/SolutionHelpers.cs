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
                var currentEngineerOperations = enginner.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move") && !x.Operation.StartsWith("new")).ToList();
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
                var currentEngineerOperations = engineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move") && !x.Operation.StartsWith("new")).ToList();
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


        static int GetBaseFeatureTime(Features features, Binary binary)
        {
            return features.Difficulty + binary.Services.Count;
        }
        ///
        /// Calculate solution score
        /// 
        public static int CalculateScore(List<Engineers> engineers, InputModel inputModel)
        {
            int score = 0;
            var processedFeatures = ProcessFeatures(inputModel);
            List<EnginnerOperation> operations = new List<EnginnerOperation>();
            foreach (var engineer in engineers)
            {
                operations.AddRange(engineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move") && !x.Operation.StartsWith("new")).ToList());
            }

            var operations1 = operations.GroupBy(x => x.FeatureName).ToList();
            foreach (var data in operations1)
            {

                var feature = data.OrderByDescending(x => x.EndTime).FirstOrDefault();
                var inputFeature = inputModel.Features.Where(x => x.Name == feature.FeatureName).FirstOrDefault();
                var featureBinaries = processedFeatures.Where(x => x.Feature.Name == feature.FeatureName).FirstOrDefault();
                if (featureBinaries.Binaries.Count() != data.Count()) continue;
                if (feature != null)
                {
                    int numDaysAvailable = inputModel.TimeLimitDays - feature.EndTime;
                    int numUsersBenefit = inputFeature.NumUsersBenefit;
                    if (numDaysAvailable < 0) numDaysAvailable = 0;
                    score += (numDaysAvailable * numUsersBenefit);
                }
            }
            return score;
        }

        public static int CalculateScore2(List<Engineers> engineers, InputModel inputModel)
        {
            int score = 0;
            List<EnginnerOperation> operations = new List<EnginnerOperation>();
            foreach (var engineer in engineers)
            {
                operations.AddRange(engineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move") && !x.Operation.StartsWith("new")).ToList());
            }

            var operations1 = operations.GroupBy(x => x.FeatureName).ToList();
            foreach (var data in operations1)
            {

                var feature = data.OrderByDescending(x => x.EndTime).FirstOrDefault();
                var inputFeature = inputModel.Features.Where(x => x.Name == feature.FeatureName).FirstOrDefault();
                var numServices = inputModel.Features.Where(x => x.Name == data.Key).FirstOrDefault().Services.Count();
                if (data.Count() != numServices) continue;
                if (feature != null)
                {
                    int numDaysAvailable = inputModel.TimeLimitDays - feature.EndTime;
                    int numUsersBenefit = inputFeature.NumUsersBenefit;
                    if (numDaysAvailable < 0) numDaysAvailable = 0;
                    score += (numDaysAvailable * numUsersBenefit);
                }
            }
            return score;
        }

        static List<Binary> BinariesWithFeatureServices(Features feature, List<Binary> binaries)
        {
            var correctBinaries = new List<Binary>();
            var featureServices = feature.Services;
            foreach (var binary in binaries)
            {
                var binaryServices = binary.Services;
                foreach (var service in featureServices)
                {
                    if (binaryServices.Select(x => x.Name).Contains(service.Name))
                    {
                        correctBinaries.Add(binary);
                        break;
                    }
                }
            }
            return correctBinaries;
        }

        static List<FeatureModel> ProcessFeatures(InputModel inputModel)
        {
            List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
            var features = inputModel.Features;
            var binaries = inputModel.Binaries;
            foreach (var feature in features)
            {
                var featureBinaries = BinariesWithFeatureServices(feature, binaries).ToList();
                //if (featureBinaries.Count == 0) continue;
                proccessedFeatures.Add(new FeatureModel
                {
                    Feature = feature.Clone(),
                    Binaries = new List<Binary>(),
                    FeatureBinaryTime = new Dictionary<int, int>(),
                    FeatureTimeBinary = 0,
                    Services = feature.Services.Select(x => x.Name).ToList()
                });
                foreach (var binary in featureBinaries)
                {
                    var baseFeatureTime = GetBaseFeatureTime(feature, binary);
                    proccessedFeatures[proccessedFeatures.Count - 1].Binaries.Add(binary.Clone());
                }

            }

            return proccessedFeatures;
        }
    }
}
