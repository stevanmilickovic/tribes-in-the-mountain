using UnityEngine;
using FishNet.Managing;

public class PauseMenuUI : MonoBehaviour
{
    public GameObject panel;
    NetworkManager nm;

    void Start()
    {
        nm = FindObjectOfType<NetworkManager>();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            panel.SetActive(!panel.activeSelf);
            PlayerInputs.Paused = panel.activeSelf;

            Cursor.lockState = panel.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = panel.activeSelf;
        }
    }

    public void ExitToMenu()
    {
        if (nm.IsServerStarted)
            nm.ServerManager.StopConnection(true);

        if (nm.IsClientStarted)
            nm.ClientManager.StopConnection();
    }

    public void Resume()
    {
        panel.SetActive(false);
        PlayerInputs.Paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
