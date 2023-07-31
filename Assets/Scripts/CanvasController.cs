using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CanvasController : MonoBehaviour
{
    
    public GameObject settingsScreen;
    
    private void Start()
    {
        settingsScreen.SetActive(false);
    }

    public void showPrefrences()
    {
        settingsScreen.SetActive(settingsScreen.activeSelf != true);
    }
}
