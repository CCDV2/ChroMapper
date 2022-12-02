using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightV3ColorBinder : MetaLightV3Binder<BeatmapLightColorEvent>, CMInput.IEventUIActions, CMInput.IWorkflowsActions
{
    public int DataIdx = 0;
    [SerializeField] private LightColorEventPlacement lightColorEventPlacement;
    protected override void InitBindings()
    {
        ObjectData = new BeatmapLightColorEvent();

        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].BrightnessDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Color.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Brightness.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Chunk.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.RandomSeed.ToString());
        InputDumpFn.Add(x => Mathf.RoundToInt(x.EventBoxes[0].Filter.Limit * 100).ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].BrightnessDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].TransitionType);
        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.RandomType);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DataDistributionEaseType);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Step");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Start");
        TextsDumpFn.Add(x => $"{DataIdx + 1}/{x.EventBoxes[0].EventDatas.Count}");
        TextsDumpFn.Add(x => DisplayingSelectedObject ? LightV3Appearance.GetTotalLightCount(x).ToString() : "-");
        TextsDumpFn.Add(x => DisplayingSelectedObject ? LightV3Appearance.GetFilteredLightCount(x).ToString() : "-");

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].BrightnessAffectFirst == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.TimeLimited);
        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.DataLimited);

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].BrightnessDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Color = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Brightness = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Chunk = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.RandomSeed = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Limit = int.Parse(s) / 100.0f);

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].BrightnessDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].TransitionType = i);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.RandomType = i);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DataDistributionEaseType = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].BrightnessAffectFirst = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.TimeLimited = b);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.DataLimited = b);

    }

    public override void Dump(BeatmapLightColorEvent obj)
    {
        var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(obj.BeatmapType);
        if (col.LoadedContainers.TryGetValue(obj, out var con))
        {
            var colorCon = con as BeatmapLightColorEventContainer;
            DataIdx = colorCon.GetRaycastedIdx();
        }
        else
        {
            DataIdx = 0;
        }
        base.Dump(obj);
    }

    public override void UpdateToPlacement()
    {
        lightColorEventPlacement.UpdateData(ObjectData);
    }

    private void DataidxSwitchDecorator(Action callback)
    {
        var cur = DataIdx;
        DataIdx = 0;
        callback();
        DataIdx = cur;
    }

    #region Input Hook
    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 0);
            InputLoadFn[6](ObjectData, "1");
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnTypeFlash(InputAction.CallbackContext context) { }
    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 0);
            InputLoadFn[6](ObjectData, "0");
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnTypeFade(InputAction.CallbackContext context) { }
    public void OnTogglePrecisionRotation(InputAction.CallbackContext context) { }
    public void OnSwapCursorInterval(InputAction.CallbackContext context) { }
    public void OnTypeTransition(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
        {
            DropdownLoadFn[3](ObjectData, 1);
        });
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }


    public void OnToggleRightButtonPanel(InputAction.CallbackContext context) { }
    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context) { }
    public void OnToggleNoteorEvent(InputAction.CallbackContext context) { }
    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "0")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "1")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DataidxSwitchDecorator(() =>
            InputLoadFn[5](ObjectData, "2")
        );
        if (!DisplayingSelectedObject) Dump(ObjectData);
        UpdateToPlacement();
    }
    public void OnPlaceObstacle(InputAction.CallbackContext context) { }
    public void OnToggleDeleteTool(InputAction.CallbackContext context) { }
    public void OnMirror(InputAction.CallbackContext context) { }
    public void OnMirrorinTime(InputAction.CallbackContext context) { }
    public void OnMirrorColoursOnly(InputAction.CallbackContext context) { }
    #endregion
}
