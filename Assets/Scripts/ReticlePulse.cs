using UnityEngine;

public class ReticlePulse : MonoBehaviour
{
    public float speed = 2f;
    public float intensity = 0.05f;

    Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float pulse = 1 + Mathf.Sin(Time.time * speed) * intensity;
        transform.localScale = baseScale * pulse;
    }
}