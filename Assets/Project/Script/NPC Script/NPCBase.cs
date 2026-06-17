using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for all NPCs (QuocAnh, ChauBui, BB, QuyNguyen, David).
/// Each NPC moves node-to-node using BFS, detects the player by distance,
/// and can be distracted / stunned with the correct item.
/// </summary>
public abstract class NPCBase : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("Identity")]
    public string npcName = "NPC";

    [Header("Starting & Patrol")]
    public NavigationNode startNode;
    public List<NavigationNode> patrolNodes = new List<NavigationNode>();

    [Header("Timing")]
    [Tooltip("Game-hour at which this NPC becomes active")]
    public float activateHour = 23f;
    [Tooltip("Game-hour at which this NPC deactivates (0 = never)")]
    public float deactivateHour = 0f;

    [Header("Detection")]
    [Tooltip("BFS node distance at which NPC 'sees' the player")]
    public int detectionRange = 2;
    [Tooltip("If player is hidden, NPC needs to be this close to still detect")]
    public int hiddenDetectionRange = 0;

    [Header("Movement Speed")]
    [Tooltip("Seconds between each node step")]
    public float stepInterval = 2.5f;

    [Header("Distract Item")]
    public ItemType distractItem;
    [Tooltip("Seconds the NPC is stunned after being distracted")]
    public float stunDuration = 10f;

    [Header("Audio")]
    public AudioClip footstepClip;
    public AudioClip detectedClip;
    public AudioClip distractedClip;

    // ── Runtime ───────────────────────────────────────────────────
    public  NavigationNode CurrentNode  { get; private set; }
    public  bool           IsActive     { get; protected set; }
    public  bool           IsStunned    { get; private set; }
    protected bool         IsChasing    { get; private set; }

    protected AudioSource _audio;
    private   float       _stepTimer;
    private   int         _patrolIndex = 0;

    // ── Lifecycle ─────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
    }

    protected virtual void Start()
    {
        if (startNode != null)
        {
            CurrentNode = startNode;
            transform.position = startNode.transform.position;
        }

        // Subscribe to time milestones
        GameEvents.OnHourChanged += OnHourChanged;
        gameObject.SetActive(false); // start inactive
    }

    protected virtual void OnDestroy()
    {
        GameEvents.OnHourChanged -= OnHourChanged;
    }

    protected virtual void Update()
    {
        if (!IsActive || IsStunned) return;

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= stepInterval)
        {
            _stepTimer = 0f;
            TakeStep();
        }
    }

    // ── Core step logic ───────────────────────────────────────────

    private void TakeStep()
    {
        var player = PlayerMovement.Instance;
        if (player == null || CurrentNode == null) return;

        int dist = NavigationNode.Distance(CurrentNode, player.CurrentNode);

        // Detection check
        bool detected = CheckDetection(dist, player.IsHidden);

        if (detected)//
        {
            // Chase the player
            IsChasing = true;
            ChasePlayer(player);
        }
        else
        {
            IsChasing = false;
            Patrol();
        }

        // Proximity audio cue
        if (dist <= 3)
            PlaySound(footstepClip);
    }

    protected virtual bool CheckDetection(int dist, bool playerHidden)
    {
        int range = playerHidden ? hiddenDetectionRange : detectionRange;
        return dist >= 0 && dist <= range;
    }

    private void ChasePlayer(PlayerMovement player)
    {
        var path = NavigationNode.FindPath(CurrentNode, player.CurrentNode, ignoreCards: true);
        if (path == null || path.Count == 0)
        {
            // At the same node
            if (CurrentNode == player.CurrentNode && !player.IsHidden)
                OnCatchPlayer(player);
            return;
        }

        MoveToNode(path[0]);

        // If we just stepped onto the player's node
        if (path[0] == player.CurrentNode && !player.IsHidden)
            OnCatchPlayer(player);
    }

    private void Patrol()
    {
        if (patrolNodes.Count == 0) return;
        var target = patrolNodes[_patrolIndex % patrolNodes.Count];
        var path   = NavigationNode.FindPath(CurrentNode, target, ignoreCards: true);
        if (path != null && path.Count > 0)
            MoveToNode(path[0]);
        else
            _patrolIndex++; // skip unreachable node
    }

    protected void MoveToNode(NavigationNode node)
    {
        CurrentNode        = node;
        transform.position = node.transform.position;
    }

    // ── Distract / stun ───────────────────────────────────────────

    /// <summary>Called by ItemUseSystem when the player uses an item near an NPC.</summary>
    public bool TryDistract(ItemType itemUsed)
    {
        if (itemUsed != distractItem)
        {
            Debug.Log($"[NPC:{npcName}] Wrong item! This NPC needs {distractItem}.");
            return false;
        }

        StartCoroutine(StunRoutine());
        PlaySound(distractedClip);
        return true;
    }

    private IEnumerator StunRoutine()
    {
        IsStunned = true;
        IsChasing = false;
        Debug.Log($"[NPC:{npcName}] Stunned for {stunDuration}s!");
        OnStunned();
        yield return new WaitForSeconds(stunDuration);
        IsStunned = false;
        OnStunEnded();
        Debug.Log($"[NPC:{npcName}] Stun ended — resuming.");
    }

    // ── Events (override in subclasses) ───────────────────────────

    /// <summary>NPC reached the player without being distracted.</summary>
    protected virtual void OnCatchPlayer(PlayerMovement player)
    {
        PlaySound(detectedClip);
        GameEvents.OnPlayerCaught?.Invoke(npcName);
        Debug.Log($"[NPC:{npcName}] Caught the player!");
    }

    protected virtual void OnStunned()  { }
    protected virtual void OnStunEnded(){ }

    // ── Activation ────────────────────────────────────────────────

    private void OnHourChanged(float hour)
    {
        if (!IsActive && hour >= activateHour)
        {
            Activate();
        }
        if (IsActive && deactivateHour > 0f && hour >= deactivateHour)
        {
            Deactivate();
        }
    }

    public virtual void Activate()
    {
        IsActive = true;
        gameObject.SetActive(true);
        Debug.Log($"[NPC:{npcName}] Activated at hour {GameTimeManager.Instance?.GameHour:F1}");
    }

    public virtual void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────
    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && _audio != null)
            _audio.PlayOneShot(clip);
    }
}