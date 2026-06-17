using UnityEngine;
using UnityEngine.UI;

public class CustomCursorSystem : MonoBehaviour
{
    public static CustomCursorSystem Instance { get; private set; }

    [Header("Cursor Image")]
    public RectTransform cursorRect;
    public Image         cursorImage;

    [Header("Cursor Sprites")]
    public Sprite handSprite;      // DEFAULT — always shown
    public Sprite arrowSprite;     // shown when there is a valid exit
    public Sprite lockedSprite;    // shown when exit needs card

    [Header("Colors")]
    public Color handColor   = Color.white;
    public Color arrowColor  = Color.white;
    public Color lockedColor = new Color(1f, 0.3f, 0.3f);

    [Header("Settings")]
    public float rotationSpeed   = 720f;
    public float raycastDistance = 8f;
    public float deadZone        = 80f; // centre pixels with no direction

    // Runtime
    private float     _targetAngle  = 0f;
    private float     _currentAngle = 0f;
    private Direction _currentDir   = Direction.None;

    public enum Direction { None, North, South, East, West }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnDestroy()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        MoveCursorToMouse();
        _currentDir = GetScreenZoneDirection();
        ApplyVisuals();
        SmoothRotate();
    }

    // ── Move cursor image to mouse position ───────────────────────
    private void MoveCursorToMouse()
    {
        if (cursorRect != null)
            cursorRect.position = Input.mousePosition;
    }

    // ── Decide which direction the mouse zone points ──────────────
    private Direction GetScreenZoneDirection()
    {
        Vector2 mouse       = Input.mousePosition;
        float   W           = Screen.width;
        float   H           = Screen.height;
        float   fromCentreX = mouse.x - W * 0.5f;
        float   fromCentreY = mouse.y - H * 0.5f;

        // Dead zone in centre — no direction
        if (Mathf.Abs(fromCentreX) < deadZone && Mathf.Abs(fromCentreY) < deadZone)
            return Direction.None;

        if (Mathf.Abs(fromCentreX) >= Mathf.Abs(fromCentreY))
            return fromCentreX > 0 ? Direction.East : Direction.West;
        else
            return fromCentreY > 0 ? Direction.North : Direction.South;
    }

    // ── Apply correct sprite ──────────────────────────────────────
    private void ApplyVisuals()
    {
        if (cursorImage == null) return;

        // Get player current node
        var playerNode = PlayerMovement.Instance?.CurrentNode;
        bool hasCard   = PlayerMovement.Instance?.HasCard ?? false;

        // Check if there is a valid exit in the current direction
        NavigationNode targetNode = GetNodeInDirection(playerNode, _currentDir);

        // No direction or no exit → show hand (default)
        if (_currentDir == Direction.None || targetNode == null)
        {
            ShowHand();
            _targetAngle = 0f;
            return;
        }

        // Exit exists but locked → show locked sprite
        if (targetNode.IsLocked && !hasCard)
        {
            cursorImage.sprite = lockedSprite ?? arrowSprite;
            cursorImage.color  = lockedColor;
            _targetAngle       = DirectionToAngle(_currentDir);
            return;
        }

        // Valid exit → show arrow pointing that way
        cursorImage.sprite = arrowSprite;
        cursorImage.color  = arrowColor;
        _targetAngle       = DirectionToAngle(_currentDir);
    }

    // ── Show hand cursor (default state) ─────────────────────────
    private void ShowHand()
    {
        cursorImage.sprite = handSprite;
        cursorImage.color  = handColor;
        _targetAngle       = 0f;
    }

    // ── Smooth rotation ───────────────────────────────────────────
    private void SmoothRotate()
    {
        // Don't rotate when showing hand
        if (cursorImage.sprite == handSprite)
        {
            _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, 0f, rotationSpeed * Time.deltaTime);
        }
        else
        {
            _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, _targetAngle, rotationSpeed * Time.deltaTime);
        }

        if (cursorRect != null)
            cursorRect.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private NavigationNode GetNodeInDirection(NavigationNode node, Direction dir)
    {
        if (node == null) return null;
        return dir switch
        {
            Direction.North => node.nodeNorth,
            Direction.South => node.nodeSouth,
            Direction.East  => node.nodeEast,
            Direction.West  => node.nodeWest,
            _               => null
        };
    }

    private float DirectionToAngle(Direction dir)
    {
        return dir switch
        {
            Direction.North => 0f,
            Direction.East  => -90f,
            Direction.South => 180f,
            Direction.West  => 90f,
            _               => 0f
        };
    }

    // ── Public API ────────────────────────────────────────────────
    public Direction GetCurrentDirection()
    {
        // Only return a direction if there is actually a valid exit
        var playerNode = PlayerMovement.Instance?.CurrentNode;
        var targetNode = GetNodeInDirection(playerNode, _currentDir);
        return targetNode != null ? _currentDir : Direction.None;
    }
}