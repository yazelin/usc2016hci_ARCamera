using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class ScreenshotButton : MonoBehaviour {
	
	public GameObject button;
	private string uploadURL = "http://a9634395.xp3.biz/insertImage.php";
	private Int32 unixTimestamp;

	// Use this for initialization
	void Start () {		
		UIEventListener.Get(button).onClick = buttonClick;
		ScreenshotManager.ScreenshotFinishedSaving += readyToUpload;

	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void buttonClick(GameObject button){
		ScreenshotManager.Save ("temp","MyScreenshots",true);
	}
	void readyToUpload(){
		unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		StartCoroutine (Upload ());
		Debug.Log (ScreenshotManager.LastSavedFilePath);
	}

	IEnumerator Upload(){
		// ../abdb/advad/filename.png
		string[] str = ScreenshotManager.LastSavedFilePath.Split ('/');
		string filename = str[str.Length-1];

		byte[] bytes = File.ReadAllBytes (ScreenshotManager.LastSavedFilePath);

		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddField("timeid", unixTimestamp);
		form.AddBinaryData("fileToUpload", bytes, filename, "image/png");

		int retryTime = 0;
		Debug.Log ("開始上傳圖片");
		WWW wwwPNG;
		while(retryTime<10){
			Debug.Log("上傳第"+(retryTime+1)+"次");
			// Upload to a cgi script
			wwwPNG = new WWW(uploadURL, form);
			yield return wwwPNG;
			if (!string.IsNullOrEmpty(wwwPNG.error)) {
				print(wwwPNG.error);
				retryTime++;
			}else {
				Debug.Log ("圖片上傳完成"+','+unixTimestamp+','+wwwPNG.text);
				break;
			}

		}

	}

}
