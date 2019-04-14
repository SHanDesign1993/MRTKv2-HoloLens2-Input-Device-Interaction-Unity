using IATK;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#elif UNITY_WSA_10_0
using Windows.Storage;
using Windows.Storage.Pickers;
#endif

[Serializable]
public class UnityVizEvent : UnityEvent<Visualisation>
{
}


public class PCPsTest : MonoBehaviour
{
    float Height = 1;
    float Width = 1;

    [SerializeField]
    DataTable table;


    CSVDataSource DataSource;
    Visualisation Visualization;
    BoxCollider Collider;
    BoundingBoxCtr Boundingbox;

    public UnityVizEvent VisualizationCreated = new UnityVizEvent();

    void Start()
    {
        Collider = gameObject.GetComponent<BoxCollider>();
        if (Collider == null)
            Collider = gameObject.AddComponent<BoxCollider>();

        Collider.center = new Vector2(-0.15f, 0.05f);
        Collider.enabled = false;
        Boundingbox = gameObject.GetComponent<BoundingBoxCtr>();
    }

    public void OnVizWidthUpdated(Single width)
    {
        Width = width;
        UpdateVizScaling(Visualization);
    }

    public void OnVizHeightUpdated(Single height)
    {
        Height = height;
        UpdateVizScaling(Visualization);
    }

    public void OnDimensionChanged()
    {
        if (Visualization == null)
            return;

        // [workaround] set viz view's size to default values
        Visualization.width = Visualization.height = 1;
        Visualization.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);

        Visualization.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionChange);
        Visualization.theVisualizationObject?.CreateVisualisation();

        UpdateVizScaling(Visualization);
    }

    public void SetDimension(DimensionFilter[] filters)
    {
        Visualization.width = Visualization.height = 1;
        Visualization.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);

        Visualization.parallelCoordinatesDimensions = new DimensionFilter[filters.Length];

        for (int i = 0; i < filters.Length; i++)
            Visualization.parallelCoordinatesDimensions[i] = filters[i];

        Visualization.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionChange);
        Visualization.theVisualizationObject?.CreateVisualisation();

        UpdateVizScaling(Visualization);
    }

    void CreateDataSource(string content)
    {
        if (DataSource != null)
            Destroy(DataSource);

        DataSource = gameObject.AddComponent<CSVDataSource>();
        DataSource.Load(content);

        CreateVisualization(DataSource);

        StartCoroutine(CreateTableCoroutine());
        //init bounding box if datasource loaded
        Boundingbox?.Init();
    }

    IEnumerator CreateTableCoroutine()
    {
        while (!DataSource.IsLoaded) 
            yield return null;

        if (DataSource.IsLoaded)
        {
            table?.Init(DataSource.dimensionData);
        }
    }

    void InitVisualization(Visualisation visualisation)
    {
        if (visualisation == null)
            return;

        visualisation.colour = Color.white * 0.3f;
        visualisation.colourDimension = "Undefined";
        visualisation.colorPaletteDimension = "Undefined";
        visualisation.size = 0.15f;
        visualisation.sizeDimension = "Undefined";
    }

    void UpdateVizScaling(Visualisation visualisation)
    {
        if (visualisation == null)
            return;

        visualisation.width = Width;
        visualisation.height = Height;
        visualisation.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);

        UpdateVizOffset(visualisation);
    }

    void UpdateVizOffset(Visualisation visualisation)
    {
        if (visualisation == null)
            return;

        Vector3 offset = new Vector3((float)(visualisation.theVisualizationObject.GameObject_Axes_Holders.Count - 1) * Width, Height);
        visualisation.transform.localPosition = offset * -0.5f;

        Collider.size = offset + new Vector3(0.5f, 0.2f, 0.1f);
    }

    void CreateVisualization(DataSource source)
    {
        if (Visualization != null)
            Destroy(Visualization.gameObject);

        Visualization = new GameObject("Visualization").AddComponent<Visualisation>();

        Visualization.transform.SetParent(transform, false);

        InitVisualization(Visualization);

        Visualization.dataSource = source;
        Visualization.CreateVisualisation(AbstractVisualisation.VisualisationTypes.PARALLEL_COORDINATES);

        UpdateVizScaling(Visualization);
        Collider.enabled = true;

        VisualizationCreated.Invoke(Visualization);
    }

    public void OpenFile()
    {
#if UNITY_EDITOR
        string file = EditorUtility.OpenFilePanel("Open File Dialog", "", "csv");

        if (!string.IsNullOrEmpty(file))
        {
            Debug.LogFormat("Open File: {0}", file);
            LoadFile(file, (x) => CreateDataSource(x));
        }
        else
        {
            Debug.Log("Open File Canceled.");
        }

#elif UNITY_WSA_10_0
        StartCoroutine(OpenFileAsync());
#endif
    }

#if UNITY_EDITOR
    public void LoadFile(string path, Action<string> callback = null)
    {
        StartTask<string>(LoadFileAsync(path), callback);
    }

    async Task<string> LoadFileAsync(string path)
    {
        using (StreamReader reader = File.OpenText(path))
        {
            return await reader.ReadToEndAsync();
        }
    }

#elif UNITY_WSA_10_0
    public IEnumerator OpenFileAsync()
    {
        bool IsCompleted = false;

        FileOpenPicker filePicker = new FileOpenPicker();
        StorageFile file = null;

        filePicker.FileTypeFilter.Add(".csv");

        UnityEngine.WSA.Application.InvokeOnUIThread(
            async () =>
            {
                file = await filePicker.PickSingleFileAsync();
                IsCompleted = true;
            }, false);

        while (!IsCompleted)
            yield return null;

        if (file != null)
        {
            Debug.LogFormat("Open File: {0}", file.Path);
            StartCoroutine(LoadFileAsync(file));
        }
        else
            Debug.Log("Open File Canceled.");
    }

    public IEnumerator LoadFileAsync(StorageFile file)
    {
        bool IsCompleted = false;
        string content = string.Empty;

        UnityEngine.WSA.Application.InvokeOnUIThread(
            async () =>
            {
                content = await FileIO.ReadTextAsync(file);
                IsCompleted = true;
            }, false);

        while (!IsCompleted)
            yield return null;

        if (!string.IsNullOrEmpty(content))
            CreateDataSource(content);
        else
            Debug.Log("File Content is empty.");
    }
#endif

    public Coroutine StartTask<T>(Task<T> task, Action<T> callback = null)
    {
        return StartCoroutine(AwaitTask<T>(task, callback));
    }

    public IEnumerator AwaitTask<T>(Task<T> task, Action<T> callback = null)
    {
        do
        {
            if (task.IsCanceled || task.IsCompleted || task.IsFaulted)
                break;

            yield return null;
        }
        while (true);

        switch (task.Status)
        {
            case TaskStatus.RanToCompletion:
                callback?.Invoke(task.Result);
                break;
            case TaskStatus.Faulted:
                Debug.LogErrorFormat("Task {0} failed: {1}", task.Id, task.Exception.InnerException.Message);
                break;
            case TaskStatus.Canceled:
                Debug.LogWarningFormat("Task {0} canceled", task.Id);
                break;
            default:
                break;

        } 
    }
}
