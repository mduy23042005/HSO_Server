using HSO_Server.Models;

public class NPCData
{
    public Npc npc;
    public int posX;
    public int posY;
}

//Trang bị riêng từng school
public class Item0Data
{
    public Item0 item0;
    public List<Item0Attribute> item0_Attributes;
    public List<HSO_Server.Models.Attribute> nameAttributes;
}
//Trang bị chung
public class Item1Data
{
    public Item1 item1;
    public List<Item1Attribute> item1_Attributes;
    public List<HSO_Server.Models.Attribute> nameAttributes;
}
//Vật phẩm nhiệm vụ
public class Item2Data
{
    public Item2 item2;
}
//Vật phẩm tiêu hao
public class Item3Data
{
    public Item3 item3;
}
//Vật phẩm cường hóa
public class Item4Data
{
    public Item4 item4;
}

//Tài nguyên của player
public class EquipmentData
{
    public int id;
    public int idItem0_1;
    public string nameItem0_1;
    public int category;
    public string slotName;
    public List<Item0Attribute> item0_Attributes;
    public List<HSO_Server.Models.Attribute> nameAttributes;
}
public class InventoryItem0Data
{
    public int id;
    public int idItem0;
    public string nameItem0;
    public string typeItem0;
    public int category;
    public int idSchool;
    public int level;
    public List<Item0Attribute> item0_Attributes;
    public List<HSO_Server.Models.Attribute> nameAttributes;
}
public class InventoryItem1Data
{
    public int id;
    public int idItem1;
    public string nameItem1;
    public string typeItem1;
    public int level;
    public List<Item1Attribute> item1_Attributes;
    public List<HSO_Server.Models.Attribute> nameAttributes;
}
public class InventoryItem2Data
{
    public int id;
    public int idItem2;
    public string nameItem2;
    public int level;
    public int quality;
}
public class InventoryItem3Data
{
    public int id;
    public int idItem3;
    public string nameItem3;
    public int level;
    public string details;
    public int quality;
}
public class InventoryItem4Data
{
    public int id;
    public int idItem4;
    public string nameItem4;
    public int level;
    public string details;
    public int quality;
}
public class ChestData
{
    public List<ChestItemX> chestItems;
}

public class AccountData
{
    public Account account;
    public List<EquipmentData> equipments;
    public List<InventoryItem0Data> inventoryItem0s;
    public List<InventoryItem1Data> inventoryItem1s;
    public List<InventoryItem2Data> inventoryItem2s;
    public List<InventoryItem3Data> inventoryItem3s;
    public List<InventoryItem4Data> inventoryItem4s;
    public Chest chest;

    public PlayerData playerData;
    public PlayerStateData playerStateData;
    public PlayerTransformData playerTransformData;
}

public class RegisterResultPacket
{
    public EnumCmdCode cmd;
    public bool success;
}
public class MoveArea
{
    /// <summary>
    /// centerX = offset(Unity)
    /// minX = posX - sizeX / 2
    /// maxX = posX + sizeX / 2
    /// minY = posY - sizeY / 2
    /// maxY = posY + sizeY / 2
    /// </summary>
    public float minX; // minX, minY ----------- maxX, minY
    public float minY; //     |                      |
    public float maxX; //     |                      |
    public float maxY; // minX, maxY ----------- maxX, maxY
}
public class MobData
{
    public Mob mob;
    public int id;
    public int hp;
    public int damage;
    public int posX;
    public int posY;
    public bool isRespawning;

    public MobController mobsAI;
}

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

public class EquipmentResultPacket
{
    public EnumCmdCode cmd;
    public List<EquipmentData> equipmentData;
}

public class InventoryResultPacket
{
    public EnumCmdCode cmd;
    public List<InventoryItem0Data> inventoryItem0Data;
    public List<InventoryItem1Data> inventoryItem1Data;
    public List<InventoryItem2Data> inventoryItem2Data;
    public List<InventoryItem3Data> inventoryItem3Data;
    public List<InventoryItem4Data> inventoryItem4Data;
}

//class trả dữ liệu về client
public class LogInResultPacket
{
    public EnumCmdCode cmd;
    public bool success;
    public int idAccount;
    public int idSchool;
    public string nameChar;
    public int hair;
    public int level;
    public int maxHP;
    public int maxMP;
    public int hp;
    public int mp;
    public string message;
}

public class DroppedItem0Data
{
    public int idMap;
    public int id; // id xác định chính xác con quái trong map
    public int idItem0;
    public float posX;
    public float posY;
    public string nameItem0;
}

public enum TileType
{
    None = -1,
    Ground = 0,
    Water = 1,
    Wall = 2,
    Decoration = 3,
    MapTransition = 4
}
public class MapData
{
    public Map map;
    public List<MobData> mobsData;
    public List<NPCData> npcsData;

    public int width;
    public int height;

    public int offsetX;
    public int offsetY;

    public byte[,] tiles;
}

public class ReadAttributesEquipmentResultPacket
{
    public EnumCmdCode cmd;
    public EquipmentData attributesData;
}

public class ReadAttributesInventoryResultPacket
{
    public EnumCmdCode cmd;
    public InventoryItem0Data attributesItem0Data;
    public InventoryItem1Data attributesItem1Data;
    public InventoryItem2Data attributesItem2Data;
    public InventoryItem3Data attributesItem3Data;
    public InventoryItem4Data attributesItem4Data;
}