
using GrimshireCoop.Messages.Shared;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using UnityEngine;
using static GrimshireCoop.Utils;

namespace GrimshireCoop.Components;

public class NetCropObject : NetworkedBehaviour
{
    public override string NetTypeID => "CropObject";

    public CropObject Crop => GetComponent<CropObject>(); // TODO cache

    public override void OnAction(ObjectAction action)
    {
        if (action.Action == "Interact")
        {
            NetCropManager.ignoreHooks = true;
            Crop.Interact();
            NetCropManager.ignoreHooks = false;
        }
    }

    public override byte[] GetReplicationData()
    {
        NetDataWriter writer = new NetDataWriter();
        var data = GetField<CropManager.PersistentCropData>(Crop, "persistenCropDataContainer"); // Field typo is from the game.
        SerializeCropData(writer, data);
        return writer.CopyData();
    }

    public override void ApplyReplicationData(byte[] data)
    {
        NetDataReader reader = new NetDataReader(data);
        CropManager.PersistentCropData cropData = DeserializeCropData(reader);
        SetField(Crop, "persistenCropDataContainer", cropData);
    }

    public static void SerializeCropData(NetDataWriter writer, CropManager.PersistentCropData data)
    {
        writer.Put(data.objID);
        writer.Put(data.nameOfScene);
        writer.Put(data.areaName);
        writer.Put(data.cropRefID);
        writer.Put(data.variationID);
        writer.Put(data.daysWatered);
        writer.Put(data.posX);
        writer.Put(data.posY);
        writer.Put(data.fertility);
        writer.Put(data.wateredToday);
        writer.Put(data.isMirrored);
        writer.Put(data.isForage);
        writer.Put(data.isDead);
        writer.Put(data.serializedSubPlantData);
        // Note: serializedSubPlantData is a JSON string for subPlantsList. These should already always be in-sync by the game.
    }

    public static CropManager.PersistentCropData DeserializeCropData(NetDataReader reader)
    {
        CropManager.PersistentCropData data = new()
        {
            objID = reader.GetInt(),
            nameOfScene = reader.GetString(),
            areaName = reader.GetString(),
            cropRefID = reader.GetInt(),
            variationID = reader.GetInt(),
            daysWatered = reader.GetInt(),
            posX = reader.GetFloat(),
            posY = reader.GetFloat(),
            fertility = reader.GetFloat(),
            wateredToday = reader.GetBool(),
            isMirrored = reader.GetBool(),
            isForage = reader.GetBool(),
            isDead = reader.GetBool(),
            serializedSubPlantData = reader.GetString(),
        };
        data.subPlantsList = JsonConvert.DeserializeObject<float[,]>(data.serializedSubPlantData);

        return data;
    }
}