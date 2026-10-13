using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EquipmentController
{
    public async Task ReadCacheEquipment(ClientConnection client)
    {
        int idAccount = RaceManager.Instance.GetIDAccount(client);

        try
        {
            var equipmentData = CacheManager.Instance.GetAccountData(idAccount).equipments;

            EquipmentResultPacket equipmentResult = new EquipmentResultPacket
            {
                cmd = EnumCmdCode.equipment,
                equipmentData = new List<EquipmentData>()
            };
            foreach (var equippedItem in equipmentData)
            {
                equipmentResult.equipmentData.Add(new EquipmentData
                {
                    id = equippedItem.id,
                    idItem0_1 = equippedItem.idItem0_1,
                    nameItem0_1 = equippedItem.nameItem0_1,
                    category = equippedItem.category,
                    slotName = equippedItem.slotName,
                });
            }

            PacketWriterManager writer = new PacketWriterManager();
            writer.WriteInt((int)equipmentResult.cmd);
            writer.WriteListCount(equipmentResult.equipmentData.Count);
            foreach (var item in equipmentResult.equipmentData)
            {
                writer.WriteInt(item.id);
                writer.WriteInt(item.idItem0_1);
                writer.WriteString(item.nameItem0_1);
                writer.WriteInt(item.category);
                writer.WriteString(item.slotName);
            }

            await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi lấy equipment: " + ex.Message);
        }
    }
}