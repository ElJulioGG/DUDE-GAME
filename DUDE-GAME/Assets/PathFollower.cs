using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public Transform[] pathPoints;
    public float speed = 2f;
    private int currentPointIndex = 0;

    void Update()
    {
        if (pathPoints.Length == 0) return;

        Transform target = pathPoints[currentPointIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
        }
    }
}
