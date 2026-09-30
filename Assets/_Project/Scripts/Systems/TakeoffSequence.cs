// Gestisce il decollo iniziale e riattiva il gameplay al suo termine.
using UnityEngine;
using BeyondSky.Input;
using BeyondSky.World;

namespace BeyondSky.Systems
{
    [DefaultExecutionOrder(200)]
    public class TakeoffSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private SpriteRenderer playerVisual;
        [SerializeField] private PlaneInput planeInput;
        [SerializeField] private MonoBehaviour[] suspendedBehaviours = new MonoBehaviour[0];

        [Header("Takeoff")]
        [SerializeField] private float duration = 2f;
        [SerializeField] private float roadSurfaceY = 13.15f;
        [SerializeField] private float roadClearance = 0.15f;
        [SerializeField] private float noseUpAngle = 25f;
        [Range(0f, 0.8f)]
        [SerializeField] private float groundRunFraction = 0.15f;

        private bool[] previousEnabledStates;
        private Vector3 flightPosition;
        private Vector3 groundPosition;
        private Quaternion originalRotation;
        private float timer;
        private bool isPlaying;
        private bool finishPending;

        private void Awake()
        {
            if (player == null || playerVisual == null || planeInput == null)
            {
                Debug.LogError("TakeoffSequence: riferimenti del player mancanti.", this);
                enabled = false;
                return;
            }

            flightPosition = player.position;
            originalRotation = playerVisual.transform.localRotation;
            float bottomOffset = player.position.y - playerVisual.bounds.min.y;
            groundPosition = flightPosition;
            groundPosition.y = Mathf.Min(flightPosition.y, roadSurfaceY + roadClearance + bottomOffset);

            previousEnabledStates = new bool[suspendedBehaviours.Length];
            for (int i = 0; i < suspendedBehaviours.Length; i++)
            {
                MonoBehaviour behaviour = suspendedBehaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }
                previousEnabledStates[i] = behaviour.enabled;
                behaviour.enabled = false;
            }

            planeInput.SetLocked(true);
            player.position = groundPosition;
            isPlaying = true;
        }

        private void Start()
        {
            if (!isPlaying)
            {
                return;
            }

            for (int i = 0; i < suspendedBehaviours.Length; i++)
            {
                WorldScroller world = suspendedBehaviours[i] as WorldScroller;
                if (world == null)
                {
                    continue;
                }

                world.PrepareWorld();

            }
        }

        private void Update()
        {
            if (!isPlaying || Time.deltaTime <= 0f)
            {
                return;
            }

            timer = timer + Time.deltaTime;
            float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            float climb = Mathf.InverseLerp(groundRunFraction, 1f, progress);
            float height = Mathf.SmoothStep(0f, 1f, climb);
            player.position = Vector3.Lerp(groundPosition, flightPosition, height);
            float angle = Mathf.Sin(climb * Mathf.PI) * noseUpAngle;
            playerVisual.transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

            if (progress >= 1f)
            {
                finishPending = true;
            }
        }

        private void LateUpdate()
        {
            if (finishPending)
            {
                Finish();
            }
        }

        private void Finish()
        {
            isPlaying = false;
            finishPending = false;
            if (player != null)
            {
                player.position = flightPosition;
            }
            if (playerVisual != null)
            {
                playerVisual.transform.localRotation = originalRotation;
            }
            if (planeInput != null)
            {
                planeInput.SetLocked(false);
            }
            for (int i = 0; i < suspendedBehaviours.Length; i++)
            {
                MonoBehaviour behaviour = suspendedBehaviours[i];
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = previousEnabledStates[i];
                }
            }
        }

        private void OnDisable()
        {
            if (isPlaying)
            {
                Finish();
            }
        }
    }
}
