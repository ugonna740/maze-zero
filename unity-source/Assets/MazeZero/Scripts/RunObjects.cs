using UnityEngine;

namespace MazeZero
{
    public sealed class OrbCollectible : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0f, 100f * Time.deltaTime, 0f, Space.World);
            transform.localPosition += Vector3.up * (Mathf.Sin(Time.time * 3f + transform.position.x) * .002f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerController>(out _)) return;
            PracticeRunManager.Instance.CollectOrb();
            Destroy(gameObject);
        }
    }

    public sealed class ExitController : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerController>(out _)) PracticeRunManager.Instance.Escape();
        }
    }
}
