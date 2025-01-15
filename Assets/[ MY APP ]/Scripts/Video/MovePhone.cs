using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePhone : MonoBehaviour
{
    public Transform target;
    public float distance = 10.0f;
    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public float x = 0.0f;
    public float y = 0.0f;

    private Vector2 oldPosition1;
    private Vector2 oldPosition2;

    private bool isSimulatingTouch = false; // To track mouse drag in editor

    void Start()
    {
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Make the rigid body not change rotation   
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    void Update()
    {
#if UNITY_EDITOR
        // Simulate single touch with left mouse button
        if (Input.GetMouseButton(0))
        {
            if (!isSimulatingTouch)
            {
                isSimulatingTouch = true;
            }

            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }
        else
        {
            isSimulatingTouch = false;
        }

        // Simulate pinch zoom with mouse scroll wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0f && distance > 5)
        {
            distance -= 0.2f;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f && distance < 18.5)
        {
            distance += 0.2f;
        }
#else
        // Touch input for devices
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        }

        if (Input.touchCount > 1)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
            {
                var tempPosition1 = Input.GetTouch(0).position;
                var tempPosition2 = Input.GetTouch(1).position;

                if (isEnlarge(oldPosition1, oldPosition2, tempPosition1, tempPosition2))
                {
                    if (distance > 5)
                    {
                        distance -= 0.2f;
                    }
                }
                else
                {
                    if (distance < 18.5)
                    {
                        distance += 0.2f;
                    }
                }

                oldPosition1 = tempPosition1;
                oldPosition2 = tempPosition2;
            }
        }
#endif
    }

    bool isEnlarge(Vector2 oP1, Vector2 oP2, Vector2 nP1, Vector2 nP2)
    {
        var leng1 = Vector2.Distance(oP1, oP2);
        var leng2 = Vector2.Distance(nP1, nP2);
        return leng1 < leng2;
    }

    void LateUpdate()
    {
        if (target)
        {
            y = ClampAngle(y, yMinLimit, yMaxLimit);
            var rotation = Quaternion.Euler(y, x, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, distance) + target.position;

            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 6);
        }
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
