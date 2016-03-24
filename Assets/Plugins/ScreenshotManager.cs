#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioSource))]
public class ScreenshotManager : MonoBehaviour {

    static ScreenshotManager Instance
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("Screenshot Manager", typeof(ScreenshotManager)).GetComponent<ScreenshotManager>();
                instance.Initialize();
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

	public static bool IsShutting
	{
		get;
		private set;
	}

    public static string LastSavedFilePath = string.Empty;
	public static event Action ScreenshotFinishedSaving;
	public static event Action ImageFinishedSaving;

    static ScreenshotManager instance;

    const int ShutterTextureCount = 6;
    const float ShutterDuration = 0.6f;
	//to most top GUI layer
	public static int guiDepth = -999;
    float lastShutterTime = float.MinValue;
    AudioClip shutterAudioClip;
    Rect screenRect;
    Texture2D[] shutterTextures;
	
	[DllImport("__Internal")]
    private static extern bool saveToGallery( string path );

    void Initialize()
    {
        shutterAudioClip = (AudioClip)Resources.Load("Shutter/camera-shutter");
        shutterTextures = new Texture2D[ShutterTextureCount * 2];
        for (int i = 0; i < ShutterTextureCount; i++)
        {
            shutterTextures[i] = (Texture2D)Resources.Load("Shutter/shutter_" + i.ToString());
            shutterTextures[shutterTextures.Length - 1 - i] = (Texture2D)Resources.Load("Shutter/shutter_" + i.ToString());
        }
        screenRect = new Rect(0, 0, Screen.width, Screen.height);
    }

    void OnGUI()
    {
		GUI.depth = guiDepth;
		
        if (Time.time < lastShutterTime + ShutterDuration)
        {
			
            GUI.DrawTexture(screenRect, shutterTextures[(int)Mathf.Lerp(0,shutterTextures.Length - 1, Mathf.InverseLerp(lastShutterTime, lastShutterTime + ShutterDuration, Time.time))]);
        }
    }

	IEnumerator Shutter()
	{
		while (IsShutting)
			yield return null;
		
		GetComponent<AudioSource>().PlayOneShot(shutterAudioClip);
		lastShutterTime = Time.time;
	}

    public static void Save(string fileName, string albumName = "MyScreenshots", bool callback = false, bool shutter = true)
    {

        Instance.StartCoroutine(SaveCoroutine(fileName, albumName, callback));
        if (shutter)
            Instance.StartCoroutine(Instance.Shutter());
    }

    public static void SaveExisting(string filePath, bool callback = false, bool shutter = true)
    {
        Instance.StartCoroutine(SaveExistingCoroutine(filePath, callback));
        if (shutter)
            Instance.StartCoroutine(Instance.Shutter());
    }
	
	static IEnumerator SaveCoroutine(string fileName, string albumName = "MyScreenshots", bool callback = false)
	{
		//Lock the GUI
		IsShutting = true;
		yield return new WaitForFixedUpdate();

		bool photoSaved = false;
		
		string date = System.DateTime.Now.ToString("dd-MM-yy");
		
		ScreenshotManager.ScreenShotNumber++;
		
		string screenshotFilename = fileName + "_" + ScreenshotManager.ScreenShotNumber + "_" + date + ".png";
		
		Debug.Log("Save screenshot " + screenshotFilename); 
		
		#if UNITY_IPHONE
		
			if(Application.platform == RuntimePlatform.IPhonePlayer) 
			{
				Debug.Log("iOS platform detected");
				
				string iosPath = Application.persistentDataPath + "/" + screenshotFilename;
		
				Application.CaptureScreenshot(screenshotFilename);

				//Screenshot captured, release GUI lock
				yield return new WaitForEndOfFrame();
				IsShutting = false;
				
				while(!photoSaved) 
				{
					photoSaved = saveToGallery( iosPath );
					
					yield return new WaitForSeconds(.5f);
				}
			
				iPhone.SetNoBackupFlag( iosPath );
				
				LastSavedFilePath = iosPath;

			
			} else {

				Application.CaptureScreenshot(screenshotFilename);
				//Screenshot captured, release GUI lock
				yield return new WaitForEndOfFrame();
				IsShutting = false;
				
				yield return new WaitForSeconds(.5f);
				LastSavedFilePath = Application.dataPath + "/../" + screenshotFilename;

			}
			
		#elif UNITY_ANDROID
		
		if(Application.platform == RuntimePlatform.Android) 
		{
			Debug.Log("Android platform detected");
			
			string androidPath = "/../../../../DCIM/" + albumName + "/" + screenshotFilename;
			//string androidPath = "/" + albumName + "/" + screenshotFilename;
			string path = Application.persistentDataPath + androidPath;
			string pathonly = Path.GetDirectoryName(path);
			Directory.CreateDirectory(pathonly);
			Application.CaptureScreenshot(androidPath);
			
			//Screenshot captured, release GUI lock
			yield return new WaitForEndOfFrame();
			IsShutting = false;
			
			AndroidJavaClass obj = new AndroidJavaClass("com.ryanwebb.androidscreenshot.MainActivity");
			
			while(!photoSaved) 
			{
				photoSaved = obj.CallStatic<bool>("scanMedia", path);
				
				yield return new WaitForSeconds(.5f);
			}
			
			LastSavedFilePath = path;
			
		} else {
			
			Application.CaptureScreenshot(screenshotFilename);
			//Screenshot captured, release GUI lock
			yield return new WaitForEndOfFrame();
			IsShutting = false;
			
			yield return new WaitForSeconds(.5f);
			LastSavedFilePath = Application.dataPath + "/../" + screenshotFilename;
			
		}
		#else
		
		while(!photoSaved) 
		{
			yield return new WaitForSeconds(.5f);
			
			Debug.Log("Screenshots only available in iOS/Android mode!");
			
			photoSaved = true;
		}
		
		#endif
		
		if(callback)
			ScreenshotFinishedSaving();
	}
	
	
	static IEnumerator SaveExistingCoroutine(string filePath, bool callback = false)
	{
		bool photoSaved = false;
		
		Debug.Log("Save existing file to gallery " + filePath);

		#if UNITY_IPHONE
		
			if(Application.platform == RuntimePlatform.IPhonePlayer) 
			{
				Debug.Log("iOS platform detected");
				
				while(!photoSaved) 
				{
					photoSaved = saveToGallery( filePath );
					
					yield return new WaitForSeconds(.5f);
				}
			
				iPhone.SetNoBackupFlag( filePath );
			}
			
		#elif UNITY_ANDROID	
				
			if(Application.platform == RuntimePlatform.Android) 
			{
				Debug.Log("Android platform detected");

				AndroidJavaClass obj = new AndroidJavaClass("com.ryanwebb.androidscreenshot.MainActivity");
					
				while(!photoSaved) 
				{
					photoSaved = obj.CallStatic<bool>("scanMedia", filePath);
							
					yield return new WaitForSeconds(.5f);
				}
			
			}
		
		#else
			
			while(!photoSaved) 
			{
				yield return new WaitForSeconds(.5f);
		
				Debug.Log("Save existing file only available in iOS/Android mode!");

				photoSaved = true;
			}
		
		#endif
		
		if(callback)
			ImageFinishedSaving();
	}
	
	
	public static int ScreenShotNumber 
	{
		set { PlayerPrefs.SetInt("screenShotNumber", value); }
	
		get { return PlayerPrefs.GetInt("screenShotNumber"); }
	}
}
