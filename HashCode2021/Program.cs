using HashCode2021;
using HashCode2021.Input;

var inputModel = ReadFile(@"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\breadth_of_choice.txt");
InitialSolution(inputModel);

static List<Engineers> InitialSolution(InputModel input)
{
    int i = 0;
    var initialSolution = new List<Engineers>(); //list of enginners 
    foreach (var enginner in input.Engineers)
    {
        enginner.AvailableDays = input.TimeLimitDays;
        initialSolution.Add(enginner);
    }

    List<string> processedFeatures = new List<string>();
    var features = input.Features;
    var binaries = input.Binaries;
    double sum = 0;
    while (++i <= 10000 && features.Count > 0)
    {
        var feature = features.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        var featureServices = feature?.Services;
        var correctBinaries = BinariesWithFeatureServices(feature, binaries);
        var engineer = initialSolution.Where(x => x.AvailableDays > 0).OrderByDescending(x => x.AvailableDays).FirstOrDefault();
        if (correctBinaries.Count > 0)
        {
            var selectedBinary = correctBinaries.OrderBy(x => Guid.NewGuid()).FirstOrDefault();  //todo check if any service is being moved from this binary
            var featureDifficulty = GetFeatureTime(feature, selectedBinary, initialSolution, input.TimeLimitDays - engineer.AvailableDays);
            if (engineer.AvailableDays >= featureDifficulty)
            {
                int endTime = (input.TimeLimitDays - engineer.AvailableDays) + featureDifficulty;
                engineer.Operations.Add(new EnginnerOperation
                {
                    StartTime = input.TimeLimitDays - engineer.AvailableDays,
                    EndTime = endTime,
                    BinaryId = selectedBinary.Id,
                    Operation = $"impl {feature.Name} {selectedBinary.Id}"
                });

                if (correctBinaries.Count > 1)
                {
                    foreach (var service in selectedBinary.Services)
                        feature.Services = feature.Services.Where(x => x.Name != service.Name).ToList(); //remove service hack
                }
                else
                {
                    features.Remove(feature);
                    int score = feature.NumUsersBenefit * (input.TimeLimitDays - endTime);
                    Console.WriteLine("Score: " + score);
                    sum += score;
                }
                engineer.AvailableDays -= featureDifficulty;
                //update Engineer time

            }
        }
    }
    Console.WriteLine("Final Score: " + sum);

    return initialSolution;
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
                if(binary.Id == operation.BinaryId && (time >= operation.StartTime && time <= operation.EndTime))
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
                int.TryParse(lineElements[1],out result);
                if(result > 0)
                {
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

#endregion