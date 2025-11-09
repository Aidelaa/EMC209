using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RestfulAPIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button addButton;
    [SerializeField] private GameObject addPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField dataInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Edit Panel")]
    [SerializeField] private GameObject editPanel;
    [SerializeField] private TMP_InputField editNameInputField;
    [SerializeField] private TMP_InputField editDataInputField;
    [SerializeField] private Button saveEditButton;
    [SerializeField] private Button cancelEditButton;

    [Header("Settings")]
    [SerializeField] private int requestTimeout = 10;

    private const string BASE_URL = "https://api.restful-api.dev/objects";
    private List<APIObject> currentObjects = new List<APIObject>();
    private APIObject currentEditingObject;

    void Start()
    {
        SetupUI();
        LoadAllData();
    }

    void SetupUI()
    {
        refreshButton.onClick.AddListener(LoadAllData);
        addButton.onClick.AddListener(ShowAddPanel);
        sendButton.onClick.AddListener(CreateNewObject);
        cancelButton.onClick.AddListener(HideAddPanel);
        saveEditButton.onClick.AddListener(SaveEdit);
        cancelEditButton.onClick.AddListener(HideEditPanel);

        HideAddPanel();
        HideEditPanel();
        UpdateStatus("Ready");
    }

    public void LoadAllData()
    {
        StartCoroutine(GetAllObjects());
    }

    IEnumerator GetAllObjects()
    {
        UpdateStatus("Loading data...");

        using (UnityWebRequest request = UnityWebRequest.Get(BASE_URL))
        {
            request.timeout = requestTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;

                    // Parse the JSON array
                    string wrappedJson = "{\"objects\":" + jsonResponse + "}";
                    ObjectList objectList = JsonUtility.FromJson<ObjectList>(wrappedJson);

                    currentObjects.Clear();
                    currentObjects.AddRange(objectList.objects);

                    UpdateUI();
                    UpdateStatus($"Loaded {currentObjects.Count} objects");
                }
                catch (System.Exception e)
                {
                    UpdateStatus($"Error parsing data: {e.Message}");
                    Debug.LogError($"JSON Parse Error: {e.Message}");
                }
            }
            else
            {
                UpdateStatus($"Failed to load data: {request.error}");
                Debug.LogError($"Request failed: {request.error}");
            }
        }
    }

    void UpdateUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (APIObject obj in currentObjects)
        {
            CreateUIItem(obj);
        }
    }

    void CreateUIItem(APIObject obj)
    {
        GameObject item = Instantiate(itemPrefab, contentParent);
        APIObjectItem itemScript = item.GetComponent<APIObjectItem>();

        if (itemScript != null)
        {
            itemScript.Setup(obj, this);
        }
    }

    void ShowAddPanel()
    {
        addPanel.SetActive(true);
        nameInputField.text = "";
        dataInputField.text = "";
        nameInputField.Select();
    }

    void HideAddPanel()
    {
        addPanel.SetActive(false);
    }

    void CreateNewObject()
    {
        if (string.IsNullOrEmpty(nameInputField.text))
        {
            UpdateStatus("Name is required!");
            return;
        }

        StartCoroutine(PostNewObject());
    }

    IEnumerator PostNewObject()
    {
        UpdateStatus("Creating new object...");

        APIObject newObj = new APIObject
        {
            name = nameInputField.text,
            data = ParseDataString(dataInputField.text)
        };

        string jsonData = JsonUtility.ToJson(newObj);

        using (UnityWebRequest request = new UnityWebRequest(BASE_URL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus("Object created successfully!");
                HideAddPanel();
                LoadAllData();
            }
            else
            {
                UpdateStatus($"Failed to create object: {request.error}");
                Debug.LogError($"POST failed: {request.error}");
            }
        }
    }

    public void DeleteObject(APIObject obj)
    {
        StartCoroutine(DeleteObjectCoroutine(obj));
    }

    IEnumerator DeleteObjectCoroutine(APIObject obj)
    {
        UpdateStatus($"Deleting {obj.name}...");

        string url = $"{BASE_URL}/{obj.id}";

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.timeout = requestTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus($"Deleted {obj.name}");
                LoadAllData();
            }
            else
            {
                UpdateStatus($"Failed to delete {obj.name}: {request.error}");
                Debug.LogError($"DELETE failed: {request.error}");
            }
        }
    }

    public void EditObject(APIObject obj)
    {
        currentEditingObject = obj;
        ShowEditPanel(obj);
    }

    void ShowEditPanel(APIObject obj)
    {
        editPanel.SetActive(true);
        editNameInputField.text = obj.name;
        editDataInputField.text = FormatDataForEditing(obj.data);
        editNameInputField.Select();
    }

    void HideEditPanel()
    {
        editPanel.SetActive(false);
        currentEditingObject = null;
    }

    void SaveEdit()
    {
        if (currentEditingObject == null || string.IsNullOrEmpty(editNameInputField.text))
        {
            UpdateStatus("Name is required!");
            return;
        }

        StartCoroutine(PutUpdateObject());
    }

    IEnumerator PutUpdateObject()
    {
        UpdateStatus($"Updating {currentEditingObject.name}...");

        APIObject updatedObj = new APIObject
        {
            id = currentEditingObject.id,
            name = editNameInputField.text,
            data = ParseDataString(editDataInputField.text)
        };

        string jsonData = JsonUtility.ToJson(updatedObj);
        string url = $"{BASE_URL}/{currentEditingObject.id}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus($"Updated {updatedObj.name}");
                HideEditPanel();
                LoadAllData();
            }
            else
            {
                UpdateStatus($"Failed to update object: {request.error}");
                Debug.LogError($"PUT failed: {request.error}");
            }
        }
    }

    ObjectData ParseDataString(string dataString)
    {
        ObjectData data = new ObjectData();

        if (string.IsNullOrEmpty(dataString))
            return data;

        string[] pairs = dataString.Split(',');
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split(':');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();

                if (string.IsNullOrEmpty(data.color))
                    data.color = value;
                else if (string.IsNullOrEmpty(data.capacity))
                    data.capacity = value;
                else if (string.IsNullOrEmpty(data.generation))
                    data.generation = value;
                else if (string.IsNullOrEmpty(data.price))
                    data.price = value;
            }
        }

        return data;
    }

    string FormatDataForEditing(ObjectData data)
    {
        List<string> pairs = new List<string>();

        if (!string.IsNullOrEmpty(data.color))
            pairs.Add($"color:{data.color}");
        if (!string.IsNullOrEmpty(data.capacity))
            pairs.Add($"capacity:{data.capacity}");
        if (!string.IsNullOrEmpty(data.generation))
            pairs.Add($"generation:{data.generation}");
        if (!string.IsNullOrEmpty(data.price))
            pairs.Add($"price:{data.price}");

        return string.Join(", ", pairs);
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Status: {message}");
    }
}


[System.Serializable]
public class APIObject
{
    public string id;
    public string name;
    public ObjectData data;
}

[System.Serializable]
public class ObjectData
{
    public string color;
    public string capacity;
    public string generation;
    public string price;
}

[System.Serializable]
public class ObjectList
{
    public APIObject[] objects;
}
