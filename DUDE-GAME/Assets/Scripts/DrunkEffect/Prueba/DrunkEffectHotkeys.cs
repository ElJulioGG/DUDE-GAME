using UnityEngine;

public class DrunkEffectHotkeys : MonoBehaviour
{
    public DrunkEffectsController controller;

    void Update()
    {
        if (!controller) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) controller.ActivateSway();
        if (Input.GetKeyDown(KeyCode.Alpha2)) controller.ActivateBreathing();
        if (Input.GetKeyDown(KeyCode.Alpha3)) controller.ActivateWorldTilt();
        if (Input.GetKeyDown(KeyCode.Alpha0)) controller.StopAllEffects();
    }
}
