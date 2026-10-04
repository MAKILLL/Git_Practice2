using UnityEngine;

public sealed class TrafficAgent : MonoBehaviour
{
    [SerializeField] private float radius = 8f;
    [SerializeField] private float speed = 0.35f;
    [SerializeField] private float phase;

    public void Configure(float newRadius, float newSpeed, float newPhase)
    {
        radius = newRadius;
        speed = newSpeed;
        phase = newPhase;
    }

    private void Update()
    {
        var angle = Time.time * speed + phase;
        transform.position = new Vector3(
            Mathf.Cos(angle) * radius,
            0.35f,
            Mathf.Sin(angle * 1.37f) * radius * 0.62f);
        transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
    }
}
