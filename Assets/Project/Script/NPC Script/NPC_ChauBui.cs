using UnityEngine;
using System.Collections;

public class NPC_ChauBui : NPCBase
{
    [Header("Electricity")]
    public ElectricalRoomSystem electricalRoom;

    [Header("Jump Scare")]
    public GameObject jumpScareOverlay;
    public AudioClip  jumpScareClip;
    public float      jumpScareDuration = 3.5f;

    [Header("Second Activation")]
    public float secondActivateHour = 27f;

    private bool _electricityOff = false;

    protected override void Start()
    {
        npcName        = "ChauBui";
        activateHour   = 25f;
        deactivateHour = 26f;
        distractItem   = ItemType.ChauBuiPhone;
        detectionRange = 1;
        stepInterval   = 2f;
        base.Start();
        GameEvents.OnHourChanged += CheckSecondActivation;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEvents.OnHourChanged -= CheckSecondActivation;
    }

    private void CheckSecondActivation(float hour)
    {
        if (hour >= secondActivateHour && !IsActive)
            Activate();
    }

    public override void Activate()
    {
        base.Activate();
        StartCoroutine(TurnOffElectricity());
    }

    private IEnumerator TurnOffElectricity()
    {
        yield return new WaitForSeconds(1.5f);
        _electricityOff = true;
        electricalRoom?.TurnOffPower();
        GameEvents.OnElectricityOff?.Invoke();
    }

    public void OnPowerRestored()
    {
        _electricityOff = false;
        Deactivate();
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