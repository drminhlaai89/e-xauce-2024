using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FileInfoNetwork : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_videoName;
    public TextMeshProUGUI VideoName { get => m_videoName; }

    [SerializeField] GameObject m_downloadProgress;
    public GameObject DownloadProgress { get => m_downloadProgress; }

    [SerializeField] TextMeshProUGUI m_progressText;
    public TextMeshProUGUI ProgressText { get => m_progressText; }

    [SerializeField] Image m_loadingBar;
    public Image LoadingBar { get => m_loadingBar; }

    [SerializeField] Button m_downloadButton;
    public Button DownloadButton { get => m_downloadButton; }

    [SerializeField] Button m_cancelButton;
    public Button CancelButton { get => m_cancelButton; }

    [SerializeField] Button m_deleteButton;
    public Button DeleteButton { get => m_deleteButton; }

    FileStatus m_fileStatus;
    public FileStatus FileStatus
    {
        get => m_fileStatus;
        set => m_fileStatus = value;
    }

    int m_folderIndex;
    public int FolderIndex
    {
        get => m_folderIndex;
        set => m_folderIndex = value;
    }

    int m_fileIndex;
    public int FileIndex
    {
        get => m_fileIndex;
        set => m_fileIndex = value;
    }

    UIManager uiManager;
    MobileManager mobileManager;
    DeviceManager deviceManager;
    DownloadManager downloadManager;
    AnimationManager animationManager;

    void Start()
    {
        uiManager = UIManager.Instance;
        mobileManager = MobileManager.Instance;
        deviceManager = DeviceManager.Instance;
        downloadManager = DownloadManager.Instance;
        animationManager = AnimationManager.Instance;

        if (PhotonNetwork.IsMasterClient)
        {
            //Thêm event Download và hủy download.
            m_downloadButton.onClick.AddListener(() => downloadManager.DownloadVideoFile(mobileManager.PlayerSelect, m_folderIndex, m_fileIndex));
            m_cancelButton.onClick.AddListener(() => downloadManager.CancelVideoDownload(mobileManager.PlayerSelect, m_folderIndex, m_fileIndex));

            //Lấy event default hiện đang có của nút confirmDelete.
            Button.ButtonClickedEvent delateDefaultEvent = mobileManager.DeleteConfirmButton.onClick;
            //Cập nhật lại event.
            m_deleteButton.onClick.AddListener(() =>
            {
                //Cập nhật thông tin tên video.
                mobileManager.DeleteInfo.text = "Voulez-vous supprimer la vidéo \"" + m_videoName.text + "\" ? ";

                //Xóa event hiện tại của nút confirmButton.
                mobileManager.DeleteConfirmButton.onClick.RemoveAllListeners();
                //Thêm lại event default vào.
                mobileManager.DeleteConfirmButton.onClick = delateDefaultEvent;
                //Thêm event Delete File của video đó vào.
                mobileManager.DeleteConfirmButton.onClick.AddListener(() => deviceManager.DeleteFile(mobileManager.PlayerSelect, m_folderIndex, m_fileIndex));

                mobileManager.DeletePopUp.SetActive(true);    
            });
        }
        else
        {
            m_downloadButton.onClick.RemoveAllListeners();
            m_cancelButton.onClick.RemoveAllListeners();
            m_deleteButton.onClick.RemoveAllListeners();
        }
    }

    void Update()
    {
        switch (m_fileStatus.status)
        {
            case Status.NotYetDownloaded:
                m_downloadButton.gameObject.SetActive(true);

                m_downloadProgress.SetActive(false);
                m_cancelButton.gameObject.SetActive(false);

                m_deleteButton.gameObject.SetActive(false);

                break;
            case Status.Downloading:
                m_downloadButton.gameObject.SetActive(false);

                m_downloadProgress.SetActive(true);
                m_cancelButton.gameObject.SetActive(true);

                m_deleteButton.gameObject.SetActive(false);
                break;
            case Status.Downloaded:
                m_downloadButton.gameObject.SetActive(false);

                m_downloadProgress.SetActive(false);
                m_cancelButton.gameObject.SetActive(false);

                m_deleteButton.gameObject.SetActive(true);
                break;
            default:
                break;
        }

        m_progressText.text = $"{(int)(m_fileStatus.progress * 100)}%";
        m_loadingBar.fillAmount = m_fileStatus.progress;
    }
}
