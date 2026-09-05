namespace TicTacToe.Domain;

/// <summary>The two marks that can occupy a cell.</summary>
public enum Player
{
    X = 0,
    O = 1
}

/// <summary>How the O side is controlled.</summary>
public enum GameMode
{
    /// <summary>Both X and O are played by humans.</summary>
    TwoPlayer = 0,

    /// <summary>X is human, O is played automatically by the backend.</summary>
    Computer = 1
}

/// <summary>Lifecycle status of a single game.</summary>
public enum GameStatus
{
    InProgress = 0,
    Won = 1,
    Draw = 2
}

/// <summary>Why a submitted move was rejected.</summary>
public enum MoveRejectionReason
{
    None = 0,

    /// <summary>Cell index outside 0..8 (or row/column outside 0..2).</summary>
    OutOfBoard = 1,

    /// <summary>The target cell already holds a mark.</summary>
    CellOccupied = 2,

    /// <summary>The game has already been won or drawn.</summary>
    GameCompleted = 3,

    /// <summary>It is not that player's turn.</summary>
    WrongPlayer = 4,

    /// <summary>In computer mode the human may only play X; O is the engine's.</summary>
    NotHumanControlled = 5
}
