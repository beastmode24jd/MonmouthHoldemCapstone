using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.Domain.DataModels
{
    /// <summary>
    /// Allows Audits created from Admin actions and/or report modifications
    ///  to be filtered through AuditService.cs calls.
    /// </summary>
    public enum AuditActionType
    {
        ReportResolved = 0,
        ReportOpened = 1,
        UserLocked = 2,
        UserUnlocked = 3,
        // Handles promoting and demoting from Admin.
        RolePromotion = 4,
        RoleDemotion = 5
        
    }
}