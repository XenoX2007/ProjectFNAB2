using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Starting Position")]
    public NavigationNode startNode;

    [Header("Movement")]
    public float moveDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Peek")]
    public float peekAngleMax = 35f;
    public float peekSpeed    = 120f;

    [Header("Audio")]
    public AudioClip footstepClip;
    public AudioClip doorOpenClip;
    public AudioClip lockedDoorClip;

    [Header("UI References")]
    public DirectionArrowUI arrowUI;
    public GameObject hidePrompt;

    // Runtime
    public NavigationNode CurrentNode { get; private set; }
    public bool IsMoving  { get; private set; }
    public bool IsHidden  { get; private set; }
    public bool HasCard   { get; private set; }

    private AudioSource _audio;
    private Camera      _cam;
    private float       _peekYaw  = 0f;
    private float       _facingY  = 0f; // current base Y rotation
    private bool _lookBehind = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio   = GetComponent<AudioSource>();
        _cam     = Camera.main;
    }

    private void Start()
    {
        if (startNode != null)
            TeleportToNode(startNode);
    }

    private void Update()
    {
        if (!IsMoving)
        {
            HandlePeek();
            HandleCursorMovement();

            if (Input.GetKeyDown(KeyCode.Space))
            ToggleLookBehind();
        }

        if (Input.GetKeyDown(KeyCode.E) && CurrentNode != null && CurrentNode.canHide)
            ToggleHide();
    }

    // ── Look Behind ──────────────────────────────────────────────────────

    private void ToggleLookBehind()
{
    _lookBehind = !_lookBehind;

    float targetY = _facingY;

    if (_lookBehind)
        targetY += 180f;

    _cam.transform.rotation =
        Quaternion.Euler(0f, targetY, 0f);
}

    // ── Peek ──────────────────────────────────────────────────────
    private void HandlePeek()
    {
        float mouseX = Input.GetAxis("Mouse X");
        _peekYaw = Mathf.Clamp(
            _peekYaw + mouseX * peekSpeed * Time.deltaTime,
            -peekAngleMax, peekAngleMax);
        float baseYaw = _facingY;

if (_lookBehind)
    baseYaw += 180f;

_cam.transform.rotation =
    Quaternion.Euler(
        0f,
        baseYaw + _peekYaw,
        0f);
    }

    // ── Cursor click movement ─────────────────────────────────────
    private void HandleCursorMovement()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        var dir = CustomCursorSystem.Instance?.GetCurrentDirection();
        if (dir == null) return;

        switch (dir)
        {
            case CustomCursorSystem.Direction.North: MoveNorth(); break;
            case CustomCursorSystem.Direction.South: MoveSouth(); break;
            case CustomCursorSystem.Direction.East:  MoveEast();  break;
            case CustomCursorSystem.Direction.West:  MoveWest();  break;
        }
    }

    // ── Movement ──────────────────────────────────────────────────
    public void MoveNorth() => TryMove(CurrentNode?.nodeNorth);
    public void MoveSouth() => TryMove(CurrentNode?.nodeSouth);
    public void MoveEast()  => TryMove(CurrentNode?.nodeEast);
    public void MoveWest()  => TryMove(CurrentNode?.nodeWest);

    private void TryMove(NavigationNode target)
    {
        if (target == null || IsMoving) return;

        if (target.IsLocked && !HasCard)
        {
            PlaySound(lockedDoorClip);
            Debug.Log("[Player] Door is locked — need Teacher Card.");
            return;
        }

        StartCoroutine(MoveToNode(target));
    }

    private IEnumerator MoveToNode(NavigationNode target)
    {
        IsMoving = true;
        IsHidden = false;

        PlaySound(footstepClip);
        if (target.parentRoom != null && NeedsDoorOpen(CurrentNode, target))
            PlaySound(doorOpenClip);

        Vector3 fromPos    = _cam.transform.position;
        Vector3 toPos      = target.transform.position;

        // Camera faces the Z forward direction of the TARGET node
        float   toFacingY  =  target.transform.eulerAngles.y;
        float   fromFacingY = _facingY;

        _peekYaw = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            float ease = moveCurve.Evaluate(Mathf.Clamp01(t));

            // Smoothly move position
            _cam.transform.position = Vector3.Lerp(fromPos, toPos, ease);

            // Smoothly rotate to face node's Z direction
            float currentFacing = Mathf.LerpAngle(fromFacingY, toFacingY, ease);
            _cam.transform.rotation = Quaternion.Euler(0f, currentFacing, 0f);

            yield return null;
        }

        _cam.transform.position = toPos;
        _cam.transform.rotation = Quaternion.Euler(0f, toFacingY, 0f);

        _facingY     = toFacingY;
        _peekYaw     = 0f;
        _lookBehind  = false;

        CurrentNode  = target;
        IsMoving     = false;

        arrowUI?.Refresh(CurrentNode, HasCard);
        hidePrompt?.SetActive(CurrentNode.canHide);
        GameEvents.OnPlayerMoved?.Invoke(CurrentNode);
    }

    // ── Teleport ──────────────────────────────────────────────────
    private void TeleportToNode(NavigationNode node)
    {
        CurrentNode             = node;
        _facingY                = node.transform.eulerAngles.y;
        _peekYaw                = 0f;
        _lookBehind = false;
        _cam.transform.position = node.transform.position;
        _cam.transform.rotation = Quaternion.Euler(0f, _facingY, 0f);

        arrowUI?.Refresh(CurrentNode, HasCard);
        hidePrompt?.SetActive(node.canHide);
    }

    // ── Other ─────────────────────────────────────────────────────
    public void PickUpCard()
    {
        HasCard = true;
        foreach (var node in FindObjectsOfType<NavigationNode>())
            if (node.requiresCard) node.Unlock();
        Debug.Log("[Player] Teacher card collected — all rooms unlocked.");
    }

    public void ToggleHide()
    {
        if (!CurrentNode.canHide) return;
        IsHidden = !IsHidden;
        hidePrompt?.SetActive(CurrentNode.canHide && !IsHidden);
    }

    public void ForceUnhide()
    {
        IsHidden = false;
        hidePrompt?.SetActive(CurrentNode != null && CurrentNode.canHide);
    }

    private bool NeedsDoorOpen(NavigationNode from, NavigationNode to)
        => from?.parentRoom != to?.parentRoom;

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) _audio.PlayOneShot(clip);
    }
}
