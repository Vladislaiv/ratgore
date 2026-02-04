using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Forge.DiscordAuth;

public sealed class MsgDiscordStatusResponse : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public bool IsLinked;
    public string DiscordId = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsLinked = buffer.ReadBoolean();
        DiscordId = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsLinked);
        buffer.Write(DiscordId);
    }
}