using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BettingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider bettingSlider;
    [SerializeField] private TMP_Text sliderValueText;

    public void InitializeBetting(int maxTokens)
    {
        if (bettingSlider != null)
        {
            bettingSlider.wholeNumbers = true;
            bettingSlider.minValue = 1;
            bettingSlider.maxValue = Mathf.Max(1, maxTokens);
            bettingSlider.value = 1;
        }

        UpdateSliderText(1f);
    }

    public void OnSliderValueChanged(float value)
    {
        int snappedValue = Mathf.RoundToInt(value);
        UpdateSliderText(snappedValue);
    }

    private void UpdateSliderText(float value)
    {
        if (sliderValueText != null)
        {
            sliderValueText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    public void ConfirmBet()
    {
        if (bettingSlider == null) return;

        int selectedBet = Mathf.RoundToInt(bettingSlider.value);

        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.SubmitBetServerRpc(selectedBet);
        }

        this.gameObject.SetActive(false);
    }
}