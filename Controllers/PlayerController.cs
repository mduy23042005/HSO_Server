public class PlayerController
{
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
        maxHP = CacheManager.Instance.GetAccountData(idAccount).playerData.maxHP;
        maxMP = CacheManager.Instance.GetAccountData(idAccount).playerData.maxMP;
        hp = CacheManager.Instance.GetAccountData(idAccount).playerData.hp;
        mp = CacheManager.Instance.GetAccountData(idAccount).playerData.mp;

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
        if (accountData != null)
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
        var map = CacheManager.Instance.GetMap(accountData.playerData.idMap);
        if (!mapController.IsWalkable(map, newTransformData.positionData.x, newTransformData.positionData.y))
        {
            if (accountData != null && accountData.playerTransformData != null)
            {
                PacketWriterManager writer = new PacketWriterManager();
                writer.WriteInt((int)EnumCmdCode.syncCallBack);

                writer.WriteFloat(accountData.playerTransformData.positionData.x);
                writer.WriteFloat(accountData.playerTransformData.positionData.y);

                byte[] packet = writer.ToArray();
                _ = RaceManager.Instance.SendPacketToClient(client, packet);
            }
        }
        else
        {
            if (mapController.IsTransitionMap(map, newTransformData.positionData.x, newTransformData.positionData.y))
            {
                if (accountData != null && accountData.playerTransformData != null)
                {
                    var playerData = CacheManager.Instance.GetAccountData(accountData.playerData.idAccount);

                    PacketWriterManager writer = new PacketWriterManager();
                    writer.WriteInt((int)EnumCmdCode.changeMap);
                    int newIDMap = 0;

                    switch (accountData.playerData.idMap)
                    {
                        case 1:
                            //từ ngôi làng nhỏ qua rừng ảo giác sẽ theo tọa độ này
                            newIDMap = 6;
                            playerData.playerTransformData.positionData.x = -8;
                            playerData.playerTransformData.positionData.y = -54;
                            break;

                        case 6:
                            newIDMap = 1;
                            playerData.playerTransformData.positionData.x = 24;
                            playerData.playerTransformData.positionData.y = 22;
                            break;
                    }

                    playerData.playerData.idMap = newIDMap;
                    writer.WriteInt(newIDMap);
                    writer.WriteFloat(playerData.playerTransformData.positionData.x);
                    writer.WriteFloat(playerData.playerTransformData.positionData.y);

                    writer.WriteFloat(accountData.playerTransformData.scaleData.x);

                    byte[] packet = writer.ToArray();
                    _ = RaceManager.Instance.SendPacketToClient(client, packet);
                }
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
                        MapController.mapPlayers[accountData.playerData.idMap][oldCell].Remove(RaceManager.Instance.GetClientByAccountId(accountData.account.Idaccount));
                        MapController.mapPlayers[accountData.playerData.idMap][newCell].Add(RaceManager.Instance.GetClientByAccountId(accountData.account.Idaccount));
                    }
                }
            }
        }
    }

    public async Task PlayerAttack(ClientConnection client, PlayerAttackDataPacket data)
    {
        var mob = CacheManager.Instance.GetMob(data.aimedMobID);

        if (mob == null)
            return;

        mob.hp = mob.hp - damage;

        PacketWriterManager writer1 = new PacketWriterManager();
        writer1.WriteInt((int)EnumCmdCode.playerAttackMob);
        writer1.WriteInt(data.idAccount);
        writer1.WriteInt(data.aimedMobID);
        writer1.WriteInt(damage);
        writer1.WriteInt(mob.hp);

        await RaceManager.Instance.SendPacketToAllClients(writer1.ToArray());
    }
}