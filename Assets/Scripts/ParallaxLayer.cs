using UnityEngine;

namespace Platformer.View
{
    public class ParallaxLayer : MonoBehaviour
    {
        public Vector3 movementScale = Vector3.one;
        public float smoothTime = 0.15f; // smaller = snappier, larger = smoother

        Transform _camera;
        Vector3 _velocity;

        void Awake()
        {
            _camera = Camera.main.transform;
        }

        void LateUpdate()
        {
            Vector3 targetPos = Vector3.Scale(_camera.position, movementScale);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
        }
    }
}
