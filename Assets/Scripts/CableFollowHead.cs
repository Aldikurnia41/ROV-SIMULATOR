using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class CableFollowHead : MonoBehaviour
{
    public Transform CableEnd;
    public float TangentDistance;

    public int endKnotIndex = -1; // -1 = otomatis pakai knot terakhir

    SplineContainer splineContainer;
    Spline spline;

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        spline = splineContainer.Spline;

        if (endKnotIndex < 0)
            endKnotIndex = spline.Count - 1;
    }

    void Update()
    {
        if (CableEnd == null) return;
        
        float3 localPos = splineContainer.transform.InverseTransformPoint(CableEnd.position);
        
        BezierKnot knot = spline[endKnotIndex];
        knot.Position = localPos;

        var rot = new Vector3(360.0f - CableEnd.eulerAngles.x, CableEnd.eulerAngles.y + 180.0f);
        knot.Rotation = Quaternion.Euler(rot);
        knot.TangentIn = 5.0f;

        spline[endKnotIndex] = knot;
    }
}