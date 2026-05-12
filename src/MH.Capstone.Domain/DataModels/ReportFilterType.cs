using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.Domain.DataModels
{
    /// <summary>
    /// Allows Reports to be filtered through ReportService.cs calls, by using different enum values for the argument.
    /// </summary>
    public enum ReportFilterType
    {
        // Int types:
            //      0 == page sort
            //      1 == reporter sort
            //      2 == date sort
            //      Parameter fields are nullable to be omitted as needed.
        PageURL = 0,
        Reporter = 1,
        Date = 2,
        Resolved = 3
    }
}