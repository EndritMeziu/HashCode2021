using HashCode2021;
using HashCode2021.Input;

var inputModel = ReadFile(@"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\distinction.txt");
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

    var features = ProcessFeatures(input);
        
   

    var binaries = input.Binaries;
    features = ProcessFeatures(input);
    var rnd = new Random();
    List<FeatureModel> doableFeatures = null;
    int count = 0;
    //todo change FeatureModel to List<Binaries>
    for (int i = 0; i < input.TimeLimitDays; i++)       
    {
        count = i;
        foreach(var engineer in initialSolution)
        {
            if (engineer.BusyUntil > i)
                continue;
            //if (rnd.Next() % 100 < 5 && i < input.TimeLimitDays / 10)
            //{
            //    performMoveService(1, features, input, engineer);
            //    features = ProcessFeatures(inputModel);
            //}

            doableFeatures = GetDoableFeatures(initialSolution, engineer.Id, features.ToList(), i);

            if (doableFeatures.Count > 0)
            {
                int random = rnd.Next() % 10;
                FeatureModel toDoFeature = null;
                if(random < 5)
                    toDoFeature = doableFeatures.OrderBy(x => x.Services.Count()).FirstOrDefault();
                else
                    toDoFeature = doableFeatures.OrderBy(x => x.Binaries.Count()).FirstOrDefault();
                //var toDoFeature = GetFeatureWithLeastBinaries(binaries, doableFeatures.ToList());
                var feature = toDoFeature.Feature;

                var featureBinary = toDoFeature.Binaries.FirstOrDefault();
                var featureTime = GetFeatureTime(feature, featureBinary, initialSolution, i);

                if (engineer.AvailableDays - featureTime >= 0)
                {
                    var endTime = i + featureTime;
                    engineer.AvailableDays -= featureTime;
                    engineer.BusyUntil = endTime;
                    ImplementFeature(engineer, feature, featureBinary, i, endTime);
                    //todo remove binary from feature
                    features.Where(x => x.Feature.Name == feature.Name).FirstOrDefault().Binaries.Where(x => x.Id == featureBinary.Id).FirstOrDefault().Done = true;
                    //featureBinary.Done = true;
                    doableFeatures.Remove(toDoFeature);
                }
                
            }
            else
            {
                if (engineer.BusyUntil <= i)
                {
                    //Wait(engineer, i, i + 1);
                    //engineer.BusyUntil = i + 1;
                    int x = 2;
                }
            }
        }
    }

    SaveSolution(initialSolution, @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt");
    int score = CalculateScore(initialSolution, input);
    Console.WriteLine("Score:"+score);
}

void performMoveService(int limit, List<FeatureModel> features, InputModel input, Engineers engineer)
{
    var toMoveServices = new List<string>();
    var rnd = new Random();

    var selectedFeatures = features.OrderByDescending(x => x.Services.Count()).Take(limit).ToList();

    foreach (var feature in selectedFeatures)
    {
        int number = rnd.Next() % feature.Services.Count();
        for (int i = 0; i < number; i++)
        {
            toMoveServices.Add(feature.Services[i]);
            if (toMoveServices.Count >= limit)
                break;
        }

        if (toMoveServices.Count >= limit)
            break;
    }

    foreach (var toMoveService in toMoveServices)
    {
        if (!string.IsNullOrEmpty(toMoveService))
        {
            int rndNum = rnd.Next() % input.NumEngineers;
            var binary = input.Binaries.Where(x => x.Services.Select(x => x.Name).Contains(toMoveService)).FirstOrDefault();
            var toMoveBinary = input.Binaries.OrderBy(x => x.Services.Count).FirstOrDefault();
            if (binary != null && toMoveBinary != null)
            {
                int moveTime = Math.Max(binary.Services.Count(), toMoveBinary.Services.Count());
                if (engineer.AvailableDays - moveTime >= 0)
                    MoveService(inputModel, binary, toMoveBinary, toMoveService, engineer, engineer.BusyUntil);
            }
            Thread.Sleep(20);
        }
    }
}

 void MoveService(InputModel inputModel, Binary firstBinary, Binary secondBinary, string serviceName, Engineers engineer, int startTime)
{
    var firstBinaryService = firstBinary.Services.Where(x => x.Name == serviceName).FirstOrDefault();
    int moveTime = Math.Max(firstBinary.Services.Count(), secondBinary.Services.Count());

    firstBinary.Services.Remove(firstBinaryService);
    secondBinary.Services.Add(firstBinaryService);

    engineer.Operations.Add(new EnginnerOperation
    {
        BinaryId = -1,
        StartTime = startTime,
        EndTime = startTime+moveTime,
        Operation = $"move {serviceName} {secondBinary.Id}"
    });
    engineer.AvailableDays -= moveTime;
    engineer.BusyUntil += moveTime;
    firstBinary.NotAvailableUntil += moveTime;
    secondBinary.NotAvailableUntil += moveTime;
}


static void CreateBinary(InputModel inputModel, Engineers engineers, int binaryId, int startTime)
{
    engineers.Operations.Add(new EnginnerOperation
    {
        BinaryId = -1,
        StartTime = startTime,
        EndTime = startTime + 1,
        Operation = "new"
    });

    var lastBinaryId = inputModel.Binaries.OrderByDescending(x => x.Id).FirstOrDefault().Id;
    inputModel.Binaries.Add(new Binary
    {
        Services = new List<Service>(),
        Done = false,
        Id = lastBinaryId + 1
    });
}

static void Wait(Engineers engineer, int startTime, int endTime)
{
    engineer.Operations.Add(new EnginnerOperation
    {
        BinaryId = -1, //wait op
        StartTime = startTime,
        EndTime = endTime,
        Operation = $"wait {endTime - startTime}"
    });
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
    List<EnginnerOperation> operations = new List<EnginnerOperation>();
    foreach(var engineer in engineers)
    {
        operations.AddRange(engineer.Operations.Where(x => !x.Operation.StartsWith("wait") && !x.Operation.StartsWith("move")).ToList());
    }

    var operations1 = operations.GroupBy(x => x.FeatureName).ToList();
    foreach(var data in operations1)
    {
            
        var feature = data.OrderByDescending(x => x.EndTime).FirstOrDefault();
        var inputFeature = inputModel.Features.Where(x => x.Name == feature.FeatureName).FirstOrDefault();
        var numBinaries = BinariesWithFeatureServices(inputFeature, inputModel.Binaries);
        if (data.Count() != numBinaries.Count) continue;
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
static List<FeatureModel> GetDoableFeatures(List<Engineers> engineers, int engineerId, List<FeatureModel> features, int day)
{
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

    if (engineer.BusyUntil > day && engineer.BusyUntil != 0)
        return availableFeatures;

    foreach (var feature in features)
    {
        foreach (var binary in feature.Binaries) 
        {
            if (feature.FeatureBinaryTime[binary.Id] <= engineer.AvailableDays && binary.NotAvailableUntil <= day)
            {
                var existingFeature = availableFeatures.Where(x => x.Feature.Name == feature.Feature.Name).FirstOrDefault();
                if (existingFeature != null)
                {
                    existingFeature.Binaries.Add(binary);
                    existingFeature.FeatureBinaryTime.Add(binary.Id, feature.FeatureBinaryTime[binary.Id]);
                    if(existingFeature.FeatureTimeBinary > feature.FeatureBinaryTime[binary.Id] && existingFeature.FeatureTimeBinary != 0)
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
                        Services = feature.Services
                    });
                }
            }
                
        }
    }
    return availableFeatures;
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