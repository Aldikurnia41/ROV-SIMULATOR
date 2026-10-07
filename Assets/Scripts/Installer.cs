using CesiumForUnity;
using UnityEngine;
using Zenject;

public class Installer : MonoInstaller
{
    public HudBinder HudSystem;
    public CesiumGeoreference CesGeo;
    public Cesium3DTileset CesTile;
    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);
        Container.DeclareSignal<ObjectFoundSignal>();
        Container.DeclareSignal<ObjectOnSightSignal>();
        Container.DeclareSignal<ScenarioCompleteSignal>();

        Container.Bind<HudBinder>().FromInstance(HudSystem);
        Container.Bind<CesiumGeoreference>().FromInstance(CesGeo);
        Container.Bind<Cesium3DTileset>().FromInstance(CesTile);
    }
}

public class ObjectFoundSignal
{
    public string Name;
}

public class ObjectOnSightSignal
{
    public GameObject TheObject;
}
public class ScenarioCompleteSignal
{
    
}