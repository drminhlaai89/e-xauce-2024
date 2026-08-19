using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadManager : MonoBehaviourPunCallbacks
{
    static DownloadManager m_instance;
    public static DownloadManager Instance { get => m_instance; }

    Dictionary<string, UnityWebRequest> m_filesDownloading = new Dictionary<string, UnityWebRequest>();
    public Dictionary<string, UnityWebRequest> FilesDownloading { get => m_filesDownloading; }

    ServerManager serverManager;
    DeviceManager deviceManager;

    private void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        serverManager = ServerManager.Instance;
        deviceManager = DeviceManager.Instance;
    }

    public void DownloadVideoFile(Player targetPlayer, int folderIndex, int fileIndex)
    {
        photonView.RPC("RPC_DownloadVideoFile", targetPlayer, folderIndex, fileIndex);
    }

    [PunRPC]
    public void RPC_DownloadVideoFile(int folderIndex, int fileIndex)
    {
        FileStatus fileStatus = serverManager.FoldersData.folders[folderIndex].mp4Files[fileIndex];
        StartCoroutine(IE_DownloadVideoFile(fileStatus));
    }

    //Download video.
    public IEnumerator IE_DownloadVideoFile(FileStatus fileStatus)
    {
        //Tạo request download file từ server vào file Temp trên thiết bị.
        UnityWebRequest request = UnityWebRequest.Get(fileStatus.serverPath + ".mp4");
        request.downloadHandler = new DownloadHandlerFile(fileStatus.devicePath + "_Temp.mp4");

        request.timeout = 0;

        //Thêm tiến trình download vào m_fileDownloading dictionary.
        m_filesDownloading[fileStatus.serverPath] = request;

        //Set Download Status
        fileStatus.status = Status.Downloading;
        fileStatus.progress = 0f;

        //Bắt đầu request.
        request.SendWebRequest();

        while (!request.isDone)
        {
            float progress = request.downloadProgress;
            fileStatus.progress = progress;
            yield return null;
        }

        //Chờ đến khi request xong.
        yield return request;

        //Kiểm tra xem download thành công hay không.
        if (request.result == UnityWebRequest.Result.Success)
        {
            //Set Download Status
            fileStatus.status = Status.Downloaded;
            fileStatus.progress = 1f;

            //Chuyển từ file temp sang file chính.
            File.Move(fileStatus.devicePath + "_Temp.mp4", fileStatus.devicePath + ".mp4");

            //Download cả thumbnail và description để sau này còn dùng cho offline mode.
            if (!File.Exists(fileStatus.devicePath + "_Thumbnail.png"))
                StartCoroutine(IE_DownloadThumbnail(fileStatus));

            if (!File.Exists(fileStatus.devicePath + "_Description.txt"))
                StartCoroutine(IE_DownloadDescription(fileStatus));
        }
        else
        {
            //Set Download Status
            fileStatus.status = Status.NotYetDownloaded;
            fileStatus.progress = 0f;
        }

        //Xóa request khỏi dictionary khi xong.
        m_filesDownloading.Remove(fileStatus.serverPath);
    }

    //Download thumbnail.
    IEnumerator IE_DownloadThumbnail(FileStatus fileStatus)
    {
        UnityWebRequest request = UnityWebRequest.Get(fileStatus.serverPath + "_Thumbnail.png");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            byte[] imageData = request.downloadHandler.data;
            File.WriteAllBytes(fileStatus.devicePath + "_Thumbnail.png", imageData);
        }
    }

    //Download Description.
    IEnumerator IE_DownloadDescription(FileStatus fileStatus)
    {
        UnityWebRequest request = UnityWebRequest.Get(fileStatus.serverPath + "_Description.txt");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            string descriptionText = request.downloadHandler.text;
            File.WriteAllText(fileStatus.devicePath + "_Description.txt", descriptionText);
        }
    }

    public void CancelVideoDownload(Player targetPlayer, int folderIndex, int fileIndex)
    {
        photonView.RPC("RPC_CancelVideoDownload", targetPlayer, folderIndex, fileIndex);
    }

    [PunRPC]
    public void RPC_CancelVideoDownload(int folderIndex, int fileIndex)
    {
        FileStatus fileStatus = serverManager.FoldersData.folders[folderIndex].mp4Files[fileIndex];
        CancelVideoDownload(fileStatus);
    }

    //Hủy download.
    public void CancelVideoDownload(FileStatus fileStatus)
    {
        //Kiểm tra coi trong dictionary có file này đang được tải hay không.
        if (m_filesDownloading.TryGetValue(fileStatus.serverPath, out UnityWebRequest request))
        {
            request.Abort();

            //Xóa file Temp đang có trong thiết bị.
            bool fileTempExist = File.Exists(fileStatus.devicePath + "_Temp.mp4");

            if (fileTempExist)
                File.Delete(fileStatus.devicePath + "_Temp.mp4");

            //Tắt giao diện quá trình download và mở lại giao diện chờ được download.
            m_filesDownloading.Remove(fileStatus.serverPath);
            fileStatus.status = Status.NotYetDownloaded;
            fileStatus.progress = 0f;
        }
    }
}
