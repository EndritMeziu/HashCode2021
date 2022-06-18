using HashCode2021.Validator.Interfaces;
using HashCode2021.Validator.Services;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = RegisterDependencies();
var _solutionValidator = serviceProvider.GetRequiredService<ISolutionValidator>();
var result = _solutionValidator.isValidSolution(@"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\five_thousand.txt",
    @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\an_example.txt");
Console.WriteLine(result);


#region RegisterDependencies
static ServiceProvider RegisterDependencies()
{
    var serviceProvider = new ServiceCollection()
            .AddScoped<ISolutionValidator, SolutionValidator>()
            .BuildServiceProvider();

    return serviceProvider;
}
#endregion