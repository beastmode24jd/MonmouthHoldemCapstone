namespace MH.Capstone.Domain.DataModels
{
    [Flags]
    public enum NotificationDeliveryChannel
    {
        Silenced = 0,
        InAppOnly = 1,
        EmailOnly = 2,
        InAppAndEmail = InAppOnly | EmailOnly
    }
}
