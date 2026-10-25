using UnityEngine;
using UnityEngine.UI;

public class SkillCheckUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider skillCheckSlider;
    [SerializeField] private Image fillAreaImage;

    [Header("Skill Check Settings")]
    [SerializeField] private float oscillationSpeed = 3f;

    private bool isActive = false;
    private float pingPongTimer = 0f;

    void Update()
    {
        if (!isActive) return;

        pingPongTimer += Time.deltaTime * oscillationSpeed;
        float normalizedValue = Mathf.PingPong(pingPongTimer, 1f);
        skillCheckSlider.value = normalizedValue;
    }

    public void InitializeSkillCheck()
    {
        isActive = true;
        pingPongTimer = 0f;
    }

    public void ConfirmSkillCheck()
    {
        if (!isActive) return;
        isActive = false;

        float rawValue = skillCheckSlider.value;
        float finalPower = CalculatePowerScore(rawValue);

        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.SubmitSkillCheckResultServerRpc(finalPower);
        }

        this.gameObject.SetActive(false);
    }

    private float CalculatePowerScore(float value)
    {
        float centerDistance = Mathf.Abs(value - 0.5f); 
        float power = Mathf.Clamp(1.0f - (centerDistance * 1.5f), 0.2f, 1.0f);
        return power;
    }
}