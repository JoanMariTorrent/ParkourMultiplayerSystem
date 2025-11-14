using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class PruebasRPC : NetworkBehaviour
{
    public List<Player> players = new();

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            ChangeColor(Color.red);
        if(Input.GetKeyDown(KeyCode.Alpha2))
            ChangeColor(Color.green);
        if(Input.GetKeyDown(KeyCode.Alpha3))
            ChangeColor(Color.blue);
        if(Input.GetKeyDown(KeyCode.Alpha4))
            ChangeColor(Color.black);
        if(Input.GetKeyDown(KeyCode.Alpha5))
            ChangeColor(Color.grey);
    }


    [ServerRpc]
    private void ChangeColor(Color color, RPCInfo info = default)
    {
        Debug.Log("ServerInput");
        //ObserversColor(color);
        Debug.Log($"<color=green> ID del sender: {info.sender} </color>");
    
        foreach(var player in players)
        {
            TargetColor(player.owner.Value, color);
        }
    }

    [ObserversRpc]
    private void ObserversColor(Color color)
    {
        GetComponent<Renderer>().sharedMaterial.color = color;
    }

    [TargetRpc]
    private void TargetColor(PlayerID target, Color color)
    {
        GetComponent<Renderer>().sharedMaterial.color = color;
    }
}
