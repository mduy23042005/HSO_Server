public class InventoryController
{
    public async Task ReadCacheInventory(ClientConnection client)
    {
        int idAccount = RaceManager.Instance.GetIDAccount(client);

        try
        {
            var inventoryItem0Data = CacheManager.Instance.GetAccountData(idAccount)?.inventoryItem0s;

            if (inventoryItem0Data == null)
            {
                Console.WriteLine($"[Server] ReadCacheInventory: InventoryItem0 data not found for idAccount: {idAccount}");
                return;
            }

            InventoryResultPacket inventoryResult = new InventoryResultPacket
            {
                cmd = EnumCmdCode.inventory,
                inventoryItem0Data = new List<InventoryItem0Data>(),
                inventoryItem1Data = new List<InventoryItem1Data>(),
                inventoryItem2Data = new List<InventoryItem2Data>(),
                inventoryItem3Data = new List<InventoryItem3Data>(),
            };

            // tạm thời chỉ có item0
            // (THÊM) Lượt 1: lấy các ô có item trước, giữ nguyên thứ tự cũ => dồn lên đầu
            foreach (var inventoryItem0 in inventoryItem0Data)
            {
                if (inventoryItem0.idItem0 == 0)
                    continue;

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
            // (SỬA) Trước đây lặp trên inventoryItem0Data (cache), nên danh sách đã dồn bị bỏ qua.
            // Giờ lặp trên inventoryResult.inventoryItem0Data để gửi đúng thứ tự đã dồn.
            foreach (var inventoryItem0 in inventoryResult.inventoryItem0Data)
            {
                writer.WriteInt(inventoryItem0.id);
                writer.WriteInt(inventoryItem0.idItem0);
                writer.WriteString(inventoryItem0.nameItem0 ?? string.Empty);
                writer.WriteString(inventoryItem0.typeItem0 ?? string.Empty);
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