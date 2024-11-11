using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private bool _isHost = false;

    private string joinCode;
    private string ip;
    
    private int port;

    private byte[] key;
    private byte[] connectionData;
    private byte[] hostConnectionData;
    private System.Guid allocationId;
    private byte[] allocationIdByte;
    public bool isHost {get {return _isHost;}}
    public static RelayManager instance;

    void Awake()
    {
        instance = this;
    }

    public string GetAllocationId()
    {
        return allocationId.ToString();
    }

    public string GetConnectionData()
    {
        return connectionData.ToString();
    }

    public async Task<string> CreateRelay(int maxConnection)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnection);
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        RelayServerEndpoint dtlsEnpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
        ip = dtlsEnpoint.Host;
        port = dtlsEnpoint.Port;

        allocationId = allocation.AllocationId;
        allocationIdByte = allocation.AllocationIdBytes;
        connectionData = allocation.ConnectionData;
        key = allocation.Key;

        _isHost = true;
        return joinCode;
    }
    
    public async Task<bool> JoinRelay(string _joinCode)
    {
        joinCode = _joinCode;
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);


        RelayServerEndpoint dtlsEnpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
        ip = dtlsEnpoint.Host;
        port = dtlsEnpoint.Port;

        allocationId = allocation.AllocationId;
        allocationIdByte = allocation.AllocationIdBytes;
        connectionData = allocation.ConnectionData;
        hostConnectionData = allocation.HostConnectionData;
        key = allocation.Key;

        return true;
    }

     public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, string _dtlsAddress, int _dtlsPort) GetHostConnectionInfo()
    {
        return (allocationIdByte, key, connectionData, ip, port);
    }
        
    public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, byte[] HostConnectionData, string _dtlsAddress, int _dtlsPort) GetClientConnectionInfo()
    {
        return (allocationIdByte, key, connectionData, hostConnectionData, ip, port);
    }

}
