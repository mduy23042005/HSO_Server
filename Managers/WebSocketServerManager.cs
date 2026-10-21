using HSO_Server.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Numerics;

public sealed class ClientConnection
{
    public Guid idConnection = Guid.NewGuid();
    public WebSocket socket;
    public string ipRemote;
    public DateTime connectedAt = DateTime.UtcNow;

    public ClientConnection(WebSocket socket, string ipRemote)
    {
        this.socket = socket;
        this.ipRemote = ipRemote;
    }
}

public class WebSocketServerManager
{
    private HttpListener listener;
    private CancellationTokenSource shutdownCts = new CancellationTokenSource();
    private volatile bool isShuttingDown = false;
    private TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private DateTime time;

    public static void Main(string[] args)
    {
        WebSocketServerManager server = new WebSocketServerManager();
        Console.WriteLine("------------------------------");
        Console.WriteLine("| Starting Web Socket Server |");
        Console.WriteLine("------------------------------");
        server.RunWebSocketServer().GetAwaiter().GetResult();
    }

    private async Task RunWebSocketServer()
    {
        try
        {
            //Khởi động Web API
            bool isAPIConnected = await WebAPIManager.Instance.InitAPI();

            if (!isAPIConnected)
            {
                time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                Console.WriteLine($"[Server] {time:hh:mm:ss tt} Connected to Web API failed! Web Socket Server stopped.");
                return;
            }

            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time:hh:mm:ss tt} Connected to Web API successfully! (http://localhost:55555)");

            //Kiểm tra port lắng nghe
            ListenToPort(55556);
            string serverIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "Unknown";
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Started Web Socket Server at: {serverIP}:55556 successfully!");

            //Khởi động Cleanup Loop để dọn dẹp client ngắt kết nối thụ động
            _ = Task.Run(InitCleanupLoop);
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized Cleanup Loop successfully!");

            //Khởi động Cache
            CacheManager.Instance.InitCache();
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized Cache successfully!");

            //Load data
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loading data...");
            await LoadData();

            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initializing Dropped Items...");
            DroppedItemManager.Instance.InitDroppedItem0s();
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized Dropped Items successfully! [{CacheManager.Instance.GetCountDroppedItem0sMob()}]");

            _ = Task.Run(() => MobManager.Instance.UpdateMobsLoop(shutdownCts));
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized UpdateMobs loop successfully!");

            _ = Task.Run(() => MobManager.Instance.SyncMobsLoop(shutdownCts));
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized SyncMobs loop successfully!");

            _ = Task.Run(() => DroppedItemManager.Instance.SyncDroppedItemsLoop(shutdownCts));
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized SyncDroppedItems loop successfully!");

            _ = Task.Run(() => PlayerManager.Instance.SyncOtherPlayersLoop(shutdownCts));
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized SyncOtherPlayers loop successfully!");

            _ = Task.Run(() => PlayerManager.Instance.SyncOtherPlayersRealtimeLoop(shutdownCts));
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized SyncOtherPlayersRealtime loop successfully!");
            
            _ = Task.Run(CountOnlinePlayersLoop);
            _ = Task.Run(ListenForQuit);

            //Chấp nhận kết nối từ client
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"\n[Server] {time.ToString("hh:mm:ss tt")} Web Socket Server is ready!");
            await AcceptClients();
        }
        catch (Exception ex)
        {
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Fatal error: {ex}");

            throw;
        }
    }   

    private void ListenToPort(int port)
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://+:{port}/");
        listener.Start();
    }
    private async Task LoadData()
    {
        string loadMap = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Map/full";
        string loadItem0 = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Item0/full";
        string loadItem1 = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Item1/full";
        string loadItem2 = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Item2/full";
        string loadItem3 = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Item3/full";
        string loadItem4 = $"{WebAPIManager.Instance.GetApiUrl()}/api/load/Item4/full";

        HttpResponseMessage res;
        string json;

        #region Load Map
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadMap);
        json = await res.Content.ReadAsStringAsync();
        var mapList = JsonConvert.DeserializeObject<List<MapData>>(json);
        if (mapList != null)
        {
            foreach (var mapData in mapList)
            {
                CacheManager.Instance.AddMap(mapData);
                CacheManager.Instance.AddClientMap(mapData);

                MapController mapController = new MapController();
                mapController.InitMap(mapData);
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Map data successfully! [{CacheManager.Instance.GetCountMap()}]");
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Initialized Map data successfully! [{CacheManager.Instance.GetCountInitedMap()}]");
        #endregion

        #region Load NPC
        if (mapList != null)
        {
            foreach (var map in mapList)
            {
                foreach (var npcData in map.npcsData)
                {
                    CacheManager.Instance.AddNPC(new NPCData
                    {
                        npc = npcData.npc,
                        posX = npcData.posX,
                        posY = npcData.posY,
                    });
                }
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded NPC data successfully! [{CacheManager.Instance.GetCountNPC()}]");
        #endregion

        #region Load Mob
        if (mapList != null)
        {
            foreach (var map in mapList)
            {
                foreach (var mobData in map.mobsData)
                {
                    mobData.mobsAI = new MobController(mobData.posX, mobData.posY, 6, 6);
                    int level = mobData.mob.Level ?? 1;
                    mobData.damage = level * 10;
                    mobData.hp = mobData.mob.Hp ?? 0;

                    CacheManager.Instance.AddMob(mobData);
                    MapController.mapMobs[map.map.Idmap][(mobData.posX - map.offsetX, mobData.posY - map.offsetY)].Add(mobData);
                }
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Mob data successfully! [{CacheManager.Instance.GetCountMob()}]");
        #endregion

        #region Load Item0 - Item4
        //Load Item0
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadItem0);
        json = await res.Content.ReadAsStringAsync();
        var item0List = JsonConvert.DeserializeObject<List<Item0Data>>(json);
        if (item0List != null)
        {
            foreach (var item in item0List)
            {
                CacheManager.Instance.AddItem0(item);
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Item0 data successfully! [{CacheManager.Instance.GetCountItem0()}]");

        //Load Item1
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadItem1);
        json = await res.Content.ReadAsStringAsync();
        var item1List = JsonConvert.DeserializeObject<List<Item1Data>>(json);
        if (item1List != null)
        {
            foreach (var item in item1List)
            {
                CacheManager.Instance.AddItem1(item);
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Item1 data successfully! [{CacheManager.Instance.GetCountItem1()}]");

        //Load Item2
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadItem2);
        json = await res.Content.ReadAsStringAsync();
        var item2List = JsonConvert.DeserializeObject<List<Item2>>(json);
        if (item2List != null)
        {
            foreach (var item in item2List)
            {
                CacheManager.Instance.AddItem2(new Item2Data
                {
                    item2 = item
                });
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Item2 data successfully! [{CacheManager.Instance.GetCountItem2()}]");

        //Load Item3
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadItem3);
        json = await res.Content.ReadAsStringAsync();
        var item3List = JsonConvert.DeserializeObject<List<Item3>>(json);
        if (item3List != null)
        {
            foreach (var item in item3List)
            {
                CacheManager.Instance.AddItem3(new Item3Data
                {
                    item3 = item
                });
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Item3 data successfully! [{CacheManager.Instance.GetCountItem3()}]");

        //Load Item4
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(loadItem4);
        json = await res.Content.ReadAsStringAsync();
        var item4List = JsonConvert.DeserializeObject<List<Item4>>(json);
        if (item4List != null)
        {
            foreach (var item in item4List)
            {
                CacheManager.Instance.AddItem4(new Item4Data
                {
                    item4 = item
                });
            }
        }
        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded Item4 data successfully! [{CacheManager.Instance.GetCountItem4()}]");
        #endregion

        time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Loaded all data successfully!");
    }
    private async Task InitCleanupLoop()
    {
        while (!shutdownCts.IsCancellationRequested)
        {
            RaceManager.Instance.RemoveDisconnectedClients();

            try
            {
                await Task.Delay(1000, shutdownCts.Token);
            }
            catch (TaskCanceledException)
            {
                // server shutdown
                break;
            }
        }
    }
    private async Task AcceptClients()
    {
        while (!shutdownCts.IsCancellationRequested)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();

                // Kiểm tra đây có phải Web Socket request không
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);

                    string IPClient = context.Request.RemoteEndPoint?.ToString() ?? "Unknown";
                    var client = new ClientConnection(wsContext.WebSocket, IPClient);

                    RaceManager.Instance.RegisterClient(client);

                    time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                    Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Socket connected: {client.ipRemote}.");

                    _ = HandleClient(client);
                }
                else
                {
                    // Nếu không phải Web Socket thì trả về 400
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }
    private async Task HandleClient(ClientConnection client)
    {
        var buffer = new byte[4096];
        var messageBuffer = new List<byte>();

        try
        {
            while (client.socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;

                do
                {
                    if (client.socket.State != WebSocketState.Open)
                        return;

                    result = await client.socket.ReceiveAsync(new ArraySegment<byte>(buffer), shutdownCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));

                } while (!result.EndOfMessage);

                byte[] fullMessage = messageBuffer.ToArray();
                messageBuffer.Clear();

                await ReceivePacketFromClient(client, fullMessage);
            }
        }
        catch (WebSocketException)
        {

        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            RaceManager.Instance.MarkLogOut(client);

            try
            {
                var accountData = CacheManager.Instance.GetAccountData(RaceManager.Instance.GetIDAccount(client));
                var map = CacheManager.Instance.GetMap(accountData.playerData.idMap);

                lock (MapController.mapPlayers)
                {
                    for (int y = 0; y < map.height; y++)
                    {
                        for (int x = 0; x < map.width; x++)
                        {
                            if (MapController.mapPlayers[map.map.Idmap].TryGetValue((x, y), out var playersInCell))
                            {
                                playersInCell.Remove(client);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] Remove disconnected player error: " + ex);
            }
        }
    }
    private async Task CountOnlinePlayersLoop()
    {
        const int targetTickRate = 1;
        const int tickMS = 1000000 / targetTickRate;

        var stopwatch = new Stopwatch();

        while (!shutdownCts.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                var clients = RaceManager.Instance.GetAllClients();

                var onlinePlayers = new List<int>();

                foreach (var client in clients)
                {
                    int idAccount = RaceManager.Instance.GetIDAccount(client);

                    if (idAccount > 0)
                    {
                        onlinePlayers.Add(idAccount);
                    }
                }

                time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Online players: [{onlinePlayers.Count}]");
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] Count online players error: " + ex.Message);
            }

            stopwatch.Stop();
            int sleep = tickMS - (int)stopwatch.ElapsedMilliseconds;
            if (sleep > 0)
                await Task.Delay(sleep, shutdownCts.Token);
        }
    }
    private void ListenForQuit()
    {
        while (!shutdownCts.IsCancellationRequested)
        {
            string input = Console.ReadLine();

            if (input == null)
                continue;

            if (input == "q")
            {
                Console.WriteLine("Do you want to quit? (y/n): ");
                string confirm = Console.ReadLine();

                if (confirm == null)
                    continue;

                if (confirm == "y")
                {
                    if (isShuttingDown) return;
                    isShuttingDown = true;

                    time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                    Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Shutting down...");

                    // Báo toàn bộ task dừng
                    shutdownCts.Cancel();

                    // Logout toàn bộ client
                    RaceManager.Instance.MarkLogOutAll();

                    // Stop listener
                    listener.Stop();

                    time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                    Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Shutdown completed.");
                    Environment.Exit(0);
                    return;
                }
                else
                {
                    Console.WriteLine("Cancel shutdown.");
                }
            }
        }
    }

    private async Task ReceivePacketFromClient(ClientConnection client, byte[] data)
    {
        try
        {
            PacketReaderManager reader = new PacketReaderManager(data);
            EnumCmdCode cmd = (EnumCmdCode)reader.ReadInt();

            switch (cmd)
            {
                case EnumCmdCode.syncOtherPlayersData:
                    var syncOtherPlayersController = new PlayerController();
                    await syncOtherPlayersController.UpdatePlayerInfo(client, data);
                    break;

                case EnumCmdCode.login:
                    string usernameLogin = reader.ReadString();
                    string passwordLogin = reader.ReadString();

                    var loginController = new LogInController();
                    await loginController.ClickLogIn(client, usernameLogin, passwordLogin);
                    break;

                case EnumCmdCode.register:
                    Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Received register request from client: {client.ipRemote}.");
                    int idSchool = reader.ReadInt();
                    string nameChar = reader.ReadString();
                    string usernameRegister = reader.ReadString();
                    string passwordRegister = reader.ReadString();
                    int hair = reader.ReadInt();
                    int blessingPoints = reader.ReadInt();

                    var registerController = new RegisterController();
                    await registerController.ClickRegister(client, idSchool, nameChar, usernameRegister, passwordRegister, hair, blessingPoints);
                    break;

                case EnumCmdCode.equipment:
                    var equipmentController = new EquipmentController();
                    await equipmentController.ReadCacheEquipment(client);
                    break;

                case EnumCmdCode.equipmentAttributes:
                    int idAccountEquipmentAttributes = reader.ReadInt();
                    int idEquipmentAttributes = reader.ReadInt();
                    int idItem0_1EquipmentAttributes = reader.ReadInt();

                    var readAttributesEquipmentController = new ReadAttributesController();
                    await readAttributesEquipmentController.ReadAttributesEquipment(client, idAccountEquipmentAttributes, idEquipmentAttributes, idItem0_1EquipmentAttributes);
                    break;

                case EnumCmdCode.inventory:
                    var inventoryController = new InventoryController();
                    await inventoryController.ReadCacheInventory(client);
                    break;

                case EnumCmdCode.inventoryAttributes:
                    int idAccountInventoryAttributes = reader.ReadInt();
                    int idInventoryAttributes = reader.ReadInt();
                    int idItem0InventoryAttributes = reader.ReadInt();

                    var readAttributesInventoryController = new ReadAttributesController();
                    await readAttributesInventoryController.ReadAttributesInventory(client, idAccountInventoryAttributes, idInventoryAttributes, idItem0InventoryAttributes);
                    break;

                case EnumCmdCode.equipItem0:
                    int idAccountEquipItem0 = reader.ReadInt();
                    int idEquipItem0 = reader.ReadInt();
                    int idItem0EquipItem0 = reader.ReadInt();
                    string slotNameEquipItem0 = reader.ReadString();

                    var equipItem0Controller = new ReadAttributesController();
                    await equipItem0Controller.EquipItem0(client, idAccountEquipItem0, idEquipItem0, idItem0EquipItem0, slotNameEquipItem0);
                    break;

                case EnumCmdCode.dropItem0:
                    int idAccountDropItem0 = reader.ReadInt();
                    int idDropItem0 = reader.ReadInt();
                    int idItem0DropItem0 = reader.ReadInt();

                    var dropItem0Controller = new DroppedItemManager();
                    await dropItem0Controller.DropItem0(client, idAccountDropItem0, idDropItem0, idItem0DropItem0);
                    break;

                case EnumCmdCode.playerAttackMob:
                    int idAccount = reader.ReadInt();
                    int aimedMobID = reader.ReadInt();

                    var playerController = new PlayerController(idAccount);
                    await playerController.PlayerAttack(client, aimedMobID);
                    break;

                default:
                    Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Received unknown command from client: {client.ipRemote}. Command: {cmd}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing packet: " + ex.Message);
        }
    }
}
