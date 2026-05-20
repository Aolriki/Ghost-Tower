using UnityEngine;

public class DocumentUIManager : MonoBehaviour
{
    public static DocumentUIManager Instance { get; private set; }

    [Header("References")]
    public GameObject documentUIPanel; // Arraste o filho "DocumentUI" aqui
    public Transform docContent;

    private GameObject _currentPage;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        documentUIPanel.SetActive(false);
    }

    public void OpenDoc(GameObject docItemPagePrefab)
    {
        if (_currentPage != null)
            Destroy(_currentPage);

        _currentPage = Instantiate(docItemPagePrefab, docContent);
        documentUIPanel.SetActive(true);

        ScreenManager.Instance?.ChangeScreen(Screens.DocPage);
    }

    public void CloseDoc()
    {
        if (_currentPage != null)
        {
            Destroy(_currentPage);
            _currentPage = null;
        }

        documentUIPanel.SetActive(false);
        ScreenManager.Instance?.ChangeScreen(Screens.Gameplay);
    }
}