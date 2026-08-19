using Michsky.UI.ModernUIPack;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRManager : MonoBehaviourPunCallbacks
{
    static VRManager m_instance;
    public static VRManager Instance { get => m_instance; }

    [SerializeField] CustomDropdown m_dropDown;
    public CustomDropdown Dropdown { get => m_dropDown; }

    [SerializeField] Sprite m_roomIcon;

    List<RoomInfo> rooms = new List<RoomInfo>();

    UIManager uiManager;
    ServerManager serverManager;
    DeviceManager deviceManager;
    NetworkManager networkManager;

    private void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        uiManager = UIManager.Instance;
        serverManager = ServerManager.Instance;
        deviceManager = DeviceManager.Instance;
        networkManager = NetworkManager.Instance;
    }

    public void UpdateRoomName()
    {
        NetworkManager.Instance.RoomName = m_dropDown.selectedText.text;
    }

    public override void OnJoinedRoom()
    {
        StartCoroutine(DataHandler());
    }

    IEnumerator DataHandler()
    {
        yield return StartCoroutine(serverManager.GetServerFolder());

        deviceManager.CreateDeviceFolders();
        uiManager.ActivateUI(uiManager.MainMenuUI);
    }

    //Connect vào server thành công
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connect to master server success !!!");

        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();

        PhotonNetwork.NickName = PlayerPrefs.HasKey("Device") ? PlayerPrefs.GetString("Device") : null;
    }

    //Chạy khi có phòng được cập nhật.
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        rooms.Clear();

        //Thêm hoặc xóa phòng trong network.
        foreach (RoomInfo roomInfo in roomList)
        {
            if (!roomInfo.RemovedFromList)
                rooms.Add(roomInfo);
        }

        //Cập nhật lại giao diện cho dropdown.
        m_dropDown.dropdownItems.Clear();
        foreach (Transform child in m_dropDown.itemParent)
            Destroy(child.gameObject);

        if (rooms.Count > 0)
        {
            m_dropDown.selectedItemIndex = m_dropDown.selectedItemIndex < rooms.Count ? m_dropDown.selectedItemIndex : rooms.Count - 1;
            foreach (RoomInfo room in rooms)
            {
                m_dropDown.SetItemIcon(m_roomIcon);
                m_dropDown.SetItemTitle(room.Name);
                m_dropDown.CreateNewItem();
            }
            networkManager.RoomName = m_dropDown.selectedText.text;
            uiManager.SessionButton.interactable = true;
        }
        else
        {
            networkManager.RoomName = null;
            uiManager.SessionButton.interactable = false;

            m_dropDown.selectedText.text = "SELECTED ROOM";
            m_dropDown.selectedImage.sprite = m_roomIcon;
            m_dropDown.selectedItemIndex = 0;
        }
    }
}
