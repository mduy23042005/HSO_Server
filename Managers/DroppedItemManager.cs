public class DroppedItemManager
{
    private static readonly Lazy<DroppedItemManager> lazyInstance = new Lazy<DroppedItemManager>(() => new DroppedItemManager());
    public static DroppedItemManager Instance => lazyInstance.Value;

    public void InitDroppedItem0s()
    {
        foreach (var mob in CacheManager.Instance.GetAllMobs())
        {
            if (mob.Value != null && CacheManager.Instance.GetDroppedItem0sMob(mob.Value.mob.Idmob) == null)
            {
                int[] droppedItem0s = new int[128];

                for (int i = 0; i < droppedItem0s.Length; i++)
                {
                    droppedItem0s[i] = i + 1;
                }

                CacheManager.Instance.AddDroppedItem0sMob(mob.Key, droppedItem0s);
            }
        }
    }

    public async Task DropItem0Mob(int id, int idMob, MapData map)
    {
        Random random = new Random();
        if (random.Next(2) == 0) // Trả về ngẫu nhiên 0 hoặc 1 (50% cơ hội trúng số 0)
        {
            // Không rớt item
            return;
        }

        int[] droppedItem0s = CacheManager.Instance.GetDroppedItem0sMob(idMob);

        int selectedIDItem0 = droppedItem0s[random.Next(droppedItem0s.Length)];

        PacketWriterManager writer = new PacketWriterManager();
        writer.WriteInt((int)EnumCmdCode.dropItem0);
        writer.WriteInt(map.map.Idmap); // Gửi ID map để client biết item rơi ở map nào
        writer.WriteInt(id); // Gửi ID mob để client biết item rơi từ vị trí của mob nào
        writer.WriteInt(selectedIDItem0);
        writer.WriteString(CacheManager.Instance.GetItem0(selectedIDItem0).item0.NameItem0);

        await RaceManager.Instance.SendPacketToAllClientsInMap(map.map.Idmap, writer.ToArray());
    }
}