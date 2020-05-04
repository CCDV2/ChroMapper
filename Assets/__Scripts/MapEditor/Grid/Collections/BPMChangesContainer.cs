﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMChangesContainer : BeatmapObjectContainerCollection {

    public float lastBPM;
    public int lastCheckedBPMIndex = 0;

    private bool firstSeen = false;
    private IEnumerable<Renderer> allGridRenderers;
    [SerializeField] private Transform gridRendererParent;
    [SerializeField] private GameObject bpmPrefab;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.BPM_CHANGE;

    private void Start()
    {
        allGridRenderers = gridRendererParent.GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name == "Grid ZDir");
        lastBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent += EditorScaleChanged;
    }

    private void EditorScaleChanged(int obj)
    {
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloat("_EditorScale", EditorScaleController.EditorScale);
        }
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
    }

    public override void SortObjects()
    {
        List<float> bpmChangeTimes = new List<float>();
        List<float> bpmChangeBPMS = new List<float>();
        bpmChangeTimes.Add(0);
        bpmChangeBPMS.Add(BeatSaberSongContainer.Instance.song.beatsPerMinute);
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        foreach (BeatmapBPMChangeContainer con in LoadedContainers)
        {
            con.UpdateGridPosition();
            BeatmapBPMChange bpmChange = con.objectData as BeatmapBPMChange;
            bpmChangeTimes.Add(bpmChange._time);
            bpmChangeBPMS.Add(bpmChange._BPM);
        }
        foreach (Renderer renderer in allGridRenderers)
        {
            renderer.material.SetFloatArray("_BPMChange_Times", bpmChangeTimes.ToArray());
            renderer.material.SetFloatArray("_BPMChange_BPMs", bpmChangeBPMS.ToArray());
            renderer.material.SetInt("_BPMChange_Count", bpmChangeBPMS.Count);
            renderer.material.SetFloat("_EditorScale", EditorScaleController.EditorScale);
        }
    }

    public float FindRoundedBPMTime(float beatTimeInSongBPM)
    {
        float snap = 1f / AudioTimeSyncController.gridMeasureSnapping;
        BeatmapBPMChange lastBPM = FindLastBPM(beatTimeInSongBPM);
        if (lastBPM is null) return (float)Math.Round(beatTimeInSongBPM / snap, MidpointRounding.AwayFromZero) * snap;
        float difference = beatTimeInSongBPM - lastBPM._time;
        float differenceInBPMBeat = difference / BeatSaberSongContainer.Instance.song.beatsPerMinute * lastBPM._BPM;
        float roundedDifference = (float)Math.Round(differenceInBPMBeat / snap, MidpointRounding.AwayFromZero) * snap;
        float roundedDifferenceInSongBPM = roundedDifference / lastBPM._BPM * BeatSaberSongContainer.Instance.song.beatsPerMinute;
        return roundedDifferenceInSongBPM + lastBPM._time;
    }

    public BeatmapBPMChange FindLastBPM(float beatTimeInSongBPM)
    {
        return LoadedContainers.LastOrDefault(x => x.objectData._time <= beatTimeInSongBPM)?.objectData as BeatmapBPMChange ?? null;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        conflicting = null;
        BeatmapBPMChangeContainer beatmapBPMChange = BeatmapBPMChangeContainer.SpawnBPMChange(obj as BeatmapBPMChange, ref bpmPrefab);
        beatmapBPMChange.transform.SetParent(GridTransform);
        beatmapBPMChange.UpdateGridPosition();
        LoadedContainers.Add(beatmapBPMChange);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapBPMChange;
    }
}
