using UnityEngine;
using System.Collections;

public class NPC_David : NPCBase
{
    [Header("David Mechanics")]
    public NavigationNode waterDispenserNode;

    [Header("Jump Scare")]
    public GameObject jumpScareOverlay;
    public AudioClip  jumpScareClip;
    public float      jumpScareDuration = 4f;

    protected override void Start()
    {
        npcName        = "David";
        activateHour   = 27f;
        deactivateHour = 29f;
        distractItem   = ItemType.CoffeeCup;
        detectionRange = 2;
        stepInterval   = 3.5f;

        if (waterDispenserNode != null)
        {
            patrolNodes.Clear();
            patrolNodes.Add(waterDispenserNode);
            foreach (var n in waterDispenserNode.GetNeighbours())
                patrolNodes.Add(n);
        }

        base.Start();
    }

    protected override void OnCatchPlayer(PlayerMovement player)
    {
        base.OnCatchPlayer(player);
        StartCoroutine(PlayJumpScare());
    }

    private IEnumerator PlayJumpScare()
    {
        if (jumpScareOverlay) jumpScareOverlay.SetActive(true);
        PlaySound(jumpScareClip);
        yield return new WaitForSeconds(jumpScareDuration);
        if (jumpScareOverlay) jumpScareOverlay.SetActive(false);
    }
}