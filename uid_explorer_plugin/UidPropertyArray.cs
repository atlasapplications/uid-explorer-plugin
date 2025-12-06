/**************************************************************************/
/*  UidPropertyArray.cs                                                   */
/**************************************************************************/
/*                         This file is part of:                          */
/*                          UID Explorer Plugin                           */
/**************************************************************************/
/* Copyright (c) 2025-present Justin Sasso                                */
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
using System.Collections.Generic;

namespace UidExplorerPluginProject;

public partial class UidPropertyArray : EditorProperty
{
	private readonly VBoxContainer outerContainer;
	private readonly SpinBox arraySizeContainer;
	private readonly FoldableContainer foldedContentContainer;
	private readonly VBoxContainer contentContainer;
	private readonly VBoxContainer innerContainer;
	private readonly TabContainer overflowContainer;
	private readonly Button addElementButton;

	private bool updating = false;

	private readonly UidExplorerPlugin plugin;
	private readonly UidInspector uidInspector;
	private readonly string propertyName;
	private readonly Variant.Type godotType;

	private readonly List<string> currentlyCachedPropertyValues = new();

	private readonly List<UidInspectorProperty> cachedPropertiesByIndex = new();
	private readonly Dictionary<ulong, int> cachedPropertyIdsById = new();
	private readonly Dictionary<ulong, UidInspectorProperty> cachedPropertiesById = new();

	private readonly List<VBoxContainer> allOverflowContainers = new();
	private int nextFoldableContainerIndex = -1;

    public UidPropertyArray() {  }
	public UidPropertyArray(UidExplorerPlugin plugin, UidInspector uidInspector, string propertyName, Variant.Type godotType)
    {
		this.plugin = plugin;
		this.uidInspector = uidInspector;
		this.propertyName = propertyName;
		this.godotType = godotType;

		NameSplitRatio = 0.3f;

		outerContainer = new VBoxContainer();
		arraySizeContainer = new SpinBox();
		foldedContentContainer = new FoldableContainer();
		contentContainer = new VBoxContainer();
		innerContainer = new VBoxContainer();
		overflowContainer = new TabContainer();
		addElementButton = new Button();

		AddChild(outerContainer);
		outerContainer.AddChild(foldedContentContainer);
		foldedContentContainer.AddChild(contentContainer);
		contentContainer.AddChild(arraySizeContainer);
		contentContainer.AddChild(overflowContainer);
		contentContainer.AddChild(innerContainer);
		contentContainer.AddChild(addElementButton);

		overflowContainer.Visible = false;

		outerContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		outerContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		arraySizeContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		arraySizeContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		foldedContentContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		foldedContentContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		contentContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		contentContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		innerContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		innerContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		overflowContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		overflowContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		addElementButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		addElementButton.SizeFlagsVertical = SizeFlags.ExpandFill;

		AddFocusable(arraySizeContainer);
		AddFocusable(addElementButton);

		outerContainer.ClipContents = true;
		arraySizeContainer.ClipContents = true;
		foldedContentContainer.ClipContents = true;
		contentContainer.ClipContents = true;
		innerContainer.ClipContents = true;
		addElementButton.ClipContents = true;

		arraySizeContainer.Prefix = "Array Size:";
		arraySizeContainer.AllowGreater = true;

		innerContainer.Name = "0";
		addElementButton.Text = "+ Add Element";

		foldedContentContainer.TitleAlignment = HorizontalAlignment.Center;
		foldedContentContainer.Folded = true;

		UpdateFoldedContainerTitle();

		//Connect(SignalName.PropertyCanRevertChanged, new(this, MethodName.OnPropertyDeleted));
		arraySizeContainer.Connect(SpinBox.SignalName.ValueChanged, new(this, MethodName.OnSizeBoxValueChanged));
		addElementButton.Connect(Button.SignalName.Pressed, new(this, MethodName.OnAddElementButtonPressed));
    }

	private void OnSizeBoxValueChanged(float value)
    {
		if (updating)
        {
            return;
        }

		int nextCount = (int)Math.Round(value);

		if (nextCount == 0)
        {
            foldedContentContainer.Folded = true;
        }
		else
        {
			foldedContentContainer.Folded = false;
        }

		var nextValues = new string[nextCount];

		UpdatePropertyEditor(nextValues, true);
		UpdateFoldedContainerTitle();
		CheckTabOverflowContainer();
    }

	private void OnAddElementButtonPressed()
    {
        AddInspectorProperty("", propertyName + (currentlyCachedPropertyValues.Count - 1));

		UpdateArraySizeContainerValue(1);

		EmitChanged(GetEditedProperty(), currentlyCachedPropertyValues.ToArray());
		UpdateFoldedContainerTitle();
		CheckTabOverflowContainer();
    }

	private void OnUidPropertyChanged(UidInspectorProperty inspectorProperty, string newValue)
    {
		if (!cachedPropertyIdsById.TryGetValue(inspectorProperty.GetInstanceId(), out int foundIndex))
        {
            return;
        }

		string oldValue = currentlyCachedPropertyValues[foundIndex];

		if (oldValue == newValue)
        {
            return;
        }

		currentlyCachedPropertyValues[foundIndex] = newValue;

		EmitChanged(GetEditedProperty(), currentlyCachedPropertyValues.ToArray());
    }

	private void OnDeleteFromArrayButton(UidInspectorProperty inspectorProperty)
    {
		RemoveInspectorProperty(inspectorProperty.GetArrayIndex());

		UpdateArraySizeContainerValue(-1);

		RecalculateArrayIndices();

		EmitChanged(GetEditedProperty(), currentlyCachedPropertyValues.ToArray());
		UpdateFoldedContainerTitle();
		CheckTabOverflowContainer(true);
    }

	private void OnPropertyDeleted(string deletedPropertyName)
    {
		PropertyReset(deletedPropertyName);
    }

	private void UpdateFoldedContainerTitle()
    {
		foldedContentContainer.Title = $"{godotType} (size {currentlyCachedPropertyValues.Count})";
    }

	private void UpdateArraySizeContainerValue(int addedAmount)
    {
        int currentValue = (int)arraySizeContainer.Value;
		currentValue += addedAmount;
		arraySizeContainer.Value = currentValue;
    }

	private void CheckTabOverflowContainer(bool ensureReSort = false)
    {
		void AddContainers(int addAmount)
        {
			for (int i = 0; i < addAmount; i++)
            {
                VBoxContainer nextContainer = new();
				nextContainer.Name = $"{allOverflowContainers.Count + 1}";
				overflowContainer.AddChild(nextContainer);
				allOverflowContainers.Add(nextContainer);
            }
        }

		void RemoveContainers(int removeAmount)
        {
			for (int i = 0; i < removeAmount; i++)
			{
				int lastIndex = allOverflowContainers.Count - 1;
				VBoxContainer foundContainer = allOverflowContainers[lastIndex];
				foundContainer.QueueFree();
				allOverflowContainers.RemoveAt(lastIndex);
			}
        }

		void ReSortContainerContents(int arrayTabCount, int? containerIndexOverride = null)
        {
			if (!containerIndexOverride.HasValue)
            {
				nextFoldableContainerIndex = 0;
            }
			else
            {
                nextFoldableContainerIndex = containerIndexOverride.Value;
            }

			for (int i = 0; i < cachedPropertiesByIndex.Count; i++)
            {
                UidInspectorProperty foundInspector = cachedPropertiesByIndex[i];

				if (foundInspector == null || !IsInstanceValid(foundInspector))
                {
					if (plugin.DevModeEnabled) GD.PrintErr($"ReSortContainerContents>invalid inspector at index: {i}.");
					RemoveInspectorProperty(i);
                    continue;
                }

				if (nextFoldableContainerIndex != -1 && ((i % arrayTabCount) == 0))
                {
					nextFoldableContainerIndex++;
                }

				Control activeAddContainer = GetActiveAddContainer();
				Control currentParent = foundInspector.GetParentOrNull<Control>();

				if (currentParent.GetInstanceId() != activeAddContainer.GetInstanceId())
                {
					foundInspector.Reparent(activeAddContainer);
                }
            }
        }

		if (plugin == null || !IsInstanceValid(plugin))
        {
			GD.PrintErr("UID Explorer Plugin>plugin is null (engine bug). Editor needs restart for plugin to work correctly. (Make sure to save first!)");
			return;
        }

		int arrayTabCount = plugin.ArrayTabCount;

		double nextCreatedTabCountFloating = (double)currentlyCachedPropertyValues.Count / (double)arrayTabCount;
		
		int nextCreatedTabCount = (int)Math.Ceiling(nextCreatedTabCountFloating);
		int currentTabCount = overflowContainer.GetChildCount();

		int difference = nextCreatedTabCount - currentTabCount;

		nextFoldableContainerIndex = currentTabCount;

		if (nextCreatedTabCount <= 1)
        {
            nextFoldableContainerIndex = -1;
        }

		if (ensureReSort && nextCreatedTabCount > 1)
        {
            ReSortContainerContents(arrayTabCount);
        }

		if (difference == 0)
        {
			return;
        }
		else if (difference > 0)
        {
			AddContainers(difference);
        }
		else // (difference < 0)
        {
			RemoveContainers(Math.Abs(difference));
        }

		if (nextCreatedTabCount > 1 && overflowContainer.Visible)
        {
            ReSortContainerContents(arrayTabCount);
        }

		if (nextCreatedTabCount <= 1)
        {
			if (overflowContainer.Visible)
            {
				ReSortContainerContents(arrayTabCount, -1);
            }
	
            overflowContainer.Visible = false;
			innerContainer.Visible = true;
        }
		else
        {
			if (!overflowContainer.Visible)
            {
               ReSortContainerContents(arrayTabCount);
            }

			overflowContainer.Visible = true;
			innerContainer.Visible = false;
        }
    }

	private VBoxContainer GetActiveAddContainer()
    {
		if (nextFoldableContainerIndex > 0 && nextFoldableContainerIndex <= allOverflowContainers.Count)
        {
			return allOverflowContainers[nextFoldableContainerIndex - 1];
        }
		else
        {
            return innerContainer;
        }
    }

	private void PropertyReset(string deletedPropertyName)
    {
		if (deletedPropertyName != propertyName)
        {
            return;
        }

        UpdatePropertyEditor(Array.Empty<string>(), false);
    }

    public override void _UpdateProperty()
    {
		string[] savedProperties = GetAllSavedProperties();

		if (savedProperties == null)
        {
            return;
        }

		if (savedProperties.Length == currentlyCachedPropertyValues.Count)
        {
            return;
        }

		updating = true;

		arraySizeContainer.Value = savedProperties.Length;

		UpdatePropertyEditor(savedProperties, false);
		UpdateFoldedContainerTitle();
		CheckTabOverflowContainer();

		updating = false;
    }

	private string[] GetAllSavedProperties()
    {
		GodotObject foundObject = GetEditedObject();

		if (foundObject == null || !IsInstanceValid(foundObject))
        {
			GD.PrintErr("UidPropertyArray>GetAllSavedProperties>object is null.");
            return null;
        }

		Variant foundVariant = foundObject.Get(GetEditedProperty());

		if (foundVariant.VariantType != Variant.Type.PackedStringArray)
        {
			GD.PrintErr($"UidPropertyArray>GetAllSavedProperties>variant type: {foundVariant.VariantType}.");
            return null;
        }

		return foundVariant.As<string[]>();
    }

	private void UpdatePropertyEditor(string[] nextValues, bool emitChanges)
    {
		void AddProperties(string[] values, int count)
        {
			for (int i = 0; i < count; i++)
			{
				AddInspectorProperty(values[i], propertyName + (currentlyCachedPropertyValues.Count - 1));
			}
        }

		void RemoveProperties(int count)
        {
            for (int i = 0; i < count; i++)
            {
				//if (cachedPropertiesByIndex.Count == 0)
				//{
				//	break;
				//}

				RemoveInspectorProperty(cachedPropertiesByIndex.Count - 1);
            }
        }

		int nextCount = nextValues.Length;
		int difference = nextCount - currentlyCachedPropertyValues.Count;

		if (difference == 0)
        {
            return;
        }
		else if (difference > 0)
        {
            AddProperties(nextValues, difference);
        }
		else // (difference < 0)
        {
            RemoveProperties(Math.Abs(difference));
        }

		if (emitChanges)
		{
			EmitChanged(GetEditedProperty(), currentlyCachedPropertyValues.ToArray());
		}
    }

	private void AddInspectorProperty(string content, string inspectorPropertyName)
    {
		UidInspectorProperty inspectorProperty = uidInspector.CreateInspectorProperty(inspectorPropertyName, true, GetActiveAddContainer());
		inspectorProperty.SetContent(content);
		inspectorProperty.SetArrayIndex(currentlyCachedPropertyValues.Count);

		inspectorProperty.Connect(UidInspectorProperty.SignalName.UidPropertyChanged, new(this, MethodName.OnUidPropertyChanged));
		inspectorProperty.Connect(UidInspectorProperty.SignalName.DeleteFromArrayButton, new(this, MethodName.OnDeleteFromArrayButton));
		
		cachedPropertiesById.TryAdd(inspectorProperty.GetInstanceId(), inspectorProperty);
		cachedPropertyIdsById.TryAdd(inspectorProperty.GetInstanceId(), currentlyCachedPropertyValues.Count);
		currentlyCachedPropertyValues.Add(content);
		cachedPropertiesByIndex.Add(inspectorProperty);
    }

	private bool RemoveInspectorProperty(int removeIndex)
    {
		UidInspectorProperty lastProperty = cachedPropertiesByIndex[removeIndex];

		if (lastProperty == null || !IsInstanceValid(lastProperty))
        {
			if (plugin.DevModeEnabled) GD.Print("UidPropertyArray>RemoveInspectorProperty>property is null.");
            return false;
        }

		if (!uidInspector.RemoveInspectorProperty(lastProperty.GetInstanceId()))
        {
            lastProperty.QueueFree();
        }

		cachedPropertiesByIndex.RemoveAt(removeIndex);
		currentlyCachedPropertyValues.RemoveAt(removeIndex);

		cachedPropertiesById.Remove(lastProperty.GetInstanceId());
		cachedPropertyIdsById.Remove(lastProperty.GetInstanceId());

		return true;
    }

	private void RecalculateArrayIndices()
    {
		for (int i = 0; i < cachedPropertiesByIndex.Count; i++)
        {
            UidInspectorProperty inspectorProperty = cachedPropertiesByIndex[i];

			if (inspectorProperty == null || !IsInstanceValid(inspectorProperty))
            {
				if (plugin.DevModeEnabled) GD.PrintErr($"RecalculateArrayIndices>property is null at index: {i}.");
                continue;
            }

			inspectorProperty.SetArrayIndex(i);
        }
    }
}

#endif