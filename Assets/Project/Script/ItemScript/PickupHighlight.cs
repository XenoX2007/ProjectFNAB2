// Create this as PickupHighlight.cs in ItemScript folder
using UnityEngine;

public class PickupHighlight : MonoBehaviour
{
    [Header("Highlight")]
    public Renderer itemRenderer;
    public Color    normalColor    = Color.white;
    public Color    highlightColor = new Color(1f, 0.9f, 0.3f);
    public float    highlightRange = 2f;

    private bool _isHighlighted = false;

    private void Update()
    {
        var playerNode = PlayerMovement.Instance?.CurrentNode;
        var myNode     = GetComponentInParent<NavigationNode>();

        if (playerNode == null || myNode == null) return;

        int dist = NavigationNode.Distance(playerNode, myNode);
        bool shouldHighlight = dist >= 0 && dist <= highlightRange;

        if (shouldHighlight != _isHighlighted)
        {
            _isHighlighted = shouldHighlight;
            if (itemRenderer != null)
                itemRenderer.material.color = 
                    _isHighlighted ? highlightColor : normalColor;
        }
    }
}