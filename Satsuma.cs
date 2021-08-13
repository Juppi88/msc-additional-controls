using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using System.Linq;

// TODO: Missing (Satsuma)
// Dashboard lights (light switch) -> long beam indicator is broken
// -> // Possible action: Action: H.P.A.SetMaterialFloat target go: RearLights , mat: satsuma_needles (UnityEngine.Material), mat id: 0, value: 0.2, val: _Intensity
// Handbrake
// Radio

class Satsuma : IVehicle
{
	public override string Name => "Satsuma";

	// Engine start related objects
	private PlayMakerFSM m_starterFsm;
	private PlayMakerFSM m_ignitionFsm;
	private GameObject m_electricsObj;
	private GameObject m_startKeyObj;
	private GameObject m_startKeyHoleObj;
	private GameObject m_chokeKnobObj;
	private PlayMakerFSM m_chokeKnobFsm;
	private PlayMakerFSM m_chokeFsm;

	// Satsuma lights
	private GameObject m_lightBeamsShort;
	private GameObject m_lightBeamsLong;
	private GameObject m_lightRear;
	private GameObject m_lightMarkers;
	private GameObject m_turnsignalObj;
	private GameObject m_lightKnobObj;
	private Renderer m_gaugesMeshRenderer;
	private Renderer m_gaugeMeshClockRenderer;
	private Renderer m_gaugeMeshTachRenderer;
	private PlayMakerFSM m_lightKnobFsm;
	private PlayMakerFSM m_beamModeFsm;

	// Light related materials
	private Material m_matGauges;
	private Material m_matGaugesLit;
	private Material m_matTach;
	private Material m_matTachLit;

	public GameObject carGameObj { get; private set; }

	public Satsuma()
	{
		carGameObj = GameObject.Find("SATSUMA(557kg, 248)");

		m_startKeyHoleObj = carGameObj.transform.Find("Dashboard/Steering/steering_column2/Ignition/Keys").gameObject;
		m_startKeyObj = m_startKeyHoleObj.transform.Find("Key").gameObject;
		m_electricsObj = carGameObj.transform.FindChild("CarSimulation/Car/Electrics").gameObject;

		var starterObj = carGameObj.transform.Find("CarSimulation/Car/Starter").gameObject;
		m_starterFsm = ActionHelpers.GetGameObjectFsm(starterObj, "Starter");

		var ignitionObj = carGameObj.transform.Find("Dashboard/Steering/steering_column2/IgnitionSatsuma").gameObject;
		m_ignitionFsm = ActionHelpers.GetGameObjectFsm(ignitionObj, "UseNew");

		var chokeObj = carGameObj.transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/Knobs/ButtonsDash/Choke").gameObject;
		m_chokeKnobFsm = ActionHelpers.GetGameObjectFsm(chokeObj, "Use");
		m_chokeFsm = ActionHelpers.GetGameObjectFsm(chokeObj, "Choke");
		m_chokeKnobObj = carGameObj.transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/Knobs/KnobChoke/knob").gameObject;

		// Lights
		var lightKnobObj = carGameObj.transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/Knobs/ButtonsDash/LightModes").gameObject;
		m_lightKnobFsm = ActionHelpers.GetGameObjectFsm(lightKnobObj, "Use");

		var beamModeObj = carGameObj.transform.Find("Dashboard/Steering/steering_column2/BeamMode").gameObject;
		m_beamModeFsm = ActionHelpers.GetGameObjectFsm(beamModeObj, "Use");

		m_lightBeamsShort = carGameObj.transform.FindChild("Electricity/PowerON/BeamsShort").gameObject;
		m_lightBeamsLong = carGameObj.transform.FindChild("Electricity/PowerON/BeamsLong").gameObject;
		m_lightRear = carGameObj.transform.FindChild("Electricity/PowerON/RearLights").gameObject;
		m_lightMarkers = carGameObj.transform.FindChild("Electricity/PowerON/Markers").gameObject;
		m_turnsignalObj = carGameObj.transform.FindChild("Dashboard/Steering/steering_column2/Lever/turnsignal").gameObject;
		m_lightKnobObj = carGameObj.transform.FindChild("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/Knobs/KnobLights/knob").gameObject;

		m_gaugesMeshRenderer = carGameObj.transform.FindChild("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/GaugesMesh").gameObject.GetComponent<Renderer>();
		m_gaugeMeshClockRenderer = carGameObj.transform.FindChild("Dashboard/pivot_dashboard/dashboard(Clone)/pivot_meters/dashboard meters(Clone)/pivot_gauge/clock gauge(Clone)/GaugeMeshClock").gameObject.GetComponent<Renderer>();
		m_gaugeMeshTachRenderer = GameObject.Find("GaugeMeshTach").gameObject.GetComponent<Renderer>();

		// Materials
		var materials = Resources.FindObjectsOfTypeAll<Material>();

		m_matGauges = materials.Single(x => x.name == "satsuma_gauges");
		m_matGaugesLit = materials.Single(x => x.name == "satsuma_gauges_lit");
		m_matTach = materials.Single(x => x.name == "satsuma_tach");
		m_matTachLit = materials.Single(x => x.name == "satsuma_tach_lit");
	}

	public override void SetLightMode(LightMode mode)
	{
		// TODO:
		// Need to enable hi beam indicator (blue light)

		switch (mode)
		{
			case LightMode.Off:
				m_lightBeamsShort.SetActive(false);
				m_lightBeamsLong.SetActive(false);
				m_lightRear.SetActive(false);
				m_lightMarkers.SetActive(false);

				m_lightKnobFsm.FsmVariables.FindFsmInt("Selection").Value = 0;
				m_beamModeFsm.FsmVariables.FindFsmInt("Selection").Value = 0;
				m_beamModeFsm.FsmVariables.FindFsmBool("HighBeam").Value = false;

				m_turnsignalObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
				m_lightKnobObj.transform.localRotation = Quaternion.Euler(0, 0, 0);

				m_gaugesMeshRenderer.material = m_matGauges;
				m_gaugeMeshClockRenderer.material = m_matGauges;
				m_gaugeMeshTachRenderer.material = m_matTach;

				MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.4f, 1, 0, "dash_button");
				ModConsole.Print("Satsuma LightMode Off");
				break;

			case LightMode.Short:
				m_lightBeamsShort.SetActive(true);
				m_lightBeamsLong.SetActive(false);
				m_lightRear.SetActive(true);
				m_lightMarkers.SetActive(true);

				m_lightKnobFsm.FsmVariables.FindFsmInt("Selection").Value = 1;
				m_beamModeFsm.FsmVariables.FindFsmInt("Selection").Value = 1;
				m_beamModeFsm.FsmVariables.FindFsmBool("HighBeam").Value = false;

				m_turnsignalObj.transform.localRotation = Quaternion.Euler(0, 0, 6);
				m_lightKnobObj.transform.localRotation = Quaternion.Euler(0, -45, 0);

				if (m_electricsObj.activeInHierarchy)
				{
					m_gaugesMeshRenderer.material = m_matGaugesLit;
					m_gaugeMeshClockRenderer.material = m_matGaugesLit;
					m_gaugeMeshTachRenderer.material = m_matTachLit;
				}

				MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.4f, 1, 0, "dash_button");
				ModConsole.Print("Satsuma LightMode Short");
				break;

			case LightMode.Long:
				m_lightBeamsShort.SetActive(false);
				m_lightBeamsLong.SetActive(true);
				m_lightRear.SetActive(true);
				m_lightMarkers.SetActive(true);

				MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.4f, 1, 0, "dash_button");
				ModConsole.Print("Satsuma LightMode Long");

				m_lightKnobFsm.FsmVariables.FindFsmInt("Selection").Value = 2;
				m_beamModeFsm.FsmVariables.FindFsmInt("Selection").Value = 2;
				m_beamModeFsm.FsmVariables.FindFsmBool("HighBeam").Value = true;

				m_turnsignalObj.transform.localRotation = Quaternion.Euler(0, 0, -6);
				m_lightKnobObj.transform.localRotation = Quaternion.Euler(0, -90, 0);

				if (m_electricsObj.activeInHierarchy)
				{
					m_gaugesMeshRenderer.material = m_matGaugesLit;
					m_gaugeMeshClockRenderer.material = m_matGaugesLit;
					m_gaugeMeshTachRenderer.material = m_matTachLit;
				}

				m_beamModeFsm.SendEvent("CHECK");
				break;
		}
	}

	public override void PowerOff()
	{
		ModConsole.Print("Satsuma PowerOff");

		m_electricsObj.SetActive(false);
		m_startKeyObj.SetActive(false);

		m_startKeyHoleObj.transform.localRotation = Quaternion.Euler(0, 0, 0);

		if (m_startKeyPos != StartKey.Off)
		{
			MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.5f, 1, 0, "carkeys_out");
			m_startKeyPos = StartKey.Off;
		}

		m_starterFsm.FsmVariables.FindFsmBool("ACC").Value = false;
		m_starterFsm.FsmVariables.FindFsmBool("ShutOff").Value = true;
		m_starterFsm.FsmVariables.FindFsmBool("ElectricsOK").Value = false;
		m_starterFsm.FsmVariables.FindFsmBool("Starting").Value = false;

		m_ignitionFsm.FsmVariables.FindFsmBool("ACC").Value = false;
		m_ignitionFsm.FsmVariables.FindFsmBool("MotorOn").Value = false;
		m_ignitionFsm.FsmVariables.FindFsmFloat("DashVolume").Value = 0;
	}

	public override void PowerOn()
	{
		ModConsole.Print("Satsuma PowerOn");

		m_electricsObj.SetActive(true);
		m_startKeyObj.SetActive(true);

		m_startKeyHoleObj.transform.localRotation = Quaternion.Euler(0, -30, 0);

		if (m_startKeyPos != StartKey.Acc)
		{
			MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.5f, 1, 0, "carkeys_in");
			m_startKeyPos = StartKey.Acc;
		}

		m_starterFsm.FsmVariables.FindFsmBool("ACC").Value = true;
		m_starterFsm.FsmVariables.FindFsmBool("ShutOff").Value = false;
		m_starterFsm.FsmVariables.FindFsmBool("Starting").Value = false;
		m_starterFsm.FsmVariables.FindFsmBool("ElectricsOK").Value = true;

		m_ignitionFsm.FsmVariables.FindFsmBool("ACC").Value = true;
		m_ignitionFsm.FsmVariables.FindFsmFloat("DashVolume").Value = 0.5f;
	}

	public override void CancelStartEngine()
	{
		ModConsole.Print("Satsuma CancelStartEngine");

		m_startKeyHoleObj.transform.localRotation = Quaternion.Euler(0, -30, 0);

		m_startKeyPos = StartKey.Acc;

		m_starterFsm.FsmVariables.FindFsmBool("Starting").Value = false;
		m_starterFsm.SendEvent("KEYUP");
	}

	public override void StartEngine()
	{
		ModConsole.Print("Satsuma StartEngine");

		m_electricsObj.SetActive(true);
		m_startKeyObj.SetActive(true);

		if (m_startKeyPos != StartKey.Start)
		{
			MasterAudio.PlaySound3DAndForget("CarFoley", m_startKeyObj.transform, false, 0.5f, 1, 0, "ignition_keys");
			m_startKeyPos = StartKey.Start;
		}

		m_startKeyHoleObj.transform.localRotation = Quaternion.Euler(0, -60, 0);

		m_starterFsm.FsmVariables.FindFsmBool("ACC").Value = true;
		m_starterFsm.FsmVariables.FindFsmBool("ShutOff").Value = false;
		m_starterFsm.FsmVariables.FindFsmBool("Starting").Value = true;
		m_starterFsm.FsmVariables.FindFsmBool("ElectricsOK").Value = true;

		m_ignitionFsm.FsmVariables.FindFsmBool("MotorOn").Value = true;
	}

	public override void SetChokeOn()
	{
		ModConsole.Print("Satsuma ChokeOn");

		m_chokeFsm.FsmVariables.FindFsmFloat("ChokeLevel").Value = 1;
		m_chokeKnobFsm.FsmVariables.FindFsmFloat("Choke").Value = 1;
		m_chokeKnobFsm.FsmVariables.FindFsmFloat("KnobPos").Value = -0.03f;

		m_chokeKnobObj.transform.localPosition = new Vector3(0, -0.03f, 0);
	}

	public override void SetChokeOff()
	{
		ModConsole.Print("Satsuma ChokeOff");

		m_chokeFsm.FsmVariables.FindFsmFloat("ChokeLevel").Value = 0;
		m_chokeKnobFsm.FsmVariables.FindFsmFloat("Choke").Value = 0;
		m_chokeKnobFsm.FsmVariables.FindFsmFloat("KnobPos").Value = 0;

		m_chokeKnobObj.transform.localPosition = new Vector3(0, 0, 0);
	}
}
