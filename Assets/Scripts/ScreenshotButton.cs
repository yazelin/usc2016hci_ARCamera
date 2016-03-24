using UnityEngine;
using System.Collections;

public class ScreenshotButton : MonoBehaviour {
	
	public GameObject button;

	// Use this for initialization
	void Start () {
		UIEventListener.Get(button).onClick = buttonClick;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void buttonClick(GameObject button){
		ScreenshotManager.Save ("temp");
	}
}
