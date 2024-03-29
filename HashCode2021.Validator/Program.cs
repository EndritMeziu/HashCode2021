﻿using HashCode2021.Validator.Interfaces;
using HashCode2021.Validator.Services;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = RegisterDependencies();
var instancePath = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Instances\five_thousand.txt";
var solutionPath = @"C:\Users\38343\source\repos\HashCode2021\HashCode2021\Solutions\hill_climbing.txt";
var _solutionValidator = serviceProvider.GetRequiredService<ISolutionValidator>();
Console.WriteLine("Score:" + _solutionValidator.CalculateScore(instancePath, solutionPath));



#region RegisterDependencies
static ServiceProvider RegisterDependencies()
{
    var serviceProvider = new ServiceCollection()
            .AddScoped<ISolutionValidator, SolutionValidator>()
            .BuildServiceProvider();

    return serviceProvider;
}
#endregion