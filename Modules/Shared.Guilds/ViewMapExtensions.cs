using Shared.Guilds.Commands;
using Shared.Guilds.Views;

namespace Shared.Guilds;

public static class ViewMapExtensions
{
    public static SubscribeChannel ToCommand(this SubscribedChannelView view, ulong guildId) =>
        new SubscribeChannel(view.Name, view.ChannelId, guildId);
}