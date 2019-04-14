using IATK;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using static IATK.AbstractVisualisation;

#if UNITY_EDITOR
using UnityEditor;
#elif UNITY_WSA_10_0
using Windows.Storage;
using Windows.Storage.Pickers;
#endif


[Serializable]
public class UnityDataSourceEvent : UnityEvent<CSVDataSource>
{
}


public class IATKManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI FileName;

    [SerializeField]
    Dropdown VizType;

    [SerializeField]
    Dropdown GeoType;

    [SerializeField]
    DimensionManager DimensionManager;

    float Height = 1;
    float Width = 1;
    float Depth = 1;

    CSVDataSource DataSource;
    Visualisation Visualization;
    BoxCollider Collider;

    public UnityDataSourceEvent DataSourceLoaded = new UnityDataSourceEvent();


    void Start()
    {
        Collider = gameObject.GetComponent<BoxCollider>();

        if (Collider == null)
            Collider = gameObject.AddComponent<BoxCollider>();

        Collider.enabled = false;
    }


    void UpdateFilePath(string path)
    {
        if (FileName != null)
            FileName.text = Path.GetFileName(path);
    }


    VisualisationTypes ParseVisualisationType(string type)
    {
        VisualisationTypes result = VisualisationTypes.PARALLEL_COORDINATES;

        if (!Enum.TryParse<VisualisationTypes>(type, out result))
            Debug.LogErrorFormat("VisualisationTypes {0} not supported.", type);

        return result;
    }

    VisualisationTypes GetCurrentVizType()
    {
        if (VizType != null)
            return ParseVisualisationType(VizType.options[VizType.value].text);
        else
            return VisualisationTypes.PARALLEL_COORDINATES;
    }

    GeometryType ParseGeometryType(string type)
    {
        GeometryType result = GeometryType.Points;

        if (!Enum.TryParse<GeometryType>(type, out result))
            Debug.LogErrorFormat("GeometryType {0} not supported.", type);

        return result;
    }

    GeometryType GetCurrentGeoType()
    {
        if (VizType != null)
            return ParseGeometryType(GeoType.options[GeoType.value].text);
        else
            return GeometryType.Points;
    }


    void CreateDataSource(string content)
    {
        if (DataSource != null)
            Destroy(DataSource);

        DataSource = gameObject.AddComponent<CSVDataSource>();
        DataSource.Load(content);

        DataSourceLoaded.Invoke(DataSource);
    }

    public void CreateVisualization()
    {
        if (DataSource != null)
            CreateVisualization(DataSource);
    }

    void CreateVisualization(DataSource source)
    {
        if (Visualization != null)
            Destroy(Visualization.gameObject);

        DimensionFilter[] filters = DimensionManager.GetDimensionFilters();
        VisualisationTypes type = GetCurrentVizType();

        Visualization = new GameObject("Visualization").AddComponent<Visualisation>();
        Visualization.transform.SetParent(transform, false);

        Visualization.dataSource = source;

        InitVisualization(type);
        UpdateDimension(filters, type);
    }

    void InitVisualization(VisualisationTypes type)
    {
        if (Visualization == null)
            return;

        AbstractVisualisation viz;

        switch (type)
        {
            case VisualisationTypes.PARALLEL_COORDINATES:

                viz = Visualization.gameObject.AddComponent<ParallelCoordinatesVisualisation>();
                viz.visualisationReference = Visualization;

                Visualization.theVisualizationObject = viz;
                Visualization.colour = Color.white * 0.3f;
                Visualization.size = 0.15f;

                break;

            case VisualisationTypes.SCATTERPLOT:

                viz = Visualization.gameObject.AddComponent<ScatterplotVisualisation>();
                viz.visualisationReference = Visualization;

                Visualization.theVisualizationObject = viz;

                Visualization.colour = Color.white;
                Visualization.geometry = GetCurrentGeoType();
                Visualization.size = 0.3f;

                break;
        }

        Visualization.colourDimension = "Undefined";
        Visualization.colorPaletteDimension = "Undefined";
        Visualization.sizeDimension = "Undefined";
    }

    void UpdateDimension(DimensionFilter[] filters, VisualisationTypes type)
    {
        switch (type)
        {
            case VisualisationTypes.PARALLEL_COORDINATES:

                Visualization.parallelCoordinatesDimensions = new DimensionFilter[filters.Length];

                for (int i = 0; i < filters.Length; i++)
                    Visualization.parallelCoordinatesDimensions[i] = filters[i];

                Visualization.theVisualizationObject.UpdateVisualisation(PropertyType.DimensionChange);
                Visualization.theVisualizationObject.CreateVisualisation();

                break;

            case VisualisationTypes.SCATTERPLOT:

                Visualization.xDimension = (filters.Length > 0) ? filters[0] : new DimensionFilter() { Attribute = "Undefined" };
                Visualization.yDimension = (filters.Length > 1) ? filters[1] : new DimensionFilter() { Attribute = "Undefined" };
                Visualization.zDimension = (filters.Length > 2) ? filters[2] : new DimensionFilter() { Attribute = "Undefined" };

                Visualization.theVisualizationObject.CreateVisualisation();

                Visualization.theVisualizationObject.UpdateVisualisation(PropertyType.X);
                Visualization.theVisualizationObject.UpdateVisualisation(PropertyType.Y);
                Visualization.theVisualizationObject.UpdateVisualisation(PropertyType.Z);

                break;
        }

        UpdateVizScaling();
    }

    void UpdateVizScaling()
    {
        if (Visualization == null)
            return;

        Visualization.width = Width;
        Visualization.height = Height;
        Visualization.depth = Depth;
        Visualization.theVisualizationObject?.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);

        UpdateVizOffset();
    }

    void UpdateVizOffset()
    {
        if (Visualization == null)
            return;

        Vector3 offset = Vector3.zero;

        switch (GetCurrentVizType())
        {
            case VisualisationTypes.PARALLEL_COORDINATES:
                offset = new Vector3((float)(Visualization.theVisualizationObject.GameObject_Axes_Holders.Count - 1) * Width, Height);
                Collider.center = new Vector2(-0.15f, 0.05f);
                Collider.size = offset + new Vector3(0.5f, 0.2f, 0.1f);
                break;

            case VisualisationTypes.SCATTERPLOT:
                offset = new Vector3(Width, Height, Depth);
                Collider.center = Vector3.one * -0.1f;
                Collider.size = offset + Vector3.one * 0.6f;
                break;
        }

        Visualization.transform.localPosition = offset * -0.5f;
    }





    public void OnVizWidthUpdated(Single width)
    {
        Width = width;
        UpdateVizScaling();
    }

    public void OnVizHeightUpdated(Single height)
    {
        Height = height;
        UpdateVizScaling();
    }

    public void OnVizDepthUpdated(Single depth)
    {
        Depth = depth;
        UpdateVizScaling();
    }

    public void OpenFile()
    {
#if UNITY_EDITOR
        string file = EditorUtility.OpenFilePanel("Open File Dialog", "", "csv");

        if (!string.IsNullOrEmpty(file))
        {
            Debug.LogFormat("Open File: {0}", file);

            LoadFile(file, 
                (x) =>
                {
                    UpdateFilePath(file);
                    CreateDataSource(x);
                });
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
        {
            UpdateFilePath(file.Path);
            CreateDataSource(content);
        }
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
