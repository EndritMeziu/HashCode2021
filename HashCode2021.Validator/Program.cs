using HashCode2021.Validator.Interfaces;
using HashCode2021.Validator.Services;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = RegisterDependencies();
var _solutionValidator = serviceProvider.GetRequiredService<ISolutionValidator>();


#region RegisterDependencies
static ServiceProvider RegisterDependencies()
{
    var serviceProvider = new ServiceCollection()
            .AddScoped<ISolutionValidator, SolutionValidator>()
            .BuildServiceProvider();

    return serviceProvider;
}
#endregion