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
	private readonly UidInspector uidInspector;
	private readonly EditorInterface editor;

	private readonly VBoxContainer outerContainer;
	private readonly HBoxContainer topContainer;
	private readonly HBoxContainer bottomContainer;

	private readonly LineEdit uidTextEdit;
	private readonly Button findUidButton;
	private readonly Button convertedPathButton;
	private readonly Button selectButton;

	private FileDialog findUidWindow;
	private bool findUidWindowConnected = false;

	private string currentFullPath = "";
	private bool updating = false;

	public UidInspectorProperty() {  }

	public UidInspectorProperty(UidInspector uidInspector)
	{
		this.uidInspector = uidInspector;

		editor = EditorInterface.Singleton;

		outerContainer = new VBoxContainer();
		topContainer = new HBoxContainer();
		bottomContainer = new HBoxContainer();

		uidTextEdit = new LineEdit();
		findUidButton = new Button();
		convertedPathButton = new Button();
		selectButton = new Button();

		AddChild(outerContainer);
		outerContainer.AddChild(topContainer);
		outerContainer.AddChild(bottomContainer);
		topContainer.AddChild(uidTextEdit);
		topContainer.AddChild(findUidButton);
		bottomContainer.AddChild(convertedPathButton);
		bottomContainer.AddChild(selectButton);

		AddFocusable(uidTextEdit);
		AddFocusable(findUidButton);
		AddFocusable(convertedPathButton);
		AddFocusable(selectButton);

		uidTextEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		uidTextEdit.SizeFlagsVertical = SizeFlags.ExpandFill;
		selectButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		selectButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		findUidButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		findUidButton.SizeFlagsVertical = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		convertedPathButton.SizeFlagsVertical = SizeFlags.ExpandFill;

		//uidTextEdit.SizeFlagsStretchRatio = 0.1f;
		//selectButton.SizeFlagsStretchRatio = 0.9f;

		uidTextEdit.PlaceholderText = "uid://";
		findUidButton.Text = "Select";
		selectButton.Text = "File System";
		//convertedPathButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		selectButton.Pressed += OnSelectButtonPressed;
		findUidButton.Pressed += OnFindUidButtonPressed;
		convertedPathButton.Pressed += OnConvertedPathButtonPressed;
		uidTextEdit.TextChanged += OnUidTextEditChanged;
		uidTextEdit.TextSubmitted += OnUidTextEditSubmitted;
	}
	~UidInspectorProperty()
	{
		selectButton.Pressed -= OnSelectButtonPressed;
		findUidButton.Pressed -= OnFindUidButtonPressed;
		convertedPathButton.Pressed -= OnConvertedPathButtonPressed;
		uidTextEdit.TextChanged -= OnUidTextEditChanged;
		uidTextEdit.TextSubmitted -= OnUidTextEditSubmitted;
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

		editor.SelectFile(validatedPath);
		//Resource foundResource = ResourceLoader.Load(foundPath);
		//editor.InspectObject(foundResource);
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
		findUidWindow.Filters = new string[] { "*.tscn", "*.tres" };

		string validatedPath = ValidateUidPath();

		if (validatedPath != null)
		{
			findUidWindow.CurrentPath = validatedPath;
		}
		else if (uidInspector.LastEditedPath != "")
		{
			findUidWindow.CurrentPath = uidInspector.LastEditedPath;
		}

		findUidWindow.CloseRequested += OnDialogWindowCloseRequested;
		findUidWindow.FileSelected += OnDialogWindowFileSelected;
		findUidWindowConnected = true;
		editor.PopupDialogCentered(findUidWindow, new Vector2I(displaySize.X / 2, displaySize.Y / 2));
	}
	private void OnConvertedPathButtonPressed()
	{
		if (updating)
		{
			return;
		}

		GD.Print("Full path: " + currentFullPath);
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
		RefreshPaths(true);
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
	public override string _GetTooltip(Vector2 atPosition)
    {
		//return currentFullPath;
		return "";
    }
	private void RefreshPaths(bool debug = false)
	{
		string validatedPath = ValidateUidPath(debug);

		if (validatedPath != null)
		{
			currentFullPath = validatedPath;
			int uidCount = uidTextEdit.Text.Length;

			if (currentFullPath.Length > uidCount)
			{
				convertedPathButton.Text = validatedPath.Remove(0, validatedPath.Length - uidCount);
			}
			else 
			{
				convertedPathButton.Text = validatedPath;
			}

			//convertedPathButton.Text = validatedPath;

			convertedPathButton.Disabled = false;
			selectButton.Disabled = false;
		}
		else 
		{
			convertedPathButton.Text = "No Resource Found";

			convertedPathButton.Disabled = true;
			selectButton.Disabled = true;
		}
	}

	private string ValidateUidPath(bool debug = false)
	{
		string foundPath = uidTextEdit.Text;

		if (foundPath == null)
		{
			if (debug) GD.Print("Path is null.");
			return null;
		}
		else if (foundPath == "")
		{
			if (debug) GD.Print("Path is empty.");
			return null;
		}
		else if (!ResourceLoader.Exists(foundPath))
		{
			if (debug) GD.Print($"Resource doesn't exist at path: {foundPath}.");
			return null;
		}

		long uidValue = ResourceUid.TextToId(foundPath);

		if (uidValue == -1 || !ResourceUid.HasId(uidValue))
		{
			if (debug) GD.Print("Invalid UID given.");
			return null;
		}

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
			if (findUidWindowConnected)
			{
				findUidWindow.CloseRequested -= OnDialogWindowCloseRequested;
				findUidWindow.FileSelected -= OnDialogWindowFileSelected;
				findUidWindowConnected = false;
			}

			findUidWindow.QueueFree();
			findUidWindow = null;
		}
	}
}

#endif