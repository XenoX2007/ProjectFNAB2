using UnityEngine;
using System.Collections;

public class NPC_QuocAnh : NPCBase
{
    [Header("Jump Scare")]
    public GameObject jumpScareOverlay;
    public AudioClip  jumpScareClip;
    public float      jumpScareDuration = 3f;

    protected override void Start()
    {
        npcName        = "QuocAnh";
        activateHour   = 19f;
        distractItem   = ItemType.BanhMiHuynhHoa;
        detectionRange = 2;
        stepInterval   = 3f;
        base.Start();
        Activate();
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