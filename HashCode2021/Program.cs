using HashCode2021;
using HashCode2021.Input;

var inputModel = ReadFile(@"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\expectation_maximisation.txt");
InitialSolution1(inputModel);

static void InitialSolution1(InputModel input)
{
    var initialSolution = new List<Engineers>(); //list of enginners 
    foreach (var enginner in input.Engineers)
    {
        enginner.AvailableDays = input.TimeLimitDays;
        initialSolution.Add(enginner);
    }

    List<string> processedFeatures = new List<string>();
    var features = ProcessFeatures(input);
    var binaries = input.Binaries;
    for (int i = 0; i < input.TimeLimitDays; i++)
    {
        foreach(var engineer in initialSolution)
        {
            var doableFeatures = GetDoableFeatures(initialSolution, engineer.Id, features.Where(x => x.Feature.Done == false).ToList(), i);
            if (doableFeatures.Count > 0)
            {
                var toDoFeature = doableFeatures.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                var featureBinary = toDoFeature.Binary;
                var feature = toDoFeature.Feature;
                var featureTime = GetFeatureTime(feature, featureBinary, initialSolution, i);

                if (engineer.AvailableDays - featureTime >= 0)
                {
                    var endTime = i + featureTime;
                    engineer.AvailableDays -= featureTime;
                    engineer.BusyUntil = endTime;
                    engineer.Operations.Add(new EnginnerOperation
                    {
                        BinaryId = featureBinary.Id,
                        StartTime = i,
                        EndTime = endTime,
                        FeatureName = feature.Name,
                        Operation = $"impl {feature.Name} {featureBinary.Id}"
                    });
                    toDoFeature.Feature.Done = true;
                }
                
            }
            else
            {
                if (engineer.BusyUntil <= i)
                {
                    engineer.Operations.Add(new EnginnerOperation
                    {
                        BinaryId = -1, //wait op
                        StartTime = i,
                        EndTime = i + 1,
                        Operation = $"wait 1"
                    });
                    engineer.BusyUntil = i + 1;
                }
            }
        }
    }

    SaveSolution(initialSolution, @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt");
    int score = CalculateScore(initialSolution, input);
    Console.WriteLine("Score:"+score);
}

static int CalculateScore(List<Engineers> engineers, InputModel inputModel)
{
    int score = 0;
    List<EnginnerOperation> operations = new List<EnginnerOperation>();
    foreach(var engineer in engineers)
    {
        operations.AddRange(engineer.Operations.Where(x => !x.Operation.StartsWith("wait")).ToList());
    }

    var operations1 = operations.GroupBy(x => x.FeatureName).ToList();
    foreach(var data in operations1)
    {
        var feature = data.OrderByDescending(x => x.EndTime).FirstOrDefault();
        if(feature != null)
        {
            int numDaysAvailable = inputModel.TimeLimitDays - feature.EndTime;
            int numUsersBenefit = inputModel.Features.Where(x => x.Name == feature.FeatureName).FirstOrDefault().NumUsersBenefit;
            if (numDaysAvailable < 0) numDaysAvailable = 0;
            score += (numDaysAvailable * numUsersBenefit);
        }
    } 
    return score;
}

static List<FeatureModel> ProcessFeatures(InputModel inputModel)
{
    List<FeatureModel> proccessedFeatures = new List<FeatureModel>();
    var features = inputModel.Features;
    var binaries = inputModel.Binaries;
    foreach(var feature in features)
    {
        var featureBinaries = BinariesWithFeatureServices(feature, binaries);
        foreach(var binary in featureBinaries)
        {
            proccessedFeatures.Add(new FeatureModel { Feature = feature.Clone(), Binary = binary.Clone() });
        }
    }

    return proccessedFeatures;
}


static List<FeatureModel> GetDoableFeatures(List<Engineers> engineers, int engineerId, List<FeatureModel> features, int day)
{
    List<FeatureModel> availableFeatures = new List<FeatureModel>();
    var engineer = engineers.Where(x => x.Id == engineerId).FirstOrDefault();

    if (engineer.BusyUntil > day && engineer.BusyUntil != 0)
        return availableFeatures;


    foreach(var feature in features)
    {
        int count = 0;
        foreach (var worker in engineers)
        {
            foreach (var operation in worker.Operations)
            {
                if (operation.FeatureName == feature.Feature.Name && operation.StartTime <= day && operation.EndTime >= day && feature.Binary.Id == operation.BinaryId)  //todo ---> check if can be done based on engineer day and feature difficulty
                    count++;
            }
        }
        if (count == 0)
            availableFeatures.Add(feature);
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
static Features GetFeatureWithLeastBinaries(List<Binary> binaries, List<Features> features)
{
    int numBinaries = int.MaxValue;
    var bestFeature = new Features();
    foreach(var feature in features)
    {
       var toUseBinaries = BinariesWithFeatureServices(feature, binaries);
       if(toUseBinaries.Count < numBinaries)
       {
            numBinaries = toUseBinaries.Count;
            bestFeature = feature.Clone();
       }
    }
    return bestFeature;
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