using FishNet.Managing;
using System.Collections;
using UnityEngine;

public class PingDisplay : MonoBehaviour
{
    private long _ping;

    private NetworkManager _manager;

    private Coroutine _pingLoop;

    void Start()
    {
        _manager = GetComponent<NetworkManager>();
        _pingLoop = StartCoroutine("PingLoop");
    }

    private IEnumerator PingLoop()
    {
        while (true)
        {
            _ping = _manager.TimeManager.RoundTripTime;
            Debug.Log(_ping);
            yield return new WaitForSeconds(1f);
        }
    }
}
