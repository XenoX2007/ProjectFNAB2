using UnityEngine;
using System.Collections;

public class NPC_QuyNguyen : NPCBase
{
    [Header("Water Bottle Gift")]
    public AudioClip arrivalClip;

    protected override void Start()
    {
        npcName      = "QuyNguyen";
        activateHour = 26.5f;
        distractItem = ItemType.None;
        stepInterval = 99f;
        base.Start();
    }

    public override void Activate()
    {
        base.Activate();
        PlaySound(arrivalClip);
        StartCoroutine(GiftWater());
    }

    private IEnumerator GiftWater()
    {
        while (PlayerMovement.Instance?.CurrentNode != CurrentNode)
            yield return new WaitForSeconds(0.5f);

        ThirstSystem.Instance?.Refill();
        Debug.Log("[QuyNguyen] Gave player a water bottle!");

        yield return new WaitForSeconds(2f);
        Deactivate();
    }

    protected override bool CheckDetection(int dist, bool playerHidden) => false;
}