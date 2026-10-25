using UnityEngine;

public class NetworkDataTypes
{
    
}

public enum GamePhase
{
    CoinToss,
    Betting,
    SkillCheck,
    Game,
    Resolution,
    EndGame
}

public enum PlayerRole
{
    Player1IsSlapper,
    Player2IsSlapper,
}

public enum SlapType
{
    Fake,
    Commit
}
