using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DeviceManager : MonoBehaviourPunCallbacks
{
    static DeviceManager m_instance;
    public static DeviceManager Instance { get => m_instance; }

    [SerializeField] string m_deviceURL;
    public string DeviceURL { get => m_deviceURL; }

    ServerManager serverManager;

    private void Awake()
    {
        m_instance = this;
        m_deviceURL = Application.persistentDataPath;
    }

    // Start is called before the first frame update
    void Start()
    {
        serverManager = ServerManager.Instance;
    }

    //Tạo folders trong thiết bị.
    public void CreateDeviceFolders()
    {
        serverManager.FoldersData.folders.ForEach(folder =>
        {
            string deviceFolderPath = Path.Combine(m_deviceURL, folder.folderName);

            //Nếu folder chưa được tạo thì tạo.
            if (!Directory.Exists(deviceFolderPath))
                Directory.CreateDirectory(deviceFolderPath);
        });
    }

    public bool CheckDeviceFile(string deviceFilePath)
    {
        string filePath = deviceFilePath + ".mp4";
        string fileTempPath = deviceFilePath + "_Temp.mp4";

        if (File.Exists(fileTempPath))
            File.Delete(fileTempPath);

        return File.Exists(filePath);
    }

    public void DeleteFile(Player targetPlayer, int folderIndex, int fileIndex)
    {
        photonView.RPC("RPC_DeleteFile", targetPlayer, folderIndex, fileIndex);
    }

    [PunRPC]
    public void RPC_DeleteFile(int folderIndex, int fileIndex)
    {
        FileStatus fileStatus = serverManager.FoldersData.folders[folderIndex].mp4Files[fileIndex];
        DeleteFile(fileStatus);
    }

    //Xóa file trong thiết bị.
    public void DeleteFile(FileStatus fileStatus)
    {
        if (CheckDeviceFile(fileStatus.devicePath))
            File.Delete(fileStatus.devicePath + ".mp4");

        bool thumbnailFileExist = File.Exists(fileStatus.devicePath + "_Thumbnail.png");
        if (thumbnailFileExist)
            File.Delete(fileStatus.devicePath + "_Thumbnail.png");

        bool descriptionFileExist = File.Exists(fileStatus.devicePath + "_Description.txt");
        if (descriptionFileExist)
            File.Delete(fileStatus.devicePath + "_Description.txt");

        fileStatus.status = Status.NotYetDownloaded;
        fileStatus.progress = 0f;
    }
}
