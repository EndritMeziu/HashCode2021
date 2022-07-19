using HashCode2021;
using HashCode2021.Extensions;
using HashCode2021.Input;
using HashCode2021.Validator;
Dictionary<int, List<int>> binaryBusyTimes = new Dictionary<int, List<int>>();
List<FeatureModel> features = new List<FeatureModel>();
var inputModel = ReadFile(@"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\five_thousand.txt");
InitialSolution1(inputModel);

///
///Generate initial solution
///
 void InitialSolution1(InputModel input)
{
    var initialSolution = new List<Engineers>();
    foreach (var enginner in input.Engineers)
    {
        enginner.AvailableDays = input.TimeLimitDays;
        initialSolution.Add(enginner);
    }

    int initialBiniaries = input.NumBinaries;


    var serviceToFeatures = ServicesToFeatures(inputModel);
    MoveAndCreateNewServices(input, initialSolution, serviceToFeatures, false);

    features = ProcessFeatures(input);
    Dictionary<int, List<FeatureModel>> engineersFeature = new Dictionary<int, List<FeatureModel>>();
    List<FeatureModel> doableFeatures;
    var rnd = new Random();
    for (int i = 0; i < input.TimeLimitDays; i++)       
    {
        foreach (var engineer in initialSolution)
        {
            if (engineer.BusyUntil > i)
                continue;


            //if (rnd.Next() % 100 < 5 && i < input.TimeLimitDays / 10)
            //{
            //    var 
            //    MoveService(inputModel, );
            //    features = ProcessFeatures(inputModel);
            //}

            if (!engineersFeature.ContainsKey(engineer.Id)) 
                engineersFeature = GetDoableFeatures(initialSolution, engineer.Id, features.ToList(), i);


            doableFeatures = engineersFeature[engineer.Id];
            if (doableFeatures.Count > 0)
            {
                FeatureModel toDoFeature;
                int count = 0;

                toDoFeature = doableFeatures.OrderByDescending(x => (x.Feature.NumUsersBenefit / (x.Feature.Services.Count * x.Feature.Difficulty))).FirstOrDefault();
                while (count < 10)
                {
                    var feature = toDoFeature?.Feature;

                    var featureBinary = toDoFeature?.Binaries.OrderBy(x => x.Services.Count()).FirstOrDefault();
                    var featureTime = GetFeatureTime(feature, featureBinary, initialSolution, i);

                    if (featureBinary.NotAvailableUntil > i)
                    {
                        count++;
                        //toDoFeature = doableFeatures.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                        continue;
                    }
                    if (engineer.AvailableDays - featureTime >= 0)
                    {
                        var endTime = i + featureTime;
                        engineer.AvailableDays -= featureTime;
                        engineer.BusyUntil = endTime;
                        featureBinary.EngineerWorkingUntil  = Math.Max(featureBinary.EngineerWorkingUntil, endTime);
                        ImplementFeature(engineer, feature, featureBinary, i, endTime);
                        features.Where(x => x.Feature.Name == feature.Name).FirstOrDefault().Binaries.Where(x => x.Id == featureBinary.Id).FirstOrDefault().Done = true;
                        RemoveFeatureFromFeatureList(engineersFeature, toDoFeature, featureBinary.Id);
                        break;
                    }
                    count++;
                }

                if (count == 10)
                {
                    if (engineer.BusyUntil <= i)
                    {
                        engineer.BusyUntil += 1;
                        engineer.AvailableDays -= 1;
                        Wait(engineer, i, i + 1);
                    }
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


    SaveSolution(initialSolution, @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt");
    var isValid = SolutionValidator.CheckTaskSchedulingBetweenEngineers(initialSolution);
    if (isValid)
    {
        int score = CalculateScore(initialSolution, input);
        Console.WriteLine("Score:" + score);
    }
}


Dictionary<string, int> ServicesToFeatures(InputModel inputModel)
{
    Dictionary<string, int> serviceToFeatures = new Dictionary<string, int>();
    var features = inputModel.Features;
    foreach(var feature in features)
    {
        foreach(var service in feature.Services)
        {
            if (serviceToFeatures.ContainsKey(service.Name))
                serviceToFeatures[service.Name]++;
            else
            {
                serviceToFeatures.Add(service.Name, 1);
            }
        }
    }

    return serviceToFeatures;
}

void MoveAndCreateNewServices(InputModel inputModel, List<Engineers> initialSolution, Dictionary<string, int> servicesToFeature, bool onlymove)
{
    if (!onlymove)
    {
        var createdBinaryId = -1;
        for (int i = 0; i < 102; i++)
        {
            var rnd = new Random();
            var enginner = initialSolution.OrderByDescending(x => x.AvailableDays).FirstOrDefault();
            createdBinaryId = CreateBinary(inputModel, enginner, enginner.BusyUntil);
            if (createdBinaryId != -1)
            {
                int rounds = 0;
                int fromBinaryId = inputModel.Binaries.OrderByDescending(x => x.Services.Count()).FirstOrDefault().Id;
                while (rounds++ <= 0)
                {
                    var firstBinary = inputModel.Binaries.Where(x => x.Id == fromBinaryId).FirstOrDefault();
                    var secondBinary = inputModel.Binaries.Where(x => x.Id == createdBinaryId).FirstOrDefault();
                    var service = firstBinary.Services.Where(x => servicesToFeature.ContainsKey(x.Name)).OrderByDescending(x => servicesToFeature[x.Name]).FirstOrDefault(); //find a better strategy to select
                    if (firstBinary != null && secondBinary != null && service != null && enginner != null)
                        MoveService(inputModel, firstBinary, secondBinary, service.Name, enginner, enginner.BusyUntil);
                }
            }
        }
    }
    else
    {
        int rounds = 0;
        for (int i = 0; i < 10; i++) 
        { 
            var rnd = new Random();
            var enginner = initialSolution.OrderByDescending(x => x.AvailableDays).FirstOrDefault();
            //already impemented services
            var firstBinary = inputModel.Binaries.OrderByDescending(x => x.Services.Count()).FirstOrDefault();
            var secondBinary = inputModel.Binaries.OrderBy(x => x.Services.Count()).FirstOrDefault();
            var service = firstBinary.Services.Where(x => servicesToFeature.ContainsKey(x.Name)).OrderByDescending(x => servicesToFeature[x.Name]).FirstOrDefault(); //find a better strategy to select
            if (firstBinary != null && secondBinary != null && service != null && enginner != null)
                MoveService(inputModel, firstBinary, secondBinary, service.Name, enginner, enginner.BusyUntil);

        }
    }
}

void DoRadomMoveService(InputModel inputModel, List<Engineers> initialSolution, int engineerId)
{
    var rnd = new Random();
    var enginner = initialSolution.Where(x => x.Id == engineerId).FirstOrDefault();
    //already impemented services
    var firstBinary = inputModel.Binaries.OrderByDescending(x => x.Services.Count()).FirstOrDefault();
    var secondBinary = inputModel.Binaries.OrderBy(x => x.Services.Count()).FirstOrDefault();
    if (firstBinary.Id == secondBinary.Id)
        return;
    var service = firstBinary.Services.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
    if (firstBinary != null && secondBinary != null && service != null && enginner != null)
    {
        MoveService(inputModel, firstBinary, secondBinary, service.Name, enginner, enginner.BusyUntil);
        features = ProcessFeatures(inputModel);
    }
}


void RemoveFeatureFromFeatureList(Dictionary<int, List<FeatureModel>> engineersFeature, FeatureModel feature, int binaryId)
{
    foreach (var elem in engineersFeature)
    {
        var toRemove = elem.Value.Where(x => x.Feature.Name == feature.Feature.Name).FirstOrDefault();
        if (toRemove.Binaries.Count == 1)
        {
            elem.Value.Remove(toRemove);
        }
        else
        {
            var binary = toRemove.Binaries.Where(x => x.Id == binaryId).FirstOrDefault();
            var binaryServices = binary.Services;
            var featureBinaryTime = toRemove.FeatureBinaryTime[binary.Id];
            toRemove.FeatureTotalTime -= featureBinaryTime;
            foreach(var service in binaryServices)
            {
                if (toRemove.Services.Contains(service.Name))
                    toRemove.Services.Remove(service.Name);

            }
            toRemove.Binaries.Remove(binary);
        }
    }
}

 void MoveService(InputModel inputModel, Binary firstBinary, Binary secondBinary, string serviceName, Engineers engineer, int startTime)
{

    if (firstBinary.EngineerWorkingUntil <= startTime && secondBinary.EngineerWorkingUntil <= startTime)
    {
        var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
        int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());

        if (engineer.AvailableDays - moveTime >= 0)
        {
            if (binaryBusyTimes.ContainsKey(firstBinary.Id)) 
            firstBinary.Services.Remove(firstBinaryService);
            secondBinary.Services.Add(firstBinaryService);
            engineer.Operations.Add(new EnginnerOperation
            {
                BinaryId = -1,
                StartTime = startTime,
                EndTime = startTime + moveTime,
                Operation = $"move {serviceName} {secondBinary.Id}"
            });
            engineer.AvailableDays -= moveTime;
            engineer.BusyUntil += moveTime;
            firstBinary.NotAvailableUntil = Math.Max(firstBinary.NotAvailableUntil, startTime + moveTime);
            secondBinary.NotAvailableUntil = Math.Max(secondBinary.NotAvailableUntil, startTime + moveTime);
        }
    }
}


static int CreateBinary(InputModel inputModel, Engineers engineers, int startTime)
{
    if (engineers.AvailableDays - inputModel.TimeToCreateBinary < 0)
        return -1;

    engineers.Operations.Add(new EnginnerOperation
    {
        BinaryId = -1,
        StartTime = startTime,
        EndTime = startTime + inputModel.TimeToCreateBinary,
        Operation = "new"
    });

    var lastBinaryId = inputModel.Binaries.OrderByDescending(x => x.Id).FirstOrDefault().Id;
    inputModel.Binaries.Add(new Binary
    {
        Services = new List<Service>(),
        Done = false,
        Id = lastBinaryId + 1
    });

    inputModel.NumBinaries++;
    engineers.BusyUntil += inputModel.TimeToCreateBinary;
    engineers.AvailableDays -= inputModel.TimeToCreateBinary;
    return lastBinaryId + 1;
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
    engineer.BusyUntil += (endTime - startTime);
    engineer.AvailableDays -= (endTime - startTime);
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

///
/// Calculate solution score
/// 
static int CalculateScore(List<Engineers> engineers, InputModel inputModel)
{
    int score = 0;
    var processedFeatures = ProcessFeatures(inputModel);
    List<EnginnerOperation> operations = new List<EnginnerOperation>();
    foreach(var engineer in engineers)
    {
        operations.AddRange(engineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move") && !x.Operation.StartsWith("new")).ToList());
    }

    var operations1 = operations.GroupBy(x => x.FeatureName).ToList();
    foreach(var data in operations1)
    {
            
        var feature = data.OrderByDescending(x => x.EndTime).FirstOrDefault();
        var inputFeature = inputModel.Features.Where(x => x.Name == feature.FeatureName).FirstOrDefault();
        var featureBinaries = processedFeatures.Where(x => x.Feature.Name == feature.FeatureName).FirstOrDefault();
        if (featureBinaries.Binaries.Count() != data.Count()) continue;
        if(feature != null)
        {
            int numDaysAvailable = inputModel.TimeLimitDays - feature.EndTime;
            int numUsersBenefit = inputFeature.NumUsersBenefit;
            if (numDaysAvailable < 0) numDaysAvailable = 0;
            score += (numDaysAvailable * numUsersBenefit);
        }
    } 
    return score;
}

///
/// Model feature list where each feature has a binary list
/// where are the services needed for its implementation
///
static List<FeatureModel> ProcessFeatures(InputModel inputModel)
{
    List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
    var features = inputModel.Features;
    var binaries = inputModel.Binaries;
    foreach(var feature in features)
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
        foreach(var binary in featureBinaries)
        {
            var baseFeatureTime = GetBaseFeatureTime(feature, binary);
            proccessedFeatures[proccessedFeatures.Count - 1].Binaries.Add(binary.Clone());
            proccessedFeatures[proccessedFeatures.Count - 1].FeatureBinaryTime.Add(binary.Id, baseFeatureTime );
            if(proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary > baseFeatureTime && proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary != 0)
                proccessedFeatures[proccessedFeatures.Count - 1].FeatureTimeBinary += baseFeatureTime;
        }

    }

    return proccessedFeatures;
}

///
///Get features that can be done
///
static Dictionary<int,List<FeatureModel>> GetDoableFeatures(List<Engineers> engineers, int engineerId, List<FeatureModel> features, int day)
{
    Dictionary<int, List<FeatureModel>> currentDict = new Dictionary<int, List<FeatureModel>>();
    features = features.Select(x => new FeatureModel
    {
        Feature = x.Feature.Clone(),
        Binaries = x.Binaries.Where(x => x.Done == false).ToList(),
        FeatureBinaryTime = x.FeatureBinaryTime,
        Services = x.Services
    }).ToList();

    features = features.Where(x => x.Binaries.Count > 0).ToList();
    List<FeatureModel> availableFeatures = new List<FeatureModel>();
    var engineer = engineers.Where(x => x.Id == engineerId).FirstOrDefault();

    foreach (var feature in features.ToList())
    {
        foreach (var binary in feature.Binaries.ToList()) 
        {
            if (GetFeatureTime(feature.Feature, binary, engineers, day) <= engineer.AvailableDays)
            {
                var existingFeature = availableFeatures.Where(x => x.Feature.Name == feature.Feature.Name).FirstOrDefault();
                if (existingFeature != null)
                {
                    existingFeature.Binaries.Add(binary);
                    existingFeature.FeatureBinaryTime.Add(binary.Id, feature.FeatureBinaryTime[binary.Id]);
                    existingFeature.FeatureTotalTime += feature.FeatureBinaryTime[binary.Id];
                    if (existingFeature.FeatureTimeBinary > feature.FeatureBinaryTime[binary.Id] && existingFeature.FeatureTimeBinary != 0)
                        existingFeature.FeatureTimeBinary = feature.FeatureBinaryTime[binary.Id];
                }
                else
                {
                    var featureBinaryTime = new Dictionary<int, int>();
                    featureBinaryTime.Add(binary.Id, feature.FeatureBinaryTime[binary.Id]);
                    availableFeatures.Add(new FeatureModel
                    {
                        Feature = feature.Feature,
                        Binaries = new List<Binary> { binary },
                        FeatureBinaryTime = featureBinaryTime,
                        FeatureTimeBinary = feature.FeatureBinaryTime[binary.Id],
                        FeatureTotalTime = feature.FeatureBinaryTime[binary.Id],
                        Services = feature.Services
                    });
                }
            }
                
        }
    }

    foreach(var elem in engineers)
    {
        List<FeatureModel> engineerFeatures = new List<FeatureModel>();
        availableFeatures.ForEach(x => engineerFeatures.Add(new FeatureModel
        {
            Binaries = Extensions.Clone<Binary>(x.Binaries.ToList()).ToList(),
            Feature = x.Feature.Clone(),
            FeatureBinaryTime = x.FeatureBinaryTime.ToDictionary(x => x.Key, y => y.Value),
            FeatureTimeBinary = x.FeatureTimeBinary,
            FeatureTotalTime = x.FeatureTotalTime,
            Services = x.Services.ToList()
        }));
        currentDict.Add(elem.Id, engineerFeatures);
    }
    return currentDict;
}

static void SaveSolution(List<Engineers> solution, string filePath)
{
    solution = solution.Where(x => x.Operations.Count > 0).ToList();
    int numEngineersWorking = solution.Count;
    using(var writer = new StreamWriter(filePath))
    {
        writer.WriteLine(numEngineersWorking);
        foreach(var enginner in solution)
        {
            writer.WriteLine(enginner.Operations.Count);
            foreach(var operation in enginner.Operations)
            {
                writer.WriteLine(operation.Operation);
            }
        }
    }
}
static FeatureModel GetFeatureWithLeastBinaries(List<Binary> binaries, List<FeatureModel> featuresModel)
{
    int numBinaries = int.MaxValue;
    var bestFeature = new FeatureModel();
    foreach(var featureModel in featuresModel)
    {
       var toUseBinaries = BinariesWithFeatureServices(featureModel.Feature, binaries);
       if(toUseBinaries.Count < numBinaries)
       {
            numBinaries = toUseBinaries.Count;
            bestFeature = featureModel.Clone();
       }
    }
    return bestFeature;
}

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
    foreach(var engineer in engineers)
    {
        if(engineer.Operations.Count > 0)
        {
            foreach(var operation in engineer.Operations)
            {
                if(binary.Id == operation.BinaryId && (time >= operation.StartTime && time < operation.EndTime))
                {
                    numEngineers++;
                }
            }
        }
    }
    return numEngineers;
}

static List<Binary> BinariesWithFeatureServices(Features feature, List<Binary> binaries)
{
    var correctBinaries = new List<Binary>();
    var featureServices = feature.Services;
    foreach(var binary in binaries)
    {
        var binaryServices = binary.Services;
        foreach(var service in featureServices)
        {
            if(binaryServices.Select(x => x.Name).Contains(service.Name))
            {
                correctBinaries.Add(binary);
                break;
            }
        }
    }
    return correctBinaries;
}
#region Methods
static InputModel ReadFile(string path)
{
    InputModel inputModel = new InputModel();
    var fileLines = File.ReadAllLines(path);
    string featureName = string.Empty;
    for(int i = 0; i < fileLines.Length; i++)
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

static bool IsNumeric(object Expression)
{
    double retNum;

    bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
    return isNum;
}

#endregion