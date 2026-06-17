using UnityEngine;
using System;

/// <summary>
/// Central event hub — decouples all systems.
/// Any system can fire events here without direct references.
/// </summary>
public static class GameEvents
{
    // Player
    public static Action<NavigationNode> OnPlayerMoved;
    public static Action<string>         OnPlayerCaught;   // string = NPC name

    // Time
    public static Action<float>          OnHourChanged;    // float = current game hour

    // Power
    public static Action                 OnElectricityOff;
    public static Action                 OnElectricityOn;

    // Deadline
    public static Action<float>          OnDeadlineProgress; // 0-100

    // Inventory
    public static Action<ItemType>       OnItemPickedUp;
    public static Action<ItemType>       OnItemUsed;

    // NPC
    public static Action<string>         OnNPCActivated;
    public static Action<string, float>  OnNPCStunned;     // name, duration
}

// ════════════════════════════════════════════════════════════════
// ROOM DATA (ScriptableObject)
// ════════════════════════════════════════════════════════════════
[CreateAssetMenu(fileName = "RoomData", menuName = "FNAB/Room Data")]
public class RoomData : ScriptableObject
{
    public string   roomName;
    public NodeType roomType;
    [TextArea]
    public string   description;
    public bool     requiresCard;
    public Sprite   mapIcon;
}

// ════════════════════════════════════════════════════════════════
// GAME TIME EVENT BROADCASTER
// Bridges Unity's Update loop to the static GameEvents.OnHourChanged
// ════════════════════════════════════════════════════════════════
public class TimeEventBroadcaster : MonoBehaviour
{
    private float _lastBroadcast = -1f;

    private void Update()
    {
        var t = GameTimeManager.Instance;
        if (t == null) return;

        // Broadcast every 0.1 game-hour change to keep NPCs responsive
        if (Mathf.Abs(t.GameHour - _lastBroadcast) >= 0.1f)
        {
            _lastBroadcast = t.GameHour;
            GameEvents.OnHourChanged?.Invoke(t.GameHour);
        }
    }
}

// ════════════════════════════════════════════════════════════════
// DANGER INDICATOR (ambient audio / UI)
// ════════════════════════════════════════════════════════════════
/// <summary>
/// Shows a red menu button indicator (like At Dead of Night) when an NPC is very close.
/// Also manages ambient tension audio.
/// </summary>
public class DangerIndicator : MonoBehaviour
{
    [Header("UI")]
    public UnityEngine.UI.Image menuButtonImage;
    public Color safeColor   = Color.white;
    public Color dangerColor = Color.red;

    [Header("Audio")]
    public AudioSource heartbeatSource;
    public AudioClip   nearbyNPCClip;
    [Range(0f, 10f)]
    public float checkInterval = 0.5f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;

        int minDist = GetClosestNPCDistance();
        bool isDanger = minDist >= 0 && minDist <= 1;

        if (menuButtonImage)
            menuButtonImage.color = isDanger ? dangerColor : safeColor;

        if (heartbeatSource)
        {
            heartbeatSource.volume = minDist <= 3 ? Mathf.Lerp(1f, 0f, minDist / 4f) : 0f;
        }
    }

    private int GetClosestNPCDistance()
    {
        var playerNode = PlayerMovement.Instance?.CurrentNode;
        if (playerNode == null) return -1;

        int min = int.MaxValue;
        foreach (var npc in FindObjectsOfType<NPCBase>())
        {
            if (!npc.IsActive) continue;
            int d = NavigationNode.Distance(playerNode, npc.CurrentNode);
            if (d >= 0 && d < min) min = d;
        }
        return min == int.MaxValue ? -1 : min;
    }
}