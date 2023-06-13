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

    [Range(2f, 6f)]
    public float speedUp = 3f;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = math.lerp(minZoom, maxZoom, zoom);
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
    }
}
