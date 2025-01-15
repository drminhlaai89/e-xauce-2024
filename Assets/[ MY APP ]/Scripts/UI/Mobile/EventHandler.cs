using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventHandler : MonoBehaviour
{
    public UnityEvent onEnable;
    public UnityEvent onDisable;

    UIManager m_uiManager;

    //Chạy khi gameobject được SetActive true.
    private void OnEnable()
    {
        m_uiManager = UIManager.Instance;
        onEnable?.Invoke();
    }

    //Chạy khi gameobject được SetActive false.
    private void OnDisable()
    {
        onDisable?.Invoke();
    }
}
