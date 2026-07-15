using System;
using MonoPatcherLib;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.SimIFace;


namespace Arro.tlmf
{
	[Plugin]
	public class Main
	{
		[Tunable]
        public static bool kInstantiator = false;
		
		static Main()
		{
			World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
		}

		private static void OnWorldLoadFinished(object sender, EventArgs e)
		{
			World.LoadHeightMapRelatedData(true); // IDK why it works <pineapple emoji>
			Simulator.AddObject(new OneShotFunctionTask(RefreshTerrainLightmap, StopWatch.TickStyles.Seconds, 5f));
		}
		
		public static void RefreshTerrainLightmap()
		{
			var cameraPosition =  CameraController.GetPosition();
			var cameraTarget = CameraController.GetTarget();
			CameraController.Instance.EnableMapViewMode(0.0001f);
			Simulator.AddObject(new OneShotFunctionTask(() => {CameraController.SetPositionAndTarget(cameraPosition, cameraTarget);}, StopWatch.TickStyles.Seconds, 0.1f));
		}

		public static void RefreshLotLightmap()
		{
			var currentLotDisplayLevel = LotManager.ActiveLot.CurrentLotDisplayLevel;
			var canDisplayLevelUp = LotManager.ActiveLot.CanLevelUp(false);
			var canDisplayLevelDown = LotManager.ActiveLot.CanLevelDown(false);
			
			if (canDisplayLevelUp)
			{
				LotManager.ActiveLot.SetDisplayLevel(currentLotDisplayLevel + 1);
			}
			else if (canDisplayLevelDown)
			{
				LotManager.ActiveLot.SetDisplayLevel(currentLotDisplayLevel - 1);
			}
			else //Empty lot
			{
				var lot = LotManager.ActiveLot;
				var center = lot.GetCenterPosition();

				var key = new ResourceKey(
					0x00000500UL, //3
					0x319e4f1dU, //1
					0x00000000U //2
				);

				var obj = GlobalFunctions.CreateObject(
					key,
					center,
					0,
					Vector3.UnitZ
				);
				obj.SetOpacity(0, 0f);
				if (obj is LightGameObject light)
				{
					light.SwitchLight(false, false);
				}
				Simulator.AddObject(new OneShotFunctionTask(() => {obj.Destroy();}, StopWatch.TickStyles.Seconds, 1f));
			}
			Simulator.AddObject(new OneShotFunctionTask(() => {LotManager.ActiveLot.SetDisplayLevel(currentLotDisplayLevel);}, StopWatch.TickStyles.Seconds, 0.1f));
		}

		[TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.TurnOff))]
		public class TurnOff : ImmediateInteraction<Sim, IUserControlledLight>
		{
			public override bool Run()
			{
				var definition =
					InteractionDefinition as LightGameObject.TurnOff.Definition;
				if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisLight)
				{
					Target.SwitchLight(false, true);
				}
				else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisRoom)
				{
					Target.SwitchLightsInRoom(false, true);
				}
				else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisHouse)
				{
					Target.SwitchLightsInHouse(false, true);
				}

				if (Target.RoomId == 0)
				{
					RefreshTerrainLightmap();
				}
				return true;
			}
		}
		
		[TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.TurnOn))]
		public class TurnOn : ImmediateInteraction<Sim, IUserControlledLight>
		{
			public override bool Run()
			{
				var definition = InteractionDefinition as LightGameObject.TurnOn.Definition;
				if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisLight)
				{
					Target.SwitchLight(true, true);
				}
				else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisRoom)
				{
					Target.SwitchLightsInRoom(true, true);
				}
				else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisHouse)
				{
					Target.SwitchLightsInHouse(true, true);
				}
				if (Target.RoomId == 0)
				{
					RefreshTerrainLightmap();
				}
				return true;
			}
		}
		
		[TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.ToggleOnOff))]
		public class ToggleOnOff : ImmediateInteraction<IActor, LightGameObject>
		{
			public override bool Run()
			{
				Target.SwitchLight(!Target.IsLightOn(), false);
				if (Target.RoomId == 0)
				{
					RefreshTerrainLightmap();
					RefreshLotLightmap();
				}
				return true;
			}
		}
	}
}