namespace MH.Capstone.Domain.DataModels
{
    public record EmailMessage(string Recipient, string Subject, string HtmlBody, string? PlainTextBody = null);
}
