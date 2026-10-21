using System.Diagnostics;
using System.Numerics;

public class DroppedItemManager
{
    private static readonly Lazy<DroppedItemManager> lazyInstance = new Lazy<DroppedItemManager>(() => new DroppedItemManager());
    public static DroppedItemManager Instance => lazyInstance.Value;

    private Dictionary<ClientConnection, List<DroppedItem0Data>> droppedItem0sInViewAreaSnapshot = new Dictionary<ClientConnection, List<DroppedItem0Data>>();
    private int nextDroppedItemId;
    private readonly object droppedItemIdLock = new object();

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

    private int GetNextDroppedItemId()
    {
        lock (droppedItemIdLock)
        {
            Instance.nextDroppedItemId = Instance.nextDroppedItemId + 1;
            return Instance.nextDroppedItemId;
        }
    }

    public async Task DropItem0Mob(int idMob, MapData map, float posX, float posY)
    {
        Random random = new Random();
        if (random.Next(2) == 0) // Trả về ngẫu nhiên 0 hoặc 1 (50% cơ hội trúng số 0)
        {
            // Không rớt item
            return;
        }

        int[] droppedItem0s = CacheManager.Instance.GetDroppedItem0sMob(idMob);
        if (droppedItem0s == null || droppedItem0s.Length == 0)
            return;

        int selectedIDItem0 = droppedItem0s[random.Next(droppedItem0s.Length)];

        // Chuyển tọa độ world thành tọa độ cell của map
        int x = (int)MathF.Round(posX - 0.5f - map.offsetX);
        int y = (int)MathF.Round(posY - 0.5f - map.offsetY);

        // Tạo dữ liệu item rơi
        DroppedItem0Data droppedItem0 = new DroppedItem0Data
        {
            id = GetNextDroppedItemId(),
            idItem0 = selectedIDItem0,
            posX = posX,
            posY = posY,
            nameItem0 = CacheManager.Instance.GetItem0(selectedIDItem0).item0.NameItem0,
            typeItem0 = CacheManager.Instance.GetItem0(selectedIDItem0).item0.TypeItem0,
            category = 1, // các item0 vừa khởi tạo giả sử thuộc hạng 1
            idSchool = (int)CacheManager.Instance.GetItem0(selectedIDItem0).item0.Idschool,
            level = (int)CacheManager.Instance.GetItem0(selectedIDItem0).item0.Level,
            item0_Attributes = CacheManager.Instance.GetItem0(selectedIDItem0).item0_Attributes,
            nameAttributes = CacheManager.Instance.GetItem0(selectedIDItem0).nameAttributes,
        };

        // Lưu item vào map để vòng lặp định kỳ lấy ra đồng bộ
        lock (MapController.mapDroppedItem0s)
        {
            if (MapController.mapDroppedItem0s.TryGetValue(map.map.Idmap, out var mapCells) && mapCells.TryGetValue((x, y), out var droppedItem0sInCell))
            {
                droppedItem0sInCell.Add(droppedItem0);
            }
        }
    }

    public async Task DropItem0(ClientConnection client, int idAccount, int idSlot, int selectedIDItem0)
    {
        var accountData = CacheManager.Instance.GetAccountData(idAccount);
        float posX = accountData.playerTransformData.positionData.x;
        float posY = accountData.playerTransformData.positionData.y;

        var map = CacheManager.Instance.GetMap(accountData.playerData.idMap);

        // Chuyển tọa độ world thành tọa độ cell của map
        int x = (int)MathF.Round(posX - 0.5f - map.offsetX);
        int y = (int)MathF.Round(posY - 0.5f - map.offsetY);

        // Tạo dữ liệu item rơi
        DroppedItem0Data droppedItem0 = new DroppedItem0Data
        {
            id = GetNextDroppedItemId(),
            posX = posX,
            posY = posY,
            idItem0 = selectedIDItem0,
            nameItem0 = accountData.inventoryItem0s[idSlot].nameItem0,
            typeItem0 = accountData.inventoryItem0s[idSlot].typeItem0,
            category = accountData.inventoryItem0s[idSlot].category,
            idSchool = accountData.inventoryItem0s[idSlot].idSchool,
            level = accountData.inventoryItem0s[idSlot].level,
            item0_Attributes = accountData.inventoryItem0s[idSlot].item0_Attributes,
            nameAttributes = accountData.inventoryItem0s[idSlot].nameAttributes,
        };

        accountData.inventoryItem0s.RemoveAt(idSlot);
        InventoryController inventoryController = new InventoryController();
        await inventoryController.ReadCacheInventory(client);

        lock (MapController.mapDroppedItem0s)
        {
            if (MapController.mapDroppedItem0s.TryGetValue(map.map.Idmap, out var mapCells) && mapCells.TryGetValue((x, y), out var droppedItem0sInCell))
            {
                Console.WriteLine($"Save id: {droppedItem0.id}");
                droppedItem0sInCell.Add(droppedItem0);
            }
        }
    }

    public async Task SyncDroppedItemsLoop(CancellationTokenSource shutdownCts)
    {
        const int targetTickRate = 1;
        const int tickMS = 1500 / targetTickRate;

        var stopwatch = new Stopwatch();

        MapController mapController = new MapController();

        while (!shutdownCts.IsCancellationRequested)
        {
            stopwatch.Restart();
            try
            {
                foreach (var client in RaceManager.Instance.GetAllClients())
                {
                    var idAccount = RaceManager.Instance.GetIDAccount(client);
                    if (idAccount <= 0)
                        continue;

                    var accountData = CacheManager.Instance.GetAccountData(idAccount);
                    if (accountData == null || accountData.playerTransformData == null || accountData.playerData == null)
                        continue;

                    var map = CacheManager.Instance.GetMap(accountData.playerData.idMap);
                    if (map == null || map.mobsData == null)
                        continue;

                    Vector2 playerPosition = new Vector2((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                    int minX = (int)playerPosition.X - 15;
                    int maxX = (int)playerPosition.X + 15;

                    int minY = (int)playerPosition.Y - 10;
                    int maxY = (int)playerPosition.Y + 10;

                    if (!droppedItem0sInViewAreaSnapshot.TryGetValue(client, out var droppedItem0sInViewArea))
                    {
                        droppedItem0sInViewArea = new List<DroppedItem0Data>();
                        droppedItem0sInViewAreaSnapshot.Add(client, droppedItem0sInViewArea);
                    }

                    droppedItem0sInViewArea.Clear();

                    lock (MapController.mapDroppedItem0s)
                    {
                        if (!MapController.mapDroppedItem0s.TryGetValue(map.map.Idmap, out var mapCells))
                            continue;

                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                if (!MapController.mapDroppedItem0s[map.map.Idmap].TryGetValue((x, y), out var droppedItem0sInCell))
                                    continue;

                                foreach (var droppedItem0 in droppedItem0sInCell)
                                {
                                    droppedItem0sInViewArea.Add(droppedItem0);
                                }
                            }
                        }
                    }

                    PacketWriterManager writer = new PacketWriterManager();
                    writer.WriteInt((int)EnumCmdCode.dropItem0s);
                    writer.WriteInt(droppedItem0sInViewArea.Count);

                    foreach (var droppedItem0 in droppedItem0sInViewArea)
                    {
                        writer.WriteInt(droppedItem0.id);
                        writer.WriteInt(droppedItem0.idItem0);
                        writer.WriteFloat(droppedItem0.posX);
                        writer.WriteFloat(droppedItem0.posY);
                        writer.WriteString(droppedItem0.nameItem0);
                    }

                    byte[] packet = writer.ToArray();

                    try
                    {
                        await RaceManager.Instance.SendPacketToClient(client, packet);
                    }
                    catch { }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] Sync Dropped Items error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;
            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
}