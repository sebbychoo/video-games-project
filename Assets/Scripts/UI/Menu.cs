using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pause menu controller. Attach to a persistent GameObject.
/// Assign MenuCanvas, playerCapsule, and PlayerFollowCamera in the Inspector.
/// </summary>
public class Menu : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject playerCapsule;
    public GameObject playerFollowCamera;

    private bool menuOpen = false;

    private void Start()
    {
        menuCanvas.SetActive(false);
    }

    private void Update()
    {
        // Don't open pause menu while interacting with any exploration UI
        if (Input.GetKeyDown(KeyCode.Escape)
            && !CardBattle.WorkBoxTrigger.IsInteracting
            && !CardBattle.BathroomShopTrigger.IsInteracting
            && !CardBattle.BreakRoomTradeTrigger.IsInteracting)
            SetPause();
    }

    public void SetPause()
    {
        menuOpen = !menuOpen;

        menuCanvas.SetActive(menuOpen);
        Time.timeScale = menuOpen ? 0f : 1f;
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = menuOpen;

        if (playerCapsule != null)
            playerCapsule.SetActive(!menuOpen);

        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(!menuOpen);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        SceneManager.LoadScene(0);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
