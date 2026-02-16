using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scripts
{
    /// <summary>
    /// Standard 7-bag spawn strategy. All seven tetromino types are shuffled into a bag
    /// and dealt one at a time. When the bag empties it refills automatically, guaranteeing
    /// every piece appears exactly once per bag cycle and preventing long piece droughts.
    /// </summary>
    [CreateAssetMenu(fileName = "BagRandomStrategy", menuName = "Tetris/Bag Random Strategy")]
    public class BagRandomStrategy : SpawnStrategy
    {
        private List<Tetromino> bag = new();

        /// <inheritdoc/>
        public override Tetromino Next()
        {
            if (bag.Count == 0)
            {
                FillBag();
            }

            int index = Random.Range(0, bag.Count);
            Tetromino piece = bag[index];
            bag.RemoveAt(index);
            return piece;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            bag.Clear();
        }

        /// <summary>
        /// Fills the bag with one of each <see cref="Tetromino"/> type.
        /// </summary>
        private void FillBag()
        {
            Tetromino[] values = (Tetromino[])Enum.GetValues(typeof(Tetromino));

            foreach (Tetromino type in values)
            {
                bag.Add(type);
            }
        }
    }
}
