using UnityEngine;
using System.Collections.Generic;

public interface IListener{
	void OnNotify(IEvent e);
}

public enum Interest { None, Crush, OnStep };



public class EventManager {

	List<ListenerRegistration> listeners;
	public EventManager() {
		listeners = new List<ListenerRegistration>();
	}

	public class ListenerRegistration {
		public Interest interestCode;
		public IListener listener;
		public ListenerRegistration(IListener listener, Interest interest) {
			this.interestCode = interest;
			this.listener = listener;
		}
	}

	public void RegisterListener(IListener listener, Interest interest) {
		listeners.Add(new ListenerRegistration(listener, interest));
	}

	public void SendEvent(IEvent e) {
		//Debug.Log("Sending event:" + e.code);
		foreach(ListenerRegistration listener in listeners) {
			if(listener.listener == null) {
				Debug.LogError("One listener is null, please remove from the list");
			}
			//Debug.Log("Event: " + e.code + " ListenerInterest:" + listener.interestCode);
			if(e.code == listener.interestCode) {
				listener.listener.OnNotify(e);
			}
		}
	}
}

public class IEvent {
	public Interest code = Interest.None;

}

public class CrushEvent : IEvent {
	public Transform transform;
	public EntityBase crusher;
	public CrushEvent(Transform t, EntityBase c) {
		crusher = c;
		transform = t;
		code = Interest.Crush;
	}
}

public class StepEvent : IEvent {
	public Giantess gts;
	public AudioSource audio;
	public Vector3 position;
	public StepEvent(Giantess gts, Vector3 position, AudioSource audio) {
		this.gts = gts;
		this.position = position;
		this.audio = audio;
		code = Interest.OnStep;
	}

}