/**************************************************************************/
/*  UidInspectorProperty.cs                                               */
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

namespace UidExplorerPluginProject;

public partial class UidInspectorProperty : EditorProperty
{
	[Signal]
	public delegate void UnpackCompletedEventHandler(UidInspectorProperty uidInspectorProperty);

	private static readonly string[] FILE_FILTER_TYPES = { "*.tscn", "*.tres", "*.gdshader", "*.png", "*.tga", "*.wav" };

	private bool devModeEnabled;
	private PressOptionE pressOption;

	private readonly UidInspector uidInspector;
	private readonly EditorInterface editor;

	private VBoxContainer outerContainer;
	private HBoxContainer topContainer;
	private HBoxContainer bottomContainer;

	private LineEdit uidTextEdit;
	private Button chooseButton;
	private Button showButton;
	private Button convertedPathButton;

	private FileDialog findUidWindow;

	private string currentFullPath = "";
	private bool updating = false;

	private bool alreadyUnpacked = false;
	private bool parentUnpacked = false;

	private ValidationFailCodeE validationFailCode = ValidationFailCodeE.Ok;

	public UidInspectorProperty() {  }
	public UidInspectorProperty(UidInspector uidInspector, bool devModeEnabled, PressOptionE pressOption)
	{
		this.uidInspector = uidInspector;

		UpdateSettings(devModeEnabled, pressOption);

		editor = EditorInterface.Singleton;

		ConstructControl();

		showButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnSelectButtonPressed));
		chooseButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnFindUidButtonPressed));
		convertedPathButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnConvertedPathButtonPressed));
		uidTextEdit.Connect(LineEdit.SignalName.TextChanged, new(this, MethodName.OnUidTextEditChanged));
		uidTextEdit.Connect(LineEdit.SignalName.TextSubmitted, new(this, MethodName.OnUidTextEditSubmitted));

		RefreshPaths();
	}
	private void ConstructControl()
	{
		outerContainer = new VBoxContainer();
		topContainer = new HBoxContainer();
		bottomContainer = new HBoxContainer();

		uidTextEdit = new LineEdit();
		chooseButton = new Button();
		convertedPathButton = new Button();
		showButton = new Button();

		AddChild(outerContainer);
		outerContainer.AddChild(topContainer);
		outerContainer.AddChild(bottomContainer);
		topContainer.AddChild(uidTextEdit);
		topContainer.AddChild(chooseButton);
		topContainer.AddChild(showButton);
		bottomContainer.AddChild(convertedPathButton);

		AddFocusable(uidTextEdit);
		AddFocusable(chooseButton);
		AddFocusable(convertedPathButton);
		AddFocusable(showButton);

		uidTextEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		uidTextEdit.SizeFlagsVertical = SizeFlags.ExpandFill;
		showButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		showButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		chooseButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		chooseButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsVertical = SizeFlags.ExpandFill;

		// Enable drag and drop to pass through all child elements
		uidTextEdit.MouseFilter = Control.MouseFilterEnum.Pass;
		chooseButton.MouseFilter = Control.MouseFilterEnum.Pass;
		showButton.MouseFilter = Control.MouseFilterEnum.Pass;
		convertedPathButton.MouseFilter = Control.MouseFilterEnum.Pass;
		topContainer.MouseFilter = Control.MouseFilterEnum.Pass;
		bottomContainer.MouseFilter = Control.MouseFilterEnum.Pass;
		outerContainer.MouseFilter = Control.MouseFilterEnum.Pass;

		uidTextEdit.SizeFlagsStretchRatio = 0.47f;
		showButton.SizeFlagsStretchRatio = 0.265f;
		chooseButton.SizeFlagsStretchRatio = 0.265f;

		uidTextEdit.ClipContents = true;
		showButton.ClipText = true;
		chooseButton.ClipText = true;
		convertedPathButton.ClipText = true;

		uidTextEdit.PlaceholderText = "uid://";
		chooseButton.Text = "Choose";
		showButton.Text = "Show";
	}
    public override void _ExitTree()
    {
		Unpack();
    }
	private void Unpack()
	{
		if (alreadyUnpacked)
		{
			return;
		}

		alreadyUnpacked = true;

		if (!parentUnpacked)
		{
			EmitSignal(SignalName.UnpackCompleted, this);
		}
	}
	public void UpdateSettings(bool devModeEnabled, PressOptionE pressOption)
	{
		this.devModeEnabled = devModeEnabled;
		this.pressOption = pressOption;
	}
	private void OnSelectButtonPressed()
	{
		if (updating)
		{
			return;
		}

		string validatedPath = ValidateUidPath();

		if (validatedPath == null)
		{
			return;
		}

		string[] selectedFiles = editor.GetSelectedPaths();
		bool isSelectedAlready;

		if (selectedFiles.Length == 1)
		{
			if (selectedFiles[0] == validatedPath)
			{
				isSelectedAlready = true;
			}
			else 
			{
				isSelectedAlready = false;
			}
		}
		else 
		{
			isSelectedAlready = false;
		}

		if (isSelectedAlready)
		{
			// -- New Feature Roadmap --
			// When this button is hit twice, edit the resource with
			// a new window. This doesn't appear to be supported 
			// out of the box as of 4.4.1.

			// This... but a new window.
			//editor.EditResource(ResourceLoader.Load(validatedPath));
		}
		else 
		{
			editor.SelectFile(validatedPath);
		}
	}

	public override bool _CanDropData(Vector2 position, Variant data)
	{
		// Check if the dropped data contains files
		if (data.VariantType == Variant.Type.Dictionary)
		{
			var dict = data.AsGodotDictionary();
			return dict.ContainsKey("type") && dict["type"].AsString() == "files";
		}

		return false;
	}

	public override void _DropData(Vector2 position, Variant data)
	{
		if (updating)
		{
			return;
		}

		if (data.VariantType == Variant.Type.Dictionary)
		{
			var dict = data.AsGodotDictionary();
			
			if (dict.ContainsKey("files"))
			{
				var files = dict["files"].AsStringArray();
				
				// Use the first dropped file
				if (files.Length > 0)
				{
					string filePath = files[0];
					long foundUid = ResourceLoader.GetResourceUid(filePath);
					
					if (foundUid != -1)
					{
						string foundUidPath = ResourceUid.IdToText(foundUid);
						uidTextEdit.Text = foundUidPath;
						uidInspector.SetLastEditedPath(filePath);
						
						PerformPropertyChange();
						RefreshPaths();
					}
				}
			}
		}
	}
	private void OnFindUidButtonPressed()
	{
		if (updating)
		{
			return;
		}

		Vector2I displaySize = DisplayServer.WindowGetSize();
		findUidWindow = new FileDialog();

		findUidWindow.Title = "Choose Resource";
		findUidWindow.FileMode = FileDialog.FileModeEnum.OpenFile;
		findUidWindow.Access = FileDialog.AccessEnum.Resources;
		findUidWindow.Filters = FILE_FILTER_TYPES;

		string validatedPath = ValidateUidPath();

		if (validatedPath != null)
		{
			findUidWindow.CurrentPath = validatedPath;
		}
		else if (uidInspector.LastEditedPath != "")
		{
			findUidWindow.CurrentPath = uidInspector.LastEditedPath;
		}

		findUidWindow.Connect(FileDialog.SignalName.CloseRequested, new(this, MethodName.OnDialogWindowCloseRequested));
		findUidWindow.Connect(FileDialog.SignalName.FileSelected, new(this, MethodName.OnDialogWindowFileSelected));

		editor.PopupDialogCentered(findUidWindow, new Vector2I(displaySize.X / 2, displaySize.Y / 2));
	}
	private async void OnConvertedPathButtonPressed()
	{
		if (updating)
		{
			return;
		}

		if (pressOption == PressOptionE.EditResource)
		{
			string validatedPath = ValidateUidPath();

			if (validatedPath == null)
			{
				return;
			}

			uidInspector.ManualUnpack(this);
			parentUnpacked = true;

			Unpack();

			// Hacky, but unfortunately I'm not sure a better way to ensure that the signal
			// in unpack is emitted because without this an error occurs every time.
			await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

			editor.EditResource(ResourceLoader.Load(validatedPath));
		}
		else if (pressOption == PressOptionE.ShowFullPath)
		{
			GD.Print("Path: " + currentFullPath);
		}
	}
	private void OnUidTextEditChanged(string newText)
	{
		if (updating)
		{
			return;
		}

		PerformPropertyChange();
		RefreshPaths();
	}
	private void OnUidTextEditSubmitted(string newText)
	{
		if (updating)
		{
			return;
		}

		PerformPropertyChange();
		RefreshPaths();
	}
	
	private void OnDialogWindowCloseRequested()
	{
		DespawnFindUidWindow();
	}
	private void OnDialogWindowFileSelected(string fileSelected)
	{
		DespawnFindUidWindow();

		long foundUid = ResourceLoader.GetResourceUid(fileSelected);

		if (foundUid == -1)
		{
			return;
		}

		string foundUidPath = ResourceUid.IdToText(foundUid);
		uidTextEdit.Text = foundUidPath;
		uidInspector.SetLastEditedPath(fileSelected);

		PerformPropertyChange();
		RefreshPaths();
	}

	public override void _UpdateProperty()
	{
		string newValue = (string)GetEditedObject().Get(GetEditedProperty());

		if (newValue == uidTextEdit.Text)
		{
			return;
		}

		updating = true;
		uidTextEdit.Text = newValue;
		RefreshPaths();
		updating = false;
	}
	private void RefreshPaths()
	{
		string validatedPath = ValidateUidPath();

		if (validatedPath != null)
		{
			currentFullPath = validatedPath;

			const int MAX_PATH_LENGTH = 40;

			if (currentFullPath.Length > MAX_PATH_LENGTH)
			{
				convertedPathButton.Text = validatedPath.Remove(0, validatedPath.Length - MAX_PATH_LENGTH);
			}
			else 
			{
				convertedPathButton.Text = validatedPath;
			}

			convertedPathButton.Disabled = false;
			showButton.Disabled = false;
		}
		else 
		{
			switch (validationFailCode)
			{
				case ValidationFailCodeE.InvalidUid:
					convertedPathButton.Text = "Invalid UID";
					break;
				case ValidationFailCodeE.IsEmpty:
					convertedPathButton.Text = "No Path Given";
					break;
				case ValidationFailCodeE.IsNull:
					convertedPathButton.Text = "String is Null";
					break;
				case ValidationFailCodeE.NoResourceFound:
					convertedPathButton.Text = "No Resource Found";
					break;
				default:
					convertedPathButton.Text = "Unhandled Fail Code";
					break;
			}

			convertedPathButton.Disabled = true;
			showButton.Disabled = true;
		}
	}
	private string ValidateUidPath()
	{
		string foundPath = uidTextEdit.Text;

		if (foundPath == null)
		{
			if (devModeEnabled) GD.Print("Path is null.");
			validationFailCode = ValidationFailCodeE.IsNull;
			return null;
		}
		else if (foundPath == "")
		{
			if (devModeEnabled) GD.Print("Path is empty.");
			validationFailCode = ValidationFailCodeE.IsEmpty;
			return null;
		}
		else if (!ResourceLoader.Exists(foundPath))
		{
			if (devModeEnabled) GD.Print($"Resource doesn't exist at path: {foundPath}.");
			validationFailCode = ValidationFailCodeE.NoResourceFound;
			return null;
		}

		long uidValue = ResourceUid.TextToId(foundPath);

		if (uidValue == -1 || !ResourceUid.HasId(uidValue))
		{
			if (devModeEnabled) GD.Print("Invalid UID given.");
			validationFailCode = ValidationFailCodeE.InvalidUid;
			return null;
		}

		validationFailCode = ValidationFailCodeE.Ok;
		return ResourceUid.GetIdPath(uidValue);
	}
    private void PerformPropertyChange()
	{
		EmitChanged(GetEditedProperty(), uidTextEdit.Text);
	}
	private void DespawnFindUidWindow()
	{
		if (findUidWindow != null && IsInstanceValid(findUidWindow))
		{
			findUidWindow.QueueFree();
			findUidWindow = null;
		}
	}

	public enum ValidationFailCodeE
	{
		Ok = -1,
		IsNull,
		IsEmpty,
		NoResourceFound,
		InvalidUid
	}
}

#endif