using UnityEngine;
using System.Collections;

public class NPC_BB : NPCBase
{
    [Header("BB Mechanics")]
    public AudioClip  cameraFlashClip;
    public GameObject flashOverlay;
    public float      flashFrequency = 30f;
    public float      flashDuration  = 0.5f;

    private float _flashTimer = 0f;

    protected override void Start()
    {
        npcName               = "BB";
        activateHour          = 19f;
        distractItem          = ItemType.PS5Portal;
        detectionRange        = 1;
        hiddenDetectionRange  = 0;
        stepInterval          = 4f;
        base.Start();
        Activate();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsActive) return;

        _flashTimer += Time.deltaTime;
        if (_flashTimer >= flashFrequency)
        {
            _flashTimer = 0f;
            int dist = NavigationNode.Distance(
                CurrentNode, PlayerMovement.Instance?.CurrentNode);
            if (dist >= 0 && dist <= 2)
                StartCoroutine(FlashWarning());
        }
    }

    protected override void OnStunned()
    {
        StartCoroutine(DespawnAndRespawn());
    }

    private IEnumerator DespawnAndRespawn()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(Random.Range(60f, 120f));
        if (patrolNodes.Count > 0)
            MoveToNode(patrolNodes[Random.Range(0, patrolNodes.Count)]);
        gameObject.SetActive(true);
    }

    private IEnumerator FlashWarning()
    {
        PlaySound(cameraFlashClip);
        if (flashOverlay) flashOverlay.SetActive(true);
        PlayerMovement.Instance?.GetComponent<PlayerInputBlocker>()?.Block(flashDuration);
        yield return new WaitForSeconds(flashDuration);
        if (flashOverlay) flashOverlay.SetActive(false);
    }

    protected override void OnCatchPlayer(PlayerMovement player)
    {
        base.OnCatchPlayer(player);
        StartCoroutine(FlashWarning());
    }
}