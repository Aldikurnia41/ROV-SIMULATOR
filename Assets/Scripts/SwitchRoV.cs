using Unity.VisualScripting;
using UnityEngine;
using Zenject;

public class SwitchRoV : MonoBehaviour
{
    public string RovName;
    public CableFollowHead Tether;
    [Inject] private HudBinder HudSystem;
    [ContextMenu("Load ROV")]
    public void DebugSwitch()
    {
        Switch(RovName);
    }

    private void Switch(string name)
    {
        //TODO: Use assetbundle
        var loadedrov =  Instantiate(Resources.Load<GameObject>(name));
       
        loadedrov.transform.position = Vector3.zero;
        loadedrov.transform.rotation = Quaternion.identity;
        var rov = loadedrov.GetComponent<RoVPhysics>();
        if (rov.TetherEnd == null)
        {
            Debug.LogWarning($"Cannot found TetherEnd in the ROV {loadedrov.name}");
        }
        else
        {
            //connect spline
            Tether.CableEnd = rov.TetherEnd;
        }

        HudSystem.SetRoV(rov);
    }
}
