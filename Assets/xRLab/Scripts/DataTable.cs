using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IATK;
using TMPro;

public class DataTable : MonoBehaviour
{
    [SerializeField]
    GameObject CellParentPrefab;
    [SerializeField]
    DataCell CellPrefab;
    [SerializeField]
    Transform Content;
    [SerializeField]
    TextMeshProUGUI rowCount;
    [SerializeField]
    TextMeshProUGUI colCount;


    public void Init(List<DataSource.DimensionData> data)
    {
        colCount.text = data.Count.ToString();
        rowCount.text = data[0].Data.Length.ToString(); 

        for (int i =0;i<data.Count;i++)
        {
            var d = data[i];
            var parent = Instantiate(CellParentPrefab,Content);
            var header = Instantiate(CellPrefab, parent.transform);
            header.data.text = d.Identifier;
            
            for (int j = 0; j < d.Data.Length; j++)
            {
                var cell = Instantiate(CellPrefab, parent.transform);

                if (d.MetaData.type == DataType.Undefined)
                    cell.data.text = "";
                else if (d.MetaData.type == DataType.String)
                {
                    var dic = d.StringTable[d.Identifier];
                    cell.data.text = dic[(int)d.OData[j]];
                }
                else if (d.MetaData.type == DataType.Int || d.MetaData.type == DataType.Float)
                {
                    cell.data.text = d.OData[j].ToString();
                }
                else 
                {
                    cell.data.text = d.MetaData.type.ToString();
                }
                

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
