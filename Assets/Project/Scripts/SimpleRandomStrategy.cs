using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Spawn strategy that picks a uniformly random tetromino each time.
    /// Equivalent to the original spawn behavior — no drought protection.
    /// </summary>
    [CreateAssetMenu(fileName = "SimpleRandomStrategy", menuName = "Tetris/Simple Random Strategy")]
    public class SimpleRandomStrategy : SpawnStrategy
    {
        /// <inheritdoc/>
        public override Tetromino Next()
        {
            return (Tetromino)Random.Range(0, System.Enum.GetValues(typeof(Tetromino)).Length);
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            // No state to reset.
        }
    }
}
