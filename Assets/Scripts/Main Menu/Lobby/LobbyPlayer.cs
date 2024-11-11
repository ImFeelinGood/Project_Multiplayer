using System.Collections;
using System.Collections.Generic;
using game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private Image isReadyImage;
    private LobbyPlayerData data;

    public void SetData(LobbyPlayerData _data)
    {
        data = _data;
        playerName.text = data.Gamertag;

        if(data.IsReady)
        isReadyImage.color = Color.green;
        else
        isReadyImage.color = Color.white;

    }
}
