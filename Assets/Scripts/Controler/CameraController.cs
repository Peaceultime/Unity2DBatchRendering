using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private float zoom = 0.4f;
    private Camera cam;

    [Range(3f, 6f)]
    public float minZoom;
    [Range(6f, 24f)]
    public float maxZoom;
    public float zoomSpeedFactor = 3f;
    public float dragSpeedFactor = 0.3f;

    [Range(2f, 6f)]
    public float speedUp = 3f;

    private bool isDragging = false;
    private Vector2 lastCursorPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = math.lerp(minZoom, maxZoom, zoom);

        isDragging = false;
    }
    void Update()
    {
        if (InputManager.scroll != null && InputManager.scroll.IsInProgress())
        {
            float zoom = InputManager.scroll.ReadValue<float>() * Time.deltaTime / 5f;
            this.zoom = math.clamp(this.zoom + zoom, 0, 1);
            cam.orthographicSize = math.lerp(minZoom, maxZoom, this.zoom);
        }

        if (InputManager.move != null && InputManager.move.IsInProgress())
        {
            Vector2 vel = InputManager.move.ReadValue<Vector2>() * (InputManager.speedup.IsPressed() ? speedUp : 1f) * Time.deltaTime * math.lerp(1, zoomSpeedFactor, zoom);
            Vector3 pos = transform.position;
            pos.x += vel.x;
            pos.y += vel.y;
            transform.position = pos;
        }

        if(InputManager.click != null && InputManager.click.IsInProgress())
        {
            if(!isDragging)
            {
                isDragging = true;
                lastCursorPosition = InputManager.cursor.ReadValue<Vector2>();
            }
            else
            {
                Vector2 vel = InputManager.cursor.ReadValue<Vector2>();
                Vector3 pos = transform.position;
                pos.x -= (vel.x - lastCursorPosition.x) * dragSpeedFactor;
                pos.y -= (vel.y - lastCursorPosition.y) * dragSpeedFactor;
                transform.position = pos;

                lastCursorPosition = vel;
            }
        }

        if(isDragging && InputManager.click != null && !InputManager.click.IsInProgress())
        {
            isDragging = false;
        }
    }
}
