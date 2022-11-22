﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlatformDescriptorV3 : PlatformDescriptor
{
    [Header("V3 Configurations")]
    [Tooltip("V3 LightsMangaers, which supports lightColorEvent/LightRotationEvent")]
    public LightsManagerV3[] LightsManagersV3;

    [Tooltip("Map discontinuous light v2 event type")]
    public List<int> LightV2Mapping;

    public bool HasTranslationEvent = false;

    private Dictionary<int, int> groupIdToLaneIdx = new Dictionary<int, int>();

    private LightColorEventCallbackController lightColorEventCallback;
    private LightRotationEventCallbackController lightRotationEventCallback;
    private LightTranslationEventCallbackController lightTranslationEventCallback;

    private LightColorEventsContainer lightColorEventsContainer;
    private LightRotationEventsContainer lightRotationEventsContainer;
    private LightTranslationEventsContainer lightTranslationEventsContainer;

    protected new void Start()
    {
        base.Start();
        for (int i = 0; i < LightsManagersV3.Length; ++i)
        {
            groupIdToLaneIdx[LightsManagersV3[i].GroupId] = i;
        }

        lightColorEventCallback = GameObject.Find("Vertical Grid Callback").GetComponent<LightColorEventCallbackController>();
        if (lightColorEventCallback == null)
        {
            Debug.LogError("Unable to find callback, maybe prerequisite is not met?");
        }
        lightColorEventCallback.ObjectPassedThreshold += LightColorEventPassed;

        lightRotationEventCallback = GameObject.Find("Vertical Grid Callback").GetComponent<LightRotationEventCallbackController>();
        if (lightColorEventCallback == null)
        {
            Debug.LogError("Unable to find callback, maybe prerequisite is not met?");
        }
        lightRotationEventCallback.ObjectPassedThreshold += LightRotationEventPassed;

        lightTranslationEventCallback = GameObject.Find("Vertical Grid Callback").GetComponent<LightTranslationEventCallbackController>();
        if (lightTranslationEventCallback == null)
        {
            Debug.LogError("Unable to find callback, maybe prerequisite is not met?");
        }
        lightTranslationEventCallback.ObjectPassedThreshold += LightTranslationEventPassed;

        lightColorEventsContainer = FindObjectOfType<LightColorEventsContainer>();
        if (lightColorEventsContainer == null)
        {
            Debug.LogError("Unable to find lightColorEventsContainer");
        }

        lightRotationEventsContainer = FindObjectOfType<LightRotationEventsContainer>();
        if (lightRotationEventsContainer == null)
        {
            Debug.LogError("Unable to find lightRotationEventsContainer");
        }

        lightTranslationEventsContainer = FindObjectOfType<LightTranslationEventsContainer>();
        if (lightTranslationEventsContainer == null)
        {
            Debug.LogError("Unable to find lightTranslationEventsContainer");
        }

        foreach (var lighColorPlacement in FindObjectsOfType<LightColorEventPlacement>()) lighColorPlacement.platformDescriptor = this;
        foreach (var lighRotationPlacement in FindObjectsOfType<LightRotationEventPlacement>()) lighRotationPlacement.platformDescriptor = this;
    }


    protected new void OnDestroy()
    {
        lightColorEventCallback.ObjectPassedThreshold -= LightColorEventPassed;
        groupIdToLaneIdx.Clear();
        base.OnDestroy();
    }

    /// <summary>
    /// return lane index of given groupId, return -1 if not found
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public int GroupIdToLaneIndex(int groupId)
    {
        if (groupIdToLaneIdx.TryGetValue(groupId, out var idx))
        {
            return idx;
        }
        return -1;
    }

    /// <summary>
    /// return groupId of given lane index, return -1 if not found
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public int LaneIndexToGroupId(int idx)
    {
        if (idx < 0 || idx >= LightsManagersV3.Length) return -1;
        return LightsManagersV3[idx].GroupId;
    }



    public Color InferColor(int c)
    {
        var color = Color.white;
        if (c == 1) color = ColorBoost ? Colors.BlueBoostColor : Colors.BlueColor;
        else if (c == 0) color = ColorBoost ? Colors.RedBoostColor : Colors.RedColor;
        return color;
    }


    public void LightColorEventPassed(bool natural, int idx, BeatmapLightColorEvent e)
    {
        if (GroupIdToLaneIndex(e.Group) == -1) return;
        var allLights = LightsManagersV3[GroupIdToLaneIndex(e.Group)].ControllingLights;
        var eb = e.EventBoxes[0];

        var filteredLightChunks = eb.Filter.Filter(allLights);
        if (filteredLightChunks.Count() == 0) return;

        float deltaAlpha = eb.BrightnessDistribution;
        if (eb.BrightnessDistributionType == 1) deltaAlpha /= BeatmapLightEventFilter.Intervals(filteredLightChunks);
        float deltaTime = eb.Distribution;
        if (eb.DistributionType == 1) deltaTime /= BeatmapLightEventFilter.Intervals(filteredLightChunks);

        BeatmapLightEventFilter.DeltaScaleByFilterLimit(allLights, filteredLightChunks, eb.Filter, ref deltaTime, ref deltaAlpha);

        for (int i = 0; i < eb.EventDatas.Count; ++i)
        {
            var ebd = eb.EventDatas[i];
            if (i == 0 && eb.BrightnessAffectFirst == 0)
                StartCoroutine(LightColorRoutine(filteredLightChunks, deltaTime, 0, e.Group, e.Time, idx, ebd));
            else
                StartCoroutine(LightColorRoutine(filteredLightChunks, deltaTime, deltaAlpha, e.Group, e.Time, idx, ebd));
        }

    }

    public override void KillLights()
    {
        base.KillLights();
        StopAllCoroutines();
        foreach (var manager in LightsManagersV3)
        {
            if (manager != null)
                manager.ChangeAlpha(0, 1, manager.ControllingLights);
        }
        ResetNoteIndex();
    }

    private void ResetNoteIndex()
    {
        foreach (var lightManager in LightsManagersV3)
        {
            if (lightManager != null)
                lightManager.ResetNoteIndex();
        }
    }


    public void SetLightColorFromData(LightingEvent light, BeatmapLightColorEventData data, float timeToTransition)
    {
        light.TargetColorId = data.Color;
        var color = InferColor(data.Color);
        var brightness = data.Brightness;
        light.UpdateTargetColor(color.Multiply(LightsManager.HDRIntensity), timeToTransition);
        light.UpdateTargetAlpha(brightness, timeToTransition);
    }

    private IEnumerator LightColorRoutine(IEnumerable<IEnumerable<LightingEvent>> lightChunks, float deltaTime, float deltaAlpha,
        int group, float baseTime, int noteIdx, BeatmapLightColorEventData data)
    {
        var deltaSecond = Atsc.GetSecondsFromBeat(deltaTime);
        float afterSeconds = Atsc.GetSecondsFromBeat(data.AddedBeat);
        if (afterSeconds != 0.0f) yield return new WaitForSeconds(afterSeconds);
        var brightness = data.Brightness;
        float extraTime = 0;
        foreach (var lightChunk in lightChunks)
        {
            foreach (var light in lightChunk)
            {
                if (!light.SetNoteIndex(noteIdx)) continue;
                var color = InferColor(data.Color);
                color = color.Multiply(LightsManager.HDRIntensity);
                if (data.TransitionType != 2)
                {
                    light.TargetColorId = data.Color;
                    light.UpdateTargetColor(color, 0);
                    light.UpdateTargetAlpha(brightness, 0);
                }
                if (lightColorEventsContainer.TryGetNextLightEventData(group, light.LightIdx, 0, baseTime + extraTime + data.Time, out var nextData))
                {
                    if (nextData.TransitionType == 1)
                    {
                        var timeToTransition = Atsc.GetSecondsFromBeat(nextData.Time - data.Time - baseTime - extraTime);
                        /*
                        light.TargetColorId = nextData.Color;
                        var nextColor = InferColor(nextData.Color);
                        var nextAlpha = nextData.Brightness;
                        light.UpdateTargetColor(nextColor.Multiply(LightsManager.HDRIntensity), timeToTransition);
                        light.UpdateTargetAlpha(nextAlpha, timeToTransition);
                        */
                        SetLightColorFromData(light, nextData, timeToTransition);
                    }
                }
            }
            if (deltaTime != 0.0f)
                yield return new WaitForSeconds(deltaSecond);
            brightness += deltaAlpha;
            extraTime += deltaTime;
        }
        yield return null;

    }

    public void LightRotationEventPassed(bool natural, int idx, BeatmapLightRotationEvent e)
    {
        if (GroupIdToLaneIndex(e.Group) == -1) return;
        var allLights = LightsManagersV3[GroupIdToLaneIndex(e.Group)].ControllingRotations;
        var eb = e.EventBoxes[0];

        if (!LightsManagersV3[GroupIdToLaneIndex(e.Group)].IsValidRotationAxis(eb.Axis)) return;

        var filteredRotationChunks = eb.Filter.Filter(allLights);
        if (filteredRotationChunks.Count() == 0) return;
        float deltaRotation = eb.RotationDistribution;
        if (eb.ReverseRotation == 1) deltaRotation = -deltaRotation;
        if (eb.RotationDistributionType == 1) deltaRotation /= BeatmapLightEventFilter.Intervals(filteredRotationChunks);
        float deltaTime = eb.Distribution;
        if (eb.DistributionType == 1) deltaTime /= BeatmapLightEventFilter.Intervals(filteredRotationChunks);

        BeatmapLightEventFilter.DeltaScaleByFilterLimit(allLights, filteredRotationChunks, eb.Filter, ref deltaTime, ref deltaRotation);

        for (int i = 0; i < eb.EventDatas.Count; ++i)
        {
            var ebd = eb.EventDatas[i];
            if (i == 0 && eb.RotationAffectFirst == 0)
                StartCoroutine(LightRotationRoutine(filteredRotationChunks, deltaTime, 0, eb.Axis, eb.ReverseRotation == 1, e.Group, e.Time, idx, ebd));
            else
                StartCoroutine(LightRotationRoutine(filteredRotationChunks, deltaTime, deltaRotation, eb.Axis, eb.ReverseRotation == 1, e.Group, e.Time, idx, ebd));
        }
    }

    public void SetLightRotationFromData(RotatingEvent rot, BeatmapLightRotationEventData data, float timeToTransition, int axis)
    {
        var axisData = rot.GetAxisData(axis);
        axisData.UpdateRotation(data.RotationValue, timeToTransition);
        axisData.SetEaseFunction(data.EaseType);
        axisData.SetLoop(data.AdditionalLoop);
        axisData.SetDirection(data.RotationDirection);
    }

    private IEnumerator LightRotationRoutine(IEnumerable<IEnumerable<RotatingEvent>> rotationChunks, float deltaTime, float deltaRotation, int axis, bool reverse,
        int group, float baseTime, int noteIdx, BeatmapLightRotationEventData data)
    {
        var deltaSecond = Atsc.GetSecondsFromBeat(deltaTime);
        float afterSeconds = Atsc.GetSecondsFromBeat(data.AddedBeat);
        if (afterSeconds != 0.0f) yield return new WaitForSeconds(afterSeconds);
        float rotation = data.RotationValue;
        if (reverse) rotation = -rotation;
        float extraTime = 0;
        foreach (var rotationChunk in rotationChunks)
        {
            foreach (var light in rotationChunk)
            {
                var axisData = light.GetAxisData(axis);
                if (!axisData.SetNoteIndex(noteIdx)) continue;
                if (data.Transition != 1)
                {
                    axisData.UpdateRotation(rotation, 0);
                }
                if (lightRotationEventsContainer.TryGetNextLightEventData(group, light.RotationIdx, axis,
                    baseTime + extraTime + data.Time, out var nextData))
                {
                    if (nextData.Transition == 0)
                    {
                        var timeToTransition = Atsc.GetSecondsFromBeat(nextData.Time - baseTime - extraTime - data.Time);
                        /*
                        axisData.UpdateRotation(nextData.RotationValue, timeToTransition);
                        axisData.SetEaseFunction(nextData.EaseType);
                        axisData.SetLoop(nextData.AdditionalLoop);
                        axisData.SetDirection(nextData.RotationDirection);
                        */
                        SetLightRotationFromData(light, nextData, timeToTransition, axis);
                    }
                }
            }
            if (deltaTime != 0)
                yield return new WaitForSeconds(deltaSecond);
            rotation += deltaRotation;
            extraTime += deltaTime;
        }

        yield return null;
    }
    private void LightTranslationEventPassed(bool natural, int idx, BeatmapLightTranslationEvent e)
    {
        if (GroupIdToLaneIndex(e.Group) == -1) return;
        var allLights = LightsManagersV3[GroupIdToLaneIndex(e.Group)].ControllingTranslations;
        var eb = e.EventBoxes[0];

        if (!LightsManagersV3[GroupIdToLaneIndex(e.Group)].IsValidTranslationAxis(eb.Axis)) return;

        var filteredRotationChunks = eb.Filter.Filter(allLights);
        if (filteredRotationChunks.Count() == 0) return;
        float deltaOffset = eb.TranslationDistribution;
        if (eb.Flip == 1) deltaOffset = -deltaOffset;
        if (eb.TranslationDistributionType == 1) deltaOffset /= BeatmapLightEventFilter.Intervals(filteredRotationChunks);
        float deltaTime = eb.Distribution;
        if (eb.DistributionType == 1) deltaTime /= BeatmapLightEventFilter.Intervals(filteredRotationChunks);

        BeatmapLightEventFilter.DeltaScaleByFilterLimit(allLights, filteredRotationChunks, eb.Filter, ref deltaTime, ref deltaOffset);

        for (int i = 0; i < eb.EventDatas.Count; ++i)
        {
            var ebd = eb.EventDatas[i];
            if (i == 0 && eb.TranslationAffectFirst == 0)
                StartCoroutine(LightTranslationRoutine(filteredRotationChunks, deltaTime, 0, eb.Axis, eb.Flip == 1, e.Group, e.Time, idx, ebd));
            else
                StartCoroutine(LightTranslationRoutine(filteredRotationChunks, deltaTime, deltaOffset, eb.Axis, eb.Flip == 1, e.Group, e.Time, idx, ebd));
        }
    }

    public void SetLightTranslationFromData(TranslationEvent rot, BeatmapLightTranslationEventData data, float timeToTransition, int axis)
    {
        var axisData = rot.GetAxisData(axis);
        axisData.UpdateTranslation(data.TranslateValue, timeToTransition);
        axisData.SetEaseFunction(data.EaseType);
    }

    private IEnumerator LightTranslationRoutine(IEnumerable<IEnumerable<TranslationEvent>> rotationChunks, float deltaTime, float deltaOffset, int axis, bool reverse,
        int group, float baseTime, int noteIdx, BeatmapLightTranslationEventData data)
    {
        var deltaSecond = Atsc.GetSecondsFromBeat(deltaTime);
        float afterSeconds = Atsc.GetSecondsFromBeat(data.AddedBeat);
        if (afterSeconds != 0.0f) yield return new WaitForSeconds(afterSeconds);
        float offset = data.TranslateValue;
        if (reverse) offset = -offset;
        float extraTime = 0;
        foreach (var rotationChunk in rotationChunks)
        {
            foreach (var light in rotationChunk)
            {
                var axisData = light.GetAxisData(axis);
                if (!axisData.SetNoteIndex(noteIdx)) continue;
                axisData.UpdateTranslation(offset, 0);
                if (lightTranslationEventsContainer.TryGetNextLightEventData(group, light.GetIndex(), axis,
                    baseTime + extraTime + data.Time, out var nextData))
                {
                    var timeToTransition = Atsc.GetSecondsFromBeat(nextData.Time - baseTime - extraTime - data.Time);
                    SetLightTranslationFromData(light, nextData, timeToTransition, axis);
                }
            }
            if (deltaTime != 0)
                yield return new WaitForSeconds(deltaSecond);
            offset += deltaOffset;
            extraTime += deltaTime;
        }

        yield return null;
    }
}
