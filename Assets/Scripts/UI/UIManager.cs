using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Main Menu References")]
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject joinMenuUI;
    [SerializeField] private GameObject pauseMenuUI;

    [Header("HUD References")]
    [SerializeField] private GameObject generalHUD;
    [SerializeField] private TMP_Text phaseTMP;
    [SerializeField] private TMP_Text roleTMP;
    [SerializeField] private TMP_Text p1TokensTMP;
    [SerializeField] private TMP_Text p2TokensTMP;
    [SerializeField] private TMP_Text currentBetTMP;

    [Header("Panel References")]
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private GameObject skillCheckPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject endGamePanel;

    [Header("Betting UI Reference")]
    [SerializeField] private BettingUI bettingUI;

    [Header("Skill Check UI Reference")]
    [SerializeField] private SkillCheckUI skillCheckUI;

    [Header("Result UI Reference")]
    [SerializeField] private ResultUI resultUI; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        menuUI.SetActive(true);
        mainMenuUI.SetActive(true);
        joinMenuUI.SetActive(false);
        pauseMenuUI.SetActive(false);
        generalHUD.SetActive(false);
    }

    public void OpenJoinMenu()
    {
        joinMenuUI.SetActive(true);
        mainMenuUI.SetActive(false);
    }

    public void OpenMainMenu()
    {
        joinMenuUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }   

    public void ClosePauseMenu()
    {
        pauseMenuUI.SetActive(false);
    }
    
    public void TogglePauseMenu()
    {
        pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
    }

    public void InitializeHUD()
    {
        mainMenuUI.SetActive(false);
        joinMenuUI.SetActive(false);
        pauseMenuUI.SetActive(false);
        generalHUD.SetActive(true);

        SubscribeToNetworkVariables();
        RefreshAllHUDTexts();

        // FIX: Explicitly evaluate betting UI check on HUD initialization 
        // in case the Betting phase already started before subscription caught it.
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.CurrentPhase.Value == GamePhase.Betting)
        {
            CheckAndOpenBettingUI();
        }
    }

    public void TriggerSkillCheckDisplay()
    {
        if (GameNetworkManager.Instance == null || !GameNetworkManager.Instance.IsLocalPlayerSlapper()) return;

        if (skillCheckPanel != null) 
            skillCheckPanel.SetActive(true);
        
        if (skillCheckUI != null) 
            skillCheckUI.InitializeSkillCheck();
    }

    public void TriggerResolutionDisplay(bool wasDodged, int betAmount)
    {
        if (resultPanel != null) 
            resultPanel.SetActive(true);

        if (resultUI != null)
            resultUI.ShowResult(wasDodged, betAmount);
    }

    private void SubscribeToNetworkVariables()
    {
        if (GameNetworkManager.Instance == null) return;

        UnsubscribeFromNetworkVariables(); // Prevent duplicate subscriptions

        GameNetworkManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;
        GameNetworkManager.Instance.ActiveRole.OnValueChanged += OnRoleChanged; 
        GameNetworkManager.Instance.Player1Tokens.OnValueChanged += OnP1TokensChanged;
        GameNetworkManager.Instance.Player2Tokens.OnValueChanged += OnP2TokensChanged;
        GameNetworkManager.Instance.CurrentBet.OnValueChanged += OnBetChanged;
    }

    private void UnsubscribeFromNetworkVariables()
    {
        if (GameNetworkManager.Instance == null) return;

        GameNetworkManager.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        GameNetworkManager.Instance.ActiveRole.OnValueChanged -= OnRoleChanged;
        GameNetworkManager.Instance.Player1Tokens.OnValueChanged -= OnP1TokensChanged;
        GameNetworkManager.Instance.Player2Tokens.OnValueChanged -= OnP2TokensChanged;
        GameNetworkManager.Instance.CurrentBet.OnValueChanged -= OnBetChanged;
    }

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        if (phaseTMP != null) 
        {
            phaseTMP.text = $"{newPhase}";
        }

        if (newPhase == GamePhase.Betting)
        {
            CheckAndOpenBettingUI();
        }
    }

    private void CheckAndOpenBettingUI()
    {
        if (GameNetworkManager.Instance == null) return;

        bool isSlapper = GameNetworkManager.Instance.IsLocalPlayerSlapper();
        Debug.Log($"[CheckAndOpenBettingUI] Local Client ID: {NetworkManager.Singleton.LocalClientId} | IsSlapper: {isSlapper}");

        if (isSlapper)
        {
            bool isP1 = NetworkManager.Singleton.LocalClientId == 0;
            int myTokens = isP1 ? GameNetworkManager.Instance.Player1Tokens.Value : GameNetworkManager.Instance.Player2Tokens.Value;
            
            if (bettingPanel != null) 
                bettingPanel.SetActive(true);

            if (bettingUI != null)
                bettingUI.InitializeBetting(myTokens);
        }
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        UpdateRoleText();
        // Re-evaluate betting UI whenever roles swap dynamically
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.CurrentPhase.Value == GamePhase.Betting)
        {
            CheckAndOpenBettingUI();
        }
    }

    private void OnP1TokensChanged(int oldTokens, int newTokens)
    {
        if (p1TokensTMP != null) p1TokensTMP.text = $"{newTokens}";
    }

    private void OnP2TokensChanged(int oldTokens, int newTokens)
    {
        if (p2TokensTMP != null) p2TokensTMP.text = $"{newTokens}";
    }

    private void OnBetChanged(int oldBet, int newBet)
    {
        if (currentBetTMP != null) currentBetTMP.text = $"{newBet}";
    }

    private void RefreshAllHUDTexts()
    {
        if (GameNetworkManager.Instance == null) return;

        if (phaseTMP != null) 
            phaseTMP.text = $"{GameNetworkManager.Instance.CurrentPhase.Value}";

        UpdateRoleText();

        if (p1TokensTMP != null) 
            p1TokensTMP.text = $"{GameNetworkManager.Instance.Player1Tokens.Value}";

        if (p2TokensTMP != null) 
            p2TokensTMP.text = $"{GameNetworkManager.Instance.Player2Tokens.Value}";

        if (currentBetTMP != null) 
            currentBetTMP.text = $"{GameNetworkManager.Instance.CurrentBet.Value}";
    }

    private void UpdateRoleText()
    {
        if (roleTMP == null || GameNetworkManager.Instance == null) return;

        bool isLocalSlapper = GameNetworkManager.Instance.IsLocalPlayerSlapper();
        roleTMP.text = isLocalSlapper ? "Slapper" : "Defender";
    }
}