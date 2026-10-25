using UnityEngine;
using TMPro;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text outcomeTitleText; // e.g., "HIT!" or "DODGED!"
    [SerializeField] private TMP_Text outcomeDescriptionText; // e.g., "Player 1 stole 3 tokens!"
    [SerializeField] private float displayDuration = 2.5f; // How long to show the result before it closes

    private Coroutine hideRoutine;

    public void ShowResult(bool wasDodged, int betAmount)
    {

        bool isLocalSlapper = GameNetworkManager.Instance != null && GameNetworkManager.Instance.IsLocalPlayerSlapper();

        if (wasDodged)
        {
            if (outcomeTitleText != null) outcomeTitleText.text = "DODGED!";
            if (outcomeDescriptionText != null)
            {
                outcomeDescriptionText.text = isLocalSlapper 
                    ? $"The defender dodged and gained {betAmount} tokens." 
                    : $"You successfully evaded the slap and stole {betAmount} tokens.";
            }
        }
        else
        {
            if (outcomeTitleText != null) outcomeTitleText.text = "HIT!";
            if (outcomeDescriptionText != null)
            {
                outcomeDescriptionText.text = isLocalSlapper 
                    ? $"You reclaimed your {betAmount} token bet." 
                    : $"You got slapped and lost {betAmount} tokens.";
            }
        }

        // Optional: Auto-hide after a few seconds if the server handles the phase transition delay
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(AutoDismissRoutine());
    }

    private IEnumerator AutoDismissRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        this.gameObject.SetActive(false);
    }
}