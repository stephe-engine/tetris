namespace Project.Scripts
{
    /// <summary>
    /// Commands that can be issued to the <see cref="GameManager"/> to control gameplay.
    /// Designed as a simple enum since Tetris commands carry no additional data.
    /// This serves as the abstraction boundary for input — commands can originate
    /// from local input, network, or AI without changing the game logic.
    /// </summary>
    public enum GameCommand
    {
        /// <summary>Move the active piece one cell to the left.</summary>
        MoveLeft,

        /// <summary>Move the active piece one cell to the right.</summary>
        MoveRight,

        /// <summary>Move the active piece one cell downward (soft drop).</summary>
        SoftDrop,

        /// <summary>Instantly drop the active piece to the lowest valid position.</summary>
        HardDrop,
    }
}
