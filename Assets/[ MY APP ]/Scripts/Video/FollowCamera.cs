using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public GameObject menuMove;
    public float speed = 3f;
    private bool allowMove;
    private float fadeTime;


    // Update is called once per frame
    void Update()
    {

        if (Quaternion.Angle(menuMove.transform.rotation, transform.rotation) > 35f)
        {
            allowMove = true;
        }
        if (Quaternion.Angle(menuMove.transform.rotation, transform.rotation) < 0.5f)
        {
            allowMove = false;
        }
        if (allowMove)
        {
            moveObject();
        }

    }

    public void moveObject()
    {
        menuMove.transform.rotation = Quaternion.Lerp(menuMove.transform.rotation, transform.rotation, speed * Time.deltaTime);
    }

}
