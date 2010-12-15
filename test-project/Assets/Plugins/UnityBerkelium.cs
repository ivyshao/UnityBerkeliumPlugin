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

public class UnityBerkelium
{
	// A rectangle (same structure as the Berkelium Rect)
	public struct Rect
	{
		public int left, top;
		public int width, height;
	}
	
	// The delegate that is called when there is a dirty rectangle
	// Note: Unity crashes when using a struct or more than two parameters
	//delegate void SetPixelsFunc(Rect rect);
	public delegate void SetPixelsFunc(/*int left, int top, int width, int height*/);
	
	// The delgate that is called when all dirty rectangles can be applied
	public delegate void ApplyTextureFunc();
	
	// The delegate that is called when the texture needs scrolling
	public delegate void ScrollRectFunc(int left, int top, int width, int height, int dx, int dy);
	
	// The delegate that is called when javascript calls to Unity
	public delegate void ExternalHostFunc(/*string message*/);
	
	// The delegate that is called when a registered hook URL is requested
	public delegate void NavHookCb(string url);
	
	// The delegate that is called when a URL has completed loading
	public delegate void LoadCb(string url);

	// Imported functions
	[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_init")]
	public static extern void init(string homeDir);
    
	[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_destroy")]
	public static extern void destroy();
    
	[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_update")]
	public static extern void update();
    
	public class Window
	{
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_create")]
		public static extern int create(int windowID, IntPtr colors, bool transparency, int width, int height, string url);

		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_destroy")]
		public static extern void destroy(int windowID);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_navigateTo")]
		public static extern void navigateTo(int windowID, string url);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_setPaintFunctions")]
		public static extern void setPaintFunctions(int windowID, SetPixelsFunc setPixelsFunc, ApplyTextureFunc applyTextureFunc, ScrollRectFunc scrollRectFunc);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_setNavigationFunctions")]
		public static extern void setNavigationFunctions(int windowID, string hookUrl, NavHookCb navCb, LoadCb loadCb);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_getLastDirtyRect")]
		public static extern Rect getLastDirtyRect(int windowID);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_focus")]
		public static extern void focus(int windowID);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_unfocus")]
		public static extern void unfocus(int windowID);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_mouseDown")]
		public static extern void mouseDown(int windowID, int button);

		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_mouseUp")]
		public static extern void mouseUp(int windowID, int button);

		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_mouseMove")]
		public static extern void mouseMove(int windowID, int x, int y);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_textEvent")]
		public static extern void textEvent(int windowID, char c);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_keyEvent")]
		public static extern void keyEvent(int windowID, bool pressed, int mods, int vk_code, int scancode);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_executeJavascript")]
		public static extern void executeJavascript(int windowID, string javascript);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_setExternalHostCallback")]
		public static extern void setExternalHostCallback(int windowID, ExternalHostFunc callback);
		
		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_getLastExternalHostMessage")]
		public static extern IntPtr getLastExternalHostMessage(int windowID);

		[DllImport ("UnityBerkeliumPlugin", EntryPoint="Berkelium_Window_everReceivedTitleUpdate")]
		public static extern bool everReceivedTitleUpdate(int windowID);
	}
	
	public int convertKeyCode(KeyCode keyCode)
	{
		return (int) keyCode;
	}
}
