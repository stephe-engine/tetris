using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts
{
    /// <summary>
    /// Reads player input via the Unity Input System and translates it into
    /// <see cref="GameCommand"/>s sent to the <see cref="GameManager"/>.
    /// Implements hold-to-repeat behavior for standard Tetris input feel
    /// on directional actions: wait <see cref="holdDelay"/>, then repeat at <see cref="holdRepeatRate"/>.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        /// <summary>Reference to the GameManager that receives commands.</summary>
        [SerializeField] private GameManager gameManager;

        /// <summary>Delay in seconds before auto-repeat starts when holding a key.</summary>
        [SerializeField] private float holdDelay = 0.17f;

        /// <summary>Interval in seconds between repeated commands while a key is held.</summary>
        [SerializeField] private float holdRepeatRate = 0.05f;

        private TetrisInput inputActions;

        private float holdTimerLeft;
        private float holdTimerRight;
        private float holdTimerDown;
        private float repeatTimerLeft;
        private float repeatTimerRight;
        private float repeatTimerDown;

        private void Awake()
        {
            inputActions = new TetrisInput();
        }

        private void OnEnable()
        {
            inputActions.Gameplay.Enable();
#if UNITY_EDITOR
            inputActions.Debug.Enable();
#endif
        }

        private void OnDisable()
        {
            inputActions.Gameplay.Disable();
#if UNITY_EDITOR
            inputActions.Debug.Disable();
#endif
        }

        private void OnDestroy()
        {
            inputActions.Dispose();
        }

        private void Update()
        {
            HandleRepeatableAction(inputActions.Gameplay.MoveLeft, GameCommand.MoveLeft,
                ref holdTimerLeft, ref repeatTimerLeft);
            HandleRepeatableAction(inputActions.Gameplay.MoveRight, GameCommand.MoveRight,
                ref holdTimerRight, ref repeatTimerRight);
            HandleRepeatableAction(inputActions.Gameplay.SoftDrop, GameCommand.SoftDrop,
                ref holdTimerDown, ref repeatTimerDown);

            // HardDrop fires once on press only — no repeat
            if (inputActions.Gameplay.HardDrop.WasPressedThisFrame())
            {
                gameManager.ExecuteCommand(GameCommand.HardDrop);
            }

            // Rotations fire once on press only — no repeat
            if (inputActions.Gameplay.RotateClockwise.WasPressedThisFrame())
            {
                gameManager.ExecuteCommand(GameCommand.RotateClockwise);
            }

            if (inputActions.Gameplay.RotateCounterClockwise.WasPressedThisFrame())
            {
                gameManager.ExecuteCommand(GameCommand.RotateCounterClockwise);
            }

#if UNITY_EDITOR
            if (inputActions.Debug.ToggleGravity.WasPressedThisFrame())
            {
                gameManager.ToggleGravity();
            }
#endif
        }

        /// <summary>
        /// Handles a repeatable input action with hold-to-repeat timing.
        /// On initial press: executes immediately. While held: waits for <see cref="holdDelay"/>,
        /// then repeats at <see cref="holdRepeatRate"/>.
        /// </summary>
        /// <param name="action">The input action to read.</param>
        /// <param name="command">The game command to execute.</param>
        /// <param name="holdTimer">Tracks time held before auto-repeat starts.</param>
        /// <param name="repeatTimer">Tracks time between auto-repeat executions.</param>
        private void HandleRepeatableAction(InputAction action, GameCommand command,
            ref float holdTimer, ref float repeatTimer)
        {
            if (action.WasPressedThisFrame())
            {
                gameManager.ExecuteCommand(command);
                holdTimer = 0f;
                repeatTimer = 0f;
            }
            else if (action.IsPressed())
            {
                holdTimer += Time.deltaTime;

                if (holdTimer >= holdDelay)
                {
                    repeatTimer += Time.deltaTime;

                    if (repeatTimer >= holdRepeatRate)
                    {
                        repeatTimer = 0f;
                        gameManager.ExecuteCommand(command);
                    }
                }
            }
            else
            {
                holdTimer = 0f;
                repeatTimer = 0f;
            }
        }
    }
}
