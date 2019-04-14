using IATK;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DimensionManager : MonoBehaviour
{
    [SerializeField]
    Dimension DimensionObj;

    [SerializeField]
    Dropdown Dropdown;

    Dimension SelectedDimension;
    List<DimensionFilter> Filters = new List<DimensionFilter>();

    public void OnDataSourceLoaded(CSVDataSource source)
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        Filters.AddRange(source.Select(x => new DimensionFilter { Attribute = x.Identifier }));

        //foreach (var filter in Filters)
        //    Instantiate(DimensionObj, transform)?.Init(this, filter);

        Dropdown.ClearOptions();
        Dropdown.AddOptions(Filters.Select(x => x.Attribute).ToList());
    }


    public void OnSelectedChanged(Dimension dim)
    {
        SelectedDimension = dim;
    }

    public void SetDimension(PCPsTest pcps)
    {
        pcps.SetDimension(GetComponentsInChildren<Dimension>().Select(x => x.Filter).ToArray());
    }


    public DimensionFilter[] GetDimensionFilters()
    {
        return GetComponentsInChildren<Dimension>().Select(x => x.Filter).ToArray();
    }

    public void AddDimension()
    {
        string attribute = Dropdown.options[Dropdown.value].text;

        DimensionFilter filter = Filters.Where(x => x.Attribute == attribute).FirstOrDefault();
        Instantiate(DimensionObj, transform)?.Init(this, filter);
    }

    public void RemoveDimension()
    {
        if (SelectedDimension != null)
            DestroyImmediate(SelectedDimension.gameObject);
    }

    public void OnVisualizationCreated(Visualisation viz)
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        Filters = new List<DimensionFilter>(viz.parallelCoordinatesDimensions);

        foreach (var filter in Filters)
            Instantiate(DimensionObj, transform)?.Init(this, filter);

        Dropdown.ClearOptions();
        Dropdown.AddOptions(Filters.Select(x => x.Attribute).ToList());
    }
}
