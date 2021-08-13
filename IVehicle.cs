public abstract class IVehicle
{
	public enum StartKey
	{
		Off,
		Acc,
		Start
	}

	public enum LightMode
	{
		Off,
		Short,
		Long
	}

	protected StartKey m_startKeyPos;

	public abstract string Name { get; }

	public virtual void Update() { }
	public abstract void PowerOff();
	public abstract void PowerOn();
	public abstract void StartEngine();
	public abstract void CancelStartEngine();
	public virtual void SetChokeOn() { }
	public virtual void SetChokeOff() { }
	public virtual void SetLightMode(LightMode mode) { }
}
