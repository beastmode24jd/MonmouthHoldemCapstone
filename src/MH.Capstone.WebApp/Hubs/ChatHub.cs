using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace MH.Capstone.WebApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IClubService _clubService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(IClubService clubService, UserManager<ApplicationUser> userManager)
        {
            _clubService = clubService;
            _userManager = userManager;
        }

        protected virtual string? GetClubIdQueryParam() =>
            Context.GetHttpContext()?.Request.Query["clubId"].ToString();

        public override async Task OnConnectedAsync()
        {
            var clubIdStr = GetClubIdQueryParam();
            if (!string.IsNullOrEmpty(clubIdStr) && Guid.TryParse(clubIdStr, out var clubId))
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(clubId));

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(Guid clubId, string content)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) return;

            await _clubService.SendMessageAsync(clubId, user.GuidId, content);

            await Clients.Group(GroupName(clubId)).SendAsync("ReceiveMessage", new
            {
                authorId = user.Id,
                authorDisplayName = user.DisplayName,
                content = content.Trim(),
                sentAtUtc = DateTimeOffset.UtcNow.ToString("o")
            });
        }

        private static string GroupName(Guid clubId) => $"club-{clubId}";
    }
}
