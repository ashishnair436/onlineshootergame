using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class LeaderboardPlayer : MonoBehaviour
{
    public TMP_Text playerNameText, killsText, deathsText;

    public void SetDetails(string name, int kills, int deaths)
    {
        playerNameText.text = name;
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString(); 
    }
}
