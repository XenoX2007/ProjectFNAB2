using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// ════════════════════════════════════════════════════════════════
// SECURITY CAMERA
// ════════════════════════════════════════════════════════════════
public class SecurityCamera : MonoBehaviour
{
    [Header("Identity")]
    public string cameraLabel = "Cam 1";
    public int    cameraIndex = 1;

    [Header("Feed")]
    public RenderTexture feedTexture;
    public NavigationNode watchNode;

    [Header("Audio")]
    public AudioClip staticClip;

    private Camera      _cam;
    private AudioSource _audio;

    private void Awake()
    {
        _cam   = GetComponent<Camera>();
        _audio = GetComponent<AudioSource>();
        if (_cam && feedTexture)
            _cam.targetTexture = feedTexture;
        if (_cam) _cam.enabled = false;
    }

    public void Activate()   { if (_cam) _cam.enabled = true;  }
    public void Deactivate() { if (_cam) _cam.enabled = false; }

    public bool HasNPCInView()
    {
        if (watchNode == null) return false;
        foreach (var npc in FindObjectsOfType<NPCBase>())
            if (npc.IsActive && npc.CurrentNode == watchNode) return true;
        return false;
    }
}

// ════════════════════════════════════════════════════════════════
// CAMERA SYSTEM
// ════════════════════════════════════════════════════════════════
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [Header("Cameras")]
    public SecurityCamera[] cameras;

    [Header("UI")]
    public GameObject cameraPanel;
    public RawImage   feedDisplay;
    public Text       cameraLabel;
    public Button[]   camButtons;

    [Header("Static")]
    public Image staticOverlay;

    private int  _activeCamIndex = 0;
    private bool _isOpen         = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < camButtons.Length && i < cameras.Length; i++)
        {
            int idx = i;
            camButtons[i].onClick.AddListener(() => SwitchToCamera(idx));
        }
    }

    public void ToggleCameras()
    {
        _isOpen = !_isOpen;
        cameraPanel?.SetActive(_isOpen);
        if (_isOpen) SwitchToCamera(_activeCamIndex);
        else cameras[_activeCamIndex]?.Deactivate();
    }

    public void SwitchToCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return;
        cameras[_activeCamIndex]?.Deactivate();
        _activeCamIndex = index;
        var cam = cameras[_activeCamIndex];
        cam.Activate();
        if (feedDisplay && cam.feedTexture) feedDisplay.texture = cam.feedTexture;
        if (cameraLabel) cameraLabel.text = cam.cameraLabel;
        bool hasNPC = cam.HasNPCInView();
        if (staticOverlay)
            staticOverlay.color = new Color(1, 1, 1, hasNPC ? Random.Range(0f, 0.4f) : 0f);
    }
}

// ════════════════════════════════════════════════════════════════
// ELECTRICAL ROOM SYSTEM
// ════════════════════════════════════════════════════════════════
public class ElectricalRoomSystem : MonoBehaviour
{
    public static ElectricalRoomSystem Instance { get; private set; }

    [Header("References")]
    public Light[]    hallwayLights;
    public Light      emergencyLight;
    public GameObject powerSwitchPrompt;

    [Header("Audio")]
    public AudioClip powerOffClip;
    public AudioClip powerOnClip;

    public bool PowerIsOn { get; private set; } = true;

    private AudioSource _audio;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio   = GetComponent<AudioSource>();
    }

    public void TurnOffPower()
    {
        PowerIsOn = false;
        foreach (var l in hallwayLights) if (l) l.enabled = false;
        if (emergencyLight) emergencyLight.enabled = true;
        _audio?.PlayOneShot(powerOffClip);
        Debug.Log("[Electrical] Power OFF!");
    }

    public void TurnOnPower()
    {
        PowerIsOn = true;
        foreach (var l in hallwayLights) if (l) l.enabled = true;
        if (emergencyLight) emergencyLight.enabled = false;
        _audio?.PlayOneShot(powerOnClip);
        FindObjectOfType<NPC_ChauBui>()?.OnPowerRestored();
        Debug.Log("[Electrical] Power ON!");
    }
}

// ════════════════════════════════════════════════════════════════
// INTERACTABLE BASE
// ════════════════════════════════════════════════════════════════
public abstract class Interactable : MonoBehaviour
{
    public string promptText   = "Click to interact";
    public int    interactRange = 1;

    public abstract void OnInteract();

    protected bool PlayerIsInRange()
    {
        var playerNode = PlayerMovement.Instance?.CurrentNode;
        if (playerNode == null) return false;
        var node = GetComponentInParent<NavigationNode>();
        if (node == null) node = GetComponent<NavigationNode>();
        if (node == null) return true;
        int dist = NavigationNode.Distance(playerNode, node);
        return dist >= 0 && dist <= interactRange;
    }

    private void OnMouseDown()
    {
        if (PlayerIsInRange()) OnInteract();
    }
}

// ════════════════════════════════════════════════════════════════
// PC WORKSTATION
// ════════════════════════════════════════════════════════════════
public class PCWorkstation : Interactable
{
    private bool _isWorking = false;

    public override void OnInteract()
    {
        _isWorking = !_isWorking;
        if (_isWorking) DeadlineSystem.Instance?.StartWorking();
        else            DeadlineSystem.Instance?.StopWorking();
        Debug.Log(_isWorking ? "[PC] Started working." : "[PC] Stopped working.");
    }
}

// ════════════════════════════════════════════════════════════════
// POWER SWITCH
// ════════════════════════════════════════════════════════════════
public class PowerSwitch : Interactable
{
    public override void OnInteract()
    {
        var sys = ElectricalRoomSystem.Instance;
        if (sys != null && !sys.PowerIsOn)
            sys.TurnOnPower();
    }
}

// ════════════════════════════════════════════════════════════════
// PLAYER INPUT BLOCKER
// ════════════════════════════════════════════════════════════════
public class PlayerInputBlocker : MonoBehaviour
{
    public bool IsBlocked { get; private set; }

    public void Block(float duration) => StartCoroutine(BlockRoutine(duration));

    private System.Collections.IEnumerator BlockRoutine(float d)
    {
        IsBlocked = true;
        yield return new WaitForSeconds(d);
        IsBlocked = false;
    }
}

// ════════════════════════════════════════════════════════════════
// WATER FILLING STATION
// ════════════════════════════════════════════════════════════════
public class WaterFilling : Interactable
{
    public float refillAmount = 100f;

    public override void OnInteract()
    {
        ThirstSystem.Instance?.Drink(refillAmount);
        Debug.Log("[Water] Thirst refilled!");
    }
}