using System;
using EPOOutline;
using UnityEngine;
using Zenject;

public class RovViewTrigger : MonoBehaviour
{
    [Inject] public SignalBus _signal;
    private void OnTriggerEnter(Collider other)
    {
        other.GetComponent<Outlinable>().enabled = true;
        _signal.Fire<ObjectOnSightSignal>(new ObjectOnSightSignal(){TheObject = other.gameObject});
    }

    private void OnTriggerExit(Collider other)
    {
        other.GetComponent<Outlinable>().enabled = false;
        _signal.Fire<ObjectOnSightSignal>(new ObjectOnSightSignal(){TheObject = null});
    }
}
