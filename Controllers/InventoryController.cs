public class InventoryController
{
    public async Task ReadCacheInventory(ClientConnection client)
    {
        int idAccount = RaceManager.Instance.GetIDAccount(client);

        try
        {
            var inventoryItem0Data = CacheManager.Instance.GetAccountData(idAccount).inventoryItem0s;

            InventoryResultPacket inventoryResult = new InventoryResultPacket
            { 
                cmd = EnumCmdCode.inventory,
                inventoryItem0Data = new List<InventoryItem0Data>(),
                inventoryItem1Data = new List<InventoryItem1Data>(),
                inventoryItem2Data = new List<InventoryItem2Data>(),
                inventoryItem3Data = new List<InventoryItem3Data>(),
            };

            // tạm thời chỉ có item0
            foreach (var inventoryItem0 in inventoryItem0Data) 
            {
                inventoryResult.inventoryItem0Data.Add(new InventoryItem0Data
                {
                    id = inventoryItem0.id,
                    idItem0 = inventoryItem0.idItem0,
                    nameItem0 = inventoryItem0.nameItem0,
                    typeItem0 = inventoryItem0.typeItem0,
                    category = inventoryItem0.category,
                    idSchool = inventoryItem0.idSchool,
                });
            }

            PacketWriterManager writer = new PacketWriterManager();
            writer.WriteInt((int)inventoryResult.cmd);
            writer.WriteListCount(inventoryResult.inventoryItem0Data.Count);
            foreach (var inventoryItem0 in inventoryItem0Data)
            {
                writer.WriteInt(inventoryItem0.id);
                writer.WriteInt(inventoryItem0.idItem0);
                writer.WriteString(inventoryItem0.nameItem0);
                writer.WriteString(inventoryItem0.typeItem0);
                writer.WriteInt(inventoryItem0.category);
                writer.WriteInt(inventoryItem0.idSchool);
            }

            await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi lấy inventory: " + ex.Message);
        }
    }
}