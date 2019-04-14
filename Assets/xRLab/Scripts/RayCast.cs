using System.Collections.Generic;
using UnityEngine;
public class RayCast : MonoBehaviour
{
    Camera portalCamera;

    private void Start()
    {
        portalCamera = this.GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // do we hit our portal plane?
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.collider.gameObject);


                var localPoint = hit.textureCoord;
                // convert the hit texture coordinates into camera coordinates
                Ray portalRay = portalCamera.ScreenPointToRay(new Vector2(localPoint.x * portalCamera.pixelWidth, localPoint.y * portalCamera.pixelHeight));
                Debug.DrawRay(portalRay.origin, portalRay.direction , Color.red);
                RaycastHit portalHit;
                // test these camera coordinates in another raycast test
                if (Physics.Raycast(portalRay, out portalHit))
                {
                    Debug.Log(portalHit.collider.gameObject);
                }
            }
        }

    }
}