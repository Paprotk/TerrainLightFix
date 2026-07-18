using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Sims3.UI;
using OneShotFunctionTask = Sims3.Gameplay.OneShotFunctionTask;
using Responder = Sims3.UI.Responder;

namespace Arro.tlmf;

public class LightDummy : Task
{
    public bool Perform;
    public GameObject LightDummyObject;
    
    public Vector3 lastPosition;
    public StopWatch moveTimer;
    public float lastMoveTime;

    public LightDummy()
    {
        UIManager.GetSceneWindow().MouseMove -= OnSceneWindowMouseMove;
        UIManager.GetSceneWindow().MouseMove += OnSceneWindowMouseMove;
    }

    public override void Simulate()
    {
        if (Perform && LightDummyObject is { HasBeenDestroyed: false })
        {
            var currentPos = LightDummyObject.Position;
            
            if (currentPos != lastPosition)
            {
                lastPosition = currentPos;
                lastMoveTime = moveTimer.GetElapsedTime();
            }
            else
            {
                if (moveTimer.GetElapsedTime() - lastMoveTime >= 1.0f)
                {
                    World.HandToolDetach();
                    if (GameStates.IsLiveState)
                    {
                        UserToolUtils.OnClose();
                        LiveDragHelperModel.ClearCachedTopDraggedObject();
                    }
                    LightDummyObject.Destroy();
                    LightDummyObject.Dispose();
                    Perform = false;
                    
                    UIManager.GetSceneWindow().MouseMove -= OnSceneWindowMouseMove;
                    
                    if (moveTimer != null)
                    {
                        moveTimer.Stop();
                        moveTimer.Dispose();
                        moveTimer = null;
                    }
                    
                    Simulator.DestroyObject(Main.LightDummyTask);
                    Main.LightDummyTask.Value = ObjectGuid.kInvalidObjectGuidValue;
                    
                    RefreshLotsLightmap();
                }
            }
        }
    }

    private void OnSceneWindowMouseMove(WindowBase sender, UIMouseEventArgs eventArgs)
    {
        var pickArgs = UIManager.GetSceneWindow().GetPickArgs();
        var hit = pickArgs.AsGameObjectHit();
        
        if (IsLotTerrainHit(hit))
        {
            HandleLotTerrain(pickArgs);
        }
    }
    
    public static bool IsLotTerrainHit(GameObjectHit hit)
    {
        return hit.mType == GameObjectHitType.LotTerrain;
    }
    
    public void HandleLotTerrain(ScenePickArgs pickArgs)
    {
        var lotTerrainPos = pickArgs.mWorldPos;
        var hitLot = LotManager.GetLotAtPoint(lotTerrainPos);
        if (hitLot == LotManager.ActiveLot)
        {
            UIManager.GetSceneWindow().MouseMove -= OnSceneWindowMouseMove;
            SpawnDummy();
        }
    }

    private void SpawnDummy()
    {
        var key = new ResourceKey(
            0x00000744UL, //3
            0x319e4f1dU, //1
            0x00000000U //2
        );
        LightDummyObject = (GameObject)GlobalFunctions.CreateObject(
            key,
            Vector3.OutOfWorld,
            0,
            Vector3.UnitZ
        );
        if (LightDummyObject is LightGameObject light)
        {
            light.SwitchLight(false, false);
        }
        LightDummyObject.SetModel(null);
        LightDummyObject.DisableAutonomousInteractions();
        
        World.HandToolAttach(LightDummyObject.ObjectId, false);
        var cameraPosition = CameraController.GetPosition();
        var cameraTarget = CameraController.GetTarget();
        var editedCameraPosition = new Vector3(cameraPosition.x, cameraPosition.y - 0.01f, cameraPosition.z);
        CameraController.SetPositionAndTarget(editedCameraPosition, cameraTarget);
        
        moveTimer = StopWatch.Create(StopWatch.TickStyles.Seconds);
        moveTimer.Start();
        
        lastPosition = LightDummyObject.Position;
        lastMoveTime = moveTimer.GetElapsedTime();
        Perform = true;
    }

    private void RefreshLotsLightmap()
    {
        foreach (Lot lot in LotManager.AllLots)
        {
            var key = new ResourceKey(
                0x00000500UL, //3
                0x319e4f1dU, //1
                0x00000000U //2
            );

            var obj = GlobalFunctions.CreateObject(
                key,
                lot.GetCenterPosition(),
                0,
                Vector3.UnitZ
            );
            obj.SetOpacity(0, 0f);
            if (obj is LightGameObject light)
            {
                light.SwitchLight(false, false);
            }

            Simulator.AddObject(new OneShotFunctionTask(() => { obj.Destroy(); }, StopWatch.TickStyles.Seconds, 1f));
        }
    }
}