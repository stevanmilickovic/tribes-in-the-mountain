using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using FishNet.Object;

public class MainMenuUI : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject joinPanel;
    public GameObject exitPanel;

    public Button hostButton;
    public Button joinButton;
    public Button quitButton;

    public Button joinConfirmButton;
    public Button joinBackButton;

    public Button exitLeaveButton;
    public Button exitShutdownButton;
    public Button exitBackButton;

    public TMP_InputField ipInput;

    NetworkManager nm;

    void Start()
    {
        nm = FindObjectOfType<NetworkManager>();

        mainMenuPanel.SetActive(true);
        joinPanel.SetActive(false);
        exitPanel.SetActive(false);

        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(OpenJoin);
        quitButton.onClick.AddListener(Quit);

        joinConfirmButton.onClick.AddListener(Join);
        joinBackButton.onClick.AddListener(BackJoin);

        exitLeaveButton.onClick.AddListener(Leave);
        exitShutdownButton.onClick.AddListener(Shutdown);
        exitBackButton.onClick.AddListener(BackExit);

        ipInput.text = "127.0.0.1";
    }

    void Host()
    {
        nm.ServerManager.StartConnection();
        nm.ClientManager.StartConnection();
        HideAll();
    }

    void OpenJoin()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    void Join()
    {
        nm.TransportManager.Transport.SetClientAddress(ipInput.text);
        nm.ClientManager.StartConnection();
        HideAll();
    }

    void BackJoin()
    {
        joinPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ShowExitMenu()
    {
        bool isHost = nm.IsServerStarted;

        exitLeaveButton.gameObject.SetActive(!isHost);
        exitShutdownButton.gameObject.SetActive(isHost);

        exitPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideExitMenu()
    {
        exitPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Leave()
    {
        nm.ClientManager.StopConnection();
        exitPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void Shutdown()
    {
        if (nm.IsServerStarted)
            nm.ServerManager.StopConnection(true);

        if (nm.IsClientStarted)
            nm.ClientManager.StopConnection();

        // Cleanup ghost players leftover in DontDestroyOnLoad
        foreach (var obj in FindObjectsOfType<NetworkObject>())
        {
            if (!obj.IsSpawned)
                Destroy(obj.gameObject);
        }

        exitPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void BackExit()
    {
        exitPanel.SetActive(false);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void HideAll()
    {
        mainMenuPanel.SetActive(false);
        joinPanel.SetActive(false);
        exitPanel.SetActive(false);
    }
}
