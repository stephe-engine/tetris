using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Abstract base for tetromino spawn strategies. Create concrete implementations
    /// as ScriptableObject assets and assign them to <see cref="Board"/> in the Inspector.
    /// </summary>
    public abstract class SpawnStrategy : ScriptableObject
    {
        /// <summary>
        /// Returns the next <see cref="Tetromino"/> to spawn.
        /// </summary>
        /// <returns>The tetromino type to spawn next.</returns>
        public abstract Tetromino Next();

        /// <summary>
        /// Resets internal state (e.g. for a new game). Called from
        /// <see cref="Board.Awake"/> to ensure clean state each play session.
        /// </summary>
        public abstract void Reset();
    }
}
