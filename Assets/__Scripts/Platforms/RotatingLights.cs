﻿using UnityEngine;

public class RotatingLights : MonoBehaviour {

    private int speed;
    private Vector3 rotationVector = Vector3.up;

    private float multiplier = 20;
    private float rotationSpeed = 0;
    private Quaternion startRotation;

    private void Start()
    {
        startRotation = transform.rotation;
    }

    private void Update()
    {
        transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed, Space.Self);
    }

    public void UpdateOffset(int Speed, float Rotation, bool RotateBackwards)
    {
        speed = Speed;
        transform.rotation = startRotation;
        if (Speed > 0)
        {
            transform.Rotate(rotationVector, Rotation, Space.Self);
            rotationSpeed = speed * multiplier * (RotateBackwards ? 1 : -1);
        }
        else rotationSpeed = 0;
    }
}
