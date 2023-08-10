using UnityEngine;
using UnityEngine.Rendering;

public class FrameRateManager : MonoBehaviour
{
    // private int lastRequestedFrame = 0;
    //
    // private void Awake()
    // {
    //     Application.targetFrameRate = 60;
    //     RequestFullFrameRate();
    // }
    //
    // public void RequestFullFrameRate()
    // {
    //     lastRequestedFrame = Time.frameCount;
    // }
    //
    // private const int BUFFER_FRAMES = 2;
    // private const int LOW_POWER_FRAME_INTERVAL = 60;
    //
    // private void Update()
    // {
    //     OnDemandRendering.renderFrameInterval =
    //         (Time.frameCount - lastRequestedFrame) < BUFFER_FRAMES ? 1 : LOW_POWER_FRAME_INTERVAL;
    // }
}