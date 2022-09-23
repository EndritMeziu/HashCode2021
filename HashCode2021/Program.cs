using HashCode2021;
using HashCode2021.Input;
using HashCode2021.Models;
using HashCode2021.ProcessingModels;
using HashCode2021.Validator;

int globalScore = 0;
string inputFile = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\five_thousand.txt";
string outputFile = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt";
string algorithmFile = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\hill_climbing.txt";
List<string> alreadyMoved = new List<string>();
List<FeatureModel> features = new List<FeatureModel>();
Dictionary<string, List<ServiceImplementedTimes>> implementedServices = new Dictionary<string, List<ServiceImplementedTimes>>();
Dictionary<int, List<BinaryBusyTimes>> binaryBusyTimes = new Dictionary<int, List<BinaryBusyTimes>>();
var input = ReadFile(inputFile);
var solution = new List<Engineers>();
int initialBinaries = 0;
while (globalScore < 1)
{
    globalScore = 0;
    binaryBusyTimes = new Dictionary<int, List<BinaryBusyTimes>>();
    implementedServices = new Dictionary<string, List<ServiceImplementedTimes>>();
    features = new List<FeatureModel>();
    alreadyMoved = new List<string>();
    input = ReadFile(inputFile);
    initialBinaries = input.NumBinaries;
    solution = InitialSolution(input);
    var notImplementedFeatures = GetNotImplementedFeatures(input, solution);
    var processedNotImplemented = ProcessNotImplementedFeatures(notImplementedFeatures, input);
}
PerformTabuSearch(input, solution, binaryBusyTimes, globalScore);

//PerformHillClimbing(input, solution, binaryBusyTimes);
///
///Generate initial solution
///
 List<Engineers> InitialSolution(InputModel input)
{
    var initialSolution = new List<Engineers>();
    foreach (var enginner in input.Engineers)
    {
        enginner.AvailableDays = input.TimeLimitDays;
        initialSolution.Add(enginner);
    }

    int initialBiniaries = input.NumBinaries;

    var serviceToFeatures = ServicesToFeatures(input);

    features = ProcessFeatures(input);
    Dictionary<int, List<FeatureModel>> engineersFeature = new Dictionary<int, List<FeatureModel>>();
    List<string> processedFeatures = new List<string>();
    var doableFeatures = ProcessFeaturesAlg(features, input, processedFeatures);
    var rnd = new Random();
    for (int i = 0; i < input.TimeLimitDays; i++)
    {
        int createCount = 0;
        //initialSolution = initialSolution.OrderBy(x => x.AvailableDays).ToList();
        foreach (var engineer in initialSolution)
        {
            if (engineer.BusyUntil > i)
                continue;

            if (input.NumBinaries < (100 * input.Services.Count) / 100)
            {
                if (MoveAndCreateNewServices(input, initialSolution, serviceToFeatures, false, alreadyMoved, i, engineer.Id))
                    doableFeatures = ProcessFeaturesAlg(doableFeatures, input, processedFeatures);
            }


            if (engineer.BusyUntil > i)
                continue;

            if (createCount < 3)
            {
                if (MoveAndCreateNewServices(input, initialSolution, serviceToFeatures, true, alreadyMoved, i, engineer.Id))
                {
                    doableFeatures = ProcessFeaturesAlg(doableFeatures, input, processedFeatures);
                    createCount++;
                }
            }

            if (engineer.BusyUntil > i)
                continue;

            if (doableFeatures.Count > 0)
            {
                int retries = 0;
                while (retries < 10) { 
                    FeatureModel toDoFeature;
                    Dictionary<int, int> binaryWorkingEngineers = new Dictionary<int, int>();

                    toDoFeature = doableFeatures.OrderByDescending(x => (x.Feature.NumUsersBenefit)/ (x.FeatureBinaryTime.Values.Sum())).Skip(retries).FirstOrDefault();
                    var feature = toDoFeature?.Feature;

                    var featureBinary = toDoFeature?.Binaries.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                    if (featureBinary == null || featureBinary.NotAvailableUntil > i || engineer.AvailableDays - GetFeatureTime(feature, featureBinary, initialSolution, i) < 0)
                    {
                        retries++;
                        continue;
                    }
                    var featureTime = GetFeatureTime(feature, featureBinary, initialSolution, i);

                    if (engineer.AvailableDays - featureTime >= 0)
                    {
                        var endTime = i + featureTime;
                        engineer.AvailableDays -= featureTime;
                        engineer.BusyUntil = endTime;
                        input.Binaries.Where(x => x.Id == featureBinary.Id).FirstOrDefault().EngineerWorkingUntil = Math.Max(featureBinary.EngineerWorkingUntil, endTime);
                        featureBinary.EngineerWorkingUntil  = Math.Max(featureBinary.EngineerWorkingUntil, endTime);
                        ImplementFeature(engineer, feature, featureBinary, i, endTime);
                        processedFeatures.Add($"impl {feature.Name} {featureBinary.Id}");
                        CheckIfServicesForScoreAddition(feature, featureBinary, endTime);
                        RemoveFeatureFromFeatureList(doableFeatures, toDoFeature, featureBinary.Id);
                        break;
                    }
                    retries++;
                }
                if (engineer.BusyUntil <= i)
                {
                    engineer.BusyUntil += 1;
                    engineer.AvailableDays -= 1;
                    Wait(engineer, i, i + 1);
                }
            }
            else
            {
                if (engineer.BusyUntil <= i)
                {
                    engineer.BusyUntil += 1;
                    engineer.AvailableDays -= 1;
                    Wait(engineer, i, i + 1);
                }
            }
        }
    }


    SaveSolution(initialSolution, outputFile);
    var isValid = SolutionValidator.CheckTaskSchedulingBetweenEngineers(initialSolution);
    Console.WriteLine("Solution validation test passed? " + isValid);
    //foreach (var engineer in initialSolution)
    //{
    //    Console.WriteLine(engineer.Operations.Count);
    //    foreach (var operation in engineer.Operations)
    //    {
    //        Console.WriteLine($"[{operation.Operation}] {operation.StartTime} -> {operation.EndTime}");
    //    }
    //}

    Console.WriteLine("Score: " + globalScore);
    var baseInput = ReadFile(inputFile);
    globalScore = ScoreCalculate(initialSolution, baseInput);
    Console.WriteLine("Score2: "+ globalScore);
    return initialSolution;
}

void PerformTabuSearch(InputModel inputModel, List<Engineers> initialSolution, Dictionary<int, List<BinaryBusyTimes>> binaryBusyTimes, int score)
{
    Dictionary<string, List<TabuModel>> tabuList = new Dictionary<string, List<TabuModel>>();
    Dictionary<List<Engineers>, int> solutionsList = new Dictionary<List<Engineers>, int>();
    List<Engineers> bestSolution = new List<Engineers>();
    int count = 0;
    var baseInput = ReadFile(inputFile);
    while (count++ < 1000000)
    {
        int listCount = 0;
        do {
            listCount++;
            var clonedSolution = CloneSolution(initialSolution);
            clonedSolution = ReplaceTwoRandomImplOperationTabu(clonedSolution, inputModel, tabuList, score);
            //SaveSolution(clonedSolution, algorithmFile);
            if (clonedSolution != null && SolutionHelpers.CheckTaskSchedulingBetweenEngineers(new SolutionFile(clonedSolution.Count) { Enginners = CloneSolution(clonedSolution), WorkingEngineers = clonedSolution.Count}))
            {
                var clonedBaseInput = CloneInputFile(baseInput);
                solutionsList.Add(clonedSolution, ScoreCalculate(clonedSolution, clonedBaseInput));
            }
        }
        while (solutionsList.Count < 15 && listCount < 100);
        solutionsList = solutionsList.Count() > 0 ? solutionsList : new Dictionary<List<Engineers>, int>();
        if (solutionsList.Count > 0)
        {
            var bestListSolution = solutionsList.OrderByDescending(x => x.Value).First();
            if (bestListSolution.Value >= score)
            {
                if (SolutionHelpers.CheckTaskSchedulingBetweenEngineers(new SolutionFile(bestListSolution.Key.Count) { Enginners = CloneSolution(bestListSolution.Key), WorkingEngineers = bestListSolution.Key.Count }))
                {
                    Console.WriteLine("Score: " + bestListSolution.Value);
                    bestSolution = CloneSolution(bestListSolution.Key);
                    initialSolution = CloneSolution(bestListSolution.Key);
                    score = bestListSolution.Value;
                    SaveSolution(initialSolution, algorithmFile);
                }
            }
        }
        solutionsList = new Dictionary<List<Engineers>, int>();
        UpdateTabuList(tabuList);
    }

    SaveSolution(initialSolution, outputFile);
    ScoreService.CalculateScore(inputFile, outputFile);
}

void PerformHillClimbing(InputModel input, List<Engineers> initialSolution, Dictionary<int, List<BinaryBusyTimes>> binaryBusyTimes)
{
    int count = 0;
    while(count++ < 10000)
    {
        var clonedSolution = CloneSolution(initialSolution);
        clonedSolution = ReplaceTwoRandomImplOperation(clonedSolution, input);

        SaveSolution(clonedSolution, outputFile);
        var modifiedScore = ScoreService.CalculateScore(inputFile, outputFile);
        Console.WriteLine("Score from fileRead: " + modifiedScore);
        Console.WriteLine("Score: " + CalculateScore(clonedSolution, input));
        var baseInput = ReadFile(inputFile);
        Console.WriteLine("ScoreCalculation: "+ ScoreCalculate(clonedSolution, baseInput));
        if (modifiedScore > globalScore)
        {
            globalScore = modifiedScore;
            initialSolution = CloneSolution(clonedSolution);
            //SaveSolution(clonedSolution, outputFile);
            SaveSolution(clonedSolution, algorithmFile);
            Console.WriteLine(globalScore);
            Console.WriteLine("Hill Climbing Change");
            //foreach (var engineer in initialSolution)
            //{
            //    Console.WriteLine(engineer.Operations.Count);
            //    foreach (var operation in engineer.Operations)
            //    {
            //        Console.WriteLine($"[{operation.Operation}] {operation.StartTime} -> {operation.EndTime}");
            //    }
            //}
            Console.WriteLine();
            Console.WriteLine("Score:" + globalScore);
            Console.WriteLine();
        }
        #region commented
        //var selectedEngineer = clonedSolution.OrderBy(x => Guid.NewGuid()).First();
        //Random rnd = new Random();
        //int pos = (rnd.Next() % (selectedEngineer.Operations.Count-1))+1;
        //var randomEngineerOperation = selectedEngineer.Operations.ElementAt(pos);

        //        if (randomEngineerOperation.Operation.StartsWith("impl"))
        //        {
        //            var beforeOperation = selectedEngineer.Operations.ElementAt(pos - 1);
        //            //var afterOperation = selectedEngineer.Operations.ElementAt(pos + 1);
        //            if (beforeOperation.Operation.StartsWith("impl"))
        //            {
        //                var beforeOpearationStartTime = beforeOperation.StartTime; //1 - 4
        //                var beforeOpearationEndTime = beforeOperation.EndTime;
        //                var beforeOperationBinary = beforeOperation.BinaryId;
        //                var beforeOperationFeatureName = beforeOperation.FeatureName;
        //                var beforeOperationOp = beforeOperation.Operation;
        //                var beforeOperationLength = beforeOpearationEndTime - beforeOpearationStartTime;

        //                var randomOperationStartTime = randomEngineerOperation.StartTime; //4 - 10
        //                var randomOperationEndTime = randomEngineerOperation.EndTime;
        //                var randomOpearationBinary = randomEngineerOperation.BinaryId;
        //                var randomOperationFeatureName = randomEngineerOperation.FeatureName;
        //                var randomOperationOp = randomEngineerOperation.Operation;
        //                var randomOperationLength = randomOperationEndTime - randomOperationStartTime;

        //                //try to switch
        //                randomEngineerOperation.StartTime = beforeOpearationStartTime;
        //                randomEngineerOperation.EndTime = beforeOpearationStartTime + randomOperationLength;
        //                randomEngineerOperation.FeatureName = randomOperationFeatureName;
        //                randomEngineerOperation.BinaryId = randomOpearationBinary;
        //                randomEngineerOperation.Operation = randomOperationOp;

        //                beforeOperation.StartTime = randomEngineerOperation.EndTime;
        //                beforeOperation.EndTime = randomEngineerOperation.EndTime + beforeOperationLength;
        //                beforeOperation.FeatureName = beforeOperationFeatureName;
        //                beforeOperation.BinaryId = beforeOperationBinary;
        //                beforeOperation.Operation = beforeOperationOp;


        //                selectedEngineer.Operations[pos - 1] = randomEngineerOperation;
        //                selectedEngineer.Operations[pos] = beforeOperation;

        //                //clonedSolution[selectedEngineer.Id] = selectedEngineer;

        //                if (binaryBusyTimes.ContainsKey(randomEngineerOperation.BinaryId.Value))
        //                {
        //                    var randomBinaryBusyTimes = binaryBusyTimes[randomEngineerOperation.BinaryId.Value];
        //                    if (randomBinaryBusyTimes.Where(x => x.StartTime <= randomEngineerOperation.StartTime && x.EndTime >= randomEngineerOperation.StartTime).Any() ||
        //                        randomBinaryBusyTimes.Where(x => x.StartTime <= randomEngineerOperation.StartTime && x.EndTime >= randomEngineerOperation.EndTime).Any() ||
        //                        randomBinaryBusyTimes.Where(x => x.StartTime <= randomEngineerOperation.EndTime && x.EndTime >= randomEngineerOperation.EndTime).Any())
        //                    {
        //                        continue;
        //                    }
        //                }

        //                if (binaryBusyTimes.ContainsKey(beforeOperation.BinaryId.Value))
        //                {
        //                    var beforeBinaryBusyTimes = binaryBusyTimes[beforeOperation.BinaryId.Value];
        //                    if (beforeBinaryBusyTimes.Where(x => x.StartTime <= beforeOperation.StartTime && x.EndTime >= randomEngineerOperation.StartTime).Any() ||
        //                       beforeBinaryBusyTimes.Where(x => x.StartTime <= beforeOperation.StartTime && x.EndTime >= randomEngineerOperation.EndTime).Any() ||
        //                       beforeBinaryBusyTimes.Where(x => x.StartTime <= beforeOperation.EndTime && x.EndTime >= randomEngineerOperation.EndTime).Any())
        //                    {
        //                        continue;
        //                    }
        //                }

        //                SaveSolution(clonedSolution, outputFile);
        //                var modifiedScore = ScoreService.CalculateScore(inputFile, outputFile);
        //                if (modifiedScore > globalScore)
        //                {
        //                    globalScore = modifiedScore;
        //                    initialSolution = CloneSolution(clonedSolution);
        ////SaveSolution(clonedSolution, outputFile);

        //                    Console.WriteLine(globalScore);
        //                    Console.WriteLine("Hill Climbing Change");
        //                    //foreach (var engineer in initialSolution)
        //                    //{
        //                    //    Console.WriteLine(engineer.Operations.Count);
        //                    //    foreach (var operation in engineer.Operations)
        //                    //    {
        //                    //        Console.WriteLine($"[{operation.Operation}] {operation.StartTime} -> {operation.EndTime}");
        //                    //    }
        //                    //}
        //                    Console.WriteLine();
        //                    Console.WriteLine("Score:" +globalScore);
        //                    Console.WriteLine();
        //                }
        //            }
        //        }
        #endregion
        if (count % 1000 == 0)
            Console.WriteLine("Iteration "+count);
    }

    SaveSolution(initialSolution, outputFile);
    ScoreService.CalculateScore(inputFile, outputFile);

}

#region EngineerOperations

static int GetWorkingEngineersInBinaries(List<Binary> binaries, Dictionary<int,int> binaryNumEngineersPairs)
{
    int engineerCount = 0;
    foreach(var binary in binaries)
    {
        engineerCount += binaryNumEngineersPairs[binary.Id];
    }
    return engineerCount;
}

static int CreateBinary(InputModel input, Engineers engineers, int startTime)
{
    if (engineers.AvailableDays - input.TimeToCreateBinary < 0)
        return -1;

    engineers.Operations.Add(new EnginnerOperation
    {
        BinaryId = -1,
        StartTime = startTime,
        EndTime = startTime + input.TimeToCreateBinary,
        Operation = "new"
    });

    input.Binaries.Add(new Binary
    {
        Services = new List<Service>(),
        Done = false,
        Id = input.Binaries.Count
    });

    input.NumBinaries++;
    engineers.BusyUntil += input.TimeToCreateBinary;
    engineers.AvailableDays -= input.TimeToCreateBinary;
    return input.Binaries.Count-1;
}

static void ImplementFeature(Engineers engineers, Features feature, Binary binary, int i, int endTime)
{
    engineers.Operations.Add(new EnginnerOperation
    {
        BinaryId = binary.Id,
        StartTime = i,
        EndTime = endTime,
        FeatureName = feature.Name,
        Operation = $"impl {feature.Name} {binary.Id}"
    });
}

static void Wait(Engineers engineer, int startTime, int endTime)
{
    var lastEngineerOperation = engineer.Operations.Count() > 0 ? engineer.Operations.ElementAt(engineer.Operations.Count - 1) : null;
    if (lastEngineerOperation != null && lastEngineerOperation.Operation.StartsWith("wait"))
    {
        lastEngineerOperation.EndTime = endTime;
        lastEngineerOperation.Operation = $"wait {endTime - lastEngineerOperation.StartTime}";
    }
    else
    {
        engineer.Operations.Add(new EnginnerOperation
        {
            BinaryId = -1, //wait op
            StartTime = startTime,
            EndTime = endTime,
            Operation = $"wait {endTime - startTime}"
        });
    }
}

bool MoveService(InputModel input, Binary firstBinary, Binary secondBinary, string serviceName, Engineers engineer, int startTime)
{

    if (firstBinary.EngineerWorkingUntil <= startTime && secondBinary.EngineerWorkingUntil <= startTime &&
        firstBinary.NotAvailableUntil <= startTime && secondBinary.NotAvailableUntil <= startTime)
    {
        var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
        int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());

        if (engineer.AvailableDays - moveTime >= 0)
        {
            AddToBinaryBusyTimes(firstBinary, secondBinary, startTime, startTime + moveTime);
            firstBinary.Services.Remove(firstBinaryService);
            secondBinary.Services.Add(firstBinaryService);
            engineer.Operations.Add(new EnginnerOperation
            {
                BinaryId = secondBinary.Id,
                StartTime = startTime,
                EndTime = startTime + moveTime,
                Operation = $"move {serviceName} {secondBinary.Id}"
            });
            engineer.AvailableDays -= moveTime;
            engineer.BusyUntil += moveTime;
            firstBinary.NotAvailableUntil = Math.Max(firstBinary.NotAvailableUntil, startTime + moveTime);
            secondBinary.NotAvailableUntil = Math.Max(secondBinary.NotAvailableUntil, startTime + moveTime);
            alreadyMoved.Add(serviceName);
            return true;
        }
    }
    return false;
}
#endregion

/*
 what to move
rather than checking number of features associated
with respective service --> sum their NumUsersBenifitValues
 */
int GetBinaryServicesValue(Dictionary<string, int> servicesToFeature, Binary binary)
{
    int binaryServicesValue = 0;
    foreach(var service in binary.Services)
    {
        if (servicesToFeature.ContainsKey(service.Name))
        {
            binaryServicesValue += servicesToFeature[service.Name];
        }
    }
    return binaryServicesValue;
}


#region Algorithm Operations
bool MoveAndCreateNewServices(InputModel input, List<Engineers> initialSolution, Dictionary<string, int> servicesToFeature, bool onlymove , List<string> alreadyMoved, int day, int engineerId)
{
    bool moved = false;
    if (!onlymove)
    {
        var createdBinaryId = -1;
        for (int i = 0; i < 1; i++)
        {
            var engineer = initialSolution.Where(x => x.Id == engineerId).FirstOrDefault();
            createdBinaryId = CreateBinary(input, engineer, day);
            if (createdBinaryId != -1)
            {
                var fromBinaryId = input.Binaries.ElementAt(engineerId % input.Binaries.Count);
                if (fromBinaryId != null)
                {
                    var firstBinary = input.Binaries.Where(x => x.Id == fromBinaryId.Id).FirstOrDefault();
                    var secondBinary = input.Binaries.Where(x => x.Id == createdBinaryId).FirstOrDefault();
                    var serviceCollection = firstBinary.Services.Where(x => servicesToFeature.ContainsKey(x.Name)).OrderByDescending(x => servicesToFeature[x.Name]).Take(2); //find a better strategy to select
                    var service = serviceCollection.Where(x => !alreadyMoved.Contains($"{x.Name}")).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    if (firstBinary != null && secondBinary != null && service != null && engineer != null)
                    {
                        if (MoveService(input, firstBinary, secondBinary, service.Name, engineer, engineer.BusyUntil)) //not day as we create binary above
                        {
                            moved = true;
                        }
                        else
                        {
                            RemoveBinary(input, engineer, day, createdBinaryId);
                        }
                    }
                    else
                    {
                        RemoveBinary(input, engineer, day, createdBinaryId);
                    }
                }
            }
        }
    }
    else
    {
        for (int i = 0; i < 10; i++)
        {
            var rnd = new Random();
            var enginner = initialSolution.Where(x => x.Id == engineerId).FirstOrDefault();
            //already impemented services
            var firstBinary = input.Binaries.OrderByDescending(x => x.Services.Count()).Take(input.Binaries.Count / 3).ElementAt(new Random().Next() % (input.Binaries.Count / 4));
            var secondBinary = input.Binaries.Where(x => x.Id != firstBinary.Id)
                                             .OrderBy(x => x.Services.Count())
                                             .FirstOrDefault();

            var service = firstBinary.Services.Where(x => servicesToFeature.ContainsKey(x.Name) && !alreadyMoved.Contains($"{x.Name}"))
                                              .OrderByDescending(x => Guid.NewGuid())
                                              .FirstOrDefault(); //find a better strategy to select

            if (firstBinary != null && secondBinary != null && service != null && enginner != null)
               return MoveService(input, firstBinary, secondBinary, service.Name, enginner, day);

        }
    }

    return moved;
}

void RemoveBinary(InputModel input, Engineers engineers, int day, int binaryId)
{
    var inputBinary = input.Binaries.Where(x => x.Id == binaryId).First();
    var lastCreateOperation = engineers.Operations.Where(x => x.Operation == "new").OrderByDescending(x => x.EndTime).FirstOrDefault();
    engineers.Operations.Remove(lastCreateOperation);
    engineers.AvailableDays += input.TimeToCreateBinary;
    engineers.BusyUntil -= input.TimeToCreateBinary;
    input.Binaries.Remove(inputBinary);
    input.NumBinaries--;
}

void RemoveFeatureFromFeatureList(List<FeatureModel> engineersFeature, FeatureModel feature, int binaryId)
{
    var implementedFeature = engineersFeature.Where(x => x.Feature.Name == feature.Feature.Name).FirstOrDefault();
    if (implementedFeature != null)
    {
        if (implementedFeature.Binaries.Count == 1)
            engineersFeature.Remove(implementedFeature);
        else
        {
            var binary = implementedFeature.Binaries.Where(x => x.Id == binaryId).FirstOrDefault();
            implementedFeature.Binaries.Remove(binary);
        }
    }
}

void AddToBinaryBusyTimes(Binary firstBinary, Binary secondBinary, int startTime, int endTime)
{
    if (binaryBusyTimes.ContainsKey(firstBinary.Id))
    {
        binaryBusyTimes[firstBinary.Id].Add(new BinaryBusyTimes
        {
            StartTime = startTime,
            EndTime = endTime
        });
    }
    else
    {
        binaryBusyTimes.Add(firstBinary.Id, new List<BinaryBusyTimes>()
        {
            new BinaryBusyTimes
            {
                StartTime = startTime,
                EndTime = endTime
            }
        });
    }

    if (binaryBusyTimes.ContainsKey(secondBinary.Id))
    {
        binaryBusyTimes[secondBinary.Id].Add(new BinaryBusyTimes
        {
            StartTime = startTime,
            EndTime = endTime
        });
    }
    else
    {
        binaryBusyTimes.Add(secondBinary.Id, new List<BinaryBusyTimes>()
        {
            new BinaryBusyTimes
            {
                StartTime = startTime,
                EndTime = endTime
            }
        });
    }
}

void CheckIfServicesForScoreAddition(Features feature, Binary featureBinary, int endTime)
{
    if (implementedServices.ContainsKey(feature.Name))
    {
        foreach (var service in feature.Services)
        {
            if (featureBinary.Services.Select(x => x.Name).Contains(service.Name))
            {
                implementedServices[feature.Name].Add(new ServiceImplementedTimes
                {
                    Name = service.Name,
                    EndTime = endTime
                });
            }
        }
    }
    else
    {
        foreach (var service in feature.Services)
        {
            if (featureBinary.Services.Select(x => x.Name).Contains(service.Name))
            {
                if (implementedServices.ContainsKey(feature.Name))
                {
                    implementedServices[feature.Name].Add(new ServiceImplementedTimes
                    {
                        Name = service.Name,
                        EndTime = endTime
                    });
                }
                else
                {
                    implementedServices.Add(feature.Name, new List<ServiceImplementedTimes>
                    {
                        new ServiceImplementedTimes
                        {
                            Name = service.Name,
                            EndTime = endTime
                        }
                    });
                }
            }
        }
    }


    if (feature.Services.Count == implementedServices[feature.Name].Count)
    {
        var lastImplementedServiceTime = implementedServices[feature.Name]
                                                .OrderByDescending(x => x.EndTime)
                                                .FirstOrDefault();

        int numDaysAvailable = input.TimeLimitDays - lastImplementedServiceTime.EndTime;
        int numUsersBenefit = feature.NumUsersBenefit;
        if (numDaysAvailable < 0) numDaysAvailable = 0;
        globalScore += (numDaysAvailable * numUsersBenefit);
    }
}

List<Engineers> CloneSolution(List<Engineers> engineers)
{
    List<Engineers> clonedSolution = new List<Engineers>();
    foreach(var engineer in engineers)
    {
        Engineers clonedEngineer = new Engineers(engineer.Id);
        clonedEngineer.AvailableDays = engineer.AvailableDays;
        clonedEngineer.BusyUntil = engineer.BusyUntil;
        clonedEngineer.Operations = new List<EnginnerOperation>();
        foreach (var operation in engineer.Operations)
        {
            clonedEngineer.Operations.Add(new EnginnerOperation
            {
                BinaryId = operation.BinaryId,
                Operation = operation.Operation,
                StartTime = operation.StartTime,
                EndTime = operation.EndTime,
                FeatureName = operation.FeatureName
            });
        }
        clonedSolution.Add(clonedEngineer);
    }

    return clonedSolution;
}

int GetNumFeaturesForServices(List<string> services, InputModel inputModel)
{
    int count = 0;
    foreach(var feature in inputModel.Features)
    {
        foreach(var featureService in feature.Services.Select(x => x.Name))
        {
            if (services.Contains(featureService))
                count++;
        }
    }
    return count;
}

List<string> GetPartiallyImplementedFeatures(InputModel inputModel, List<Engineers> initialSolution)
{
    List<Features> features = new List<Features>();
    Dictionary<string, int> implementedServices = new Dictionary<string, int>();
    List<string> impOperations = new List<string>();
    var operations = new List<EnginnerOperation>();
    foreach(var engineer in initialSolution)
    {
        foreach(var operation in engineer.Operations)
        {
            operations.Add(operation);
        }
    }

    var groupedOperations = operations.Where(x => !x.Operation.Contains("wait") && 
                                                  !x.Operation.Contains("move") && 
                                                  !x.Operation.Contains("new"))
                                      .GroupBy(x => x.FeatureName)
                                      .ToList();

    foreach(var operation in groupedOperations)
    {
        var featureName = operation.Key;
        var feature = input.Features.Where(x => x.Name == featureName).FirstOrDefault();
        var featureServices = feature.Services.Select(x => x.Name).ToList();
        var featureBinaries = GetBinariesWithFeatureServices(feature, input.Binaries);
        if(operation.Count() != featureBinaries.Count())
        {
            foreach(var op in operation)
            {
                featureBinaries = featureBinaries.Where(x => x.Id != op.BinaryId).ToList();
            }

            foreach(var featureService in featureServices)
            {
                foreach(var binary in featureBinaries)
                {
                    if(binary.Services.Select(x => x.Name).Contains(featureService))
                    {
                        impOperations.Add($"imp {feature.Name} {binary.Id}");
                    }
                }
            }
        }
    }
    return impOperations;
}

List<string> GetNotImplementedFeatures(InputModel inputModel, List<Engineers> initialSolution)
{
    //Features not touched
    List<string> implementedFeatures = new List<string>();
    List<string> notImplementedFeatures = new List<string>();
    foreach(var engineer in initialSolution)
    {
        foreach(var operation in engineer.Operations.Where(x => x.Operation.Contains("impl")).ToList())
        {
            if(!implementedFeatures.Contains(operation.Operation.Split(' ')[1]))
                implementedFeatures.Add(operation.Operation.Split(' ')[1]);
        }
    }

    foreach(var feature in inputModel.Features)
    {
        if (!implementedFeatures.Contains(feature.Name))
            notImplementedFeatures.Add(feature.Name);
    }

    return notImplementedFeatures;
} 

List<string> GetServicesByFeatureName(InputModel inputModel, string featureName)
{
    return inputModel.Features.Where(x => x.Name == featureName)
                              .Select(x => x.Services)
                              .FirstOrDefault()
                              .Select(x => x.Name)
                              .ToList();
}
#endregion

#region Input Processing Operations
///
/// Model feature list where each feature has a binary list
/// where are the services needed for its implementation
///
static List<FeatureModel> ProcessFeatures(InputModel input)
{
    List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
    var features = input.Features;
    var binaries = input.Binaries;
    foreach(var feature in features)
    {
        var featureBinaries = GetBinariesWithFeatureServices(feature, binaries).ToList();
        //if (featureBinaries.Count == 0) continue;
        proccessedFeatures.Add(new FeatureModel 
        { 
            Feature = feature.Clone(), 
            Binaries = new List<Binary>(),
            FeatureBinaryTime = new Dictionary<int, int>(),
            FeatureTimeBinary = 0,
            Services = feature.Services.Select(x => x.Name).ToList()
        });
        foreach(var binary in featureBinaries)
        {
            var baseFeatureTime = GetBaseFeatureTime(feature, binary);
            proccessedFeatures[proccessedFeatures.Count - 1].Binaries.Add(binary.Clone());
            proccessedFeatures[proccessedFeatures.Count - 1].FeatureBinaryTime.Add(binary.Id, baseFeatureTime );

            //if(proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary > baseFeatureTime && proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary != 0)
            //    proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary += baseFeatureTime;
        }

    }

    return proccessedFeatures;
}


static List<FeatureModel> ProcessNotImplementedFeatures(List<string> notImplementedFeatures, InputModel inputModel)
{
    List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
    foreach (var notImplementedFeature in notImplementedFeatures)
    {
        var feature = inputModel.Features.Where(x => x.Name == notImplementedFeature).FirstOrDefault();
        var featureBinaries = GetBinariesWithFeatureServices(feature, inputModel.Binaries);
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
            proccessedFeatures[proccessedFeatures.Count - 1].FeatureBinaryTime.Add(binary.Id, baseFeatureTime);
        }
    }
    return proccessedFeatures;
}

static List<FeatureModel> ProcessFeaturesAlg(List<FeatureModel> features, InputModel input, List<string> alreadyImplemented)
{
    List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
    features = features.Where(x => x.Binaries.Count > 0).ToList();
    foreach (var feature in features)
    {
        var featureBinaries = GetBinariesWithFeatureServices(feature.Feature, input.Binaries).ToList(); //adds binary even if it is removed before
        //if (featureBinaries.Count == 0) continue;
        proccessedFeatures.Add(new FeatureModel
        {
            Feature = feature.Feature.Clone(),
            Binaries = new List<Binary>(),
            FeatureBinaryTime = new Dictionary<int, int>(),
            FeatureTimeBinary = 0,
            Services = feature.Services.ToList()
        });
        foreach (var binary in featureBinaries)
        {
            var baseFeatureTime = GetBaseFeatureTime(feature.Feature, binary);
            proccessedFeatures[proccessedFeatures.Count - 1].Binaries.Add(binary.Clone());
            if(binary.NotAvailableUntil > 0)
                proccessedFeatures[proccessedFeatures.Count - 1].FeatureBinaryTime.Add(binary.Id, baseFeatureTime - ((binary.NotAvailableUntil/input.TimeLimitDays) * 2));
            else
                proccessedFeatures[proccessedFeatures.Count - 1].FeatureBinaryTime.Add(binary.Id, baseFeatureTime);
            if (proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary > baseFeatureTime && proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary != 0)
                proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary += baseFeatureTime;
        }

    }


    if(alreadyImplemented.Count > 0)
    {
        foreach(var feature in alreadyImplemented)
        {
            var featureParts = feature.Split(' ');
            var processedFeature = proccessedFeatures.Where(x => x.Feature.Name == featureParts[1]).FirstOrDefault();
            if (processedFeature != null)
            {
                var processedFeatureBinaries = processedFeature.Binaries;
                if(processedFeatureBinaries.Count > 0)
                {
                    var alreadyImplementedInBinary = processedFeatureBinaries.Where(x => x.Id == int.Parse(featureParts[2])).FirstOrDefault();
                    if (alreadyImplementedInBinary != null)
                    {
                        processedFeature.Binaries.Remove(alreadyImplementedInBinary);
                    }
                }
            }
        }
    }

    return proccessedFeatures;
}

Dictionary<string, int> ServicesToFeatures(InputModel input)
{
    Dictionary<string, int> serviceToFeatures = new Dictionary<string, int>();
    var features = input.Features;
    var processedFeatures = ProcessFeatures(input);
    processedFeatures = processedFeatures.OrderByDescending(x => (x.Feature.NumUsersBenefit)/ (x.FeatureBinaryTime.Values.Sum())).Take(processedFeatures.Count).ToList();   
    foreach (var feature in processedFeatures)
    {
        foreach (var service in feature.Feature.Services)
        {
            if (serviceToFeatures.ContainsKey(service.Name))
                serviceToFeatures[service.Name] += (feature.Feature.NumUsersBenefit/feature.Feature.Difficulty);
            else
            {
                serviceToFeatures.Add(service.Name, feature.Feature.NumUsersBenefit/feature.Feature.Difficulty);
            }
        }
    }

    return serviceToFeatures;
}
#endregion

#region Getter methods
static int GetBaseFeatureTime(Features features, Binary binary)
{
    return features.Difficulty + binary.Services.Count;
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

static List<Binary> GetBinariesWithFeatureServices(Features feature, List<Binary> binaries)
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
#endregion

#region Input/Output operations
static InputModel ReadFile(string path)
{
    InputModel inputModel = new InputModel();
    var fileLines = new List<string>();

    using (StreamReader file = new StreamReader(path))
    {
        string ln;

        while ((ln = file.ReadLine()) != null)
        {
            fileLines.Add(ln);
        }
        file.Close();
        file.Dispose();
    }
    string featureName = string.Empty;
    for(int i = 0; i < fileLines.Count; i++)
    {
        var line = fileLines[i];
        if(i == 0)
        {
            var lineElements = line.Split(' ');
            inputModel.TimeLimitDays = int.Parse(lineElements[0]);
            inputModel.NumEngineers = int.Parse(lineElements[1]);
            for(int j = 0; j < inputModel.NumEngineers; j++)
            {
                inputModel.Engineers.Add(new Engineers(j));
            }    
            inputModel.NumServices = int.Parse(lineElements[2]);
            inputModel.NumBinaries = int.Parse(lineElements[3]);
            for(int j = 0; j < inputModel.NumBinaries; j++)
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
                if(IsNumeric(lineElements[1]))
                {
                    int.TryParse(lineElements[1], out result);
                    inputModel.Services.Add(new Service(lineElements[0]));
                    inputModel.Binaries[result].Services.Add(new Service(lineElements[0]));
                    continue;
                }
            } 
            if(line.Split(' ').Length == 4)
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

            if(featureName != string.Empty)
            {
                var lineElements = line.Split(' ');
                foreach (var elem in lineElements)
                    inputModel.Features.Where(x => x.Name == featureName)?.FirstOrDefault()?.Services.Add(new Service(elem));
            }

        }
    }
    return inputModel;
}

static void SaveSolution(List<Engineers> solution, string filePath)
{
    solution = solution.Where(x => x.Operations.Count > 0).ToList();
    int numEngineersWorking = solution.Count;

    try
    {
        StreamWriter writer = new StreamWriter(filePath);
        writer.WriteLine(numEngineersWorking);
        foreach (var enginner in solution)
        {
            writer.WriteLine(enginner.Operations.Count);
            foreach (var operation in enginner.Operations)
            {
                writer.WriteLine(operation.Operation);
            }
        }
        writer.Close();
        writer.Dispose();
    }
    catch(Exception ex)
    {
        throw ex;
    }
}

static bool IsNumeric(object Expression)
{
    double retNum;

    bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
    return isNum;
}


static int ScoreCalculate(List<Engineers> solution, InputModel baseInputModel)
{
    try
    {

        Dictionary<int, int> engineerOperationMapping = new Dictionary<int, int>();
        List<Engineers> newSolution = new List<Engineers>();
        foreach (var enginneer in solution)
        {
            engineerOperationMapping.Add(enginneer.Id, 0);
            newSolution.Add(new Engineers(enginneer.Id));
            newSolution.Where(x => x.Id == enginneer.Id).First().AvailableDays = baseInputModel.TimeLimitDays;
            enginneer.AvailableDays = baseInputModel.TimeLimitDays;
        }
        int createdBinariesNum = 0;
        foreach (var engineer in solution)
        {
            foreach (var operation in engineer.Operations)
            {
                if (operation.Operation.Contains("new"))
                {
                    CreateBinaryScore(baseInputModel);
                    createdBinariesNum++;
                }
            }
        }

        while (true)
        {
            var engineerSolution = solution.OrderByDescending(x => x.AvailableDays).FirstOrDefault();
            var engineer = newSolution.Where(x => x.Id == engineerSolution.Id).First();
            //index based operation query does not work as different engineers have different number of operations done
            int operationIndex = engineerOperationMapping[engineer.Id];
            EnginnerOperation operation;
            if (engineerSolution.Operations.Count() > operationIndex)
                operation = engineerSolution.Operations.ElementAt(operationIndex);
            else
                break;

            //wait
            if (operation.Operation.Contains("wait"))
            {
                operation.StartTime = engineer.BusyUntil;
                operation.EndTime = engineer.BusyUntil + int.Parse(operation.Operation.Split(' ')[1]);
                engineer.BusyUntil += int.Parse(operation.Operation.Split(' ')[1]);
                engineer.AvailableDays -= int.Parse(operation.Operation.Split(' ')[1]);
                engineerSolution.AvailableDays -= int.Parse(operation.Operation.Split(' ')[1]);
                engineer.Operations.Add(new EnginnerOperation
                {
                    FeatureName = operation.FeatureName,
                    BinaryId = operation.BinaryId,
                    StartTime = operation.StartTime,
                    EndTime = operation.EndTime,
                    Operation = operation.Operation
                });
            }
            else if (operation.Operation.Contains("impl"))
            {
                var feature = baseInputModel.Features.Where(x => x.Name == operation.FeatureName).FirstOrDefault();
                var binary = baseInputModel.Binaries.Where(x => x.Id == operation.BinaryId.Value).FirstOrDefault();
                var featureTime = GetFeatureTime(feature, binary, newSolution, engineer.BusyUntil);
                operation.StartTime = engineer.BusyUntil;
                operation.EndTime = engineer.BusyUntil + featureTime;
                engineer.AvailableDays -= featureTime;
                engineerSolution.AvailableDays -= featureTime;
                engineer.BusyUntil = engineer.BusyUntil + featureTime;
                binary.EngineerWorkingUntil = Math.Max(binary.EngineerWorkingUntil, operation.EndTime);
                engineer.Operations.Add(new EnginnerOperation
                {
                    FeatureName = operation.FeatureName,
                    BinaryId = operation.BinaryId,
                    StartTime = operation.StartTime,
                    EndTime = operation.EndTime,
                    Operation = operation.Operation
                });
                //CheckIfServicesForScoreAddition(feature, binary, operation.EndTime);
            }
            else if (operation.Operation.Contains("move"))
            {
                var toMoveService = operation.Operation.Split(' ')[1];
                var fromBinary = baseInputModel.Binaries.Where(x => x.Services.Select(x => x.Name).Contains(toMoveService)).FirstOrDefault();
                var toBinary = baseInputModel.Binaries.Where(x => x.Id == int.Parse(operation.Operation.Split(' ')[2])).FirstOrDefault();
                int moveTime = Math.Max(fromBinary.Services.Count(), toBinary.Services.Count());
                operation.StartTime = engineer.BusyUntil;
                operation.EndTime = engineer.BusyUntil + moveTime;
                engineer.AvailableDays -= moveTime;
                engineerSolution.AvailableDays -= moveTime;
                engineer.BusyUntil += moveTime;
                fromBinary.NotAvailableUntil += Math.Max(fromBinary.NotAvailableUntil, engineer.BusyUntil + moveTime);
                toBinary.NotAvailableUntil += Math.Max(toBinary.NotAvailableUntil, engineer.BusyUntil + moveTime);
                engineer.Operations.Add(new EnginnerOperation
                {
                    FeatureName = operation.FeatureName,
                    BinaryId = operation.BinaryId,
                    StartTime = operation.StartTime,
                    EndTime = operation.EndTime,
                    Operation = operation.Operation
                });
                MoveServiceScore(baseInputModel, fromBinary, toBinary, toMoveService);

            }
            else if (operation.Operation.Contains("new"))
            {
                operation.StartTime = engineer.BusyUntil;
                operation.EndTime = engineer.BusyUntil + baseInputModel.TimeToCreateBinary;
                engineer.BusyUntil = engineer.BusyUntil + baseInputModel.TimeToCreateBinary;
                engineer.AvailableDays -= baseInputModel.TimeToCreateBinary;
                engineerSolution.AvailableDays -= baseInputModel.TimeToCreateBinary;
                engineer.Operations.Add(new EnginnerOperation
                {
                    FeatureName = operation.FeatureName,
                    BinaryId = operation.BinaryId,
                    StartTime = operation.StartTime,
                    EndTime = operation.EndTime,
                    Operation = operation.Operation
                });
                //CreateBinary(inputModel, engineer, engineer.BusyUntil);
            }
            engineerOperationMapping[engineer.Id] += 1;
            //new
        }

        return SolutionHelpers.CalculateScore(newSolution, baseInputModel);
    }
    catch (Exception)
    {
        return 0;
    }
}

static int CalculateScore(List<Engineers> engineers, InputModel inputModel)
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
        if (featureBinaries.Binaries.Count() != data.Count()) // to fix move service can cause incorrect validation
            continue;
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
#endregion

#region Operators
List<Engineers> RemoveRandomImplOperator(List<Engineers> engineers, int timeLimit)
{
    var clonedSolution = CloneSolution(engineers);

    var random = new Random();
    var randomEngineerIndex = random.Next(0, engineers.Count);
    var engineer = clonedSolution[randomEngineerIndex];
    var randomEnginnerOperationIndex = random.Next(0, engineer.Operations.Count);

    if (engineer.Operations.ElementAt(randomEnginnerOperationIndex).Operation.Contains("impl"))
    {
        //todo validate using binaryBusyTimes
        engineer.Operations.RemoveAt(randomEnginnerOperationIndex);
        for (int i = randomEnginnerOperationIndex; i < engineer.Operations.Count; i++)
        {
            int beforeOperationEndTime = engineer.Operations.ElementAt(i - 1).EndTime;
            int operationTime = engineer.Operations[i].EndTime - engineer.Operations[i].StartTime;
            engineer.Operations[i].StartTime = beforeOperationEndTime;
            engineer.Operations[i].EndTime = beforeOperationEndTime + operationTime;
        }
        engineer.AvailableDays = timeLimit - engineer.Operations.ElementAt(engineer.Operations.Count - 1).EndTime;
        engineer.BusyUntil = engineer.Operations.ElementAt(engineer.Operations.Count - 1).EndTime;
    }

    return clonedSolution;
}

List<Engineers> RemoveRandomImplFeature(List<Engineers> engineers, InputModel inputModel)
{
    var clonedSolution = CloneSolution(engineers);

    var random = new Random();
    var randomEngineerIndex = random.Next(0, engineers.Count);
    var selectedEngineer = clonedSolution[randomEngineerIndex];
    var randomEnginnerOperationIndex = random.Next(0, selectedEngineer.Operations.Count);
    var featureOperationList = new List<string>();

    if (selectedEngineer.Operations.ElementAt(randomEnginnerOperationIndex).Operation.Contains("impl"))
    {
        var implementedFeature = selectedEngineer.Operations.ElementAt(randomEnginnerOperationIndex).Operation.Split(' ')[1];
        var feature = inputModel.Features.Where(x => x.Name == implementedFeature).FirstOrDefault();
        var featureBinaries = GetBinariesWithFeatureServices(feature, inputModel.Binaries);
        
        foreach (var featureBinary in featureBinaries)
            featureOperationList.Add($"impl {feature.Name} {featureBinary.Id}");

        foreach(var operation in featureOperationList)
        {
            var engineer = clonedSolution.Where(x => x.Operations.Select(y => y.Operation).Contains(operation)).FirstOrDefault();
            if(engineer != null)
            {
                var operationIndex = FindEngineerOperationIndex(engineer, operation);
                engineer.Operations.RemoveAt(operationIndex);
                for (int i = randomEnginnerOperationIndex; i < engineer.Operations.Count; i++)
                {
                    int beforeOperationEndTime = engineer.Operations.ElementAt(i - 1).EndTime;
                    int operationTime = engineer.Operations[i].EndTime - engineer.Operations[i].StartTime;
                    engineer.Operations[i].StartTime = beforeOperationEndTime;
                    engineer.Operations[i].EndTime = beforeOperationEndTime + operationTime;
                }
                engineer.AvailableDays = inputModel.TimeLimitDays - engineer.Operations.ElementAt(engineer.Operations.Count - 1).EndTime;
                engineer.BusyUntil = engineer.Operations.ElementAt(engineer.Operations.Count - 1).EndTime;
            }
        }

        return clonedSolution;
    }

    return null;
}

List<Engineers> AddNotImplementedFeature(List<Engineers> engineers, List<FeatureModel> processedFeatures, InputModel inputModel)
{
    var clonedSolution = CloneSolution(engineers);
    var featureModel = processedFeatures.OrderBy(x => x.FeatureBinaryTime.Values.Sum()).FirstOrDefault();
    foreach(var pair in featureModel.FeatureBinaryTime)
    {
        var selectedEngineer = clonedSolution.OrderByDescending(engineer => engineer.AvailableDays).FirstOrDefault();
        if (pair.Value <= selectedEngineer.AvailableDays)
        {
            var featureBinary = inputModel.Binaries.Where(x => x.Id == pair.Key).FirstOrDefault();
            var featureTime = GetFeatureTime(featureModel.Feature, featureBinary, clonedSolution, selectedEngineer.BusyUntil);
            var endTime = selectedEngineer.BusyUntil + featureTime;
            ImplementFeature(selectedEngineer, featureModel.Feature, featureBinary, selectedEngineer.BusyUntil, endTime);
            selectedEngineer.AvailableDays -= featureTime;
            selectedEngineer.BusyUntil = endTime;
        }
    }

    return clonedSolution;

}

List<Engineers> MoveImplBetweenEngineers(List<Engineers> engineers, InputModel inputModel)
{
    var clonedSolution = CloneSolution(engineers);

    var rnd = new Random();
    int firstEngineerIndex = rnd.Next(0, clonedSolution.Count - 1);
    int secondEngineerIndex = rnd.Next(0, clonedSolution.Count - 1);
    if(firstEngineerIndex == secondEngineerIndex)
        secondEngineerIndex = (secondEngineerIndex + 1) % clonedSolution.Count;

    var firstEngineer = clonedSolution.ElementAt(firstEngineerIndex);
    var secondEngineer = clonedSolution.ElementAt(secondEngineerIndex);

    //todo 

    return clonedSolution;
}

List<Engineers> ReplaceTwoRandomImplOperation(List<Engineers> engineers, InputModel inputModel)
{
    var clonedSolution = CloneSolution(engineers);

    var rnd = new Random();
    int selectedEngineerIndex = rnd.Next(0, clonedSolution.Count-1);
    var selectedEngineer = clonedSolution.ElementAt(selectedEngineerIndex);

    int firstOperationIndex = 0;
    int secondOperationIndex = 0;
    do
    {
        firstOperationIndex = rnd.Next(0, selectedEngineer.Operations.Count - 1);
        secondOperationIndex = rnd.Next(0, selectedEngineer.Operations.Count - 1);
    }
    while (!selectedEngineer.Operations.ElementAt(firstOperationIndex).Operation.StartsWith("impl") ||
           !selectedEngineer.Operations.ElementAt(secondOperationIndex).Operation.StartsWith("impl") ||
           firstOperationIndex == secondOperationIndex);

    int operationCount = 0;
    if(secondOperationIndex < firstOperationIndex)
    {
        var temp = firstOperationIndex;
        firstOperationIndex = secondOperationIndex;
        secondOperationIndex = temp;
    }


    operationCount = firstOperationIndex;
    var firstOperation = selectedEngineer.Operations.ElementAt(firstOperationIndex).Clone();
    var secondOperation = selectedEngineer.Operations.ElementAt(secondOperationIndex).Clone();
    while(operationCount <= secondOperationIndex)
    {
        var lastOperation = selectedEngineer.Operations.ElementAt(operationCount - 1);
        if(operationCount == firstOperationIndex)
        {
            selectedEngineer.Operations.ElementAt(firstOperationIndex).Operation = secondOperation.Operation;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).EndTime = lastOperation.EndTime + (secondOperation.EndTime - secondOperation.StartTime);
            selectedEngineer.Operations.ElementAt(firstOperationIndex).FeatureName = secondOperation.FeatureName;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).BinaryId = secondOperation.BinaryId;
        }
        else if(operationCount == secondOperationIndex)
        {
            selectedEngineer.Operations.ElementAt(secondOperationIndex).Operation = firstOperation.Operation;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).EndTime = lastOperation.EndTime + (firstOperation.EndTime - firstOperation.StartTime);
            selectedEngineer.Operations.ElementAt(secondOperationIndex).FeatureName = firstOperation.FeatureName;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).BinaryId = firstOperation.BinaryId;
        }
        else
        {
            int currentOperationTime = selectedEngineer.Operations.ElementAt(operationCount).EndTime -
                                        selectedEngineer.Operations.ElementAt(operationCount).StartTime;

            selectedEngineer.Operations.ElementAt(operationCount).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(operationCount).EndTime = lastOperation.EndTime + currentOperationTime;
        }
        operationCount++;
    }



    return clonedSolution;
}

List<Engineers> ReplaceTwoRandomImplOperationTabu(List<Engineers> engineers, InputModel inputModel, Dictionary<string, List<TabuModel>> tabuList, int score)
{
    var clonedSolution = CloneSolution(engineers);
    Engineers selectedEngineer;
    int selectedEngineerIndex = 0;

    var rnd = new Random();
    while(true) 
    {
        selectedEngineerIndex = rnd.Next() % clonedSolution.Count;
        selectedEngineer = clonedSolution.ElementAt(selectedEngineerIndex);
        if (selectedEngineer.Operations.Where(x => x.Operation.StartsWith("impl")).Count() > 1)
            break;
    }

    int firstOperationIndex = 0;
    int secondOperationIndex = 0;
    do
    {
        firstOperationIndex = rnd.Next(0, selectedEngineer.Operations.Count - 1);
        //Thread.Sleep(50);
        secondOperationIndex = rnd.Next(0, selectedEngineer.Operations.Count - 1);
        //secondOperationIndex = (firstOperationIndex + 1) % (selectedEngineer.Operations.Count);
    }
    while (!selectedEngineer.Operations.ElementAt(firstOperationIndex).Operation.StartsWith("impl") ||
           !selectedEngineer.Operations.ElementAt(secondOperationIndex).Operation.StartsWith("impl") ||
           firstOperationIndex == secondOperationIndex);

    int operationCount = 0;
    if (secondOperationIndex < firstOperationIndex)
    {
        var temp = firstOperationIndex;
        firstOperationIndex = secondOperationIndex;
        secondOperationIndex = temp;
    }


    operationCount = firstOperationIndex;
    var firstOperation = selectedEngineer.Operations.ElementAt(firstOperationIndex).Clone();
    var secondOperation = selectedEngineer.Operations.ElementAt(secondOperationIndex).Clone();
   

    while (operationCount <= secondOperationIndex)
    {
        EnginnerOperation lastOperation;
        if (firstOperation.StartTime == 0)
            lastOperation = selectedEngineer.Operations.ElementAt(operationCount);
        else
            lastOperation = selectedEngineer.Operations.ElementAt(operationCount - 1);
        if (operationCount == firstOperationIndex)
        {
            selectedEngineer.Operations.ElementAt(firstOperationIndex).Operation = secondOperation.Operation;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).EndTime = lastOperation.EndTime + (secondOperation.EndTime - secondOperation.StartTime);
            selectedEngineer.Operations.ElementAt(firstOperationIndex).FeatureName = secondOperation.FeatureName;
            selectedEngineer.Operations.ElementAt(firstOperationIndex).BinaryId = secondOperation.BinaryId;
        }
        else if (operationCount == secondOperationIndex)
        {
            selectedEngineer.Operations.ElementAt(secondOperationIndex).Operation = firstOperation.Operation;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).EndTime = lastOperation.EndTime + (firstOperation.EndTime - firstOperation.StartTime);
            selectedEngineer.Operations.ElementAt(secondOperationIndex).FeatureName = firstOperation.FeatureName;
            selectedEngineer.Operations.ElementAt(secondOperationIndex).BinaryId = firstOperation.BinaryId;
        }
        else
        {
            int currentOperationTime = selectedEngineer.Operations.ElementAt(operationCount).EndTime -
                                        selectedEngineer.Operations.ElementAt(operationCount).StartTime;

            selectedEngineer.Operations.ElementAt(operationCount).StartTime = lastOperation.EndTime;
            selectedEngineer.Operations.ElementAt(operationCount).EndTime = lastOperation.EndTime + currentOperationTime;
        }
        operationCount++;
    }

    //add to tabu list
    if (tabuList.ContainsKey(firstOperation.Operation))
    {
        if (!tabuList[firstOperation.Operation].Select(x => x.Operation).Contains(secondOperation.Operation))
        {
            tabuList[firstOperation.Operation].Add(new TabuModel
            {
                Operation = secondOperation.Operation,
                NumIterations = 40
            });
        }
        else
        {
            //check aspiration criteria
            var baseInput = ReadFile(inputFile);
            if (ScoreCalculate(clonedSolution, baseInput) < score)
                return null;
        }
    }
    else
    {
        tabuList.Add(firstOperation.Operation, new List<TabuModel>()
        {
            new TabuModel()
            {
                NumIterations = 40,
                Operation = secondOperation.Operation
            }
        });
    }


    if (tabuList.ContainsKey(secondOperation.Operation))
    {
        if (!tabuList[secondOperation.Operation].Select(x => x.Operation).Contains(firstOperation.Operation))
        {
            tabuList[secondOperation.Operation].Add(new TabuModel
            {
                Operation = firstOperation.Operation,
                NumIterations = 40
            });
        }
        else
        {
            //check aspiration criteria
            var baseInput = ReadFile(inputFile);
            if (ScoreCalculate(clonedSolution, baseInput) < score)
                return null;
        }
    }
    else
    {
        tabuList.Add(secondOperation.Operation, new List<TabuModel>()
        {
            new TabuModel()
            {
                NumIterations = 40,
                Operation = firstOperation.Operation
            }
        });
    }

    return clonedSolution;
}

#endregion


#region Method Helpers
int FindEngineerOperationIndex(Engineers engineer, string operation)
{
    int index = 0;
    foreach(var engineerOperation in engineer.Operations.Select(x => x.Operation))
    {
        if (operation == engineerOperation)
            break;

        index++;
    }

    return index;
}

#endregion


static void UpdateTabuList(Dictionary<string, List<TabuModel>> tabuList)
{
    Dictionary<string,List<TabuModel>> toRemoveModels = new Dictionary<string, List<TabuModel>>();
    foreach(var element in tabuList)
    {
        foreach(var tabuModel in element.Value)
        {
            tabuModel.NumIterations -= 1;
            if (tabuModel.NumIterations <= 0)
            {
                tabuModel.NumIterations = 0;
                if(toRemoveModels.ContainsKey(element.Key))
                {
                    toRemoveModels[element.Key].Add(tabuModel);
                }
                else
                    toRemoveModels.Add(element.Key, new List<TabuModel> { tabuModel});
            }
        }
    }

    foreach(var element in toRemoveModels)
    {
        foreach(var model in element.Value)
            tabuList[element.Key].Remove(model);
    }
}

static void CreateBinaryScore(InputModel inputModel)
{
    var lastBinaryId = inputModel.Binaries.OrderByDescending(x => x.Id).FirstOrDefault().Id;
    inputModel.Binaries.Add(new Binary
    {
        Services = new List<Service>(),
        Done = false,
        Id = lastBinaryId + 1
    });
}

static void MoveServiceScore(InputModel input, Binary firstBinary, Binary secondBinary, string serviceName)
{
    var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
    int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());
    firstBinary.Services.Remove(firstBinaryService);
    secondBinary.Services.Add(firstBinaryService);
}

static InputModel CloneInputFile(InputModel inputModel)
{
    return inputModel.Clone();
}

