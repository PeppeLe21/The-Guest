using UnityEngine;

public class ClosetInteraction : MonoBehaviour
{
    public GameObject disguiseUI; // Il pannello UI con i bottoni (Panel)
    private bool isPlayerInRange = false;

    void Start()
    {
        // Nascondi il menu all'inizio
        if (disguiseUI != null) disguiseUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            CloseMenu();
        }
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            // Se è aperto lo chiude, se è chiuso lo apre
            bool isOpen = disguiseUI.activeSelf;
            if (isOpen) CloseMenu();
            else OpenMenu();
        }
    }

    void OpenMenu()
    {
        disguiseUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None; // Sblocca il mouse
        Cursor.visible = true;
    }

    void CloseMenu()
    {
        disguiseUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; // Blocca il mouse per giocare
        Cursor.visible = false;
    }
}