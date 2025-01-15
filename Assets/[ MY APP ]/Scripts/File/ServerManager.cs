using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class ServerFolders
{
    public List<FolderInfo> folders;
}

[Serializable]
public class FolderInfo
{
    public string folderName;
    public List<FileStatus> mp4Files;
}

[Serializable]
public class FileStatus
{
    public string fileName;
    public string location;
    public string serverPath;
    public string devicePath;
    public Status status;
    public float progress;
}

public enum Status
{
    NotYetDownloaded,
    Downloading,
    Downloaded
}

public class ServerManager : MonoBehaviour
{
    static ServerManager m_instance;
    public static ServerManager Instance { get => m_instance; }

    private void Awake()
    {
        m_instance = this;
    }

    [SerializeField] string m_serverURL;
    public string ServerURL
    {
        get => m_serverURL;
    }

    [SerializeField] ServerFolders m_foldersData;
    public ServerFolders FoldersData
    {
        get => m_foldersData;
    }

    UIManager uiManager;
    DeviceManager deviceManager;
    private const int MaxRetries = 3;

    // Start is called before the first frame update
    void Start()
    {
        uiManager = UIManager.Instance;
        deviceManager = DeviceManager.Instance;
    }

    //Lấy thông tin folder trên server.
    public IEnumerator GetServerFolder()
    {
        //Gọi vào link file php trên server để lấy thông tin dưới dạng Json.
        string serverURLPath = Path.Combine(m_serverURL, "Test.php");
        UnityWebRequest request = UnityWebRequest.Get(serverURLPath);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            Debug.Log("Get server folders data success !!!");
            string json = request.downloadHandler.text;
            m_foldersData.folders.Clear();

            //Gán thông tin folder vào biến.
            m_foldersData = JsonUtility.FromJson<ServerFolders>(json);

            m_foldersData.folders = m_foldersData.folders.OrderBy(folder => folder.folderName).ToList();
            m_foldersData.folders.ForEach(folder =>
            {
                folder.mp4Files = folder.mp4Files.OrderBy(fileStatus => fileStatus.fileName).ToList();

                folder.mp4Files.ForEach(fileStatus =>
                {
                    string serverFilePath = Path.Combine(m_serverURL, folder.folderName, fileStatus.fileName);
                    fileStatus.serverPath = serverFilePath;
                    
                    string deviceFilePath = Path.Combine(deviceManager.DeviceURL, folder.folderName, fileStatus.fileName);
                    fileStatus.devicePath = deviceFilePath;

                    if (deviceManager.CheckDeviceFile(deviceFilePath))
                    {
                        fileStatus.status = Status.Downloaded;
                        fileStatus.progress = 1f;
                    }
                    else
                    {
                        fileStatus.status = Status.NotYetDownloaded;
                        fileStatus.progress = 0f;
                    }
                });
            });
        }
    }

    //Lấy dữ liệu hình trên server.
    public IEnumerator LoadImage(string imageURL, Action<Sprite> thumnail)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageURL);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            //Tạo một sprite ảo.
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite serverThumbnail = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);

            thumnail(serverThumbnail);
        }
    }

    //Lấy dữ liệu thông tin mô tả trên server.
    public IEnumerator LoadText(string textURL, Action<string> description)
    {
        int retries = 0;
        bool success = false;
        string serverDescription = "Description de la vidéo"; // Default description

        while (retries < MaxRetries && !success)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(textURL))
            {
                // Send the web request and wait for it to complete
                yield return request.SendWebRequest();

                // Check for network errors or HTTP errors
#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    // Successfully received response
                    serverDescription = request.downloadHandler.text.Trim();
                    success = true;
                }
                else
                {
                    // Log the error
                    Debug.LogWarning($"Attempt {retries + 1} failed: {request.error}");

                    retries++;

                    if (retries < MaxRetries)
                    {
                        yield return new WaitForSeconds(1f);
                    }
                }
            }
        }

        if (success)
        {
            // Invoke the callback with the fetched description
            description(serverDescription);
        }
        else
        {
            // Invoke the callback with the default description after all retries failed
            description("Description de la vidéo");
            Debug.LogWarning($"Failed to load text from {textURL} after {MaxRetries} attempts.");
        }
    }
}
