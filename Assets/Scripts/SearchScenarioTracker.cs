using System.Collections.Generic;
using System.Linq;
using EPOOutline;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SearchScenarioTracker : MonoBehaviour
{
    [Inject] public SignalBus _signal;
    public Button CaptureButton;
    public List<Transform> Objectives = new List<Transform>();
    private List<Transform> _activeObjectives = new List<Transform>();
    public TMP_Text ObjectiveText;
    private GameObject ObjectOnSight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _activeObjectives = Objectives;
        _signal.Subscribe<ObjectFoundSignal>(OnObjectFound);
        _signal.Subscribe<ObjectOnSightSignal>(OnObjectOnSight);
        CaptureButton.onClick.AddListener(FoundObject);

        foreach (var obj in _activeObjectives)
        {
            var outline = obj.gameObject.AddComponent<Outlinable>();
            outline.OutlineParameters.Color = Color.red;
            outline.AddRenderer(obj.gameObject.GetComponent<Renderer>());
            outline.enabled = false;
        }

        ObjectiveText.text = $"Objective found: {Objectives.Count-_activeObjectives.Count}/{Objectives.Count}";
    }

    private void OnObjectOnSight(ObjectOnSightSignal Obj)
    {
        ObjectOnSight = Obj.TheObject;
        var value = Obj.TheObject != null;
        CaptureButton.gameObject.SetActive(value);
    }

    public void FoundObject()
    {
        _signal.Fire<ObjectFoundSignal>(new ObjectFoundSignal(){Name = ObjectOnSight.name});
        ObjectOnSight.SetActive(false);
        ObjectiveText.text = $"Objective found: {Objectives.Count-_activeObjectives.Count}/{Objectives.Count}";
    }
    private void OnObjectFound(ObjectFoundSignal Obj)
    {
        var found = _activeObjectives.FirstOrDefault(x => x.name == Obj.Name);
        if (found == null)
        {
            Debug.Log("Object already found");
            return;
        }

        _activeObjectives.Remove(found);
        if (_activeObjectives.Count == 0)
            _signal.Fire<ScenarioCompleteSignal>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
