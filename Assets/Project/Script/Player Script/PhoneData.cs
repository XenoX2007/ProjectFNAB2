using UnityEngine;

public class PhoneData : MonoBehaviour
{
    [Header("Phone Battery")]
    public float _currentBattery = 100f;
    public float _maxBattery = 100f;
    [Header("Phone FlashLight")]
    public bool _flashLightOn;
    [Header("Phone Camera")]
    public int _currentCameraIndex;

}
