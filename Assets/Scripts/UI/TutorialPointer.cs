using UnityEngine;

public class TutorialPointer : MonoBehaviour
{
    public Transform target;
    public float bounceSpeed = 4f;
    public float bounceHeight = 0.5f;
    public float heightOffset = 2f; // Base height above the target

    private Vector3 _startLocalPos;

    void Start()
    {
        _startLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (target != null)
        {
            // Position above target and add bounce
            Vector3 basePos = target.position + Vector3.up * heightOffset;
            float bounceY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            transform.position = basePos + Vector3.up * bounceY;
            
            // Billboard effect (always face camera)
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }
    }

    public static TutorialPointer Create(Transform targetToPoint, GameObject pointerPrefab)
    {
        if (pointerPrefab == null || targetToPoint == null) return null;
        GameObject go = Instantiate(pointerPrefab);
        TutorialPointer tp = go.GetComponent<TutorialPointer>();
        if (tp == null) tp = go.AddComponent<TutorialPointer>();
        tp.target = targetToPoint;
        return tp;
    }
}
