using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    public float rotationSpeed = 1f;
    
    public Material skybox0;
    public Material skybox1;
    public Material skybox2;
    public Material skybox3;
    public Material skybox4;
    public Material skybox5;

    void Update()
    {
        // Rotate the skybox based on the time elapsed since the game started
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
    }

    public void ChangeBackground0()
    {
        RenderSettings.skybox = skybox0;
        DynamicGI.UpdateEnvironment();
    }
    
    public void ChangeBackground1()
    {
        RenderSettings.skybox = skybox1;
        DynamicGI.UpdateEnvironment();
    }
    
    public void ChangeBackground2()
    {
        RenderSettings.skybox = skybox2;
        DynamicGI.UpdateEnvironment();
    }
    
    public void ChangeBackground3()
    {
        RenderSettings.skybox = skybox3;
        DynamicGI.UpdateEnvironment();
    }
    
    public void ChangeBackground4()
    {
        RenderSettings.skybox = skybox4;
        DynamicGI.UpdateEnvironment();
    }
    
    public void ChangeBackground5()
    {
        RenderSettings.skybox = skybox5;
        DynamicGI.UpdateEnvironment();
    }
}