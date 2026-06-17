using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows/hides the four directional arrow buttons based on the exits available
/// at the player's current node. Attach to a Canvas child.
/// </summary>
public class DirectionArrowUI : MonoBehaviour
{
    [Header("Arrow Buttons")]
    public Button btnNorth;
    public Button btnSouth;
    public Button btnEast;
    public Button btnWest;

    [Header("Arrow Images (optional tint)")]
    public Image imgNorth;
    public Image imgSouth;
    public Image imgEast;
    public Image imgWest;

    public Color availableColor = Color.white;
    public Color lockedColor    = new Color(1f, 0.8f, 0f); // yellow = needs card

    private void Awake()
    {
        // Wire up buttons to PlayerMovement
        btnNorth?.onClick.AddListener(() => PlayerMovement.Instance.MoveNorth());
        btnSouth?.onClick.AddListener(() => PlayerMovement.Instance.MoveSouth());
        btnEast ?.onClick.AddListener(() => PlayerMovement.Instance.MoveEast());
        btnWest ?.onClick.AddListener(() => PlayerMovement.Instance.MoveWest());
    }

    /// <summary>Called whenever the player moves to a new node.</summary>
    public void Refresh(NavigationNode node, bool hasCard)
    {
        SetArrow(btnNorth, imgNorth, node.nodeNorth, hasCard);
        SetArrow(btnSouth, imgSouth, node.nodeSouth, hasCard);
        SetArrow(btnEast,  imgEast,  node.nodeEast,  hasCard);
        SetArrow(btnWest,  imgWest,  node.nodeWest,  hasCard);
    }

    private void SetArrow(Button btn, Image img, NavigationNode target, bool hasCard)
    {
        if (btn == null) return;
        bool exists = target != null;
        btn.gameObject.SetActive(exists);

        if (!exists) return;

        bool locked = target.IsLocked && !hasCard;
        btn.interactable = true; // always interactable — PlayerMovement handles the lock sound

        if (img != null)
            img.color = locked ? lockedColor : availableColor;
    }
}