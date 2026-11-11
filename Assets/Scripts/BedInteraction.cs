using UnityEngine;

public sealed class BedInteraction : MonoBehaviour
{
    // Connected to the standard proximity/E interaction prompt on the bed.
    public void Interact()
    {
        SaveGameService.Instance?.GetComponent<BedDayCycle>().Sleep();
    }
}
