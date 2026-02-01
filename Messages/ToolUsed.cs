
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class ToolUsed : OwnedMessage
{
    public enum ToolType : byte
    {
        Axe,
        Scythe,
        Pickaxe,
        FishingRod,
        Hoe,
        WaterCan,
        // Milker, // No animation in the game
        // Shears, // TODO requires extra checks (doesn't use state machine)
    }

    public override string MessageType => "Shared.ToolUsed";

    public override Direction SyncDirection => Direction.ServerToClient;

    public int NetId;
    public ToolType ToolId;

    public ToolUsed() { }

    public ToolUsed(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
        writer.Put((byte)ToolId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
        ToolId = (ToolType)reader.GetByte();
    }
}
