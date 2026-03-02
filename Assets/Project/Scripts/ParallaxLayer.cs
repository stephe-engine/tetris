using UnityEngine;

namespace Project.Scripts
{
    /// <summary>
    /// Auto-scrolls child sprite copies leftward at a constant speed.
    /// Recycles copies seamlessly when they exit the left screen edge.
    /// Subscribes to <see cref="GameManager"/> events to pause with the game.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        /// <summary>Scroll speed in world units per second.</summary>
        [SerializeField] private float scrollSpeed = 1f;

        private Transform[] copies;
        private float spriteWidth;
        private float recycleX;
        private bool paused;

        private void OnEnable()
        {
            if (gameManager)
            {
                gameManager.OnPaused += HandlePaused;
                gameManager.OnResumed += HandleResumed;
                gameManager.OnGameStart += HandleGameStart;
            }
        }

        private void OnDisable()
        {
            if (gameManager)
            {
                gameManager.OnPaused -= HandlePaused;
                gameManager.OnResumed -= HandleResumed;
                gameManager.OnGameStart -= HandleGameStart;
            }
        }

        private void Start()
        {
            // Collect only children that have a SpriteRenderer (ignores lights, etc.)
            System.Collections.Generic.List<Transform> spriteChildren = new();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<SpriteRenderer>())
                    spriteChildren.Add(child);
            }
            copies = spriteChildren.ToArray();

            SpriteRenderer sr = copies[0].GetComponent<SpriteRenderer>();
            spriteWidth = sr.bounds.size.x;

            // Compute the left screen edge in world space at this layer's depth
            Camera cam = Camera.main;
            float distToLayer = Mathf.Abs(cam.transform.position.z - transform.position.z);
            float halfWidth = distToLayer * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
            recycleX = cam.transform.position.x - halfWidth;
        }

        private void Update()
        {
            if (paused) return;

            transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

            // Recycle any copy whose right edge has passed the left screen edge
            float totalWidth = spriteWidth * copies.Length;
            foreach (Transform copy in copies)
            {
                if (copy.position.x + spriteWidth * 0.5f < recycleX)
                    copy.position += new Vector3(totalWidth, 0f, 0f);
            }
        }

        private void HandlePaused() => paused = true;
        private void HandleResumed() => paused = false;
        private void HandleGameStart() => paused = false;
    }
}
