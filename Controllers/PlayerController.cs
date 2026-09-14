public enum State
{
    Stand = 0,
    Move = 1,
    Attack = 2,
    Injured = 3,
    Die = 4
}
public enum Direction
{
    Front = 0,
    Back = 1,
    Left = 2,
    Right = 3,
}
public enum Category
{
    Stand = 0,
    Move = 1,
    Atk = 2,
    Injured = 3,
    Die = 4,
}
public enum Label
{
    StandFrontFrame0 = 4,
    StandFrontFrame1 = 5,
    StandBackFrame0 = 6,
    StandBackFrame1 = 7,
    StandLeftFrame0 = 8,
    StandLeftFrame1 = 9,
    StandRightFrame0 = 10,
    StandRightFrame1 = 11,

    MoveFrontFrame0 = 12,
    MoveFrontFrame1 = 13,
    MoveBackFrame0 = 14,
    MoveBackFrame1 = 15,
    MoveLeftFrame0 = 16,
    MoveLeftFrame1 = 17,
    MoveRightFrame0 = 18,
    MoveRightFrame1 = 19,

    AtkFrontFrame0 = 20,
    AtkFrontFrame1 = 21,
    AtkBackFrame0 = 22,
    AtkBackFrame1 = 23,
    AtkLeftFrame0 = 24,
    AtkLeftFrame1 = 25,
    AtkRightFrame0 = 26,
    AtkRightFrame1 = 27,

    InjuredFrontFrame0 = 28,
    InjuredFrontFrame1 = 29,
    InjuredBackFrame0 = 30,
    InjuredBackFrame1 = 31,
    InjuredLeftFrame0 = 32,
    InjuredLeftFrame1 = 33,
    InjuredRightFrame0 = 34,
    InjuredRightFrame1 = 35,

    DieFrame0 = 36
}
public class PositionData
{
    public float x;
    public float y;
    public float z;
}
public class RotationData
{
    public float x;
    public float y;
    public float z;
}
public class ScaleData
{
    public float x;
    public float y;
    public float z;
}
public class ColorData
{
    public float r;
    public float g;
    public float b;
    public float a;
}
public class PartBodyData
{
    public Category category;
    public Label label;
    public PositionData positionData;
    public RotationData rotationData;
    public ScaleData scaleData;
    public ColorData colorData;
}
public class PlayerStateData
{
    public State stateData;
    public Direction directionData;
    public List<PartBodyData> partBodyTransforms;
}
public class PlayerTransformData
{
    public PositionData positionData;
    public ScaleData scaleData;
}
public class PlayerData
{
    public int idMap;
    public int idAccount;
    public string nameChar;
    public int level;
    public int idSchool;
    public int hair;
    public int weapon;
    public int helmet;
    public int armor;
    public int legArmor;
    public int gloves;
    public int shoes;
    public int ring1;
    public int ring2;
    public int necklace;
    public int medal;
    public int cloak;
    public int wing;
    public int skinWing;
    public int mounts;
    public int pet;
    public int skin;
    public int maxHP;
    public int maxMP;
    public int hp;
    public int mp;
    public TileType currentTile;
}

public class PlayerSyncData
{
    public PlayerData playerData;
    public PlayerTransformData playerTransformData;
    public PlayerStateData playerStateData;
}
public class PlayerSyncDataRequestPacket
{
    public string cmd;
    public PlayerSyncData playerSyncData;
}
public class OtherPlayerSyncData
{
    public PlayerData otherPlayerData;
    public PlayerTransformData otherPlayerTransformData;
    public PlayerStateData otherPlayerStateData;
}

public class PlayerAttackDataPacket
{
    public EnumCmdCode cmd;
    public int idAccount;
    public int aimedMobID;
}
public class PlayerAttackMobDataResult
{
    public EnumCmdCode cmd;
    public int aimedMobID;
    public int damage;
    public int hpMobAfterAttack;
}
public class OtherPlayerAttackMobDataResult
{
    public EnumCmdCode cmd;
    public int aimedMobID;
    public int damage;
    public int hpMobAfterAttack;
}

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

                writer.WriteFloat(accountData.playerTransformData.scaleData.x);

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

        PacketWriterManager writer = new PacketWriterManager();
        writer.WriteInt((int)EnumCmdCode.playerAttackMob);
        writer.WriteInt(data.aimedMobID);
        writer.WriteInt(damage);
        writer.WriteInt(mob.hp);
        await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());

        PacketWriterManager writer1 = new PacketWriterManager();
        writer1.WriteInt((int)EnumCmdCode.otherPlayerAttackMob);
        writer1.WriteInt(data.idAccount);
        writer1.WriteInt(data.aimedMobID);
        writer1.WriteInt(damage);
        writer1.WriteInt(mob.hp);
        await RaceManager.Instance.SendPacketToAllClients(writer1.ToArray(), client);
    }
}