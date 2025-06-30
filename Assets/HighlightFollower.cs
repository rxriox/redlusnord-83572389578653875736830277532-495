using UnityEngine;

public class HighlightFollower : MonoBehaviour
{
    public Transform targetToFollow;

    void LateUpdate()
    {
        if (targetToFollow != null)
        {
            transform.position = targetToFollow.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}