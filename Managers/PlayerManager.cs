using System.Diagnostics;
using System.Numerics;

public class PlayerManager
{
    private static readonly Lazy<PlayerManager> lazyInstance = new Lazy<PlayerManager>(() => new PlayerManager());
    public static PlayerManager Instance => lazyInstance.Value;

    private Dictionary<ClientConnection, List<ClientConnection>> playersInViewAreaSnapshot = new Dictionary<ClientConnection, List<ClientConnection>>();

    public async Task SyncOtherPlayersLoop(CancellationTokenSource shutdownCts)
    {
        const int targetTickRate = 1;
        const int tickMS = 2000 / targetTickRate;

        var stopwatch = new Stopwatch();

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
                    if (map == null)
                        continue;

                    Vector2 playerPosition = new Vector2((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                    int minX = (int)playerPosition.X - 15;
                    int maxX = (int)playerPosition.X + 15;

                    int minY = (int)playerPosition.Y - 10;
                    int maxY = (int)playerPosition.Y + 10;

                    PacketWriterManager writer = new PacketWriterManager();

                    lock (playersInViewAreaSnapshot)
                    {
                        if (!playersInViewAreaSnapshot.TryGetValue(client, out var playersInViewArea))
                        {
                            playersInViewArea = new List<ClientConnection>();
                            playersInViewAreaSnapshot.Add(client, playersInViewArea);
                        }

                        playersInViewArea.Clear();

                        lock (MapController.mapPlayers)
                        {
                            for (int y = minY; y <= maxY; y++)
                            {
                                for (int x = minX; x <= maxX; x++)
                                {
                                    if (!MapController.mapPlayers[map.map.Idmap].TryGetValue((x, y), out var playersInCell))
                                        continue;

                                    foreach (var player in playersInCell)
                                    {
                                        if (RaceManager.Instance.GetIDAccount(player) == 0)
                                            continue;

                                        playersInViewArea.Add(player);
                                    }
                                }
                            }
                        }

                        writer.WriteInt((int)EnumCmdCode.syncOtherPlayersData);
                        writer.WriteListCount(playersInViewArea.Count - 1);

                        foreach (var player in playersInViewArea)
                        {
                            if (idAccount == RaceManager.Instance.GetIDAccount(player))
                                continue;

                            var playerAccountData = CacheManager.Instance.GetAccountData(RaceManager.Instance.GetIDAccount(player));

                            if (playerAccountData == null || playerAccountData.playerData == null)
                                continue;

                            writer.WriteInt(playerAccountData.playerData.idAccount);
                            writer.WriteString(playerAccountData.playerData.nameChar);
                            writer.WriteInt(playerAccountData.playerData.level);
                            writer.WriteInt(playerAccountData.playerData.idSchool);
                            writer.WriteInt(playerAccountData.playerData.hair);
                            writer.WriteInt(playerAccountData.playerData.weapon);
                            writer.WriteInt(playerAccountData.playerData.helmet);
                            writer.WriteInt(playerAccountData.playerData.armor);
                            writer.WriteInt(playerAccountData.playerData.legArmor);
                        }

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
                Console.WriteLine("[Server] Sync other player error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;

            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
    public async Task SyncOtherPlayersRealtimeLoop(CancellationTokenSource shutdownCts)
    {
        const int targetTickRate = 30;
        const int tickMS = 1000 / targetTickRate;

        var stopwatch = new Stopwatch();

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
                    if (map == null)
                        continue;

                    Vector2 playerPosition = new Vector2((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                    int minX = (int)playerPosition.X - 15;
                    int maxX = (int)playerPosition.X + 15;

                    int minY = (int)playerPosition.Y - 10;
                    int maxY = (int)playerPosition.Y + 10;

                    PacketWriterManager writer = new PacketWriterManager();

                    lock (playersInViewAreaSnapshot)
                    {
                        if (!playersInViewAreaSnapshot.TryGetValue(client, out var playersInViewArea))
                        {
                            playersInViewArea = new List<ClientConnection>();
                            playersInViewAreaSnapshot.Add(client, playersInViewArea);
                        }

                        playersInViewArea.Clear();

                        lock (MapController.mapPlayers)
                        {
                            for (int y = minY; y <= maxY; y++)
                            {
                                for (int x = minX; x <= maxX; x++)
                                {
                                    if (!MapController.mapPlayers[map.map.Idmap].TryGetValue((x, y), out var playersInCell))
                                        continue;

                                    foreach (var player in playersInCell)
                                    {
                                        if (RaceManager.Instance.GetIDAccount(player) == 0)
                                            continue;

                                        playersInViewArea.Add(player);
                                    }
                                }
                            }
                        }

                        writer.WriteInt((int)EnumCmdCode.syncOtherPlayersRealtimeData);
                        writer.WriteListCount(playersInViewArea.Count - 1);

                        foreach (var player in playersInViewArea)
                        {
                            if (idAccount == RaceManager.Instance.GetIDAccount(player))
                                continue;

                            var playerAccountData = CacheManager.Instance.GetAccountData(RaceManager.Instance.GetIDAccount(player));

                            if (playerAccountData == null || playerAccountData.playerData == null)
                                continue;

                            writer.WriteInt(playerAccountData.playerData.idAccount);
                            writer.WriteInt(playerAccountData.playerData.maxHP);
                            writer.WriteInt(playerAccountData.playerData.hp);
                            writer.WriteInt((int)playerAccountData.playerData.currentTile);

                            writer.WriteFloat(playerAccountData.playerTransformData.positionData.x);
                            writer.WriteFloat(playerAccountData.playerTransformData.positionData.y);

                            writer.WriteFloat(playerAccountData.playerTransformData.scaleData.x);

                            writer.WriteInt((int)playerAccountData.playerStateData.stateData);
                            writer.WriteInt((int)playerAccountData.playerStateData.directionData);

                            writer.WriteInt((int)playerAccountData.playerStateData.partBodyTransforms[0].category);
                            writer.WriteInt((int)playerAccountData.playerStateData.partBodyTransforms[0].label);

                            writer.WriteInt((int)playerAccountData.playerStateData.partBodyTransforms[1].category);
                            writer.WriteInt((int)playerAccountData.playerStateData.partBodyTransforms[1].label);
                        }
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
                Console.WriteLine("[Server] Sync other player error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;

            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
}