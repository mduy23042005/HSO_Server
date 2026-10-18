public enum EnumCmdCode
{
    login = 0x0000,
    register = 0x0002,
    changeMap = 0x0003,

    equipment = 0x0010,
    equipmentAttributes = 0x0011,
    inventory = 0x0012,
    inventoryAttributes = 0x0013,

    equipItem0 = 0x0014,

    syncMobsData = 0x0100,

    syncCallBack = 0x0140,
    syncOtherPlayersData = 0x0150,
    syncOtherPlayersRealtimeData = 0x0151,

    mobsAttackPlayer = 0x0200,
    mobsHeal = 0x0201,
    mobsInjured = 0x0202,
    mobsDie = 0x0203,
    mobsAttackOtherPlayer = 0x0204,

    updateHP = 0x0300,
    updateMP = 0x0301,

    playerAttackMob = 0x0400,
    otherPlayerAttackMob = 0x0401,

    dropItem0 = 0x0500,
    dropItem1 = 0x0501,
    dropItem2 = 0x0502,
    dropItem3 = 0x0503,
    dropItem4 = 0x0504,

    dropItem0s = 0x0510,
    dropItem1s = 0x0511,
    dropItem2s = 0x0512,
    dropItem3s = 0x0513,
    dropItem4s = 0x0514,
}