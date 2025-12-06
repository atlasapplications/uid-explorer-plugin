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
using DictionaryG = Godot.Collections.Dictionary;

namespace UidExplorerPluginProject;

public partial class UidInspectorProperty : EditorProperty
{
	[Signal]
	public delegate void UnpackCompletedEventHandler(UidInspectorProperty uidInspectorProperty);
	[Signal]
	public delegate void UidPropertyChangedEventHandler(UidInspectorProperty inspectorProperty, string newValue);
	[Signal]
	public delegate void DeleteFromArrayButtonEventHandler(UidInspectorProperty uidInspectorProperty);

	private static readonly string[] DEFAULT_FILE_FILTER_TYPES = [ "*.tscn", "*.tres", "*.gdshader", "*.png", 
		"*.tga", "*.wav", "*.mp4", "*.ttf", "*.glb", "*.res" ];

	private bool devModeEnabled;
	private PressOptionE pressOption;

	private readonly UidExplorerPlugin plugin;
	private readonly UidInspector uidInspector;
	private readonly EditorInterface editor;
	private readonly bool partOfArray;

	private readonly VBoxContainer outerContainer;
	private readonly HBoxContainer topContainer;
	private readonly HBoxContainer bottomContainer;
	private readonly HBoxContainer indexContainer;

	private readonly Label indexLabel;
	private readonly Button deleteFromArrayButton;

	private readonly LineEdit uidTextEdit;
	private readonly Button chooseButton;
	private readonly Button showButton;
	private readonly Button convertedPathButton;

	private FileDialog findUidWindow;

	private string currentFullPath = "";
	private bool updating = false;
	private int arrayIndex = -1;

	private bool alreadyUnpacked = false;
	private bool parentUnpacked = false;

	private ValidationFailCodeE validationFailCode = ValidationFailCodeE.Ok;

	public UidInspectorProperty() {  }
	public UidInspectorProperty(UidExplorerPlugin plugin, UidInspector uidInspector, 
		bool partOfArray, bool devModeEnabled, PressOptionE pressOption)
	{
		this.plugin = plugin;
		this.uidInspector = uidInspector;
		this.partOfArray = partOfArray;

		if (partOfArray)
        {
            NameSplitRatio = 0.0f;
        }
		else
        {
            NameSplitRatio = 0.3f;
        }

		UpdateSettings(devModeEnabled, pressOption);

		editor = EditorInterface.Singleton;

		outerContainer = new VBoxContainer();
		topContainer = new HBoxContainer();
		bottomContainer = new HBoxContainer();
		indexContainer = new HBoxContainer();

		indexLabel = new Label();
		deleteFromArrayButton = new Button();

		uidTextEdit = new LineEdit();
		chooseButton = new Button();
		convertedPathButton = new Button();
		showButton = new Button();

		ConstructControl();

		showButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnSelectButtonPressed));
		chooseButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnFindUidButtonPressed));
		convertedPathButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnConvertedPathButtonPressed));
		uidTextEdit.Connect(LineEdit.SignalName.TextChanged, new(this, MethodName.OnUidTextEditChanged));
		uidTextEdit.Connect(LineEdit.SignalName.TextSubmitted, new(this, MethodName.OnUidTextEditSubmitted));
		
		if (partOfArray)
        {
            deleteFromArrayButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnDeleteFromArrayButtonPressed));
        }

		RefreshPaths();
	}
	private void ConstructControl()
	{
		AddChild(indexContainer);
		indexContainer.AddChild(indexLabel);
		indexContainer.AddChild(outerContainer);
		indexContainer.AddChild(deleteFromArrayButton);
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

		if (partOfArray)
        {
			AddFocusable(deleteFromArrayButton);
        }

		outerContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		outerContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		indexLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		indexLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
		uidTextEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		uidTextEdit.SizeFlagsVertical = SizeFlags.ExpandFill;
		showButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		showButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		chooseButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		chooseButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		deleteFromArrayButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		deleteFromArrayButton.SizeFlagsVertical = SizeFlags.ExpandFill;

		// Enable drag and drop to pass through all child elements.
		uidTextEdit.MouseFilter = MouseFilterEnum.Pass;
		chooseButton.MouseFilter = MouseFilterEnum.Pass;
		showButton.MouseFilter = MouseFilterEnum.Pass;
		convertedPathButton.MouseFilter = MouseFilterEnum.Pass;
		topContainer.MouseFilter = MouseFilterEnum.Ignore;
		bottomContainer.MouseFilter = MouseFilterEnum.Ignore;
		outerContainer.MouseFilter = MouseFilterEnum.Ignore;
		indexLabel.MouseFilter = MouseFilterEnum.Ignore;
		indexContainer.MouseFilter = MouseFilterEnum.Ignore;
		deleteFromArrayButton.MouseFilter = MouseFilterEnum.Pass;

		outerContainer.SizeFlagsStretchRatio = 0.9f;
		indexLabel.SizeFlagsStretchRatio = 0.05f;
		deleteFromArrayButton.SizeFlagsStretchRatio = 0.05f;

		uidTextEdit.SizeFlagsStretchRatio = 0.6f;
		showButton.SizeFlagsStretchRatio = 0.2f;
		chooseButton.SizeFlagsStretchRatio = 0.2f;

		uidTextEdit.ClipContents = true;
		showButton.ClipText = true;
		chooseButton.ClipText = true;
		convertedPathButton.ClipText = true;
		indexLabel.ClipText = true;
		deleteFromArrayButton.ClipText = true;

		indexLabel.HorizontalAlignment = HorizontalAlignment.Center;
		indexLabel.VerticalAlignment = VerticalAlignment.Center;

		uidTextEdit.PlaceholderText = "uid://";

		Texture2D chooseIcon = editor.GetEditorTheme().GetIcon("File", "EditorIcons");
		chooseButton.Icon = chooseIcon;
		chooseButton.IconAlignment = HorizontalAlignment.Center;

		Texture2D removeIcon = editor.GetEditorTheme().GetIcon("Remove", "EditorIcons");
		deleteFromArrayButton.Icon = removeIcon;
		deleteFromArrayButton.IconAlignment = HorizontalAlignment.Center;

		Texture2D showInFileSystemIcon = editor.GetEditorTheme().GetIcon("ShowInFileSystem", "EditorIcons");
		showButton.Icon = showInFileSystemIcon;
		showButton.IconAlignment = HorizontalAlignment.Center;

		indexLabel.Visible = partOfArray;
		deleteFromArrayButton.Visible = partOfArray;
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
			// out of the box as of 4.3.0 - 4.5.1.

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
		// Check if the dropped data contains files.
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

		if (data.VariantType != Variant.Type.Dictionary)
        {
            return;
        }

		var dict = data.As<DictionaryG>();
			
		if (!dict.ContainsKey("files"))
		{
			return;
		}

		var files = dict["files"].As<string[]>();
		
		if (files.Length == 0)
        {
            return;
        }

		// Use the first dropped file.
		string filePath = files[0];
		long foundUid = ResourceLoader.GetResourceUid(filePath);
		
		if (foundUid == -1)
        {
            return;
        }

		string foundUidPath = ResourceUid.IdToText(foundUid);
		uidTextEdit.Text = foundUidPath;
		uidInspector.SetLastEditedPath(filePath);
		
		PerformPropertyChange();
		RefreshPaths();
	}

	private void OnFindUidButtonPressed()
	{
		if (updating)
		{
			return;
		}

		Vector2I displaySize = DisplayServer.WindowGetSize();
		findUidWindow = new FileDialog();

		findUidWindow.FileMode = FileDialog.FileModeEnum.OpenFile;
		findUidWindow.Access = FileDialog.AccessEnum.Resources;
		findUidWindow.Filters = DEFAULT_FILE_FILTER_TYPES;
		findUidWindow.DisplayMode = FileDialog.DisplayModeEnum.List;
		findUidWindow.Title = "Choose Resource";

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

			// Work in progress.
			//plugin.AddToBackQueueResource(editor.GetInspector().GetEditedObject());
			//plugin.AddToBackQueueNodes(editor.GetSelection().GetSelectedNodes());

			Resource foundResource = ResourceLoader.Load(validatedPath);
			editor.EditResource(foundResource);
		}
		else if (pressOption == PressOptionE.ShowFullPath)
		{
			GD.Print("-- Path --\n" + currentFullPath);
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

	private void OnDeleteFromArrayButtonPressed()
    {
        EmitSignal(SignalName.DeleteFromArrayButton, this);
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

	public string GetContent()
    {
        return uidTextEdit.Text;
    }

	public void SetContent(string content)
    {
        uidTextEdit.Text = content;

		RefreshPaths();
    }

	public void SetArrayIndex(int arrayIndex)
    {
        this.arrayIndex = arrayIndex;

		UpdateIndexLabel(arrayIndex);
    }

	public int GetArrayIndex()
    {
        return arrayIndex;
    }

	private void RefreshPaths()
	{
		string validatedPath = ValidateUidPath();

		if (validatedPath != null)
		{
			currentFullPath = validatedPath;

			const int MAX_PATH_LENGTH = 48;

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

			// Causes editor crash.
			//TooltipText = validatedPath;
		}
		else 
		{
			string result;

			switch (validationFailCode)
			{
				case ValidationFailCodeE.InvalidUid:
					result = "Invalid UID";
					break;
				case ValidationFailCodeE.IsEmpty:
					result = "No Path Given";
					break;
				case ValidationFailCodeE.IsNull:
					result = "String is Null";
					break;
				case ValidationFailCodeE.NoResourceFound:
					result = "No Resource Found";
					break;
				default:
					result = "Unhandled Fail Code";
					break;
			}

			convertedPathButton.Disabled = true;
			showButton.Disabled = true;

			convertedPathButton.Text = result;
			//TooltipText = result;
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
		EmitSignal(SignalName.UidPropertyChanged, this, uidTextEdit.Text);
	}

	private void UpdateIndexLabel(int index)
    {
        indexLabel.Text = $"{index}";
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