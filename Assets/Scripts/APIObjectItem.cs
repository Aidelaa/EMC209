using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class APIObjectItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dataText;
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    private APIObject apiObject;
    private RestfulAPIManager manager;

    public void Setup(APIObject obj, RestfulAPIManager apiManager)
    {
        apiObject = obj;
        manager = apiManager;

        UpdateDisplay();
        SetupButtons();
    }

    void UpdateDisplay()
    {
        if (nameText != null)
            nameText.text = apiObject.name;

        if (dataText != null)
            dataText.text = FormatDataDisplay(apiObject.data);
    }

    void SetupButtons()
    {
        if (editButton != null)
            editButton.onClick.AddListener(OnEditClicked);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    void OnEditClicked()
    {
        manager.EditObject(apiObject);
    }

    void OnDeleteClicked()
    {
        manager.DeleteObject(apiObject);
    }

    string FormatDataDisplay(ObjectData data)
    {
        if (data == null)
            return "No data";

        string result = "";

        if (!string.IsNullOrEmpty(data.color))
            result += $"Color: {data.color}\n";
        if (!string.IsNullOrEmpty(data.capacity))
            result += $"Capacity: {data.capacity}\n";
        if (!string.IsNullOrEmpty(data.generation))
            result += $"Generation: {data.generation}\n";
        if (!string.IsNullOrEmpty(data.price))
            result += $"Price: {data.price}\n";

        return string.IsNullOrEmpty(result) ? "No data" : result.TrimEnd('\n');
    }
}
