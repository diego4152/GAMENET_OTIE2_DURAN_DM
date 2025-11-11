using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerReadyState : MonoBehaviour
{
    private L2D_RoomPlayer roomPlayer;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI readyText;

    public L2D_RoomPlayer RoomPlayer => roomPlayer;

    private void Start()
    {
        SetReady(false);
    }

    public void SetRoomPlayer(L2D_RoomPlayer _roomPlayer)
    {
        roomPlayer = _roomPlayer;
        roomPlayer.OnChangeReady += SetReady;
        roomPlayer.OnChangeName += SetName;
        roomPlayer.OnDisconnect += DestroyState;
    }

    public void DestroyState()
    {
        Destroy(this);
    }

    public void SetName(string playerName)
    {
        nameText.text = playerName;

        this.gameObject.name = playerName;
    }

    public void SetReady(bool isReady)
    {
        readyText.text = isReady ? "Ready" : "Not Ready";
        readyText.color = isReady ? Color.green : Color.red;
    }
}