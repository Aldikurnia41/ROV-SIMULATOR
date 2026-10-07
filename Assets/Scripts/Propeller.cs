using UnityEngine;

public class Propeller : MonoBehaviour
{
    public float Rpm;

    public RotateOrientation Orientation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var visibleRpm = Mathf.Clamp(Rpm, -600, 600);
        switch (Orientation)
        {
            case RotateOrientation.X:
                transform.localEulerAngles += new Vector3(Rpm * Time.deltaTime,0,0);
                break;
            case RotateOrientation.Y:
                transform.localEulerAngles += new Vector3(0,Rpm * Time.deltaTime,0);
                break;
            case RotateOrientation.Z:
                transform.localEulerAngles += new Vector3(0,0,Rpm * Time.deltaTime);
                //Debug.Log($"{transform.name} {Rpm}");
                break;
        }
        
    }

    public enum RotateOrientation
    {
        X,Y,Z
    }
}
