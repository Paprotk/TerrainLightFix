using System;
using MonoPatcherLib;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using OneShotFunctionTask = Sims3.Gameplay.OneShotFunctionTask;


namespace Arro.tlmf
{
    [Plugin]
    public class Main
    {
        public static ObjectGuid LightDummyTask;

        static Main()
        {
            World.sOnWorldLoadFinishedEventHandler += OnWorldLoadFinished;
        }

        private static void OnWorldLoadFinished(object sender, EventArgs e)
        {
            Simulator.AddObject(new OneShotFunctionTask(RefreshTerrainLightmap, StopWatch.TickStyles.Seconds, 1f));
        }

        public static void RefreshTerrainLightmap()
        {
            World.LoadHeightMapRelatedData(true);
            LightDummyTask = Simulator.AddObject(new LightDummy());
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

                if (definition != null && (Target.RoomId == 0 || definition.TargetLights == LightGameObject.LightsToChange.ThisHouse))
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

                if (definition != null && (Target.RoomId == 0 || definition.TargetLights == LightGameObject.LightsToChange.ThisHouse))
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
                }
                return true;
            }
        }

        [TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.SetColor))]
        public class SetColor : ImmediateInteraction<Sim, ILight>
        {
            public override bool Run()
            {
                LightGameObject.SetColor.Definition definition = InteractionDefinition as LightGameObject.SetColor.Definition;
                if (definition != null && definition.TargetColor == LightGameObject.LightColor.CustomColor)
                {
                    Vector3 vector = new Vector3(Target.CustomColorRed, Target.CustomColorGreen, Target.CustomColorBlue);
                    LightGameObject.GetCustomColorVec(ref vector);
                    Target.CustomColorRed = vector.x;
                    Target.CustomColorGreen = vector.y;
                    Target.CustomColorBlue = vector.z;
                }

                if (definition != null)
                {
                    switch (definition.TargetLights)
                    {
                        case LightGameObject.LightsToChange.ThisLight:
                            LightGameObject.SetColorLight(Target, definition.TargetColor);
                            break;
                        case LightGameObject.LightsToChange.ThisRoom:
                            LightGameObject.SetColorRoom(Target, definition.TargetColor);
                            break;
                        case LightGameObject.LightsToChange.ThisHouse:
                            LightGameObject.SetColorHouse(Target, definition.TargetColor);
                            break;
                    }

                    if (Target.RoomId == 0 || definition.TargetLights == LightGameObject.LightsToChange.ThisHouse)
                    {
                        RefreshTerrainLightmap();
                    }
                }
                return true;
            }
        }
        
        [TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.SetIntensity))]
        public class SetIntensity : ImmediateInteraction<Sim, LightGameObject>
        {
            public override bool Run()
            {
                LightGameObject.SetIntensity.Definition definition =
                    InteractionDefinition as LightGameObject.SetIntensity.Definition;
                if (definition != null && definition.TargetIntensity == LightGameObject.LightIntensity.CustomIntensity)
                {
                    Target.mCustomIntensity = ParserFunctions.ParseFloat(StringInputDialog.Show(
                            LightGameObject.LocalizeString("SettingCustomIntensity", new object[0]),
                            LightGameObject.LocalizeString("CustomIntensityDialog", new object[]
                            {
                                LightGameObject.kIntensityMaxCustomValue.ToString()
                            }), Target.mCustomIntensity.ToString(), StringInputDialog.Validation.FloatNumber),
                        LightGameObject.kIntensityNormal);
                    if (Target.mCustomIntensity < 0f)
                    {
                        Target.mCustomIntensity = 0f;
                    }
                    else if (Target.mCustomIntensity > LightGameObject.kIntensityMaxCustomValue)
                    {
                        Target.mCustomIntensity = LightGameObject.kIntensityMaxCustomValue;
                    }
                }

                if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisLight)
                {
                    Target.SetLightIntensity(definition.TargetIntensity);
                }
                else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisRoom)
                {
                    Target.SetIntensityRoom(definition.TargetIntensity);
                }
                else if (definition != null && definition.TargetLights == LightGameObject.LightsToChange.ThisHouse)
                {
                    Target.SetIntensityHouse(definition.TargetIntensity);
                }
                
                if (definition != null && (Target.RoomId == 0 || definition.TargetLights == LightGameObject.LightsToChange.ThisHouse))
                {
                    RefreshTerrainLightmap();
                }
                return true;
            }
        }

        [TypePatch(typeof(Sims3.Gameplay.Abstracts.LightGameObject.ToggleBlackLight))]
        public class ToggleBlackLight : ImmediateInteraction<Sim, LightGameObject>
        {
            public override bool Run()
            {
                LightGameObject.ToggleBlackLight.Definition definition =
                    InteractionDefinition as LightGameObject.ToggleBlackLight.Definition;
                if (definition != null)
                {
                    Target.SetBlackLights(!Target.IsBlackLight, definition.TargetLights);

                    if (Target.RoomId == 0 || definition.TargetLights == LightGameObject.LightsToChange.ThisHouse)
                    {
                        RefreshTerrainLightmap();
                    }
                }
                return true;
            }
        }
    }
}