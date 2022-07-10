using HashCode2021.Validator.Models;

namespace HashCode2021.Validator.Interfaces
{
    public interface ISolutionValidator
    {
        bool isValidSolution(string instancePath, string solutionPath);
        InputModel ReadInputFile(string instancePath);
        SolutionFile ReadSolutionFile(string solutionPath, InputModel inputModel);
        int CalculateScore(string instancePath, string solutionPath);
    }
}
