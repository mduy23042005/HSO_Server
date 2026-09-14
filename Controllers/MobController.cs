using System;
using System.Collections.Generic;
using System.Numerics;
using HSO_Server.Models;

public class SyncMobsResultData
{
    public int id;
    public int idMob;
    public string nameMob;
    public float posX;
    public float posY;
    public int maxHP;
    public int hp;
    public int level;
    public State state;
    public int idState;
    public Direction direction;
    public TileType currentTile;
}
public class SyncMobsResultPacket
{
    public string cmd;
    public List<SyncMobsResultPacket> mobs;
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
public class MobController
{
    private float moveSpeed = 2f;
    private float waitAfterMove = 0f;
    private bool isAttacking = false;
    private MoveArea moveArea;
    private bool isDie = false;

    private Vector2 currentPosition;
    private Vector2 spawnPosition;
    private float direction;
    private int idState;

    private float visionRadius = 5f;      // vùng nhìn thấy
    private float attackRange = 1.5f;
    private float attackCooldown = 0f;
    private float attackInterval = 1.2f; // thời gian giữa 2 đòn
    private int targetPlayerId = -1;      // player đang bị target

    private (int x, int y) startPosition;
    private (int x, int y) endMovementPosition;
    private (int x, int y) endAttackPosition;
    private List<(int x, int y)> path;
    private int pathIndex;
    private AStarManager astar = new AStarManager();

    private static readonly Random random = new Random();

    public MobController(float posX, float posY, float sizeX, float sizeY)
    {
        moveArea = new MoveArea();
        moveArea.minX = posX - sizeX / 2;
        moveArea.maxX = posX + sizeX / 2;
        moveArea.minY = posY - sizeY / 2;
        moveArea.maxY = posY + sizeY / 2;

        spawnPosition = new Vector2(posX, posY);
        currentPosition = spawnPosition;
    }

    public Vector2 GetRandomPosition()
    {
        float x = (float)(random.NextDouble() * (moveArea.maxX - moveArea.minX) + moveArea.minX);
        float y = (float)(random.NextDouble() * (moveArea.maxY - moveArea.minY) + moveArea.minY);

        return new Vector2(x, y);
    }
    public void Move(float deltaTime, MapData map, int id)
    {
        (int, int) oldCell = ((int)currentPosition.X - map.offsetX, (int)currentPosition.Y - map.offsetY);

        if (isAttacking)
            return;

        if (isDie)
            return;

        // đang nghỉ
        if (waitAfterMove > 0f)
        {
            waitAfterMove -= deltaTime;
            return;
        }

        // chưa có path thì tạo mới
        if (path == null || path.Count == 0)
        {
            startPosition = ToGrid(currentPosition);

            do
            {
                endMovementPosition = ToGrid(GetRandomPosition());
            } while (!astar.IsWalkable(map, endMovementPosition.x, endMovementPosition.y));

            path = astar.FindPath(map, moveArea, startPosition.x, startPosition.y, endMovementPosition.x, endMovementPosition.y);

            if (path == null || path.Count == 0)
                return;

            pathIndex = 0;  
        }

        // nếu đi hết path thì nghỉ
        if (pathIndex >= path.Count)
        {
            waitAfterMove = 0.3f + (float)random.NextDouble() * 0.7f;
            path = null; // reset để lần sau tạo path mới
            startPosition = endMovementPosition;
            return;
        }

        // lấy node tiếp theo
        var node = path[pathIndex];
        Vector2 targetNode = new Vector2(node.x + 0.5f, node.y + 0.5f);
        Vector2 directionToTarget = targetNode - currentPosition; // vector hướng tới node tiếp theo
        float distanceToTarget = directionToTarget.Length();

        if (distanceToTarget > 0.05f) // gần tới target node
        {
            direction = Math.Sign(directionToTarget.X) != 0 ? Math.Sign(directionToTarget.X) : direction;
            float step = Math.Min(moveSpeed * deltaTime, 0.4f);

            // giới hạn bước đi không vượt quá khoảng cách tới node
            Vector2 nextPosition = currentPosition + (directionToTarget / distanceToTarget) * Math.Min(step, distanceToTarget);

            var nextNode = ToGrid(nextPosition);

            // kiểm tra nếu nextNode có thể walkable thì mới di chuyển, nếu không thì reset path
            if (astar.IsWalkable(map, nextNode.x, nextNode.y))
            {
                currentPosition = nextPosition;
            }
            else
            {
                path = null;
                return;
            }
        }

        // kiểm tra chuyển Node
        if (Vector2.Distance(currentPosition, targetNode) < 0.1f)
        {
            pathIndex++;
        }

        (int, int) newCell = ((int)(currentPosition.X - map.offsetX), (int)(currentPosition.Y - map.offsetY));

        if (oldCell != newCell)
        {
            lock (MapController.mapMobs)
            {
                MapController.mapMobs[map.map.Idmap][oldCell].Remove(CacheManager.Instance.GetMob(id));
                MapController.mapMobs[map.map.Idmap][newCell].Add(CacheManager.Instance.GetMob(id));
            }
        }
    }
    private (int x, int y) ToGrid(Vector2 pos)
    {
        return ((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y));
    }

    private int FindNearestPlayerInVision()
    {
        float minDistance = float.MaxValue; //gán 1 số cực lớn để đánh dấu chưa có player nào gần nhất
        int nearestPlayer = -1;

        foreach (var kv in CacheManager.Instance.GetAllAccountData())
        {
            var playerData = kv.Value.playerTransformData;
            var hpAimedPlayer = kv.Value.playerData.hp;
            if (playerData == null) 
                continue;

            if (hpAimedPlayer <= 0)
                continue;

            Vector2 playerPosition = new Vector2(playerData.positionData.x, playerData.positionData.y);
            float distance = Vector2.Distance(currentPosition, playerPosition);

            if (distance <= visionRadius && distance < minDistance)
            {
                minDistance = distance; // cập nhật player gần nhất
                nearestPlayer = kv.Key;
            }
        }

        return nearestPlayer;
    }
    public void Attack(float deltaTime, MapData map, int idMob, int mobDamage)
    {
        if (isDie)
            return;

        // cooldown đòn đánh
        if (attackCooldown > 0f)
            attackCooldown -= deltaTime;

        // Nếu chưa có target player thì tìm target player
        if (targetPlayerId == -1)
        {
            targetPlayerId = FindNearestPlayerInVision();
            if (targetPlayerId == -1)
            {
                // không attack nữa thì reset
                isAttacking = false;
                idState = 0;
                return;
            }
        }

        var account = CacheManager.Instance.GetAccountData(targetPlayerId);
        if (account == null || account.playerStateData == null || account.playerData.hp <= 0)
        {
            targetPlayerId = -1;
            isAttacking = false;
            idState = 0;

            // nếu dí theo player mà player disconnect thì tìm đường về moveArea
            if (currentPosition.X < moveArea.minX || currentPosition.X > moveArea.maxX || currentPosition.Y < moveArea.minY || currentPosition.Y > moveArea.maxY)
            {
                float returnX = Clamp(currentPosition.X, moveArea.minX, moveArea.maxX);
                float returnY = Clamp(currentPosition.Y, moveArea.minY, moveArea.maxY);

                startPosition = ToGrid(currentPosition);
                endMovementPosition = ToGrid(new Vector2(returnX, returnY));

                path = astar.FindPath(map, startPosition.x, startPosition.y, endMovementPosition.x, endMovementPosition.y);

                pathIndex = 0;
            }
            else
            {
                path = null;
            }
            return;
        }

        Vector2 playerPosition = new Vector2(account.playerTransformData.positionData.x, account.playerTransformData.positionData.y);
        Vector2 directionToPlayer = playerPosition - currentPosition;
        float distanceToPlayer = directionToPlayer.Length();
        // cập nhật hướng để gửi tới client
        direction = directionToPlayer.X;

        // player ra khỏi vùng nhìn thấy thì isAttacking = false
        // khi vào hàm Move() thì path nó vẫn còn tồn tại và mob vẫn chưa đi hết path nên hàm FindPathForMob() không chạy mà nó sẽ di chuyển tới hết path return
        // khi hết path return thì quay lại logic như cũ
        if (distanceToPlayer > visionRadius)
        {
            targetPlayerId = -1;
            isAttacking = false;
            idState = 0;

            // nếu dí theo player mà ra khỏi moveArea thì tìm đường về moveArea
            if (currentPosition.X < moveArea.minX || currentPosition.X > moveArea.maxX || currentPosition.Y < moveArea.minY || currentPosition.Y > moveArea.maxY)
            {
                float returnX = Clamp(currentPosition.X, moveArea.minX, moveArea.maxX);
                float returnY = Clamp(currentPosition.Y, moveArea.minY, moveArea.maxY);

                startPosition = ToGrid(currentPosition);
                endMovementPosition = ToGrid(new Vector2(returnX, returnY));

                path = astar.FindPath(map, startPosition.x, startPosition.y, endMovementPosition.x, endMovementPosition.y);

                pathIndex = 0;
            }
            else
            {
                path = null;
            }
            return;
        }

        // tìm path tới player (endAttackPosition != path[path.Count - 1] nghĩa là nếu player move endAttackPosition sẽ đổi thì path cũng phải đổi theo)
        if (path == null || path.Count == 0 || pathIndex >= path.Count || endAttackPosition != path[path.Count - 1])
        {
            startPosition = ToGrid(currentPosition);
            endAttackPosition = ToGrid(playerPosition);

            // cho phép vượt ra khỏi vùng moveArea để dí theo player
            path = astar.FindPath(map, startPosition.x, startPosition.y, endAttackPosition.x, endAttackPosition.y);

            if (path == null || path.Count == 0)
                return;

            pathIndex = 0;

            if (pathIndex >= path.Count)
            {
                path = null; // reset để lần sau tạo path mới
                startPosition = endMovementPosition;
                return;
            }
        }
        // bắt đầu lao tới player
        var node = path[pathIndex];
        Vector2 targetNode = new Vector2(node.x + 0.5f, node.y + 0.5f);
        Vector2 directionToTarget = targetNode - currentPosition; // vector hướng tới node tiếp theo
        float distanceToTarget = directionToTarget.Length();

        if (distanceToTarget > 0.05f) // gần tới target node
        {
            direction = Math.Sign(directionToTarget.X) != 0 ? Math.Sign(directionToTarget.X) : direction;
            float step = Math.Min(moveSpeed * deltaTime, 0.4f);

            // giới hạn bước đi không vượt quá khoảng cách tới node
            Vector2 nextPosition = currentPosition + (directionToTarget / distanceToTarget) * Math.Min(step, distanceToTarget);

            var nextNode = ToGrid(nextPosition);

            // kiểm tra nếu nextNode có thể walkable thì mới di chuyển, nếu không thì reset path
            if (astar.IsWalkable(map, nextNode.x, nextNode.y))
            {
                currentPosition = nextPosition;
            }
            else
            {
                path = null;
                return;
            }
        }

        // kiểm tra chuyển Node
        if (Vector2.Distance(currentPosition, targetNode) < 0.1f)
        {
            pathIndex++;
        }

        if (Vector2.Distance(currentPosition, playerPosition) <= attackRange && attackCooldown <= 0f)
        {
            isAttacking = true;
            idState++;
            attackCooldown = attackInterval;
            path = null; // reset để lần sau tạo path mới
            startPosition = endMovementPosition;

            var player = CacheManager.Instance.GetAccountData(targetPlayerId);
            if (player == null)
                return;
            
            player.playerData.hp = player.playerData.hp - mobDamage;
            var client = RaceManager.Instance.GetClientByAccountId(targetPlayerId);

            PacketWriterManager writer = new PacketWriterManager();
            writer.WriteInt((int)EnumCmdCode.mobsAttackPlayer);
            writer.WriteInt(idMob);
            writer.WriteInt(player.playerData.idAccount);
            writer.WriteInt(mobDamage);
            writer.WriteInt(player.playerData.hp);
            RaceManager.Instance.SendPacketToClient(client, writer.ToArray());

            PacketWriterManager writer1 = new PacketWriterManager();
            writer1.WriteInt((int)EnumCmdCode.mobsAttackOtherPlayer);
            writer1.WriteInt(idMob);
            writer1.WriteInt(player.playerData.idAccount);
            writer1.WriteInt(mobDamage);
            writer1.WriteInt(player.playerData.hp);
            RaceManager.Instance.SendPacketToAllClients(writer1.ToArray());
        }
    }
    private float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public void Die()
    {
        isAttacking = false;
        waitAfterMove = 0f;
        targetPlayerId = -1;
        path = null;
        idState = 0;
        attackCooldown = 0f;
        isDie = true;
    }
    public void Respawn()
    {
        currentPosition = spawnPosition;
        isDie = false;
        isAttacking = false;
        targetPlayerId = -1;
        path = null;
    }

    public Vector2 GetCurrentPosition()
    {
        return currentPosition;
    }
    public State GetState()
    {
        if (isAttacking)
            return State.Attack;
        if (waitAfterMove > 0f)
            return State.Stand;
        if (isDie)
            return State.Die;
        return State.Move;
    }
    public int GetIDState()
    {
        return idState;
    }
    public Direction GetDirection()
    {
        if (direction < 0)
            return Direction.Right;
        return Direction.Left;
    }
}