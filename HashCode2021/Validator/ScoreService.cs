using HashCode2021.Input;
using HashCode2021.Models;
using HashCode2021.ProcessingModels;

namespace HashCode2021.Validator
{
    public static class ScoreService
    {
        static Dictionary<string, List<ServiceImplementedTimes>> implementedServices = new Dictionary<string, List<ServiceImplementedTimes>>();
        static int globalScore = 0;
        public static bool isValidSolution(string instancePath, string solutionPath)
        {
            var inputFile = ReadInputFile(instancePath);
            var solutionFile = ReadSolutionFile(solutionPath, inputFile);

            Dictionary<int, int> engineerOperationMapping = new Dictionary<int, int>();
            foreach (var enginneer in solutionFile.Enginners)
            {
                engineerOperationMapping.Add(enginneer.Id, 0);
            }

            for (int i = 0; i < inputFile.TimeLimitDays; i++)
            {
                foreach (var engineer in solutionFile.Enginners)
                {
                    if (engineer.BusyUntil > i)
                        continue;

                    int operationIndex = engineerOperationMapping[engineer.Id];
                    EnginnerOperation operation;
                    if (engineer.Operations.Count() > operationIndex)
                        operation = engineer.Operations.ElementAt(operationIndex);
                    else
                        continue;

                    //wait
                    if (operation.Operation.Contains("wait"))
                    {
                        operation.StartTime = i;
                        operation.EndTime = i + int.Parse(operation.Operation.Split(' ')[1]);
                        engineer.BusyUntil += int.Parse(operation.Operation.Split(' ')[1]);
                        engineer.AvailableDays -= int.Parse(operation.Operation.Split(' ')[1]);
                    }
                    else if (operation.Operation.Contains("impl"))
                    {
                        var feature = inputFile.Features.Where(x => x.Name == operation.FeatureName).FirstOrDefault();
                        var binary = inputFile.Binaries.Where(x => x.Id == operation.BinaryId.Value).FirstOrDefault();
                        if (feature.Name == "sbafaddd" && binary.Id == 67)
                            continue;
                        var featureTime = GetFeatureTime(feature, binary, solutionFile.Enginners, i);
                        operation.StartTime = i;
                        operation.EndTime = i + featureTime;
                        engineer.AvailableDays -= featureTime;
                        engineer.BusyUntil = i + featureTime;
                        binary.EngineerWorkingUntil = Math.Max(binary.EngineerWorkingUntil, operation.EndTime);
                    }
                    else if (operation.Operation.Contains("move"))
                    {
                        var toMoveService = operation.Operation.Split(' ')[1];
                        var fromBinary = inputFile.Binaries.Where(x => x.Services.Select(x => x.Name).Contains(toMoveService)).FirstOrDefault();
                        var toBinary = inputFile.Binaries.Where(x => x.Id == int.Parse(operation.Operation.Split(' ')[2])).FirstOrDefault();
                        int moveTime = Math.Max(fromBinary.Services.Count(), toBinary.Services.Count());
                        operation.StartTime = i;
                        operation.EndTime = i + moveTime;
                        engineer.AvailableDays -= moveTime;
                        engineer.BusyUntil += moveTime;
                        fromBinary.NotAvailableUntil = Math.Max(fromBinary.NotAvailableUntil, i + moveTime);
                        toBinary.NotAvailableUntil += Math.Max(toBinary.NotAvailableUntil, i + moveTime);
                        MoveService(inputFile, fromBinary, toBinary, toMoveService, engineer, i);

                    }
                    else if (operation.Operation.Contains("new"))
                    {
                        operation.StartTime = i;
                        operation.EndTime = i + inputFile.TimeToCreateBinary;
                        engineer.BusyUntil = i + inputFile.TimeToCreateBinary;
                        engineer.AvailableDays -= inputFile.TimeToCreateBinary;
                        CreateBinary(inputFile, engineer, i);
                    }
                    engineerOperationMapping[engineer.Id] += 1;
                    //new
                }
            }
            //check each feature for each enginner if is being done by 2+ in same binary
            var result = SolutionHelpers.CheckTaskSchedulingBetweenEngineers(solutionFile);


            Console.WriteLine("Global Score:" + globalScore);
            return result;
        }

        public static InputModel ReadInputFile(string instancePath)
        {
            InputModel inputModel = new InputModel();
            var fileLines = new List<string>();

            using (StreamReader file = new StreamReader(instancePath))
            {
                int counter = 0;
                string ln;

                while ((ln = file.ReadLine()) != null)
                {
                    fileLines.Add(ln);
                }
                file.Close();
            }
            
            string featureName = string.Empty;
            for (int i = 0; i < fileLines.Count; i++)
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
        public static SolutionFile ReadSolutionFile(string solutionPath, InputModel inputModel)
        {
            var solutionLines = new List<string>();

            using (StreamReader file = new StreamReader(solutionPath))
            {
                int counter = 0;
                string ln;

                while ((ln = file.ReadLine()) != null)
                {
                    solutionLines.Add(ln);
                }
                file.Close();
            }
            int numWorkingEngineers = int.Parse(solutionLines[0]);
            SolutionFile solutionFile = new SolutionFile(numWorkingEngineers);
            for (int i = 0; i < solutionFile.WorkingEngineers; i++)
            {
                solutionFile.Enginners.Add(new Engineers(i)
                {
                    AvailableDays = inputModel.TimeLimitDays,
                    BusyUntil = 0,
                    Operations = new List<EnginnerOperation>()
                });
            }

            var createBinaryOperations = solutionLines.Where(x => x.StartsWith("new")).ToList();
            for (int i = 1; i < solutionLines.Count; i++)
            {
                var solutionLine = solutionLines[i];
                if (solutionLine.Split(' ').Count() == 1)
                {
                    int engineerNumOperations = int.Parse(solutionLine);
                    var engineer = solutionFile.Enginners.Where(x => x.Operations.Count == 0).FirstOrDefault();
                    int startTime = 0;
                    for (int j = i + 1; j <= i + engineerNumOperations; j++)
                    {
                        var operationArray = solutionLines[j].Split(' ');
                        var featureName = string.Empty;
                        var binary = -1;

                        if (operationArray.Count() == 1)
                        {
                            //new
                            engineer.Operations.Add(new EnginnerOperation
                            {
                                BinaryId = -1,
                                Operation = solutionLines[j]
                            });
                        }
                        else if (operationArray.Count() == 2)
                        {
                            //wait
                            engineer.Operations.Add(new EnginnerOperation
                            {
                                BinaryId = -1,
                                Operation = solutionLines[j]
                            });
                        }
                        else if (operationArray.Count() == 3)
                        {
                            //impl and move

                            binary = int.Parse(operationArray[2]);
                            featureName = operationArray[1];
                            engineer.Operations.Add(new EnginnerOperation()
                            {
                                BinaryId = binary,
                                FeatureName = featureName,
                                Operation = solutionLines[j]
                            });
                        }


                    }
                    i += engineerNumOperations;
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
                        if (binary.Id == operation.BinaryId && (time >= operation.StartTime && time < operation.EndTime))
                        {
                            numEngineers++;
                        }
                    }
                }
            }
            return numEngineers;
        }

        // order engineers by endtime or available days and execute their operations that way
        public static int CalculateScore(string instancePath, string solutionPath)
        {
            var score = 0;
            var inputModel = ReadInputFile(instancePath);
            var solutionFile = ReadSolutionFile(solutionPath, inputModel);

            Dictionary<int, int> engineerOperationMapping = new Dictionary<int, int>();
            foreach (var enginneer in solutionFile.Enginners)
            {
                engineerOperationMapping.Add(enginneer.Id, 0);
                enginneer.AvailableDays = inputModel.TimeLimitDays;
            }
            int createdBinariesNum = 0;
            foreach (var engineer in solutionFile.Enginners)
            {
                foreach (var operation in engineer.Operations)
                {
                    if (operation.Operation.Contains("new"))
                    {
                        CreateBinary(inputModel, engineer);
                        createdBinariesNum++;
                    }
                }
            }

            while (true)
            {
                var engineer = solutionFile.Enginners.OrderByDescending(x => x.AvailableDays).FirstOrDefault();

                //index based operation query does not work as different engineers have different number of operations done
                int operationIndex = engineerOperationMapping[engineer.Id];
                EnginnerOperation operation;
                if (engineer.Operations.Count() > operationIndex)
                    operation = engineer.Operations.ElementAt(operationIndex);
                else
                    break;

                //wait
                if (operation.Operation.Contains("wait"))
                {
                    operation.StartTime = engineer.BusyUntil;
                    operation.EndTime = engineer.BusyUntil + int.Parse(operation.Operation.Split(' ')[1]);
                    engineer.BusyUntil += int.Parse(operation.Operation.Split(' ')[1]);
                    engineer.AvailableDays -= int.Parse(operation.Operation.Split(' ')[1]);
                }
                else if (operation.Operation.Contains("impl"))
                {
                    var feature = inputModel.Features.Where(x => x.Name == operation.FeatureName).FirstOrDefault();
                    var binary = inputModel.Binaries.Where(x => x.Id == operation.BinaryId.Value).FirstOrDefault();
                    var featureTime = GetFeatureTime(feature, binary, solutionFile.Enginners, engineer.BusyUntil);
                    operation.StartTime = engineer.BusyUntil;
                    operation.EndTime = engineer.BusyUntil + featureTime;
                    engineer.AvailableDays -= featureTime;
                    engineer.BusyUntil = engineer.BusyUntil + featureTime;
                    binary.EngineerWorkingUntil = Math.Max(binary.EngineerWorkingUntil, operation.EndTime);
                    //CheckIfServicesForScoreAddition(feature, binary, operation.EndTime);
                }
                else if (operation.Operation.Contains("move"))
                {
                    var toMoveService = operation.Operation.Split(' ')[1];
                    var fromBinary = inputModel.Binaries.Where(x => x.Services.Select(x => x.Name).Contains(toMoveService)).FirstOrDefault();
                    var toBinary = inputModel.Binaries.Where(x => x.Id == int.Parse(operation.Operation.Split(' ')[2])).FirstOrDefault();
                    int moveTime = Math.Max(fromBinary.Services.Count(), toBinary.Services.Count());
                    operation.StartTime = engineer.BusyUntil;
                    operation.EndTime = engineer.BusyUntil + moveTime;
                    engineer.AvailableDays -= moveTime;
                    engineer.BusyUntil += moveTime;
                    fromBinary.NotAvailableUntil += Math.Max(fromBinary.NotAvailableUntil, engineer.BusyUntil + moveTime);
                    toBinary.NotAvailableUntil += Math.Max(toBinary.NotAvailableUntil, engineer.BusyUntil + moveTime);
                    MoveService(inputModel, fromBinary, toBinary, toMoveService, engineer, engineer.BusyUntil);

                }
                else if (operation.Operation.Contains("new"))
                {
                    operation.StartTime = engineer.BusyUntil;
                    operation.EndTime = engineer.BusyUntil + inputModel.TimeToCreateBinary;
                    engineer.BusyUntil = engineer.BusyUntil + inputModel.TimeToCreateBinary;
                    engineer.AvailableDays -= inputModel.TimeToCreateBinary;
                    //CreateBinary(inputModel, engineer, engineer.BusyUntil);
                }
                engineerOperationMapping[engineer.Id] += 1;
                //new
            }

            //solutionFile.Enginners = solutionFile.Enginners.OrderBy(x => x.Id).ToList();
            //foreach (var engineer in solutionFile.Enginners)
            //{
            //    Console.WriteLine(engineer.Operations.Count);
            //    foreach (var operation in engineer.Operations)
            //    {
            //        Console.WriteLine($"[{operation.Operation}] {operation.StartTime} -> {operation.EndTime}");
            //    }
            //}
            //Console.WriteLine();
            //var result = SolutionHelpers.CheckTaskSchedulingBetweenEngineers(solutionFile);
            //Console.WriteLine(result);
            //Console.WriteLine("Global Score: " + );

            var result = SolutionHelpers.CheckTaskSchedulingBetweenEngineers(solutionFile);
            if (result)
                return SolutionHelpers.CalculateScore(solutionFile.Enginners, inputModel);
            else
                return 0;
        }

        static int CreateBinary(InputModel inputModel, Engineers engineers, int startTime)
        {
            if (engineers.AvailableDays - inputModel.TimeToCreateBinary < 0)
                return -1;

            var lastBinaryId = inputModel.Binaries.OrderByDescending(x => x.Id).FirstOrDefault().Id;
            inputModel.Binaries.Add(new Binary
            {
                Services = new List<Service>(),
                Done = false,
                Id = lastBinaryId + 1
            });

            inputModel.NumBinaries++;
            return lastBinaryId + 1;
        }

        static void CreateBinary(InputModel inputModel, Engineers engineers)
        {
            var lastBinaryId = inputModel.Binaries.OrderByDescending(x => x.Id).FirstOrDefault().Id;
            inputModel.Binaries.Add(new Binary
            {
                Services = new List<Service>(),
                Done = false,
                Id = lastBinaryId + 1
            });
        }

        //void MoveService(InputModel inputModel, Binary firstBinary, Binary secondBinary, string serviceName, Engineers engineer, int startTime)
        //{

        //    if (firstBinary.EngineerWorkingUntil <= startTime && secondBinary.EngineerWorkingUntil <= startTime &&
        //        firstBinary.NotAvailableUntil <= startTime && secondBinary.NotAvailableUntil <= startTime)
        //    {
        //        var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
        //        int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());

        //        if (engineer.AvailableDays - moveTime >= 0)
        //        {
        //            firstBinary.Services.Remove(firstBinaryService);
        //            secondBinary.Services.Add(firstBinaryService);
        //        }
        //    }
        //}

        static void MoveService(InputModel input, Binary firstBinary, Binary secondBinary, string serviceName, Engineers engineer, int startTime)
        {


            var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
            int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());


            firstBinary.Services.Remove(firstBinaryService);
            secondBinary.Services.Add(firstBinaryService);
            //engineer.Operations.Add(new EnginnerOperation
            //{
            //    BinaryId = -1,
            //    StartTime = startTime,
            //    EndTime = startTime + moveTime,
            //    Operation = $"move {serviceName} {secondBinary.Id}"
            //});
            //engineer.AvailableDays -= moveTime;
            //engineer.BusyUntil += moveTime;
            //firstBinary.NotAvailableUntil = Math.Max(firstBinary.NotAvailableUntil, startTime + moveTime);
            //econdBinary.NotAvailableUntil = Math.Max(secondBinary.NotAvailableUntil, startTime + moveTime);

        }
    }
}
#endregion