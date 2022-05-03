using Shared.Guilds.Commands;
using Shared.Guilds.Views;

namespace Shared.Guilds;

public static class ViewMapExtensions
{
    public static SubscribeChannelCommand ToCommand(this SubscribedChannelView view, ulong guildId) =>
        new SubscribeChannelCommand(view.Name, view.ChannelId, guildId);
}