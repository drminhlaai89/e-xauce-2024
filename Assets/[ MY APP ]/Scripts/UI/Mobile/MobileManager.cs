using exauce;
using ExitGames.Client.Photon;
using Michsky.UI.ModernUIPack;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class MobileManager : MonoBehaviourPunCallbacks
{
    static MobileManager m_instance;
    public static MobileManager Instance { get => m_instance; }

    #region MAIN MENU
    [Header("[ MAIN MENU UI ]")]
    [Header("Room Info")]
    [SerializeField] TextMeshProUGUI m_roomNameText;
    public TextMeshProUGUI RoomNameText { get => m_roomNameText; }

    [SerializeField] TextMeshProUGUI m_deviceConnectText;
    public TextMeshProUGUI DeviceConnectText { get => m_deviceConnectText; }

    [SerializeField] TextMeshProUGUI m_statusConnectText;
    public TextMeshProUGUI StatusConnectText { get => m_statusConnectText; }

    [Header("__Tab__")]
    [SerializeField] List<Button> m_tabButtons = new List<Button>();
    public List<Button> TabButtons { get => m_tabButtons; }

    [SerializeField] List<GameObject> m_tabs = new List<GameObject>();
    public List<GameObject> Tabs { get => m_tabs; }

    [Header("__Folder__")]
    [SerializeField] Transform m_foldersContent;
    public Transform FoldersContent { get => m_foldersContent; }

    [SerializeField] GameObject m_folderPrefab;
    public GameObject FolderPrefab { get => m_folderPrefab; }

    [Header("__File Info__")]
    [SerializeField] TextMeshProUGUI m_title;
    public TextMeshProUGUI Title { get => m_title; }

    [SerializeField] GameObject m_deletePopUp;
    public GameObject DeletePopUp { get => m_deletePopUp; }

    [SerializeField] TextMeshProUGUI m_deleteInfo;
    public TextMeshProUGUI DeleteInfo { get => m_deleteInfo; }

    [SerializeField] Button m_deleteConfirmButton;
    public Button DeleteConfirmButton { get => m_deleteConfirmButton; }

    [SerializeField] GameObject m_fileHolderPrefab;
    public GameObject FileContentPrefab { get => m_fileHolderPrefab; }

    GameobjectLists m_fileHolderList = new GameobjectLists();

    [SerializeField] GameObject m_filePrefab;
    public GameObject FilePrefab { get => m_filePrefab; }

    [Header("__Private File Info__")]
    [SerializeField] TMP_InputField m_privateVideoName;
    public TMP_InputField PrivateVideoName { get => m_privateVideoName; }

    [SerializeField] GameObject m_passCode;
    public GameObject PassCode { get => m_passCode; }

    [SerializeField] GameObject m_wrongPasscode;
    public GameObject WrongPasscode { get => m_wrongPasscode; }

    [SerializeField] GameObject m_privateFileHolder;
    public GameObject PrivateFileHolder { get => m_privateFileHolder; }

    [SerializeField] Button m_backPrivateButton;
    public Button BackPrivateButton { get => m_backPrivateButton; }

    [Header("__Network-Device List__")]
    [SerializeField] GameObject m_deviceList;
    public GameObject DeviceList { get => m_deviceList; }

    [SerializeField] GameObject m_loadingGameobject;
    public GameObject LoadingGameobject { get => m_loadingGameobject; }

    [SerializeField] Transform m_devicesContent;
    public Transform DevicesContent { get => m_devicesContent; }

    [SerializeField] GameObject m_devicePrefab;
    public GameObject DevicePrefab { get => m_devicePrefab; }

    List<GameObject> m_deviceListExist = new List<GameObject>();

    [Header("__Network-File Network List__")]
    [SerializeField] GameObject m_deviceListFile;
    public GameObject DeviceListFile { get => m_deviceListFile; }

    [SerializeField] Transform m_videoNetworkContent;
    public Transform VideoNetworkContent { get => m_videoNetworkContent; }

    [SerializeField] GameObject m_videoNetworkPrefab;
    public GameObject VideoNetworkPrefab { get => m_videoNetworkPrefab; }

    [SerializeField] TextMeshProUGUI m_deviceSelectName;
    public TextMeshProUGUI DeviceSelectName { get => m_deviceSelectName; }

    [SerializeField] TMP_InputField m_playerNickname;
    public TMP_InputField PlayerNickname { get => m_playerNickname; }

    Player m_playerSelect;
    public Player PlayerSelect { get => m_playerSelect; }

    List<GameObject> m_fileNetworkObjectList = new List<GameObject>();
    [SerializeField] ServerFolders m_foldersNetworkData;

    #endregion

    #region VIDEO CONTROL
    [Header("[ VIDEO CONTROL UI ]")]
    [SerializeField] TextMeshProUGUI m_videoLengthText;
    public TextMeshProUGUI VideoLengthText { get => m_videoLengthText; }

    [SerializeField] TextMeshProUGUI m_videoTimeText;
    public TextMeshProUGUI VideoTimeText { get => m_videoTimeText; }

    [SerializeField] Slider m_videoSlider;
    public Slider VideoSlider { get => m_videoSlider; }
    #endregion

    #region SINGLETON
    UIManager uiManager;
    VideoManager videoManager;
    DeviceManager deviceManager;
    ServerManager serverManager;
    NetworkManager networkManager;
    AnimationManager animationManager;
    #endregion

    MovePhone movePhone;


    private void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        uiManager = UIManager.Instance;
        videoManager = VideoManager.Instance;
        deviceManager = DeviceManager.Instance;
        serverManager = ServerManager.Instance;
        networkManager = NetworkManager.Instance;
        animationManager = AnimationManager.Instance;

        movePhone = FindAnyObjectByType<MovePhone>();

        ApplyTabButtonsFunction();

        //Gán function cho event trigger của slider.
        EventTrigger trigger = m_videoSlider.GetComponent<EventTrigger>();
        if (trigger)
        {
            trigger.triggers.Add(CreateNewEntry(EventTriggerType.PointerUp, (data) =>
            {
                videoManager.ResumeVideo();
                movePhone.enabled = true;
            }));
            trigger.triggers.Add(CreateNewEntry(EventTriggerType.PointerClick, (data) => { SliderUpdateVideo(); }));
            trigger.triggers.Add(CreateNewEntry(EventTriggerType.PointerDown, (data) =>
            {
                videoManager.PauseVideo();
                movePhone.enabled = false;
            }));
            trigger.triggers.Add(CreateNewEntry(EventTriggerType.Drag, (data) => { SliderUpdateVideo(); }));
        }

        //Set độ dài video lên UI text.
        videoManager.VideoPlayer.prepareCompleted += (VideoPlayer videoPlayer) =>
        {
            m_videoLengthText.text = FormatTime(videoManager.VideoPlayer.length);
            movePhone.x = 90;
            movePhone.y = 0;
        };
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnReceivedResponseData;
    }

    void Update()
    {
        //Cập nhật giá trị của slider khi video đang chạy.
        if (videoManager.VideoPlayer.isPlaying)
            m_videoSlider.value = (float)videoManager.VideoPlayer.frame / videoManager.VideoPlayer.frameCount;

        if (!videoManager.VideoPlayer.isPrepared)
            m_videoSlider.value = 0;

        if (uiManager.MainMenuUI.activeInHierarchy)
        {
            movePhone.x = 90;
            movePhone.y = 0;
        }
    }

    public void UpdateRoomName(string roomName)
    {
        networkManager.RoomName = roomName;
    }

    //Connect vào server thành công
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connect to master server success !!!");
        uiManager.SessionButton.interactable = true;
        PhotonNetwork.NickName = PlayerPrefs.HasKey("Device") ? PlayerPrefs.GetString("Device") : null;
    }

    //Join room thành công.
    public override void OnJoinedRoom()
    {
        Debug.Log("Join room success !!!");

        m_roomNameText.text = "Room " + networkManager.RoomName;
        m_statusConnectText.text = "Statut de connection: " + PhotonNetwork.NetworkClientState;
        UpdateDeviceList();

        //Xử lý dữ liệu trên server.
        StartCoroutine(DataHandler());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateDeviceList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateDeviceList();

        if (otherPlayer == m_playerSelect)
            BackToDeviceList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateDeviceList();
    }

    #region DATA UI HANDLER
    IEnumerator DataHandler()
    {
        yield return StartCoroutine(serverManager.GetServerFolder());
        //Tạo Folder trong thiết bị.
        deviceManager.CreateDeviceFolders();

        yield return StartCoroutine(ApplyFolldersUI());

        //Sau khi tạo xong hết UI thì mở UI MainMenu lên.
        Animator loadingAnimator = uiManager.LoadingUI.GetComponent<Animator>();
        yield return StartCoroutine(animationManager.CloseAnimationEvent(loadingAnimator, "PanelFadeOut", uiManager.MainMenuUI));

        //Bật tab đầu tiên trong MainMenu lên.
        m_tabButtons[0].onClick.Invoke();
    }

    //Tạo UI cho folders.
    IEnumerator ApplyFolldersUI()
    {
        ServerFolders foldersData = serverManager.FoldersData;

        foreach (Transform child in m_foldersContent)
            Destroy(child.gameObject);

        m_fileHolderList.ObjectsList.ForEach(obj => Destroy(obj));
        m_fileHolderList.ObjectsList.Clear();

        foreach (Transform child in MapManager.Instance.MarkerContent)
            Destroy(child.gameObject);

        OnlineMapsMarkerManager.instance.RemoveAll();
        OnlineMaps.instance.OnMapUpdated = null;
        OnlineMapsCameraOrbit.instance.OnCameraControl = null;


        if (!uiManager.GameobjectLists.Contains(m_fileHolderList))
            uiManager.GameobjectLists.Add(m_fileHolderList);

        BackToDeviceList();
        uiManager.ActivateUI(m_passCode);

        RectTransform folderContentTransform = m_foldersContent.GetComponent<RectTransform>();
        folderContentTransform.anchoredPosition = new Vector2(folderContentTransform.anchoredPosition.x, 0);

        foldersData.folders.ForEach(folder =>
        {
            //Tạo folder.
            GameObject objFolder = Instantiate(m_folderPrefab, m_foldersContent);
            objFolder.name = folder.folderName;
            objFolder.GetComponentInChildren<TextMeshProUGUI>().text = folder.folderName;

            //Lấy thứ tự của folder trong list foldersData.
            int folderIndex = foldersData.folders.IndexOf(folder);

            //Gán tên folder vào text của FileHolder.
            m_title.text = folder.folderName;

            //Tạo FileHolder
            GameObject fileHolder = Instantiate(m_fileHolderPrefab, m_tabs[1].transform);
            fileHolder.name = folder.folderName + "_Holder";
            m_fileHolderList.ObjectsList.Add(fileHolder);

            //Bật FileHolder đầu tiên lên.
            fileHolder.SetActive(folderIndex == 0);

            //Gán Event cho Button của folder.
            Transform fileContent = fileHolder.GetComponent<ScrollRect>().content;
            objFolder.GetComponent<Button>().onClick.AddListener(() =>
            {
                uiManager.ActivateUI(fileHolder);
                animationManager.PlayTabAnimation(1);

                //Scroll lên trên cùng FileHolder của folder đó.
                RectTransform fileHolderContentTransform = fileContent.GetComponent<RectTransform>();
                fileHolderContentTransform.anchoredPosition = new Vector2(fileHolderContentTransform.anchoredPosition.x, 0);
            });

            //Set màu background khác cho UI folder nếu vị trí là số chẵn.
            if ((folderIndex + 1) % 2 == 0)
                objFolder.GetComponentInChildren<Image>().color = new Color32(244, 245, 246, 255);

            //Tạo UI cho các files trong từng FileHolder.
            ApplyFilesUI(folderIndex, fileContent);
        });

        yield return null;
    }

    void ApplyFilesUI(int folderIndex, Transform fileContent)
    {
        FolderInfo folder = serverManager.FoldersData.folders[folderIndex];

        folder.mp4Files.ForEach(fileStatus =>
        {
            //Tạo file.
            GameObject filePrefab = Instantiate(m_filePrefab, fileContent);
            filePrefab.name = fileStatus.fileName;

            //Set màu background cho UI file. Vị màu bắt đầu của file đầu tiên trong list sẽ tương đương với màu background của Folder.
            int fileIndex = folder.mp4Files.IndexOf(fileStatus);

            //nếu folder có vị trí là số chẵn thì các file trong list ở vị trí là số lẻ sẽ được đổi màu. Và ngược lại.
            if ((folderIndex + 1) % 2 == 0 && (fileIndex + 1) % 2 != 0)
                filePrefab.GetComponentInChildren<Image>().color = new Color32(244, 245, 246, 255);
            else if ((folderIndex + 1) % 2 != 0 && (fileIndex + 1) % 2 == 0)
                filePrefab.GetComponentInChildren<Image>().color = new Color32(244, 245, 246, 255);

            //Gán thông tin dữ liệu cho từng file.
            FileInfo fileInfo = filePrefab.GetComponent<FileInfo>();
            fileInfo.FileStatus = fileStatus;
            StartCoroutine(ApplyFileInfo(fileInfo));
        });
    }

    //Gán dữ liệu cho FileInfo.
    IEnumerator ApplyFileInfo(FileInfo fileInfo)
    {
        FileStatus fileStatus = fileInfo.FileStatus;
        //Tên video.
        fileInfo.VideoName.text = fileStatus.fileName + ".mp4";

        //Load hình ảnh và mô tả cho file lên UI.
        string serverImageURL = fileStatus.serverPath + "_Thumbnail.png";
        string serverDescriptionURL = fileStatus.serverPath + "_Description.txt";
        yield return StartCoroutine(serverManager.LoadImage(serverImageURL, thumbnail => fileInfo.ThumbnailImage.sprite = thumbnail));
        yield return StartCoroutine(serverManager.LoadText(serverDescriptionURL, description =>
        {
            fileInfo.DescriptionText.text = Regex.Replace(description, @"(?i)(Longtitude:.*?(\n|$))|(Latitude:.*?(\n|$))", string.Empty);
            MapManager.Instance.CreateMarker(description, fileInfo);
        }));
    }

    //Chạy PlayTabAnimation khi nhấn TabButton. Với index là của TabButton được chọn.
    void ApplyTabButtonsFunction()
    {
        for (int i = 0; i < m_tabButtons.Count; i++)
        {
            int buttonIndex = i;
            Button tabButton = m_tabButtons[i];

            tabButton.onClick.AddListener(() => animationManager.PlayTabAnimation(buttonIndex));
        }
    }
    #endregion

    #region NETWORK
    //Cập nhật thông tin trong phòng.
    public void UpdateDeviceList()
    {
        //Số lượng người đang trong phòng.
        m_deviceConnectText.text = "Nbr. d'appareils: " + PhotonNetwork.PlayerList.Length;

        m_deviceListExist.ForEach(device => { Destroy(device); });
        m_deviceListExist.Clear();

        //Tạo UI List các player trong room và đặt lại tên cho người chơi.
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            Player player = PhotonNetwork.PlayerList[i];

            string name = player.NickName;
            if (string.IsNullOrEmpty(player.NickName))
                name = player.IsMasterClient ? "Master" : ("Client " + player.ActorNumber.ToString());

            GameObject deviceObj = Instantiate(m_devicePrefab, m_devicesContent);
            deviceObj.name = name;
            TextMeshProUGUI deviceName = deviceObj.GetComponentInChildren<TextMeshProUGUI>();
            deviceName.text = name + (player.IsLocal ? " (you)" : "");

            deviceObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_deviceSelectName.text = name;
                m_loadingGameobject.SetActive(true);

                PhotonNetwork.NetworkingClient.EventReceived += OnReceivedResponseData;
                m_playerSelect = player;
                RequestPlayerData();
            });

            m_deviceListExist.Add(deviceObj);
        }
    }

    void CreateFilesNetworkUI()
    {
        m_foldersNetworkData.folders.ForEach(folder =>
        {
            folder.mp4Files.ForEach(fileStatus =>
            {
                string fileText = folder.folderName + "//" + fileStatus.fileName + ".mp4";
                GameObject fileNetworkPrefab = Instantiate(m_videoNetworkPrefab, m_videoNetworkContent);
                fileNetworkPrefab.name = fileText;

                FileInfoNetwork fileInfoNetwork = fileNetworkPrefab.GetComponent<FileInfoNetwork>();
                fileInfoNetwork.VideoName.text = fileText;
                fileInfoNetwork.FileStatus = fileStatus;
                fileInfoNetwork.FolderIndex = m_foldersNetworkData.folders.IndexOf(folder);
                fileInfoNetwork.FileIndex = folder.mp4Files.IndexOf(fileStatus);

                m_fileNetworkObjectList.Add(fileNetworkPrefab);
            });
        });
    }

    void RequestPlayerData()
    {
        RaiseEventOptions options = new RaiseEventOptions
        {
            TargetActors = new int[] { m_playerSelect.ActorNumber },
        };

        PhotonNetwork.RaiseEvent(0, null, options, SendOptions.SendReliable);
    }

    void OnReceivedResponseData(EventData eventData)
    {
        if (eventData.Code == 1)
        {
            string responseData = (string)eventData.CustomData;
            ServerFolders playerFoldersData = JsonUtility.FromJson<ServerFolders>(responseData);

            if (m_foldersNetworkData.folders.Count == 0)
            {
                m_foldersNetworkData = playerFoldersData;
                CreateFilesNetworkUI();
            }
            else
            {
                for (int i = 0; i < m_foldersNetworkData.folders.Count; i++)
                {
                    FolderInfo folderInfo = m_foldersNetworkData.folders[i];

                    for (int j = 0; j < folderInfo.mp4Files.Count; j++)
                    {
                        FileStatus fileStatus = folderInfo.mp4Files[j];
                        FileStatus playerFileStatus = playerFoldersData.folders[i].mp4Files[j];

                        fileStatus.fileName = playerFileStatus.fileName;
                        fileStatus.location = playerFileStatus.location;
                        fileStatus.serverPath = playerFileStatus.serverPath;
                        fileStatus.devicePath = playerFileStatus.devicePath;
                        fileStatus.status = playerFileStatus.status;
                        fileStatus.progress = playerFileStatus.progress;
                    }
                }
            }

            RequestPlayerData();

            if (m_loadingGameobject.activeInHierarchy)
            {
                m_loadingGameobject.SetActive(false);
                uiManager.ActivateUI(m_deviceListFile);
            }
        }
    }

    public void BackToDeviceList()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnReceivedResponseData;

        m_playerSelect = null;

        m_fileNetworkObjectList.ForEach(fileNetworkObject => Destroy(fileNetworkObject));
        m_fileNetworkObjectList.Clear();
        m_foldersNetworkData.folders.Clear();

        uiManager.ActivateUI(m_deviceList);
    }

    public void SetPlayerNickName()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            string playerNickName = m_playerNickname.text.Trim();

            if (!string.IsNullOrEmpty(playerNickName))
            {
                m_deviceSelectName.text = playerNickName;
                networkManager.SetPlayerNickName(m_playerSelect, playerNickName);
            }
        }
    }
    #endregion

    #region PRIVATE FILE
    //Kiểm tra video có tồn tại trong folder Private hay không.
    public void CheckPrivateFile()
    {
        string videoName = m_privateVideoName.text.Trim();

        if (!string.IsNullOrEmpty(videoName))
        {
            StartCoroutine(CheckPrivatFileExist(videoName));
        }
    }

    //Gọi lên server để kiểm tra thông tin trong folder Private.
    IEnumerator CheckPrivatFileExist(string videoName)
    {
        string serverVideoPath = System.IO.Path.Combine(serverManager.ServerURL, "Personnalisé", videoName);

        using (UnityWebRequest request = UnityWebRequest.Head(serverVideoPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Tạo UI cho video private.
                StartCoroutine(ApplyFileInfoPrivate(videoName));
            }
            else if (request.responseCode == 404)
            {
                //Hiện thông báo sai thông tin, video không tồn tại.
                m_wrongPasscode.SetActive(true);
            }
            else
            {
                Debug.Log($"Error checking file: {request.error}");
            }
        }
    }

    //Gán dữ liệu cho UI FileInfo Private.
    IEnumerator ApplyFileInfoPrivate(string videoName)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(videoName);
        string location = System.IO.Path.Combine("Personnalisé", fileName);
        string serverFilePath = System.IO.Path.Combine(serverManager.ServerURL, location);
        string deviceFilePath = System.IO.Path.Combine(deviceManager.DeviceURL, location);

        FileStatus fileStatus = new FileStatus
        {
            fileName = fileName,
            location = location,
            serverPath = serverFilePath,
            devicePath = deviceFilePath,
            status = deviceManager.CheckDeviceFile(deviceFilePath) ? Status.Downloaded : Status.NotYetDownloaded,
            progress = deviceManager.CheckDeviceFile(deviceFilePath) ? 1f : 0f,
        };

        FileInfo fileInfo = m_privateFileHolder.GetComponentInChildren<FileInfo>();
        fileInfo.FileStatus = fileStatus;
        yield return StartCoroutine(ApplyFileInfo(fileInfo));

        uiManager.ActivateUI(m_privateFileHolder);

        //Gán event cho nút back trong tab private.
        m_backPrivateButton.onClick.RemoveAllListeners();
        m_backPrivateButton.onClick.AddListener(() =>
        {
            DownloadManager.Instance.CancelVideoDownload(fileStatus);
            StartCoroutine(AnimationManager.Instance.CloseAnimationEvent(m_privateFileHolder.GetComponent<Animator>(), "PanelFadeOut", m_passCode));
        });
    }
    #endregion

    #region VIDEO PLAYER
    //Convert value của slider thành format 00:00 để bỏ vài text UI.
    string FormatTime(double timeInSeconds)
    {
        int minutes = Mathf.FloorToInt((float)timeInSeconds / 60);
        int seconds = Mathf.FloorToInt((float)timeInSeconds % 60);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    //Cập nhật videoTime text khi giá trị của slider thay đổi.
    public void SliderValueChange(Single value)
    {
        m_videoTimeText.text = FormatTime(value * videoManager.VideoPlayer.length);
        m_videoLengthText.text = FormatTime((1 - value) * videoManager.VideoPlayer.length);

    }

    //Thay đổi thời gian của video khi drag hay click slider.
    void SliderUpdateVideo()
    {
        long videoTime = (long)(m_videoSlider.value * videoManager.VideoPlayer.frameCount);
        videoManager.UpdateVIdeoTime(videoTime);
    }

    //Function để tạo event cho event trigger.
    EventTrigger.Entry CreateNewEntry(EventTriggerType eventTriggerType, UnityAction<BaseEventData> eventData)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventTriggerType;
        entry.callback.AddListener(eventData);

        return entry;
    }
    #endregion
}
