using HashCode2021.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCode2021.Validator
{
    internal class SolutionValidator
    {
        public static bool CheckTaskSchedulingBetweenEngineers(List<Engineers> engineers)
        {
            //check if tasks are done in time limit
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
                                {
                                    Console.WriteLine("Same binary same feature same interval");
                                    return false;
                                }

                                if (otherEngineerOperation.EndTime > currentEngineerOperation.StartTime &&
                                    otherEngineerOperation.EndTime <= currentEngineerOperation.EndTime)
                                {
                                    Console.WriteLine("Same binary same feature same interval");
                                    return false;
                                }

                                if (currentEngineerOperation.StartTime >= otherEngineerOperation.StartTime &&
                                    currentEngineerOperation.EndTime <= otherEngineerOperation.EndTime)
                                {
                                    Console.WriteLine("Same binary same feature same interval");
                                    return false;
                                }

                                if (currentEngineerOperation.StartTime >= otherEngineerOperation.StartTime &&
                                    currentEngineerOperation.EndTime >= otherEngineerOperation.EndTime)
                                {
                                    Console.WriteLine("Same binary same feature same interval");
                                    return false;
                                }
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
                                Console.WriteLine($"Feature {currentEngineerOperation.FeatureName} is done multiple times");
                                return false;
                            }
                        }
                    }
                }
            }

            //working in binary while move is being done
            foreach (var engineer in engineers)
            {
                var currentEngineerOperations = engineer.Operations.Where(x => x.Operation.StartsWith("move")).ToList();
                var otherEngineers = engineers.Where(x => x.Id != engineer.Id).ToList();
                foreach (var currentEngineerOperation in currentEngineerOperations)
                {
                    foreach (var otherEngineer in otherEngineers)
                    {
                        var otherEngineerOperations = otherEngineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("new")).ToList();
                        foreach (var otherEngineerOperation in otherEngineerOperations)
                        {
                            if (currentEngineerOperation.BinaryId == otherEngineerOperation.BinaryId &&
                                currentEngineerOperation.StartTime >= otherEngineerOperation.StartTime &&
                                currentEngineerOperation.StartTime < otherEngineerOperation.EndTime)
                                return false;

                            if (currentEngineerOperation.BinaryId == otherEngineerOperation.BinaryId &&
                                currentEngineerOperation.EndTime > otherEngineerOperation.StartTime &&
                                currentEngineerOperation.EndTime <= otherEngineerOperation.EndTime)
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
