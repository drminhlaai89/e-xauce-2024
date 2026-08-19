using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using System;

[Serializable]
public class GameobjectLists
{
    [SerializeField] List<GameObject> m_objectsList = new List<GameObject>();
    public List<GameObject> ObjectsList
    {
        get => m_objectsList;
        set { m_objectsList = value; }
    }
}

public class UIManager : MonoBehaviour
{
    static UIManager m_instance;
    public static UIManager Instance { get => m_instance; }

    #region CREATE ROOM
    [Header("[ SESSION ROOM UI ]")]
    [SerializeField] GameObject m_sessionRoomUI;
    public GameObject SessionRoomUI { get => m_sessionRoomUI; }

    [SerializeField] Button m_sessionButton;
    public Button SessionButton { get => m_sessionButton; }

    [SerializeField] TextMeshProUGUI m_announceText;
    public TextMeshProUGUI AnnounceText { get => m_announceText; }
    #endregion

    #region LOADING
    [Header("[ LOADING UI ]")]
    [SerializeField] GameObject m_loadingUI;
    public GameObject LoadingUI { get => m_loadingUI; }
    #endregion

    #region MAIN MENU
    [Header("[ MAIN MENU UI ]")]
    [SerializeField] GameObject m_mainMenuUI;
    public GameObject MainMenuUI { get => m_mainMenuUI; }
    #endregion

    #region VIDEO PLAYER
    [Header("[ VIDEO CONTROL UI ]")]
    [SerializeField] GameObject m_videoControlUI;
    public GameObject VideoControlUI { get => m_videoControlUI; }

    [SerializeField] GameObject m_pleaseWaitingUI;
    public GameObject PleaseWaitingUI { get => m_pleaseWaitingUI; }
    #endregion

    [SerializeField] List<GameobjectLists> m_gameobjectLists = new List<GameobjectLists>();
    public List<GameobjectLists> GameobjectLists { get => m_gameobjectLists; }

    private void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ActivateUI(m_sessionRoomUI);
    }

    //Bật Gamobject được chọn và tắt tất cả những gameobject còn lại trong cùng một List.
    public void ActivateUI(GameObject activateObj)
    {
        // Lập qua cá List<GameObject> để xem cái nào đang chứa gameobject đó.
        m_gameobjectLists.ForEach(gameobjectsList =>
        {
            if (gameobjectsList.ObjectsList.Contains(activateObj))
                gameobjectsList.ObjectsList.ForEach(obj => { obj.SetActive(obj == activateObj); });
        });
    }
}
