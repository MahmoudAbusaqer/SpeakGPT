using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroMusic : MonoBehaviour
{
    
    public AudioSource introMusic;
    
    // Start is called before the first frame update
    void Start()
    {
        introMusic = GetComponent<AudioSource>();
        if (introMusic == null)
        {
            introMusic = gameObject.AddComponent<AudioSource>();
        }
        introMusic.Play();    
    }

    public void StopMusic()
    {
        introMusic.Stop();
    }
}
