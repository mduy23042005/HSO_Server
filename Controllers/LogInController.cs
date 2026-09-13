using Newtonsoft.Json;
using HSO_Server.Models;

//class hứng dữ liệu từ client gửi lên
public class LogInRequestPacket
{
    public EnumCmdCode cmd;
    public string username;
    public string password;
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

public class LogOutRequestPacket
{
    public EnumCmdCode cmd;
    public int idAccount;
}

public class LogInController
{
    private TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private DateTime time;

    public async Task ClickLogIn(ClientConnection client, LogInRequestPacket loginPacket)
    {
        LogInResultPacket loginResult;

        string urlAccount = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/login?username={loginPacket.username}&password={loginPacket.password}";
        HttpResponseMessage res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlAccount);
        string json = await res.Content.ReadAsStringAsync();
        var acc = JsonConvert.DeserializeObject<Account>(json);

        PacketWriterManager writer;

        if (acc != null)
        {
            if (CacheManager.Instance.IsAccountOnline(acc.Idaccount))
            {
                loginResult = new LogInResultPacket
                {
                    cmd = EnumCmdCode.login,
                    success = false,
                    idAccount = 0,
                    idSchool = 0,
                    nameChar = null,
                    hair = 0,
                    level = 0,
                    maxHP = 0,
                    maxMP = 0,
                    hp = 0,
                    mp = 0,
                    message = $"Tài khoản {acc.NameChar} đang online."
                };

                writer = new PacketWriterManager();
                writer.WriteInt((int)loginResult.cmd);
                writer.WriteBool(loginResult.success);
                writer.WriteInt(loginResult.idAccount);
                writer.WriteInt(loginResult.idSchool);
                writer.WriteString(loginResult.nameChar);
                writer.WriteInt(loginResult.hair);
                writer.WriteInt(loginResult.level);
                writer.WriteInt(loginResult.maxHP);
                writer.WriteInt(loginResult.maxMP);
                writer.WriteInt(loginResult.hp);
                writer.WriteInt(loginResult.mp);
                writer.WriteString(loginResult.message);

                await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());

                return;
            }
            else
            {
                loginResult = new LogInResultPacket
                {
                    cmd = EnumCmdCode.login,
                    success = acc != null,
                    idAccount = acc.Idaccount,
                    idSchool = acc.Idschool ?? 0,
                    nameChar = acc.NameChar,
                    hair = acc.Hair ?? 0,
                    level = acc.Level ?? 1,
                    message = $"Đăng nhập {acc.NameChar} thành công."
                };

                await LoadAccountData(acc);

                loginResult.maxHP = CacheManager.Instance.GetAccountData(acc.Idaccount).playerData.maxHP;
                loginResult.maxMP = CacheManager.Instance.GetAccountData(acc.Idaccount).playerData.maxMP;
                loginResult.hp = CacheManager.Instance.GetAccountData(acc.Idaccount).playerData.hp;
                loginResult.mp = CacheManager.Instance.GetAccountData(acc.Idaccount).playerData.mp;

                time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Welcome back {loginResult.nameChar}.");
                RaceManager.Instance.BindAccountToClient(client, acc.Idaccount);
            }
        }
        else
        {
            loginResult = new LogInResultPacket
            {
                cmd = EnumCmdCode.login,
                success = false,
                idAccount = 0,
                idSchool = 0,
                nameChar = null,
                hair = 0,
                level = 0,
                maxHP = 0,
                maxMP = 0,
                hp = 0,
                mp = 0,
                message = "Username hoặc Password không đúng."
            };
        }

        writer = new PacketWriterManager();
        writer.WriteInt((int)loginResult.cmd);
        writer.WriteBool(loginResult.success);
        writer.WriteInt(loginResult.idAccount);
        writer.WriteInt(loginResult.idSchool);
        writer.WriteString(loginResult.nameChar);
        writer.WriteInt(loginResult.hair);
        writer.WriteInt(loginResult.level);
        writer.WriteInt(loginResult.maxHP);
        writer.WriteInt(loginResult.maxMP);
        writer.WriteInt(loginResult.hp);
        writer.WriteInt(loginResult.mp);
        writer.WriteString(loginResult.message);

        if (acc != null)
        {
            EquipmentController equipmentController = new EquipmentController();
            await equipmentController.ReadCacheEquipment(client);
            InventoryController inventoryController = new InventoryController();
            await inventoryController.ReadCacheInventory(client);
        }

        await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());
    }
    private async Task LoadAccountData(Account acc)
    {
        HttpResponseMessage res;
        string json;

        string urlEquipment = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/equipment?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlEquipment);
        json = await res.Content.ReadAsStringAsync();
        var equipmentData = JsonConvert.DeserializeObject<List<EquipmentData>>(json);

        string urlInventoryItem0 = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/inventoryItem0?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlInventoryItem0);
        json = await res.Content.ReadAsStringAsync();
        var inventoryItem0Data = JsonConvert.DeserializeObject<List<InventoryItem0Data>>(json);

        string urlInventoryItem1 = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/inventoryItem1?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlInventoryItem1);
        json = await res.Content.ReadAsStringAsync();
        var inventoryItem1Data = JsonConvert.DeserializeObject<List<InventoryItem1Data>>(json);

        string urlInventoryItem2 = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/inventoryItem2?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlInventoryItem2);
        json = await res.Content.ReadAsStringAsync();
        var inventoryItem2Data = JsonConvert.DeserializeObject<List<InventoryItem2Data>>(json);

        string urlInventoryItem3 = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/inventoryItem3?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlInventoryItem3);
        json = await res.Content.ReadAsStringAsync();
        var inventoryItem3Data = JsonConvert.DeserializeObject<List<InventoryItem3Data>>(json);

        string urlInventoryItem4 = $"{WebAPIManager.Instance.GetApiUrl()}/api/account/{acc.Idaccount}/inventoryItem4?idAccount={acc.Idaccount}";
        res = await WebAPIManager.Instance.GetHttpClient().GetAsync(urlInventoryItem4);
        json = await res.Content.ReadAsStringAsync();
        var inventoryItem4Data = JsonConvert.DeserializeObject<List<InventoryItem4Data>>(json);

        AccountData accountData = new AccountData();
        accountData.account = acc;
        accountData.equipments = equipmentData;
        if (inventoryItem0Data != null)
        {
            accountData.inventoryItem0s = inventoryItem0Data;
        }
        if (inventoryItem2Data != null)
        {
            accountData.inventoryItem2s = inventoryItem2Data;
        }
        if (inventoryItem3Data != null)
        {
            accountData.inventoryItem3s = inventoryItem3Data;
        }
        if (inventoryItem4Data != null)
        {
            accountData.inventoryItem4s = inventoryItem4Data;
        }

        accountData.playerData = new PlayerData();
        accountData.playerData.idAccount = acc.Idaccount;
        accountData.playerData.level = acc.Level ?? 1;
        accountData.playerData.idSchool = acc.Idschool ?? 0;
        accountData.playerData.nameChar = acc.NameChar;
        accountData.playerData.hair = acc.Hair ?? 0;
        accountData.playerData.weapon = accountData.equipments[0].idItem0_1;
        accountData.playerData.helmet = accountData.equipments[1].idItem0_1;
        accountData.playerData.armor = accountData.equipments[2].idItem0_1;
        accountData.playerData.legArmor = accountData.equipments[3].idItem0_1;
        accountData.playerData.idMap = 1;

        var playerController = new PlayerController(accountData.account.Idaccount, acc.Point0 ?? 5, acc.Point1 ?? 5, acc.Point2 ?? 5, acc.Point3 ?? 5);
        accountData.playerData.maxHP = playerController.GetMaxHP();
        accountData.playerData.maxMP = playerController.GetMaxMP();
        accountData.playerData.hp = playerController.GetHP();
        accountData.playerData.mp = playerController.GetMP();
        CacheManager.Instance.AddAccountData(accountData);
    }
}
