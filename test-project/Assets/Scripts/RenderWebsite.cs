// Copyright (c) 2010 Jeroen Dierckx - Expertise Centre for Digital Media. All Rights reserved.
// Part of this source code is developed for the flemish (Belgian) OSMA project (http://osma.phl.be/)
//
// Contributors (Unity forum usernames): reissgrant, agentdm
//
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;

public class RenderWebsite : MonoBehaviour
{
	public int width = 512;
	public int height = 512;
	public string url = "http://www.google.com";
	public string navHookUrl = "http://url_i_am_looking_for.com";
	public bool interactive = true;
	public bool transparency = false;
	public bool waitForTitle = true;
	public bool initBerkelium = true;
	
	private Texture2D m_Texture;
	private Color[] m_Pixels;
	private GCHandle m_PixelsHandle;
	private int m_TextureID;
	private bool m_BrowserReady;
	
	private bool m_IsGuiTexture;
	private Rect texRect;
	private float texScaleX;
	private float texScaleY;
	private Vector3 lastMousePos;
	
	private bool m_HookUrlWasRequested;
	private string m_HookUrl;

	private UnityBerkelium.SetPixelsFunc m_setPixelsFunc;
	private UnityBerkelium.ApplyTextureFunc m_applyTextureFunc;
	private UnityBerkelium.ScrollRectFunc m_scrollRectFunc;
	private UnityBerkelium.ExternalHostFunc m_externalHostFunc;
	private UnityBerkelium.NavHookCb m_navHookCb;
	private UnityBerkelium.LoadCb m_loadCb;
	
	private Component m_eventSubscriber;
	
	
    void Start ()
	{
		if(initBerkelium)
		{
			string appDir = "/tmp/webcache";
			
#if UNITY_STANDALONE_OSX
			appDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
			appDir = appDir + "/Library/Application Support/MyAppName/webcache";
#endif
			
#if UNITY_STANDALONE_WIN			
			appDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			appDir = appDir + "/MyAppName/webcache";
#endif
			
			System.IO.Directory.CreateDirectory(appDir);
			Debug.Log(appDir);
			
			// Initialize Berkelium
			UnityBerkelium.init(appDir);
		}
		
		// Create the texture that will represent the website (with optional transparency and without mipmaps)
		TextureFormat texFormat = transparency ? TextureFormat.ARGB32 : TextureFormat.RGB24;
		m_Texture = new Texture2D (width, height, texFormat, false);

		// Create the pixel array for the plugin to write into at startup    
		m_Pixels = m_Texture.GetPixels (0);
		// "pin" the array in memory, so we can pass direct pointer to it's data to the plugin,
		// without costly marshaling of array of structures.
		m_PixelsHandle = GCHandle.Alloc(m_Pixels, GCHandleType.Pinned);

		// Save the texture ID
		m_TextureID = m_Texture.GetInstanceID();
		
		// Improve rendering at shallow angles
		m_Texture.filterMode = FilterMode.Trilinear;
		m_Texture.anisoLevel = 2;
		
		// set us to initially not ready
		m_BrowserReady = false;
		
		m_IsGuiTexture = false;
		
		m_HookUrlWasRequested = false;
		
		m_eventSubscriber = null;
		
		// init the texture
		initTexturePixels();

		// Assign texture to the renderer
		if (renderer)
		{
			renderer.material.mainTexture = m_Texture;
			
			// Transparency?
			if(transparency)
				renderer.material.shader = Shader.Find("Transparent/Diffuse");
			else
				renderer.material.shader = Shader.Find("Unshaded");
			
			renderer.enabled = false;
		}
		// or gui texture
		else if (GetComponent(typeof(GUITexture)))
		{
			GUITexture gui = GetComponent(typeof(GUITexture)) as GUITexture;
			gui.texture = m_Texture;
			guiTexture.enabled = false;
			m_IsGuiTexture = true;
	        texRect = gui.GetScreenRect();
    	    texScaleX = texRect.width / width; //these two lines are important.
	        texScaleY = texRect.height / height; //they scale the screen position to match the same pixel on
		}
		else
		{
			Debug.Log("Game object has no renderer or gui texture to assign the generated texture to!");
		}
		
		// Create new web window
		UnityBerkelium.Window.create(m_TextureID, m_PixelsHandle.AddrOfPinnedObject(), transparency, width,height, url);
		print("Created new web window: " + m_TextureID);
		
		// Paint callbacks
		m_setPixelsFunc = new UnityBerkelium.SetPixelsFunc(this.SetPixels);
		m_applyTextureFunc = new UnityBerkelium.ApplyTextureFunc(this.ApplyTexture);
		m_scrollRectFunc = new UnityBerkelium.ScrollRectFunc(this.ScrollRect);
		UnityBerkelium.Window.setPaintFunctions(m_TextureID, m_setPixelsFunc, m_applyTextureFunc, m_scrollRectFunc);
		
		// Set the external host callback (for calling Unity functions from javascript)
		m_externalHostFunc = new UnityBerkelium.ExternalHostFunc(this.onExternalHost);
		UnityBerkelium.Window.setExternalHostCallback(m_TextureID, m_externalHostFunc);
		
		// Set the navigation callbacks
		m_navHookCb = new UnityBerkelium.NavHookCb(this.onNavHook);
		m_loadCb = new UnityBerkelium.LoadCb(this.onLoad);
		UnityBerkelium.Window.setNavigationFunctions(m_TextureID, navHookUrl, m_navHookCb, m_loadCb);
    }
	
	void SetPixels(/*int left, int top, int width, int height*/)
	{
		UnityBerkelium.Rect rect = UnityBerkelium.Window.getLastDirtyRect(m_TextureID);
		//print("Painting rect: (" + rect.left + ", " + rect.top + ", " + rect.width + ", " + rect.height + ")");
		m_Texture.SetPixels(rect.left, rect.top, rect.width, rect.height, m_Pixels, 0);
	}
	
	void ApplyTexture()
	{
		//print("Applying texture");
		m_Texture.Apply();
	}
	
	void ScrollRect(int left, int top, int width, int height, int dx, int dy)
	{
		//print("Scroll rect: " + left + ", " + top + ", " + width + ", " + height);
		
		Color[] scrollPixels = m_Texture.GetPixels (left, top, width, height);
			
		m_Texture.SetPixels(left + dx, top + dy, width, height, scrollPixels, 0);
	}

	void onExternalHost(/*string message*/)
	{
		string message = Marshal.PtrToStringUni(UnityBerkelium.Window.getLastExternalHostMessage(m_TextureID));
		print("Message from javascript: " + message);
		
		// Broadcast the message
		SendMessage("OnExternalHost", message, SendMessageOptions.DontRequireReceiver);

		// Parse the JSON object
		object parsedObject = JSON.JsonDecode(message);
		if(parsedObject is Hashtable)
		{
			Hashtable table = (Hashtable) parsedObject;
			
			string func = (string) table["func"];
			Hashtable args = (Hashtable) table["args"];
			
			print("  function: " + func);
			print("  #arguments: " + args.Count);
			
			IDictionaryEnumerator enumerator = args.GetEnumerator();
			while(enumerator.MoveNext())
				print("  " + enumerator.Key.ToString() + " = " + enumerator.Value.ToString());
			
			// Broadcast the function
			SendMessage(func, args, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	void onNavHook(string url)
	{
		if(m_eventSubscriber)
		{
			m_eventSubscriber.SendMessage("onNavHook", url);
		}
		
		m_HookUrl = url;
		m_HookUrlWasRequested = true;
		if(m_IsGuiTexture)
		{
			guiTexture.enabled = false;
		}
		else
		{
			renderer.enabled = false;
		}		
	}

	void onLoad(string url)
	{
		if(m_BrowserReady || UnityBerkelium.Window.everReceivedTitleUpdate(m_TextureID) || (waitForTitle == false))
		{
			if(m_IsGuiTexture)
			{
				guiTexture.enabled = true;
			}
			else
			{
				renderer.enabled = true;
			}
			m_BrowserReady = true;
		}
	}

    void OnDisable() {
		// Destroy the web window
		UnityBerkelium.Window.destroy(m_TextureID);
		
        // Free the pinned array handle.
        m_PixelsHandle.Free();
    }
	
	void OnApplicationQuit()
	{
		// Destroy Berkelium
		//UnityBerkelium.destroy();
		//print("Destroyed Berkelium");
	}

    void Update ()
	{
		// Update Berkelium
		// TODO This only has to be done once in stead of per object
		UnityBerkelium.update();
    }
	
	void OnMouseEnter()
	{
	}
	
	void OnMouseExit()
	{
	}
	
	void TestMouseOver()
	{
		// Only when interactive is enabled
		if(!interactive)
			return;
		
		bool eventActive = false;
		int x = 0, y = 0;
		
		if(m_IsGuiTexture)
		{
			eventActive = texRect.Contains(Input.mousePosition);
     		float offset_x = Input.mousePosition.x - texRect.xMin; //Find the mouse position
     		float offset_y = height - (Input.mousePosition.y - texRect.yMin); //Find the mouse position

     		x = (int)(offset_x / texScaleX);
			y = (int)(offset_y / texScaleY);
		}
		else
		{
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit) && hit.transform == this.transform)
			{
				eventActive = true;
				x = /*width -*/ (int) (hit.textureCoord.x * width);
				y = height - (int) (hit.textureCoord.y * height);
			}
		}
		
		if(eventActive)
		{
			networkView.RPC ("BerkeliumMouseOver", RPCMode.All, x, y);
		}
	}
	
	void TestMouseDown()
	{
		// Only when interactive is enabled
		if(!interactive)
			return;
		
		bool eventActive = false;
		int x = 0, y = 0;
		
		if(m_IsGuiTexture)
		{
			eventActive = texRect.Contains(Input.mousePosition);
     		float offset_x = Input.mousePosition.x - texRect.xMin; //Find the mouse position
     		float offset_y = height - (Input.mousePosition.y - texRect.yMin); //Find the mouse position

     		x = (int)(offset_x / texScaleX);
			y = (int)(offset_y / texScaleY);
		}
		else
		{
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit) && hit.transform == this.transform)
			{
				eventActive = true;
				x = /*width -*/ (int) (hit.textureCoord.x * width);
				y = height - (int) (hit.textureCoord.y * height);
			}
		}
		
		if(eventActive)
		{
			networkView.RPC ("BerkeliumMouseDown", RPCMode.All, x, y);
		}
	}
	
	void TestMouseUp()
	{
		// Only when interactive is enabled
		if(!interactive)
			return;

		bool eventActive = false;
		int x = 0, y = 0;
		
		if(m_IsGuiTexture)
		{
			eventActive = texRect.Contains(Input.mousePosition);
     		float offset_x = Input.mousePosition.x - texRect.xMin; //Find the mouse position
     		float offset_y = height - (Input.mousePosition.y - texRect.yMin); //Find the mouse position

     		x = (int)(offset_x / texScaleX);
			y = (int)(offset_y / texScaleY);
		}
		else
		{
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay(Input.mousePosition), out hit) && hit.transform == this.transform)
			{
				eventActive = true;
				x = /*width -*/ (int) (hit.textureCoord.x * width);
				y = height - (int) (hit.textureCoord.y * height);
			}
		}
		
		if(eventActive)
		{
			networkView.RPC ("BerkeliumMouseUp", RPCMode.All, x, y);
		}
	}
	
	void OnGUI()
	{
		if(!interactive) return;
		
		if(Vector3.Distance(Input.mousePosition, lastMousePos) >= 2)
		{
			lastMousePos = Input.mousePosition;
			TestMouseOver();
		}
		
		if(Event.current == null) return;
		
		// Inject input into the page when the GUI doesn't have focus
		if(Event.current.isKey && GUIUtility.keyboardControl == 0)
		{
			KeyCode key = Event.current.keyCode;
			bool pressed = (Event.current.type == EventType.KeyDown);
			
			if(key == KeyCode.Return)
			{
				UnityBerkelium.Window.keyEvent(m_TextureID, pressed, 0, 13, 0);
				return;
			}
			
			// Insert character
			UnityBerkelium.Window.textEvent(m_TextureID, Event.current.character);
			
			// Special case for backspace or tab
			if(key == KeyCode.Backspace)
				UnityBerkelium.Window.keyEvent(m_TextureID, pressed, 0, 08, 0);
			else if(key == KeyCode.Tab)
				// shift mod is 1 << 0
				UnityBerkelium.Window.keyEvent(m_TextureID, pressed, Event.current.shift ? 1 : 0, 09, 0);
			
			// TODO Handle all keys
			/*int mods = 0;
			int vk_code = UnityBerkelium.convertKeyCode(Event.current.keyCode);
			int scancode = 0;
			UnityBerkelium.Window.keyEvent(m_TextureID, pressed, mods, vk_code, scancode);
			print("Key event: " + pressed + ", " + Event.current.keyCode);*/
		}
		else
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
					TestMouseDown();
					break;

				case EventType.MouseUp:
					TestMouseUp();
					break;
			}
		}
	}
	
	void initTexturePixels()
	{
	    int mipCount = Mathf.Min( 3, m_Texture.mipmapCount );
	    
	    // tint each mip level
	    for( int mip = 0; mip < mipCount; ++mip ) {
	        Color[] cols = m_Texture.GetPixels( mip );
	        for( int i = 0; i < cols.Length; ++i ) {
	            cols[i] = new Color( 0, 0, 0 );
	        }
	        m_Texture.SetPixels( cols, mip );
	    }
	    
	    // actually apply all SetPixels, don't recalculate mip levels
	    m_Texture.Apply( false );
	}
	
    public string getHookUrl()
	{
		return m_HookUrl;
	}
	
    public bool hookUrlWasRequested()
	{
		return m_HookUrlWasRequested;
	}
	
	public void navigateTo(string url)
	{
		print("Changing url to " + url);
		UnityBerkelium.Window.navigateTo(m_TextureID, url);
	}
	
	public void subscribeForEvents(Component eventSubscriber)
	{
		m_eventSubscriber = eventSubscriber;
	}

	public void executeJavascript(string javascript)
	{
		print("Executing Javascript: " + javascript);
		UnityBerkelium.Window.executeJavascript(m_TextureID, javascript);
	}
	
	public Texture2D getTexture()
	{
		return m_Texture;
	}

	[RPC] 
	void BerkeliumMouseUp(int x, int y) 
	{
		UnityBerkelium.Window.mouseMove(m_TextureID, x, y);
		UnityBerkelium.Window.mouseUp(m_TextureID, 0);
	}
	
	[RPC]
	void BerkeliumMouseDown(int x, int y)
	{
		// Focus the window
		UnityBerkelium.Window.focus(m_TextureID);
	
		UnityBerkelium.Window.mouseMove(m_TextureID, x, y);
		UnityBerkelium.Window.mouseDown(m_TextureID, 0);
	}
	
	[RPC]
	void BerkeliumMouseOver(int x, int y)
	{
		UnityBerkelium.Window.mouseMove(m_TextureID, x, y);
	}
}
