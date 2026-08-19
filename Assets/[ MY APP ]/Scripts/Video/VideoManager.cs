using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

public class VideoManager : MonoBehaviourPunCallbacks
{
    static VideoManager m_instance;
    public static VideoManager Instance { get => m_instance; }

    [SerializeField] Material m_videoMaterial;
    public Material VideoMaterial { get => m_videoMaterial; }

    [SerializeField] VideoPlayer m_videoPlayer;
    public VideoPlayer VideoPlayer { get => m_videoPlayer; }

    [SerializeField] GameObject leftController;
    [SerializeField] GameObject rightController;

    UIManager uiManager;
    DeviceManager deviceManager;

    bool isPlayingVideoPrivate;
    bool m_enterVideoMode;
    public bool EnterVideoMode { 
        get => m_enterVideoMode;  
        set => m_enterVideoMode = value;
    }

    void OnEnable()
    {
        m_videoPlayer.prepareCompleted += OnPrepareVideo;
        m_videoPlayer.loopPointReached += StopWhenEndVideo;
    }

    void OnDisable()
    {
        m_videoPlayer.prepareCompleted -= OnPrepareVideo;
        m_videoPlayer.loopPointReached -= StopWhenEndVideo;
    }

    private void Awake()
    {
        m_instance = this;
    }

    void Start()
    {
        uiManager = UIManager.Instance;
        deviceManager = DeviceManager.Instance;
    }

    //Setup thông tin cho video cần play.
    public void ApplyVideoToPlay(string mp4File, bool isPrivate)
    {
        if (!isPrivate)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.OpCleanRpcBuffer(photonView);
                photonView.RPC("RPC_EnterVideoMode", RpcTarget.All, mp4File, isPrivate);
            }
        }
        else
        {
            RPC_ApplyVideoToPlay(mp4File, isPrivate);
        }
    }

    [PunRPC]
    public void RPC_EnterVideoMode(string mp4File, bool isPrivate)
    {
        m_enterVideoMode = true;

        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_ApplyVideoToPlay", RpcTarget.AllBuffered, mp4File, isPrivate);
    }

    [PunRPC]
    public void RPC_ApplyVideoToPlay(string mp4File, bool isPrivate)
    {
        isPlayingVideoPrivate = isPrivate;
        StartCoroutine(IE_RPC_ApplyVideoToPlay(mp4File));
    }

    IEnumerator IE_RPC_ApplyVideoToPlay(string mp4File)
    {
        yield return new WaitUntil(() => uiManager.MainMenuUI.activeInHierarchy || uiManager.VideoControlUI.activeInHierarchy);
        if (uiManager.VideoControlUI.activeInHierarchy)
            RPC_StopVideo();

        //Bật UI VideoControl.
        if (isPlayingVideoPrivate || PhotonNetwork.IsMasterClient)
        {
            uiManager.ActivateUI(uiManager.VideoControlUI);
        }
        else
        {
            uiManager.MainMenuUI.SetActive(false);
            uiManager.VideoControlUI.SetActive(false);
        }

        //Mở UI pleaseWaiting trong thời gian setup.
        uiManager.PleaseWaitingUI.SetActive(true);
        m_videoPlayer.url = null;

        string mp4FilePath = Path.Combine(deviceManager.DeviceURL, mp4File);
        //Nếu có thiết bị có video đã được download về thì gắn đường dẫn vào videoPlayer và prepare video.
        if (File.Exists(mp4FilePath))
        {
            m_videoPlayer.url = mp4FilePath;
            m_videoPlayer.Prepare();
        }
    }

    //Chuẩn bị video.
    void OnPrepareVideo(VideoPlayer source)
    {
        //Tạo render texture bằng đúng kích thước của video.
        int width = (int)m_videoPlayer.width;
        int height = (int)m_videoPlayer.height;
        RenderTexture renderTexture = new RenderTexture(width, height, 0);

        // Set video material bằng render texture vừa được tạo.
        m_videoMaterial.SetTexture("_MainTex", renderTexture);
        m_videoPlayer.GetComponent<MeshRenderer>().material = m_videoMaterial;

        //Tắt UI pleaseWaiting và play video.
        uiManager.PleaseWaitingUI.SetActive(false);

        if (m_enterVideoMode)
        {
            m_videoPlayer.Play();
        }
        else
        {
            if (!isPlayingVideoPrivate && !PhotonNetwork.IsMasterClient)
                GetMasterVideoState();
        }

        leftController.SetActive(false);
        rightController.SetActive(false);
    }

    //Dừng khi hết video.
    void StopWhenEndVideo(VideoPlayer videoPlayer)
    {
        RPC_StopVideo();
    }

    void GetMasterVideoState()
    {
        photonView.RPC("RPC_GetMasterVideoState", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_GetMasterVideoState(int playerRequest)
    {
        photonView.RPC("RPC_SendVideoStateToPlayer", PhotonNetwork.CurrentRoom.GetPlayer(playerRequest), m_videoPlayer.isPlaying, m_videoPlayer.frame);
    }

    [PunRPC]
    void RPC_SendVideoStateToPlayer(bool isPlaying, long videoFrame)
    {
        m_videoPlayer.frame = videoFrame;
        m_videoPlayer.Play();

        if (!isPlaying)
            m_videoPlayer.Stop();
    }

    //Tạm Dừng video.
    public void PauseVideo()
    {
        if (!isPlayingVideoPrivate)
            photonView.RPC("RPC_PauseVideo", RpcTarget.All);
        else
            RPC_PauseVideo();
    }

    [PunRPC]
    public void RPC_PauseVideo()
    {
        if (m_videoPlayer.url != null && m_videoPlayer.isPlaying)
            m_videoPlayer.Pause();
    }

    //Resume Video
    public void ResumeVideo()
    {
        if (!isPlayingVideoPrivate)
            photonView.RPC("RPC_ResumeVideo", RpcTarget.All);
        else
            RPC_ResumeVideo();
    }

    [PunRPC]
    public void RPC_ResumeVideo()
    {
        if (m_videoPlayer.url != null && !m_videoPlayer.isPlaying)
            m_videoPlayer.Play();
    }

    //Dừng video.
    public void StopVideo()
    {
        if (!isPlayingVideoPrivate)
            photonView.RPC("RPC_StopVideo", RpcTarget.All);
        else
            RPC_StopVideo();
    }

    [PunRPC]
    public void RPC_StopVideo()
    {
        if (m_videoPlayer.url != null && m_videoPlayer.frame > 0)
        {
            m_videoPlayer.Stop();
            m_videoMaterial.SetTexture("_MainTex", null);
            uiManager.PleaseWaitingUI.SetActive(true);
        }
    }

    //Thoát khỏi chế độ xem video.
    public void ExitVideoPlayer()
    {
        if (!isPlayingVideoPrivate)
        {
            PhotonNetwork.OpCleanRpcBuffer(photonView);
            photonView.RPC("RPC_ExitVideoPlayer", RpcTarget.All);
        }
        else
        {
            RPC_ExitVideoPlayer();
        }
    }

    [PunRPC]
    public void RPC_ExitVideoPlayer()
    {
        if (m_videoPlayer.url != null && m_videoPlayer.frame > 0)
            RPC_StopVideo();

        uiManager.ActivateUI(uiManager.MainMenuUI);

        leftController.SetActive(true);
        rightController.SetActive(true);
    }

    public void UpdateVIdeoTime(long videoTime)
    {
        if (!isPlayingVideoPrivate)
            photonView.RPC("RPC_UpdateVIdeoTime", RpcTarget.All, videoTime);
        else
            RPC_UpdateVIdeoTime(videoTime);
    }

    [PunRPC]
    public void RPC_UpdateVIdeoTime(long videoTime)
    {
        if (m_videoPlayer.url != null)
            m_videoPlayer.frame = videoTime;
    }
}
