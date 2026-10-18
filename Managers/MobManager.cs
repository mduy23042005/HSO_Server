using System.Diagnostics;
using System.Numerics;

public class MobManager
{
    private static readonly Lazy<MobManager> lazyInstance = new Lazy<MobManager>(() => new MobManager());
    public static MobManager Instance => lazyInstance.Value;

    private Dictionary<ClientConnection, List<MobData>> mobsInViewAreaSnapshot = new Dictionary<ClientConnection, List<MobData>>();

    public async Task UpdateMobsLoop(CancellationTokenSource shutdownCts)
    {
        const int targetTickRate = 10;
        const int tickMS = 1000 / targetTickRate;

        var stopwatch = new Stopwatch();

        while (!shutdownCts.IsCancellationRequested)
        {
            stopwatch.Restart();
            try
            {
                float deltaTime = 1f / targetTickRate;

                for (int i = 1; i <= CacheManager.Instance.GetCountMap(); i = i + 1)
                {
                    var map = CacheManager.Instance.GetMap(i);

                    if (map == null || map.mobsData == null)
                        continue;

                    var mobs = map.mobsData;

                    foreach (var mob in mobs)
                    {
                        if (CacheManager.Instance.GetMob(mob.id).hp > 0)
                        {
                            mob.mobsAI.Attack(deltaTime, map, mob.id, mob.damage);
                            mob.mobsAI.Move(deltaTime, map, mob.id);
                        }
                        else
                        {
                            if (!mob.isRespawning)
                            {
                                mob.isRespawning = true;

                                mob.mobsAI.Die();
                                DroppedItemManager.Instance.DropItem0Mob(mob.id, mob.mob.Idmob, map, mob.mobsAI.GetCurrentPosition().X, mob.mobsAI.GetCurrentPosition().Y);

                                _ = Task.Run(async () =>
                                {
                                    await Task.Delay(5000);

                                    mob.hp = mob.mob.Hp ?? 0;
                                    mob.mobsAI.Respawn();
                                    mob.isRespawning = false;
                                });
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] Update Mobs error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;
            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
    public async Task SyncMobsLoop(CancellationTokenSource shutdownCts)
    {
        const int targetTickRate = 20;
        const int tickMS = 1000 / targetTickRate;

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
                    
                    if (!mobsInViewAreaSnapshot.TryGetValue(client, out var mobsInViewArea))
                    {
                        mobsInViewArea = new List<MobData>();
                        mobsInViewAreaSnapshot.Add(client, mobsInViewArea);
                    }

                    mobsInViewArea.Clear();

                    lock (MapController.mapMobs)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                if (!MapController.mapMobs[map.map.Idmap].TryGetValue((x, y), out var mobsInCell))
                                    continue;

                                foreach (var mob in mobsInCell)
                                {
                                    mobsInViewArea.Add(mob);
                                }
                            }
                        }
                    }

                    PacketWriterManager writer = new PacketWriterManager();
                    writer.WriteInt((int)EnumCmdCode.syncMobsData);
                    writer.WriteInt(mobsInViewArea.Count);

                    foreach (var mob in mobsInViewArea)
                    {
                        var currentHPMob = CacheManager.Instance.GetMob(mob.id).hp;
                        var currentTile = mapController.GetTileType(map, mob.mobsAI.GetCurrentPosition().X - map.offsetX, mob.mobsAI.GetCurrentPosition().Y - map.offsetY);

                        writer.WriteInt(mob.id);
                        writer.WriteInt(mob.mob.Idmob);
                        writer.WriteInt(mob.mob.Hp ?? 0);
                        writer.WriteInt(currentHPMob);
                        writer.WriteInt(mob.mob.Level ?? 1);
                        writer.WriteFloat(mob.mobsAI.GetCurrentPosition().X);
                        writer.WriteFloat(mob.mobsAI.GetCurrentPosition().Y);
                        writer.WriteInt((int)mob.mobsAI.GetState());
                        writer.WriteInt(mob.mobsAI.GetIDState());
                        writer.WriteInt((int)mob.mobsAI.GetDirection());
                        writer.WriteInt((int)currentTile);
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
                Console.WriteLine("[Server] Sync Mobs error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;
            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
}