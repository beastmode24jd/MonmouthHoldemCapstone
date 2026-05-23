using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.Domain.DataModels
{
    /// <summary>
    /// Allows Reports to be filtered through ReportService.cs calls, by using different enum values for the argument.
    /// </summary>
    public enum AuditActionType
    {
        // Int types:
            //      0 == ResolveReport
            //      1 == OpenReport
            //      2 == date sort
            //      Parameter fields are nullable to be omitted as needed.
        ReportResolved = 0,
        ReportOpened = 1,
        UserLocked = 2,
        UserUnlocked = 3
        
    }
}