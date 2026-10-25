using UnityEngine;
using Unity.Netcode;

public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    [Header("Network States (Server Authority)")]
    public NetworkVariable<GamePhase> CurrentPhase = new(
        GamePhase.CoinToss, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerRole> ActiveRole = new(
        PlayerRole.Player1IsSlapper, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Player1Tokens = new(5);
    public NetworkVariable<int> Player2Tokens = new(5);
    public NetworkVariable<int> CurrentBet = new(0);

    [Header("Base Game Settings")]
    [SerializeField] private float baseSlapDuration = 0.5f;
    [SerializeField] private float dodgeIFrameWindow = 0.25f;

    // --- Private Server-Only Memory Fields ---
    private float serverSlapPower = 0f;
    private double lastSlapCommitTime = -1.0;
    private double lastDodgeTime = -1.0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public bool IsLocalPlayerSlapper()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isP1 = (localId == 0);
        bool result = (isP1 && ActiveRole.Value == PlayerRole.Player1IsSlapper) ||
                      (!isP1 && ActiveRole.Value == PlayerRole.Player2IsSlapper);
        return result;
    }

    // =========================================================================
    // CLIENT TO SERVER RPCs
    // =========================================================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InitiateCoinTossServerRpc()
    {
        if (CurrentPhase.Value != GamePhase.CoinToss) return;

        bool player1Starts = Random.value > 0.5f;
        ActiveRole.Value = player1Starts ? PlayerRole.Player1IsSlapper : PlayerRole.Player2IsSlapper;
        CurrentPhase.Value = GamePhase.Betting;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitBetServerRpc(int betAmount, RpcParams rpcParams = default)
    {
        if (CurrentPhase.Value != GamePhase.Betting) return;

        int maxBet = (ActiveRole.Value == PlayerRole.Player1IsSlapper) ? Player1Tokens.Value : Player2Tokens.Value;
        CurrentBet.Value = Mathf.Clamp(betAmount, 1, maxBet);

        CurrentPhase.Value = GamePhase.SkillCheck;
        StartSkillCheckClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitSkillCheckResultServerRpc(float skillPowerScore, RpcParams rpcParams = default)
    {
        if (CurrentPhase.Value != GamePhase.SkillCheck) return;

        serverSlapPower = Mathf.Clamp01(skillPowerScore);
        CurrentPhase.Value = GamePhase.Game;
        PromptDecisionClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PerformDodgeServerRpc(RpcParams rpcParams = default)
    {
        if (CurrentPhase.Value != GamePhase.Game && CurrentPhase.Value != GamePhase.Resolution) return;
        lastDodgeTime = NetworkManager.ServerTime.Time;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ExecuteSlapDecisionServerRpc(SlapType decision, RpcParams rpcParams = default)
    {
        if (CurrentPhase.Value != GamePhase.Game) return;

        if (decision == SlapType.Fake)
        {
            serverSlapPower *= 0.8f;
            PlaySlapAnimationClientRpc(SlapType.Fake, wasDodged: false, serverSlapPower);
            return;
        }

        CurrentPhase.Value = GamePhase.Resolution;
        lastSlapCommitTime = NetworkManager.ServerTime.Time;

        double slapDuration = baseSlapDuration * (1.0f - (serverSlapPower * 0.5f));
        double slapImpactTime = lastSlapCommitTime + slapDuration;

        bool isDodged = EvaluateDodgeSuccess(slapImpactTime);
        ResolveTokenTransfer(isDodged);
        PlaySlapAnimationClientRpc(SlapType.Commit, isDodged, serverSlapPower);
        CheckWinConditionOrNextTurn();
    }

    // =========================================================================
    // SERVER-SIDE LOGIC EVALUATORS
    // =========================================================================

    private bool EvaluateDodgeSuccess(double slapImpactTime)
    {
        if (lastDodgeTime < 0) return false;

        double dodgeStart = lastDodgeTime;
        double dodgeEnd = lastDodgeTime + dodgeIFrameWindow;

        bool timingValid = (slapImpactTime >= dodgeStart) && (slapImpactTime <= dodgeEnd);
        lastDodgeTime = -1.0; 

        return timingValid;
    }

    private void ResolveTokenTransfer(bool isDodged)
    {
        int bet = CurrentBet.Value;

        if (isDodged)
        {
            if (ActiveRole.Value == PlayerRole.Player1IsSlapper)
            {
                Player1Tokens.Value -= bet;
                Player2Tokens.Value += bet;
            }
            else
            {
                Player2Tokens.Value -= bet;
                Player1Tokens.Value += bet;
            }
        }
        else
        {
            if (ActiveRole.Value == PlayerRole.Player1IsSlapper)
            {
                Player1Tokens.Value += bet;
                Player2Tokens.Value -= bet;
            }
            else
            {
                Player2Tokens.Value += bet;
                Player1Tokens.Value -= bet;
            }
        }
    }

    private void CheckWinConditionOrNextTurn()
    {
        if (Player1Tokens.Value >= 10 || Player2Tokens.Value >= 10)
        {
            CurrentPhase.Value = GamePhase.EndGame;
            ulong winningClientId = (Player1Tokens.Value >= 10) ? (ulong)0 : (ulong)1;
            NotifyGameOverClientRpc(winningClientId);
        }
        else
        {
            // Swap roles for the next turn
            ActiveRole.Value = (ActiveRole.Value == PlayerRole.Player1IsSlapper) 
                ? PlayerRole.Player2IsSlapper 
                : PlayerRole.Player1IsSlapper;

            CurrentBet.Value = 0;
            serverSlapPower = 0f;
            
            // Keep phase as Resolution while the result is being viewed, 
            // then start the delay coroutine before switching to Betting!
            CurrentPhase.Value = GamePhase.Resolution;
            
            // Wait 2.5 seconds (matching ResultUI display duration) before clearing animations & resetting to betting
            StartCoroutine(ResetTurnAfterDelayRoutine(2.5f));
        }
    }

    private System.Collections.IEnumerator ResetTurnAfterDelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Clear animations after result display is finished
        ClearAnimations();

        // Now safely transition to the Betting phase for the next round
        CurrentPhase.Value = GamePhase.Betting;
    }

    private void ClearAnimations()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<ArmAnimation>(out var armAnim))
            {
                armAnim.ResetAnimatorParameters();
            }
        }
    }

    // =========================================================================
    // SERVER TO CLIENT RPCs
    // =========================================================================

    [ClientRpc]
    private void StartSkillCheckClientRpc()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TriggerSkillCheckDisplay();
        }
    }

    [ClientRpc]
    private void PromptDecisionClientRpc() { }

    [ClientRpc]
    private void PlaySlapAnimationClientRpc(SlapType decision, bool wasDodged, float power)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<ArmAnimation>(out var armAnim))
            {
                ulong clientId = client.ClientId;
                bool isP1Slapper = (ActiveRole.Value == PlayerRole.Player1IsSlapper);
                bool isThisPlayerSlapper = (clientId == 0 && isP1Slapper) || (clientId != 0 && !isP1Slapper);

                if (decision == SlapType.Fake)
                {
                    if (isThisPlayerSlapper) armAnim.PlayFakeAction();
                }
                else if (decision == SlapType.Commit)
                {
                    if (isThisPlayerSlapper) armAnim.PlayCommitResult(!wasDodged);
                    else armAnim.PlayDodgeAction(wasDodged);
                }
            }
        }

        if (decision == SlapType.Commit && UIManager.Instance != null)
        {
            UIManager.Instance.TriggerResolutionDisplay(wasDodged, CurrentBet.Value);
        }
    }

    [ClientRpc]
    private void NotifyGameOverClientRpc(ulong winningClientId) { }
}