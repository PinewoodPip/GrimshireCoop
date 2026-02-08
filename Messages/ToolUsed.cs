
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class ToolUsed : NetObjectMessage
{
    public override string MessageType => "Client.ToolUsed";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

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

    public ToolType ToolId;

    public ToolUsed() { }

    public ToolUsed(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put((byte)ToolId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        ToolId = (ToolType)reader.GetByte();
    }
}
