using TMPro;
using UnityEngine;
using UnityEngine.UI;

//TODO: Use zenject
public class HudBinder : MonoBehaviour
{
    //Inject this
    private RoVPhysics _sub;
    public Transform Compass;
    public TMP_Text Heading;
    public Transform AttPitch;
    public Transform AttRoll;

    public TMP_Text Depth;
    public Transform DepthNeedle;
    
    public TMP_Text WaterTemp;
    public TMP_Text InternalTemp;
    public TMP_Text BattV;
    public TMP_Text BattI;
    public Slider ThrusterVert;
    public Slider ThrusterHorz;

    private bool _isSubAssigned;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetRoV(RoVPhysics sub)
    {
        _sub = sub;
        _isSubAssigned = true;
    }

    public void RemoveRoV()
    {
        _isSubAssigned = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_isSubAssigned == false)
        {
            return;
        }

        var pitch = _sub.transform.localEulerAngles.x;
        if (pitch > 180.0f)
        {
            pitch -= 360.0f;
        }else if (pitch < -180.0f)
        {
            pitch += 360.0f;
        }
        AttPitch.localPosition = new Vector3(0, -3.0f+5.88f*pitch, 0);
        //AttPitch.localPosition = new Vector3(0,Mathf.Lerp(-3,175,_sub.transform.localEulerAngles.x/30.0f),0);
        AttRoll.transform.localEulerAngles = new Vector3(0,0,_sub.transform.eulerAngles.z);
        Heading.text = ((int)_sub.transform.eulerAngles.y).ToString();
        Depth.text = ((int)-_sub.transform.position.y).ToString() + "\n m";
        DepthNeedle.transform.localEulerAngles = new Vector3(0,0,Mathf.Lerp(58,-237,(-_sub.transform.position.y)/400.0f));
        
    }
}
