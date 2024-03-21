using UnityEngine;

public class CanvasScript : MonoBehaviour
{
    public GameObject[] windowsBackgrounds;
    public GameObject[] windowsTaskbars;

    private int currentIndex;

    // Start is called before the first frame update
    void Start()
    {
        currentIndex = 0;
        InstantiateWindows();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void NextWindowsVersion()
    {
        Destroy(windowsBackgrounds[currentIndex]);
        Destroy(windowsTaskbars[currentIndex]);

        ++currentIndex;
        InstantiateWindows();
    }

    private void InstantiateWindows()
    {
        // Instantiate Windows Background
        GameObject windowsBackgroud = windowsBackgrounds[currentIndex];
        Instantiate(windowsBackgroud, windowsBackgroud.transform.position, windowsBackgroud.transform.rotation);

        // Instantiate Windows Taskbar
        GameObject windowsTaskbar = windowsTaskbars[currentIndex];
        Instantiate(windowsTaskbar, windowsTaskbar.transform.position, windowsTaskbar.transform.rotation);
    }
}
