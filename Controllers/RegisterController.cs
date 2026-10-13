using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using HSO_Server.Models;

public class RegisterController
{
    private TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private DateTime time;

    public async Task ClickRegister(ClientConnection client, RegisterRequestPacket registerPacket)
    {
        int weapon = 0, helmet = 0, armor = 0, legArmor = 0;
        switch (registerPacket.idSchool)
        {
            case 1: weapon = 1; helmet = 9; armor = 17; legArmor = 25; break;
            case 2: weapon = 2; helmet = 10; armor = 18; legArmor = 26; break;
            case 3: weapon = 3; helmet = 11; armor = 19; legArmor = 27; break;
            case 4: weapon = 4; helmet = 12; armor = 20; legArmor = 28; break;
        }
        // Tạo object để gửi lên API
        var registerData = new
        {
            Account = new Account
            {
                Username = registerPacket.username,
                Password = registerPacket.password,
                NameChar = registerPacket.nameChar,
                Idschool = registerPacket.idSchool,
                Level = 1,
                SkillPoints = 0,
                StatPoints = 0,
                Exp = 0,
                Hair = registerPacket.hair,
                Gold = 20000,
                Gem = 2000,
                Point0 = 5,
                Point1 = 5,
                Point2 = 5,
                Point3 = 5,
                PointArena = 0,
                PointActive = 100000,
                Skill0 = 1,
                Skill1 = 0,
                Skill2 = 0,
                Skill3 = 0,
                Skill4 = 0,
                Skill5 = 0,
                Skill6 = 0,
                Skill7 = 0,
                Skill8 = 0,
                Skill9 = 0,
                Skill10 = 0,
                Skill11 = 0,
                Skill12 = 0,
                Skill13 = 0,
                Skill14 = 0,
                Skill15 = 0,
                Skill16 = 0,
                Skill17 = 0,
                Skill18 = 0,
                Skill19 = 0,
                Skill20 = 0,
                Clan = null,
                BlessingPoints = registerPacket.blessingPoints
            },
            Equipment = new List<AccountEquipment>
            {
                new AccountEquipment { Iditem01 = weapon, SlotName = "Weapon", Category = 1 },
                new AccountEquipment { Iditem01 = helmet, SlotName = "Helmet", Category = 1 },
                new AccountEquipment { Iditem01 = armor, SlotName = "Armor", Category = 1 },
                new AccountEquipment { Iditem01 = legArmor, SlotName = "LegArmor", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Gloves", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Shoes", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Ring1", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Ring2", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Necklace", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Medal", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Cloak", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Wing", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "SkinWing", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Mounts", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Pet", Category = 1 },
                new AccountEquipment { Iditem01 = 0, SlotName = "Skin", Category = 1 },
            }
        };

        string json = JsonConvert.SerializeObject(registerData);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage result = await WebAPIManager.Instance.GetHttpClient().PostAsync($"{WebAPIManager.Instance.GetApiUrl()}/api/account/register", content);

        RegisterResultPacket registerResult;

        if (result.IsSuccessStatusCode)
        {
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Register successfully.");

            registerResult = new RegisterResultPacket
            {
                cmd = EnumCmdCode.register,
                success = result.IsSuccessStatusCode
            };
        }
        else
        {
            string errorMsg = await result.Content.ReadAsStringAsync();
            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Register failed: {errorMsg}");

            registerResult = new RegisterResultPacket
            {
                cmd = EnumCmdCode.register,
                success = result.IsSuccessStatusCode
            };
        }

        PacketWriterManager writer = new PacketWriterManager();
        writer.WriteInt((int)registerResult.cmd);
        writer.WriteBool(registerResult.success);

        await RaceManager.Instance.SendPacketToClient(client, writer.ToArray());
    }
}