using Michsky.UI.ModernUIPack;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace exauce
{
    public class FileInfo : MonoBehaviour
    {
        [SerializeField] bool m_isPrivate;
        public bool IsPrivate { get => m_isPrivate; }

        [SerializeField] TextMeshProUGUI m_videoName;
        public TextMeshProUGUI VideoName { get => m_videoName; }

        [SerializeField] TextMeshProUGUI m_descriptionText;
        public TextMeshProUGUI DescriptionText { get => m_descriptionText; }

        [SerializeField] GameObject m_downloadProgress;
        public GameObject DownloadProgress { get => m_downloadProgress; }

        [SerializeField] TextMeshProUGUI m_progressText;
        public TextMeshProUGUI ProgressText { get => m_progressText; }

        [SerializeField] Image m_loadingBar;
        public Image LoadingBar { get => m_loadingBar; }

        [SerializeField] Image m_thumbnailImage;
        public Image ThumbnailImage { get => m_thumbnailImage; }

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

            //Thêm event Download và hủy download.
            m_downloadButton.onClick.AddListener(() => downloadManager.StartCoroutine(downloadManager.IE_DownloadVideoFile(m_fileStatus)));
            m_cancelButton.onClick.AddListener(() => downloadManager.CancelVideoDownload(m_fileStatus));

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
                mobileManager.DeleteConfirmButton.onClick.AddListener(() => deviceManager.DeleteFile(m_fileStatus));

                mobileManager.DeletePopUp.SetActive(true);
            });

            //Lấy event default của MainMenu EventHandler.
            EventHandler mainMenuEventHandler = uiManager.MainMenuUI.GetComponent<EventHandler>();
            UnityEvent mainMenuEvent = mainMenuEventHandler.onEnable;

            //Cập nhật lại event.
            UnityAction playVideoEvent = () =>
            {
                //Xóa event hiện tại.
                mainMenuEventHandler.onEnable.RemoveAllListeners();
                //Thêm lại event default vào.
                mainMenuEventHandler.onEnable = mainMenuEvent;
                //Thêm event PlayTabAnimation để sau khi thoát khỏi chế độ play video thì mở đúng tab trước khi play video được chạy.
                mobileManager.Tabs.ForEach(tab =>
                {
                    if (tab.activeInHierarchy)
                        mainMenuEventHandler.onEnable.AddListener(() => animationManager.PlayTabAnimation(mobileManager.Tabs.IndexOf(tab)));
                });

                VideoManager.Instance.ApplyVideoToPlay(m_fileStatus.location + ".mp4", m_isPrivate);
            };

            //Thêm event để play video.
            gameObject.GetComponent<Button>().onClick.AddListener(playVideoEvent);
            m_thumbnailImage.GetComponent<Button>().onClick.AddListener(playVideoEvent);
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
}
