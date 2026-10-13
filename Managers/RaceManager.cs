using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public sealed class RaceManager
{
    private static readonly Lazy<RaceManager> lazyInstance = new Lazy<RaceManager>(() => new RaceManager());
    private readonly Dictionary<ClientConnection, SemaphoreSlim> socketSendLocks = new Dictionary<ClientConnection, SemaphoreSlim>();
    private readonly HashSet<ClientConnection> logoutClients = new HashSet<ClientConnection>();

    private readonly List<ClientConnection> connectedClients;
    private readonly Dictionary<ClientConnection, int> clientAccountMapping;

    private readonly object clientCollectionLock;

    private TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private DateTime time;

    public static RaceManager Instance => lazyInstance.Value;
    private RaceManager()
    {
        connectedClients = new List<ClientConnection>();
        socketSendLocks = new Dictionary<ClientConnection, SemaphoreSlim>();
        clientAccountMapping = new Dictionary<ClientConnection, int>();
        logoutClients = new HashSet<ClientConnection>();
        clientCollectionLock = new object();
    }

    public void RegisterClient(ClientConnection client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        lock (clientCollectionLock)
        {
            if (!connectedClients.Contains(client))
            {
                connectedClients.Add(client);
                socketSendLocks[client] = new SemaphoreSlim(1, 1);
            }
        }
    }
    public void UnregisterClient(ClientConnection client)
    {
        if (client == null)
            return;

        lock (clientCollectionLock)
        {
            connectedClients.Remove(client);
            clientAccountMapping.Remove(client);

            if (socketSendLocks.TryGetValue(client, out var semaphore))
            {
                semaphore.Dispose();
                socketSendLocks.Remove(client);
            }
        }
    }

    public void BindAccountToClient(ClientConnection client, int idAccount)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        lock (clientCollectionLock)
        {
            clientAccountMapping[client] = idAccount;
        }
    }
    public int GetIDAccount(ClientConnection client)
    {
        if (client == null)
            return 0;

        lock (clientCollectionLock)
        {
            return clientAccountMapping.TryGetValue(client, out var idAccount) ? idAccount : 0;
        }
    }
    public ClientConnection GetClientByAccountId(int idAccount)
    {
        lock (clientCollectionLock)
        {
            foreach (var kv in clientAccountMapping)
            {
                if (kv.Value == idAccount)
                    return kv.Key;
            }
        }
        return null;
    }

    private List<ClientConnection> CreateClientSnapshot()
    {
        lock (clientCollectionLock)
        {
            return new List<ClientConnection>(connectedClients);
        }
    }

    public async Task SendPacketToAllClients(byte[] data, ClientConnection excludedClient = null)
    {
        if (data == null)
            return;

        List<ClientConnection> snapshot = CreateClientSnapshot();
        List<Task> tasks = new List<Task>();

        foreach (ClientConnection client in snapshot)
        {
            if (client == excludedClient)
                continue;

            if (client.socket == null || client.socket.State != WebSocketState.Open)
                continue;

            tasks.Add(SendPacketToClient(client, data));
        }

        await Task.WhenAll(tasks);
    }
    public async Task SendPacketToAllClientsInMap(int mapID, byte[] data, ClientConnection excludedClient = null)
    {
        if (data == null)
            return;

        List<ClientConnection> snapshot = CreateClientSnapshot();
        List<Task> tasks = new List<Task>();

        foreach (ClientConnection client in snapshot)
        {
            if (client == excludedClient)
                continue;

            if (client.socket == null || client.socket.State != WebSocketState.Open)
                continue;

            var map = CacheManager.Instance.GetMap(mapID);
            if (map == null || map.mobsData == null)
                continue;

            var idAccount = GetIDAccount(client);
            var accountData = CacheManager.Instance.GetAccountData(idAccount);

            if (accountData.playerData.idMap != mapID)
                continue;

            tasks.Add(SendPacketToClient(client, data));
        }

        await Task.WhenAll(tasks);
    }
    public async Task SendPacketToClient(ClientConnection targetClient, byte[] data)
    {
        if (targetClient == null)
            return;

        if (targetClient.socket.State != WebSocketState.Open)
            return;

        if (data == null)
            return;

        SemaphoreSlim sendLock;
        lock (clientCollectionLock)
        {
            if (!socketSendLocks.TryGetValue(targetClient, out sendLock))
                return;
        }

        try
        {
            await sendLock.WaitAsync();

            if (targetClient.socket.State != WebSocketState.Open)
                return;

            if (targetClient.socket.State == WebSocketState.Open)
            {
                if (targetClient.socket == null)
                    return;

                if (targetClient.socket.State != WebSocketState.Open)
                {
                    MarkLogOut(targetClient);
                    return;
                }
                await targetClient.socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        catch (ObjectDisposedException)
        {
            MarkLogOut(targetClient);
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine("[SEND WS ERROR] " + ex.Message);
            MarkLogOut(targetClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SEND ERROR] " + ex);
            MarkLogOut(targetClient);
        }
        finally
        {
            try
            {
                sendLock.Release();
            }
            catch { }
        }
    }

    public void MarkLogOutAll()
    {
        List<ClientConnection> snapshot;

        lock (clientCollectionLock)
        {
            snapshot = CreateClientSnapshot();
        }

        foreach (var client in snapshot)
        {
            if (client == null)
                continue;

            lock (clientCollectionLock)
            {
                logoutClients.Add(client);
            }
        }
    }
    public void MarkLogOut(ClientConnection client)
    {
        if (client == null)
            return;

        lock (clientCollectionLock)
        {
            logoutClients.Add(client);
        }
    }

    public List<ClientConnection> GetAllClients()
    {
        lock (clientCollectionLock)
        {
            List<ClientConnection> result = new List<ClientConnection>();

            foreach (var client in connectedClients)
            {
                if (client == null)
                    continue;

                if (logoutClients.Contains(client))
                    continue;

                if (client.socket == null)
                    continue;

                if (client.socket.State != WebSocketState.Open)
                    continue;

                result.Add(client);
            }

            return result;
        }
    }

    public void RemoveDisconnectedClients()
    {
        List<ClientConnection> needCleanup = new List<ClientConnection>();

        lock (clientCollectionLock)
        {
            foreach (var client in connectedClients)
            {
                if (client == null)
                {
                    needCleanup.Add(client);
                    continue;
                }

                // logout chủ động
                if (logoutClients.Contains(client))
                {
                    needCleanup.Add(client);
                    continue;
                }

                // disconnect bất ngờ
                if (client.socket.State != WebSocketState.Open)
                {
                    needCleanup.Add(client);
                }
            }
        }

        foreach (var client in needCleanup)
        {
            int idAccount = GetIDAccount(client);
            string nameAccount = CacheManager.Instance.GetAllAccountData().TryGetValue(idAccount, out var value) ? value.account.NameChar : null;

            if (idAccount != 0)
            {
                CacheManager.Instance.RemoveAccountData(idAccount);

                time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Goodbye {nameAccount}.");
            }

            if (client.socket.State == WebSocketState.Open || client.socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    client.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Logout", CancellationToken.None).Wait();
                }
                catch { }
            }

            UnregisterClient(client);
            client.socket.Dispose();

            time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            Console.WriteLine($"[Server] {time.ToString("hh:mm:ss tt")} Closed socket: {client.ipRemote}.");

            lock (clientCollectionLock)
            {
                logoutClients.Remove(client);
            }
        }
    }
}