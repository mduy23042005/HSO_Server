public class PlayerController
{
    int idAccount;
    private MapController mapController = new MapController();
    private int maxHP;
    private int maxMP;

    private int hp;
    private int mp;

    private int damage;

    // Contructer này để lấy dữ liệu từ cache ra khi cần tính toán hoặc xử lý logic
    public PlayerController() { }
    public PlayerController(int idAccount)
    {
        this.idAccount = idAccount;

        var accountData = CacheManager.Instance.GetAccountData(idAccount);
        if (accountData == null || accountData.playerData == null)
            return;

        maxHP = accountData.playerData.maxHP;
        maxMP = accountData.playerData.maxMP;
        hp = accountData.playerData.hp;
        mp = accountData.playerData.mp;

        damage = 125;
    }
    // Contructer này để vừa login vào nó khởi tạo và đưa vào cache luôn
    public PlayerController(int idAccount, int point0, int point1, int point2, int point3)
    {
        maxHP = point0 * 100;
        maxMP = point3 * 100;
        hp = maxHP;
        mp = maxMP;

        damage = 125;
    }
    public int GetMaxHP()
    {
        return maxHP;
    }
    public int GetMaxMP()
    {
        return maxMP;
    }
    public int GetHP()
    {
        return hp;
    }
    public int GetMP()
    {
        return mp;
    }

    public async Task UpdatePlayerInfo(ClientConnection client, byte[] data)
    {
        PacketReaderManager reader = new PacketReaderManager(data);
        EnumCmdCode cmd = (EnumCmdCode)reader.ReadInt();
        int idAccount = reader.ReadInt();

        var accountData = CacheManager.Instance.GetAccountData(idAccount);
        if (accountData != null && accountData.playerData != null)
        {
            TileType currentTile = (TileType)reader.ReadInt();
            accountData.playerData.currentTile = currentTile;

            if (accountData.playerTransformData == null)
            {
                accountData.playerTransformData = new PlayerTransformData();
                accountData.playerTransformData.positionData = new PositionData();
                accountData.playerTransformData.scaleData = new ScaleData();
            }

            var playerTransformData = new PlayerTransformData();
            playerTransformData.positionData = new PositionData();
            playerTransformData.scaleData = new ScaleData();

            playerTransformData.positionData.x = reader.ReadFloat();
            playerTransformData.positionData.y = reader.ReadFloat();
            playerTransformData.scaleData.x = reader.ReadFloat();

            if (accountData.playerStateData == null)
                accountData.playerStateData = new PlayerStateData();
            accountData.playerStateData.stateData = (State)reader.ReadInt();
            accountData.playerStateData.directionData = (Direction)reader.ReadInt();

            if (accountData.playerStateData.partBodyTransforms == null || accountData.playerStateData.partBodyTransforms.Count == 0)
            {
                accountData.playerStateData.partBodyTransforms = new List<PartBodyData>();

                PartBodyData faceBodyData = new PartBodyData();
                faceBodyData.category = (Category)reader.ReadInt();
                faceBodyData.label = (Label)reader.ReadInt();
                PartBodyData partBodyData = new PartBodyData();
                partBodyData.category = (Category)reader.ReadInt();
                partBodyData.label = (Label)reader.ReadInt();

                accountData.playerStateData.partBodyTransforms.Add(faceBodyData);
                accountData.playerStateData.partBodyTransforms.Add(partBodyData);
            }
            else
            {
                accountData.playerStateData.partBodyTransforms[0].category = (Category)reader.ReadInt();
                accountData.playerStateData.partBodyTransforms[0].label = (Label)reader.ReadInt();
                accountData.playerStateData.partBodyTransforms[1].category = (Category)reader.ReadInt();
                accountData.playerStateData.partBodyTransforms[1].label = (Label)reader.ReadInt();
            }
            await CheckPosition(client, accountData, playerTransformData);
        }
    }

    private async Task CheckPosition(ClientConnection client, AccountData accountData, PlayerTransformData newTransformData)
    {
        if (accountData == null || accountData.account == null || accountData.playerData == null || accountData.playerTransformData == null || accountData.playerTransformData.positionData == null || accountData.playerTransformData.scaleData == null)
            return;

        if (newTransformData == null || newTransformData.positionData == null)
            return;

        var map = CacheManager.Instance.GetMap(accountData.playerData.idMap);
        if (map == null)
            return;

        if (!mapController.IsWalkable(map, newTransformData.positionData.x, newTransformData.positionData.y))
        {
            PacketWriterManager writer = new PacketWriterManager();
            writer.WriteInt((int)EnumCmdCode.syncCallBack);

            writer.WriteFloat(accountData.playerTransformData.positionData.x);
            writer.WriteFloat(accountData.playerTransformData.positionData.y);

            byte[] packet = writer.ToArray();
            await RaceManager.Instance.SendPacketToClient(client, packet);
        }
        else
        {
            if (mapController.IsTransitionMap(map, newTransformData.positionData.x, newTransformData.positionData.y))
            {
                PacketWriterManager writer = new PacketWriterManager();
                writer.WriteInt((int)EnumCmdCode.changeMap);
                int oldMapId = accountData.playerData.idMap;
                (int, int) oldCell = ((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                int newIDMap = 0;
                switch (accountData.playerData.idMap)
                {
                    case 1:
                        //từ ngôi làng nhỏ qua rừng ảo giác sẽ theo tọa độ này
                        newIDMap = 6;
                        accountData.playerTransformData.positionData.x = -8;
                        accountData.playerTransformData.positionData.y = -54;
                        break;

                    case 6:
                        newIDMap = 1;
                        accountData.playerTransformData.positionData.x = 24;
                        accountData.playerTransformData.positionData.y = 22;
                        break;
                }

                var newMap = CacheManager.Instance.GetMap(newIDMap);
                if (newMap == null)
                    return;

                (int, int) newCell = ((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - newMap.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - newMap.offsetY));

                lock (MapController.mapPlayers)
                {
                    if (MapController.mapPlayers.TryGetValue(oldMapId, out var oldCells) && oldCells.TryGetValue(oldCell, out var oldList))
                        oldList.Remove(client);

                    if (MapController.mapPlayers.TryGetValue(newIDMap, out var newCells) && newCells.TryGetValue(newCell, out var newList))
                        newList.Add(client);
                }

                accountData.playerData.idMap = newIDMap;
                writer.WriteInt(newIDMap);
                writer.WriteFloat(accountData.playerTransformData.positionData.x);
                writer.WriteFloat(accountData.playerTransformData.positionData.y);

                writer.WriteFloat(accountData.playerTransformData.scaleData.x);

                byte[] packet = writer.ToArray();
                await RaceManager.Instance.SendPacketToClient(client, packet);
            }
            else
            {
                (int, int) oldCell = ((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                accountData.playerTransformData = newTransformData;

                (int, int) newCell = ((int)MathF.Round(accountData.playerTransformData.positionData.x - 0.5f - map.offsetX), (int)MathF.Round(accountData.playerTransformData.positionData.y - 0.5f - map.offsetY));

                if (oldCell != newCell)
                {
                    lock (MapController.mapPlayers)
                    {
                        var clientConnection = RaceManager.Instance.GetClientByAccountId(accountData.account.Idaccount);
                        if (clientConnection == null)
                            return;

                        MapController.mapPlayers[accountData.playerData.idMap][oldCell].Remove(clientConnection);
                        MapController.mapPlayers[accountData.playerData.idMap][newCell].Add(clientConnection);
                    }
                }
            }
        }
    }

    public async Task PlayerAttack(ClientConnection client, int aimedMobID)
    {
        var mob = CacheManager.Instance.GetMob(aimedMobID);

        if (mob == null)
            return;

        mob.hp = mob.hp - damage;

        PacketWriterManager writer1 = new PacketWriterManager();
        writer1.WriteInt((int)EnumCmdCode.playerAttackMob);
        writer1.WriteInt(idAccount);
        writer1.WriteInt(aimedMobID);
        writer1.WriteInt(damage);
        writer1.WriteInt(mob.hp);

        await RaceManager.Instance.SendPacketToAllClients(writer1.ToArray());
    }
}