using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.Networking;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    static NetworkManager m_instance;
    public static NetworkManager Instance { get => m_instance; }

    [SerializeField] string m_roomName;
    public string RoomName
    {
        get => m_roomName;
        set => m_roomName = value;
    }

    UIManager uiManager;
    VideoManager videoManager;
    ServerManager serverManager;


    private void Awake()
    {
        m_instance = this;
    }

    void Start()
    {
        //Connect vào server.
        /// <seealso cref="OnConnectedToMaster"/>
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        PhotonNetwork.KeepAliveInBackground = 3600;

        uiManager = UIManager.Instance;
        videoManager = VideoManager.Instance;
        serverManager = ServerManager.Instance;

        PhotonNetwork.NetworkingClient.EventReceived += OnReceivedRequestData;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnReceivedRequestData;
    }

    //Tạo hoặc tham gia phòng.
    public void JoinOrCreateRoom()
    {
        if (string.IsNullOrEmpty(m_roomName))
        {
            //Nếu inputfield chưa được nhập tên thì thông báo.
            StartCoroutine(Announce("S'il vous plaît, entrez le nom de la salle !"));
        }
        else
        {
            //Bật UI Loading lên.
            uiManager.ActivateUI(uiManager.LoadingUI);

            //Thiết lập thông tin phòng.
            RoomOptions options = new RoomOptions { MaxPlayers = 8, PlayerTtl = 5000, EmptyRoomTtl = 0 };

            /// <seealso cref="OnJoinedRoom"/>
            PhotonNetwork.JoinOrCreateRoom(m_roomName.Trim(), options, null);
        }
    }

    //Tham gia phòng.
    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(m_roomName))
        {
            //Nếu inputfield chưa được nhập tên thì thông báo.
            StartCoroutine(Announce("S'il vous plaît, entrez le nom de la salle !"));
        }
        else
        {
            //Bật UI Loading lên.
            uiManager.ActivateUI(uiManager.LoadingUI);

            //Thiết lập thông tin phòng.
            RoomOptions options = new RoomOptions { MaxPlayers = 8, PlayerTtl = 5000 };

            /// <seealso cref="OnJoinedRoom"/>
            PhotonNetwork.JoinRoom(m_roomName.Trim());
        }
    }

    public void LeaveRoom()
    {
        //Bật UI Loading.
        uiManager.PleaseWaitingUI.SetActive(false);
        uiManager.ActivateUI(uiManager.LoadingUI);

        PhotonNetwork.LeaveRoom();
    }

    //Chạy thông báo
    IEnumerator Announce(string announceText)
    {
        if (string.IsNullOrEmpty(uiManager.AnnounceText.text))
        {
            //Nếu thông báo đang không có chữ thì gán chữ vào.
            uiManager.AnnounceText.text = announceText;

            yield return new WaitForSeconds(4.0f);

            // Chờ sau một khoảng thời gian rồi tắt thông báo.
            uiManager.AnnounceText.text = null;
        }
    }

    //Join rôm không thành công.
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        //Thông báo.
        StartCoroutine(Announce("Rejoindre la salle à échoué. Attendre la création de la salle puis réessayer"));
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (videoManager.VideoPlayer != null)
        {
            videoManager.VideoPlayer.Stop();
            videoManager.VideoMaterial.SetTexture("_MainTex", null);
            uiManager.PleaseWaitingUI.SetActive(true);
        }
        uiManager.ActivateUI(uiManager.MainMenuUI);

        LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Dictionary<string, UnityWebRequest> filesDownloading = DownloadManager.Instance.FilesDownloading;
        if (filesDownloading.Count > 0)
        {
            // Hủy các download còn đang tải.
            foreach (KeyValuePair<string, UnityWebRequest> file in filesDownloading)
            {
                file.Value.Abort();
            }

            filesDownloading.Clear();
        }

        uiManager.ActivateUI(uiManager.SessionRoomUI);
        videoManager.EnterVideoMode = false;
    }

    void OnReceivedRequestData(EventData eventData)
    {
        if (eventData.Code == 0)
        {
            string responseValue = JsonUtility.ToJson(serverManager.FoldersData);

            RaiseEventOptions options = new RaiseEventOptions
            {
                TargetActors = new int[] { eventData.Sender },
            };

            PhotonNetwork.RaiseEvent(1, responseValue, options, SendOptions.SendReliable);
        }
    }

    public void SetPlayerNickName(Player targetPlayer, string playerNickname)
    {
        photonView.RPC("RPC_SetPlayerNickName", targetPlayer, playerNickname);
    }

    [PunRPC]
    void RPC_SetPlayerNickName(string nickName)
    {
        PhotonNetwork.NickName = nickName;
        PlayerPrefs.SetString("Device", nickName);
    }
}
