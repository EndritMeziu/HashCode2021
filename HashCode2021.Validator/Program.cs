using HashCode2021.Validator.Interfaces;
using HashCode2021.Validator.Services;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = RegisterDependencies();
var instancePath = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\an_example.txt";
var solutionPath = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt";
var _solutionValidator = serviceProvider.GetRequiredService<ISolutionValidator>();
var result = _solutionValidator.isValidSolution(instancePath, solutionPath);

if(result)
{
    Console.WriteLine("Solution is valid");
    Console.WriteLine("Score:" + _solutionValidator.CalculateScore(instancePath, solutionPath));

}
else
{
    Console.WriteLine("Solution file is not valid");
}


#region RegisterDependencies
static ServiceProvider RegisterDependencies()
{
    var serviceProvider = new ServiceCollection()
            .AddScoped<ISolutionValidator, SolutionValidator>()
            .BuildServiceProvider();

    return serviceProvider;
}
#endregion