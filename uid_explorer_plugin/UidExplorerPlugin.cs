/**************************************************************************/
/*  UidExplorerPlugin.cs                                                  */
/**************************************************************************/
/*                         This file is part of:                          */
/*                          UID Explorer Plugin                           */
/**************************************************************************/
/* Copyright (c) 2024-present Justin Sasso                                */
/*                                                                        */
/* Permission is hereby granted, free of charge, to any person obtaining  */
/* a copy of this software and associated documentation files (the        */
/* "Software"), to deal in the Software without restriction, including    */
/* without limitation the rights to use, copy, modify, merge, publish,    */
/* distribute, sublicense, and/or sell copies of the Software, and to     */
/* permit persons to whom the Software is furnished to do so, subject to  */
/* the following conditions:                                              */
/*                                                                        */
/* The above copyright notice and this permission notice shall be         */
/* included in all copies or substantial portions of the Software.        */
/*                                                                        */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/

#if TOOLS

using System;
using Godot;
using DictionaryG = Godot.Collections.Dictionary;

namespace UidExplorerPluginProject;

[Tool]
public partial class UidExplorerPlugin : EditorPlugin
{
	// Plugin Settings
	private const string SETTINGS_PATH = "addons/UidExplorerPlugin/";

	private const string DEV_MODE_ENABLED_PATH = SETTINGS_PATH + "DevModeEnabled";
	private const bool DEV_MODE_ENABLED_DEFAULT = false;

	private const string PRESS_OPTION_PATH = SETTINGS_PATH + "PressOption";
	private const PressOptionE PRESS_OPTION_DEFAULT = PressOptionE.EditResource;

	private bool devModeEnabled;
	private PressOptionE pressOption;

	private UidInspector uidInspector;

	public override void _EnterTree()
	{
		CheckForSettings();
		uidInspector = new UidInspector(devModeEnabled, pressOption);
		AddInspectorPlugin(uidInspector);
		ProjectSettings.Singleton.Connect(ProjectSettings.SignalName.SettingsChanged, new(this, MethodName.OnProjectSettingChanged));
	}
	public override void _ExitTree()
	{
		RemoveInspectorPlugin(uidInspector);
		uidInspector = null;
	}
	private void OnProjectSettingChanged()
	{
		CheckForSettings();
		uidInspector.UpdateSettings(devModeEnabled, pressOption);
	}

	/// <summary>
	/// Checks for if the project settings exist, and if not, create them.
	/// </summary>
	private void CheckForSettings()
	{
		if (ProjectSettings.HasSetting(DEV_MODE_ENABLED_PATH))
		{
			devModeEnabled = ProjectSettings.GetSetting(DEV_MODE_ENABLED_PATH).As<bool>();
		}
		else 
		{
			ProjectSettings.SetSetting(DEV_MODE_ENABLED_PATH, DEV_MODE_ENABLED_DEFAULT);
			ProjectSettings.SetInitialValue(DEV_MODE_ENABLED_PATH, DEV_MODE_ENABLED_DEFAULT);
			devModeEnabled = DEV_MODE_ENABLED_DEFAULT;
		}

		if (ProjectSettings.HasSetting(PRESS_OPTION_PATH))
		{
			pressOption = ProjectSettings.GetSetting(PRESS_OPTION_PATH).As<PressOptionE>();
		}
		else 
		{
			ProjectSettings.SetSetting(PRESS_OPTION_PATH, (int)PRESS_OPTION_DEFAULT);
			ProjectSettings.SetInitialValue(PRESS_OPTION_PATH, (int)PRESS_OPTION_DEFAULT);
			pressOption = PRESS_OPTION_DEFAULT;
		}

		ProjectSettings.AddPropertyInfo(GetPressOptionPropertyInfo());
	}

	private static DictionaryG GetPressOptionPropertyInfo()
	{
		var propertyInfo = new DictionaryG
		{
			{ "name", PRESS_OPTION_PATH },
			{ "type", (int)Variant.Type.Int },
			{ "hint", (int)PropertyHint.Enum },
			{ "hint_string", "ShowFullPath,EditResource" }
		};

		return propertyInfo;
	}
}

public enum PressOptionE
{
	ShowFullPath,
	EditResource
}

#endif