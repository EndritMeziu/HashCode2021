using HashCode2021.Input;
using HashCode2021.Validator.Helpers;
using HashCode2021.Validator.Interfaces;
using HashCode2021.Validator.Models;

namespace HashCode2021.Validator.Services
{
    public class SolutionValidator : ISolutionValidator
    {
        public bool isValidSolution(string instancePath, string solutionPath)
        {
            var inputFile = ReadInputFile(instancePath);
            var solutionFile = ReadSolutionFile(solutionPath, inputFile);
            //check each feature for each enginner if is being done by 2+ in same binary
            var result = SolutionHelpers.CheckTaskSchedulingBetweenEngineers(solutionFile);

            

            return result;
        }

        public InputModel ReadInputFile(string instancePath)
        {
            InputModel inputModel = new InputModel();
            var fileLines = File.ReadAllLines(instancePath);
            string featureName = string.Empty;
            for (int i = 0; i < fileLines.Length; i++)
            {
                var line = fileLines[i];
                if (i == 0)
                {
                    var lineElements = line.Split(' ');
                    inputModel.TimeLimitDays = int.Parse(lineElements[0]);
                    inputModel.NumEngineers = int.Parse(lineElements[1]);
                    for (int j = 0; j < inputModel.NumEngineers; j++)
                    {
                        inputModel.Engineers.Add(new Engineers(j));
                    }
                    inputModel.NumServices = int.Parse(lineElements[2]);
                    inputModel.NumBinaries = int.Parse(lineElements[3]);
                    for (int j = 0; j < inputModel.NumBinaries; j++)
                    {
                        inputModel.Binaries.Add(new Binary(j));
                    }
                    inputModel.NumFeatures = int.Parse(lineElements[4]);
                    inputModel.TimeToCreateBinary = int.Parse(lineElements[5]);
                }
                else
                {
                    if (line.Split(' ').Length == 2)
                    {
                        int result;
                        var lineElements = line.Split(' ');
                        if (IsNumeric(lineElements[1]))
                        {
                            int.TryParse(lineElements[1], out result);
                            inputModel.Services.Add(new Service(lineElements[0]));
                            inputModel.Binaries[result].Services.Add(new Service(lineElements[0]));
                            continue;
                        }
                    }
                    if (line.Split(' ').Length == 4)
                    {
                        int result;
                        var lineElements = line.Split(' ');
                        int.TryParse(lineElements[1], out result);
                        if (result > 0)
                        {
                            featureName = lineElements[0];
                            inputModel.Features.Add(new Features(featureName, int.Parse(lineElements[1]), int.Parse(lineElements[2]), int.Parse(lineElements[3])));
                            continue;
                        }
                    }

                    if (featureName != string.Empty)
                    {
                        var lineElements = line.Split(' ');
                        foreach (var elem in lineElements)
                            inputModel.Features.Where(x => x.Name == featureName)?.FirstOrDefault()?.Services.Add(new Service(elem));
                    }

                }
            }
            return inputModel;
        }

        /// <summary>
        ///  Constructs Solution using solution path and inputModel
        /// </summary>
        /// <param name="solutionPath"></param>
        /// <param name="inputModel"></param>
        /// <returns></returns>
        public SolutionFile ReadSolutionFile(string solutionPath, InputModel inputModel)
        {
            var solutionLines = File.ReadAllLines(solutionPath);
            int numWorkingEngineers = int.Parse(solutionLines[0]);
            SolutionFile solutionFile = new SolutionFile(numWorkingEngineers);
            for (int i = 0; i < solutionFile.WorkingEngineers; i++)
            {
                solutionFile.Enginners.Add(new Engineers(i)
                {
                    AvailableDays = -1,
                    BusyUntil = -1,
                    Operations = new List<EnginnerOperation>()
                });
            }


            for (int i = 1; i < solutionLines.Length; i++)
            {
                var solutionLine = solutionLines[i];
                if (solutionLine.Split(' ').Count() == 1)
                {
                    int enginnerNumOperations = int.Parse(solutionLine);
                    var enginner = solutionFile.Enginners.Where(x => x.Operations.Count == 0).FirstOrDefault();
                    int startTime = 0;
                    for (int j = i + 1; j <= i + enginnerNumOperations; j++)
                    {
                        var operationArray = solutionLines[j].Split(' ');
                        var featureName = string.Empty;
                        var binary = -1;

                        if (enginner.Operations.Count > 0)
                        {
                            startTime = enginner.Operations.ElementAt(enginner.Operations.Count - 1).EndTime;
                        }

                        if (operationArray.Length == 3)
                        {
                            binary = int.Parse(operationArray[2]);
                            featureName = operationArray[1];
                            
                            var feature = inputModel.Features.Where(x => x.Name == featureName).FirstOrDefault();
                            var endTime = startTime + GetFeatureTime(feature, inputModel.Binaries.Where(x => x.Id == binary).FirstOrDefault(), solutionFile.Enginners, startTime);
                            enginner.Operations.Add(new EnginnerOperation()
                            {
                                BinaryId = binary,
                                StartTime = startTime,
                                EndTime = endTime,
                                FeatureName = featureName,
                                Operation = solutionLines[j]
                            });
                        }
                        else if(operationArray.Length == 2)
                        {
                            //wait op
                            enginner.Operations.Add(new EnginnerOperation
                            {
                                BinaryId = -1,
                                StartTime = startTime,
                                EndTime = startTime + int.Parse(operationArray[1]),
                                FeatureName = string.Empty,
                                Operation = solutionLines[j]
                            });
                        }

                    }
                    i += enginnerNumOperations;
                }
            }
            return solutionFile;
        }

        #region HelperMethods
        static bool IsNumeric(object Expression)
        {
            double retNum;

            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }

        static int GetFeatureTime(Features feature, Binary binary, List<Engineers> engineers, int time)
        {
            return feature.Difficulty + binary.Services.Count + GetNumberOfEnginnersWorkingOnCurrentBinary(binary, engineers, time);
        }


        static int GetNumberOfEnginnersWorkingOnCurrentBinary(Binary binary, List<Engineers> engineers, int time)
        {
            
            int numEngineers = 0;
            foreach (var engineer in engineers)
            {
                if (engineer.Operations.Count > 0)
                {
                    foreach (var operation in engineer.Operations)
                    {
                        if(operation.StartTime == 0 && operation.EndTime == 15 && binary.Id == 0)
                        {
                            int x = 2;
                        }
                        if (binary.Id == operation.BinaryId && (time >= operation.StartTime && time < operation.EndTime))
                        {
                            numEngineers++;
                        }
                    }
                }
            }
            return numEngineers;
        }

        public int CalculateScore(string instancePath, string solutionPath)
        {
            var inputModel = ReadInputFile(instancePath);
            var solutionFile = ReadSolutionFile(solutionPath, inputModel);
            return SolutionHelpers.CalculateScore(solutionFile.Enginners, inputModel);
        }
        #endregion
    }
}
