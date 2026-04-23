using UnityEngine;

namespace LochyIGorzala.Player
{
    /// <summary>
    /// Smoothly follows the player character with the camera.
    /// Orthographic camera sized for pixel-perfect display of 32x32 tiles.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Bounds (optional)")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 50f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 50f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                // Try to find player automatically
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            // Snap to target position immediately on start
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            if (useBounds && cam != null)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;

                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX + halfWidth, maxX - halfWidth);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY + halfHeight, maxY - halfHeight);
            }

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        /// <summary>
        /// Sets the camera bounds based on dungeon size.
        /// Called by DungeonGenerator after map creation.
        /// </summary>
        public void SetBounds(float mapWidth, float mapHeight)
        {
            minX = 0f;
            maxX = mapWidth;
            minY = 0f;
            maxY = mapHeight;
            useBounds = true;
        }

        /// <summary>
        /// Disables bounds clamping so the camera follows the player freely.
        /// Used for the lobby where the map is small enough that clamping
        /// locks the camera near the centre instead of tracking the player.
        /// </summary>
        public void DisableBounds()
        {
            useBounds = false;
        }
    }
}
