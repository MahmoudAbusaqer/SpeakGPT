using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    public float rotationSpeed = 1f;
        
        void Update()
        {
            // Rotate the skybox based on the time elapsed since the game started
            RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
        }
}
