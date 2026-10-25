using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : NetworkBehaviour
{
    [Header("General TMP References")]
    [SerializeField] private TextMeshProUGUI player1TokenText;
    [SerializeField] private TextMeshProUGUI player2TokenText;
    [SerializeField] private TextMeshProUGUI currentBetText;
    [SerializeField] private TextMeshProUGUI gamePhaseText;

    [Header("Receiver TMP References")]
    [SerializeField] private Slider receiverStaminaBar;

    [Header("Slapper TMP References")]
    [SerializeField] private Slider slapperPowerBar;

    [Header("UI Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject coinPanel;
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private GameObject skillCheckPanel;
    [SerializeField] private GameObject resultPanel;

    public override void OnNetworkSpawn()
    {
        // Subscribe to state change events
        GameNetworkManager.Instance.Player1Tokens.OnValueChanged += OnP1TokensChanged;
        GameNetworkManager.Instance.Player2Tokens.OnValueChanged += OnP2TokensChanged;
        GameNetworkManager.Instance.CurrentBet.OnValueChanged += OnBetChanged;
        GameNetworkManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;

        // Initialize UI values with current network state
        UpdateAllUI();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe when destroyed to prevent memory leaks
        GameNetworkManager.Instance.Player1Tokens.OnValueChanged -= OnP1TokensChanged;
        GameNetworkManager.Instance.Player2Tokens.OnValueChanged -= OnP2TokensChanged;
        GameNetworkManager.Instance.CurrentBet.OnValueChanged -= OnBetChanged;
        GameNetworkManager.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
    }

    // --- EVENT CALLBACKS ---

    private void OnP1TokensChanged(int previous, int current)
    {
        player1TokenText.text = $"Tokens: {current}";
    }

    private void OnP2TokensChanged(int previous, int current)
    {
        player2TokenText.text = $"Tokens: {current}";
    }

    private void OnBetChanged(int previous, int current)
    {
        currentBetText.text = $"Bet: {current}";
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase current)
    {
        gamePhaseText.text = $"Phase: {current}";

        // Show/hide panels based on the current phase
        hudPanel.SetActive(current != GamePhase.Betting && current != GamePhase.Game && current != GamePhase.Resolution);
        bettingPanel.SetActive(current == GamePhase.Betting);
        skillCheckPanel.SetActive(current == GamePhase.Game);
        resultPanel.SetActive(current == GamePhase.Resolution || current == GamePhase.EndGame);
    }

    private void UpdateAllUI()
    {
        OnP1TokensChanged(0, GameNetworkManager.Instance.Player1Tokens.Value);
        OnP2TokensChanged(0, GameNetworkManager.Instance.Player2Tokens.Value);
        OnBetChanged(0, GameNetworkManager.Instance.CurrentBet.Value);
        OnPhaseChanged(GamePhase.CoinToss, GameNetworkManager.Instance.CurrentPhase.Value);
    }
}
