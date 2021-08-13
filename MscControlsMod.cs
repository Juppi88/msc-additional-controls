using MSCLoader;
using UnityEngine;
using HutongGames.PlayMaker;

#if DEBUG
using System.IO;
using System.Text;
using HutongGames.PlayMaker.Actions;
#endif

namespace MscButtonBox
{
	public class MscButtonBox : Mod
	{
		public override string ID => "MscControlsMod";
		public override string Name => "MSC Extra Control Mod";
		public override string Author => "Juppi";
		public override string Version => "0.0.1";
		public override bool UseAssetsFolder => false;

		private readonly Keybind m_bindPowerAcc = new Keybind("ignAcc", "Vehicle power on", KeyCode.Joystick2Button12);
		private readonly Keybind m_bindPowerStart = new Keybind("ignStart", "Vehicle start", KeyCode.Joystick2Button13);
		private readonly Keybind m_bindChoke = new Keybind("choke", "Choke on", KeyCode.Joystick2Button18);
		private readonly Keybind m_bindLightsOff = new Keybind("lightsOff", "Lights off", KeyCode.Joystick2Button15);
		private readonly Keybind m_bindLightsOnShort = new Keybind("lightsOnShort", "Lights on (short beam)", KeyCode.Joystick2Button16);
		private readonly Keybind m_bindLightsOnLong = new Keybind("lightsOnLong", "Lights on (long beam)", KeyCode.Joystick2Button17);

#if DEBUG
		private Keybind m_bindDumpObjects = new Keybind("dump", "Dump scene objects", KeyCode.F3);
		private Transform m_rayHitTransform;
#endif

		private Satsuma m_satsuma;

		public override void OnLoad()
		{
			m_satsuma = new Satsuma();

			ModConsole.Print("MscButtonBox loaded");
		}

		public override void ModSettings()
		{
			Keybind.Add(this, m_bindPowerAcc);
			Keybind.Add(this, m_bindPowerStart);
			Keybind.Add(this, m_bindChoke);
			Keybind.Add(this, m_bindLightsOff);
			Keybind.Add(this, m_bindLightsOnShort);
			Keybind.Add(this, m_bindLightsOnLong);
		}

		public override void Update()
		{
#if DEBUG
			GetObjectUnderCursor();

			if (m_bindDumpObjects.GetKeybindDown())
			{
				DumpSceneObjects();
			}
#endif

			var currentVehicle = GetCurrentVehicle();

			if (currentVehicle == null)
			{
				return;
			}

			currentVehicle.Update();

			// Process ignition key binds.
			if (m_bindPowerStart.GetKeybindUp())
			{
				currentVehicle.CancelStartEngine();
			}
			else if (m_bindPowerStart.GetKeybind())
			{
				currentVehicle.StartEngine();
			}
			if (m_bindPowerAcc.GetKeybindDown())
			{
				currentVehicle.PowerOn();
			}
			else if (m_bindPowerAcc.GetKeybindUp())
			{
				currentVehicle.PowerOff();
			}

			// Process light modes.
			if (m_bindLightsOff.GetKeybindDown())
			{
				currentVehicle.SetLightMode(IVehicle.LightMode.Off);
			}
			else if (m_bindLightsOnShort.GetKeybindDown())
			{
				currentVehicle.SetLightMode(IVehicle.LightMode.Short);
			}
			else if (m_bindLightsOnLong.GetKeybindDown())
			{
				currentVehicle.SetLightMode(IVehicle.LightMode.Long);
			}

			// Process Satsuma choke.
			if (m_bindChoke.GetKeybindDown())
			{
				currentVehicle.SetChokeOn();
			}
			if (m_bindChoke.GetKeybindUp())
			{
				currentVehicle.SetChokeOff();
			}
		}

		private IVehicle GetCurrentVehicle()
		{
			switch (FsmVariables.GlobalVariables.FindFsmString("PlayerCurrentVehicle").Value)
			{
				case "Satsuma":
					return m_satsuma;

				default:
					return null;
			}
		}

#if DEBUG
		public override void OnGUI()
		{
			if (m_rayHitTransform == null)
			{
				return;
			}

			var width = 400;
			var height = 25;

			GUI.Label(
				new Rect(Screen.width - width, Screen.height - height, width, height),
				Helpers.GetObjectHierarchy(m_rayHitTransform.gameObject)
			);
		}

		private void GetObjectUnderCursor()
		{
			var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

			if (Physics.Raycast(ray, out RaycastHit hit, 100.0f))
			{
				m_rayHitTransform = hit.transform;
			}
		}

		private void DumpSceneObjects()
		{
			// Dump all active scene gameobjects into a file.
			var objs = Object.FindObjectsOfType<GameObject>();
			var sb = new StringBuilder();

			foreach (var obj in objs)
			{
				var sbChildren = new StringBuilder();

				for (int i = 0; i < obj.transform.childCount; i++)
				{
					sbChildren.Append($"{obj.transform.GetChild(i).name}({obj.transform.GetChild(i).gameObject.activeInHierarchy}), ");
				}

				sb.AppendLine($"{obj.name} ({Helpers.GetObjectHierarchy(obj)}) - active: {obj.activeInHierarchy}, children: {sbChildren}");

				var components = obj.GetComponents<Component>();

				foreach (var component in components)
				{
					sb.AppendLine($" -- {component.GetType()}");
					if (component.GetType() == typeof(PlayMakerFSM))
					{
						var fsm = (PlayMakerFSM)component;
						sb.AppendLine($"    FSM Name: {fsm.FsmName}, Description: {fsm.FsmDescription}");

						// FSM states, transitions and actions
						foreach (var state in fsm.FsmStates)
						{
							sb.AppendLine($"      State: {state.Name}");
							foreach (var action in state.Actions)
							{
								var typeString = action.GetType().ToString().Replace("HutongGames.PlayMaker.Actions.", "H.P.A.");

								if (action.GetType() == typeof(MousePickEvent))
								{
									sb.AppendLine($"        Action: {typeString} {((MousePickEvent)action).GameObject.GameObject.Name}");
								}
								else if (action.GetType() == typeof(SetBoolValue))
								{
									sb.AppendLine($"        Action: {typeString} {((SetBoolValue)action).boolVariable.Name} {((SetBoolValue)action).boolValue.Value}");
								}
								else if (action.GetType() == typeof(GetFsmBool))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((GetFsmBool)action).fsmName.Value}, go: {((GetFsmBool)action).gameObject.GameObject.Name} {((GetFsmBool)action).variableName.Value}");
								}
								else if (action.GetType() == typeof(LoadBool))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((LoadBool)action).uniqueTag.Value}");
								}
								else if (action.GetType() == typeof(SetFsmBool))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((SetFsmBool)action).fsmName.Value}, go: {((SetFsmBool)action).gameObject.GameObject.Name} {((SetFsmBool)action).variableName.Value} {((SetFsmBool)action).setValue.Value}");
								}
								else if (action.GetType() == typeof(SetStringValue))
								{
									sb.AppendLine($"        Action: {typeString} {((SetStringValue)action).stringVariable.Name} {((SetStringValue)action).stringValue.Value}");
								}
								else if (action.GetType() == typeof(StringCompare))
								{
									sb.AppendLine($"        Action: {typeString} {((StringCompare)action).stringVariable.Name} = {((StringCompare)action).compareTo.Value}, eq: {(((StringCompare)action).equalEvent != null ? ((StringCompare)action).equalEvent.Name : string.Empty)}, not eq: {(((StringCompare)action).notEqualEvent != null ? ((StringCompare)action).notEqualEvent.Name : string.Empty)}");
								}
								else if (action.GetType() == typeof(GetFsmFloat))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((GetFsmFloat)action).fsmName.Value}, go: {((GetFsmFloat)action).gameObject.GameObject.Name} {((GetFsmFloat)action).variableName.Value}");
								}
								else if (action.GetType() == typeof(GetFsmInt))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((GetFsmInt)action).fsmName.Value}, go: {((GetFsmInt)action).gameObject.GameObject.Name} {((GetFsmInt)action).variableName.Value}");
								}
								else if (action.GetType() == typeof(SetFloatValue))
								{
									sb.AppendLine($"        Action: {typeString} {((SetFloatValue)action).floatVariable.Name} {((SetFloatValue)action).floatValue.Value}");
								}
								else if (action.GetType() == typeof(SetIntValue))
								{
									sb.AppendLine($"        Action: {typeString} {((SetIntValue)action).intVariable.Name} {((SetIntValue)action).intValue.Value}");
								}
								else if (action.GetType() == typeof(SetFsmFloat))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((SetFsmFloat)action).fsmName.Value}, go: {((SetFsmFloat)action).gameObject.GameObject.Name} {((SetFsmFloat)action).variableName.Value} {((SetFsmFloat)action).setValue.Value}");
								}
								else if (action.GetType() == typeof(SetFsmInt))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((SetFsmInt)action).fsmName.Value}, go: {((SetFsmInt)action).gameObject.GameObject.Name} {((SetFsmInt)action).variableName.Value} {((SetFsmInt)action).setValue.Value}");
								}
								else if (action.GetType() == typeof(ActivateGameObject))
								{
									sb.AppendLine($"        Action: {typeString} {(((ActivateGameObject)action).gameObject.GameObject.Value != null ? ((ActivateGameObject)action).gameObject.GameObject.Value.name : ((ActivateGameObject)action).gameObject.GameObject.Name)} {((ActivateGameObject)action).activate}");
								}
								else if (action.GetType() == typeof(SetRotation))
								{
									sb.AppendLine($"        Action: {typeString} {(((SetRotation)action).gameObject.GameObject.Value != null ? ((SetRotation)action).gameObject.GameObject.Value.name : ((SetRotation)action).gameObject.GameObject.Name)} {((SetRotation)action).xAngle} {((SetRotation)action).yAngle} {((SetRotation)action).zAngle}");
								}
								else if (action.GetType() == typeof(SetPosition))
								{
									sb.AppendLine($"        Action: {typeString} {(((SetPosition)action).gameObject.GameObject.Value != null ? ((SetPosition)action).gameObject.GameObject.Value.name : ((SetPosition)action).gameObject.GameObject.Name)} {((SetPosition)action).x} {((SetPosition)action).y} {((SetPosition)action).z}");
								}
								else if (action.GetType() == typeof(Wait))
								{
									sb.AppendLine($"        Action: {typeString} {((Wait)action).time.Value}, event: {((Wait)action).finishEvent.Name}");
								}
								else if (action.GetType() == typeof(EnableFSM))
								{
									sb.AppendLine($"        Action: {typeString} fsm: {((EnableFSM)action).fsmName.Value}, go: {(((EnableFSM)action).gameObject.GameObject.Value != null ? ((EnableFSM)action).gameObject.GameObject.Value.name : ((EnableFSM)action).gameObject.GameObject.Name)}, enable: {((EnableFSM)action).enable.Value}");
								}
								else if (action.GetType() == typeof(SetProperty))
								{
									sb.AppendLine($"        Action: {typeString} {((SetProperty)action).targetProperty.TargetTypeName}.{((SetProperty)action).targetProperty.PropertyName}");
								}
								else if (action.GetType() == typeof(BoolTest))
								{
									sb.AppendLine($"        Action: {typeString} {((BoolTest)action).boolVariable.Name} true: {(((BoolTest)action).isTrue != null ? ((BoolTest)action).isTrue.Name : string.Empty)}, false: {(((BoolTest)action).isFalse != null ? ((BoolTest)action).isFalse.Name : string.Empty)}");
								}
								else if (action.GetType() == typeof(BoolAllTrue))
								{
									var test = (BoolAllTrue)action;
									var sb2 = new StringBuilder();

									foreach (var v in test.boolVariables)
									{
										sb2.Append($"{v.Name}, ");
									}

									sb.AppendLine($"        Action: {typeString} send event: {(test.sendEvent != null ? test.sendEvent.Name : "???")}, store: {(test.storeResult != null ? test.storeResult.Name : "")} -- bools: {sb2}");
								}
								else if (action.GetType() == typeof(GetMouseButtonUp))
								{
									sb.AppendLine($"        Action: {typeString} {((GetMouseButtonUp)action).button}, event: {((GetMouseButtonUp)action).sendEvent.Name}");
								}
								else if (action.GetType() == typeof(MasterAudioPlaySound))
								{
									sb.AppendLine($"        Action: {typeString} snd grp: {((MasterAudioPlaySound)action).soundGroupName.Value}, variation: {((MasterAudioPlaySound)action).variationName.Value}, volume: {((MasterAudioPlaySound)action).volume.Value}, delay: {((MasterAudioPlaySound)action).delaySound.Value}, attach to go: {((MasterAudioPlaySound)action).attachToGameObject}, use this loc: {((MasterAudioPlaySound)action).useThisLocation.Value}, use fixed pitch: {((MasterAudioPlaySound)action).useFixedPitch.Value}, fixed pitch: {((MasterAudioPlaySound)action).fixedPitch.Value}");
								}
								else if (action.GetType() == typeof(SendEventByName))
								{
									sb.AppendLine($"        Action: {typeString} target go: {(((SendEventByName)action).eventTarget.gameObject.GameObject.Value != null ? ((SendEventByName)action).eventTarget.gameObject.GameObject.Value.name : ((SendEventByName)action).eventTarget.gameObject.GameObject.Name)}, fsm: {((SendEventByName)action).eventTarget.fsmName} event: {((SendEventByName)action).sendEvent.Value}");
								}
								else if (action.GetType() == typeof(SetMaterial))
								{
									sb.AppendLine($"        Action: {typeString} target go: {(((SetMaterial)action).gameObject.GameObject.Value != null ? ((SetMaterial)action).gameObject.GameObject.Value.name : ((SetMaterial)action).gameObject.GameObject.Name)}, mat: {((SetMaterial)action).material.Value}, mat id: {((SetMaterial)action).materialIndex.Value}");
								}
								else if (action.GetType() == typeof(SetMaterialFloat))
								{
									sb.AppendLine($"        Action: {typeString} target go: {Helpers.GetActionTargetName(((SetMaterialFloat)action).gameObject, action.Owner)} {(((SetMaterialFloat)action).gameObject.GameObject.Value != null ? ((SetMaterialFloat)action).gameObject.GameObject.Value.name : ((SetMaterialFloat)action).gameObject.GameObject.Name)}, mat: {((SetMaterialFloat)action).material.Value}, mat id: {((SetMaterialFloat)action).materialIndex.Value}, value: {((SetMaterialFloat)action).floatValue.Value}, val: {((SetMaterialFloat)action).namedFloat.Value}");
								}
								else
								{
									sb.AppendLine($"        Action: {typeString}");
								}
							}
							foreach (var x in state.Transitions)
							{
								sb.AppendLine($"        Transition: {x.EventName} -> {x.ToState}");
							}
						}

						// FSM variables
						foreach (var x in fsm.FsmVariables.BoolVariables)
						{
							sb.AppendLine($"    Var (b): {x.Name}");
						}
						foreach (var x in fsm.FsmVariables.IntVariables)
						{
							sb.AppendLine($"    Var (i): {x.Name}");
						}
						foreach (var x in fsm.FsmVariables.FloatVariables)
						{
							sb.AppendLine($"    Var (f): {x.Name}");
						}
						foreach (var x in fsm.FsmVariables.StringVariables)
						{
							sb.AppendLine($"    Var (s): {x.Name}");
						}
					}
				}
			}

			File.WriteAllText("SCENEOBJECTS.txt", sb.ToString());

			ModConsole.Print("Scene objects dumped");
		}

		private void DumpFsmData()
		{
			// Dump global FSM events and variables to file
			var sb = new StringBuilder();

			sb.AppendLine("PlayMaker events");
			foreach (var e in PlayMakerGlobals.Instance.Events)
			{
				sb.AppendLine(e);
			}

			sb.AppendLine();
			sb.AppendLine("------> PlayMaker bool variables");
			foreach (var x in PlayMakerGlobals.Instance.Variables.BoolVariables)
			{
				sb.AppendLine($"{x.Name} -> {x.Value}");
			}

			sb.AppendLine();
			sb.AppendLine("------> PlayMaker float variables");
			foreach (var x in PlayMakerGlobals.Instance.Variables.FloatVariables)
			{
				sb.AppendLine($"{x.Name} -> {x.Value}");
			}

			sb.AppendLine();
			sb.AppendLine("------> PlayMaker string variables");
			foreach (var x in PlayMakerGlobals.Instance.Variables.StringVariables)
			{
				sb.AppendLine($"{x.Name} -> {x.Value}");
			}

			sb.AppendLine();
			sb.AppendLine("------> PlayMaker int variables");
			foreach (var x in PlayMakerGlobals.Instance.Variables.IntVariables)
			{
				sb.AppendLine($"{x.Name} -> {x.Value}");
			}

			File.WriteAllText("FSMEVENTS.txt", sb.ToString());
		}
#endif
	}
}
