using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RenderTextureActionHandler : MonoBehaviour,IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
{
    //Drag Orthographic top down camera here
    public Camera cam;
    RawImage rawImg;
    Vector2 localCursor = new Vector2(0, 0);
    [SerializeField]
    bool IsHovered = false;
    [SerializeField]
    bool IsClicked = false;

    float minFov = 15f;
    float maxFov = 90f;
    float sensitivity = 10f;

    float dragSpeed = 0.1f;
    private Vector3 dragOrigin;


    public void OnPointerClick(PointerEventData eventData)
    {
        IsClicked = true;
        HandlePointerRay(eventData.pressPosition , eventData.pressEventCamera);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHovered = false;
    }

    private void HandlePointerRay(Vector3 pos ,Camera cam) {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImg.rectTransform, pos, cam, out localCursor))
        {
            Texture tex = rawImg.texture;
            Rect r = rawImg.rectTransform.rect;

            //Using the size of the texture and the local cursor, clamp the X,Y coords between 0 and width - height of texture
            float coordX = Mathf.Clamp(0, (((localCursor.x - r.x) * tex.width) / r.width), tex.width);
            float coordY = Mathf.Clamp(0, (((localCursor.y - r.y) * tex.height) / r.height), tex.height);

            //Convert coordX and coordY to % (0.0-1.0) with respect to texture width and height
            float recalcX = coordX / tex.width;
            float recalcY = coordY / tex.height;

            localCursor = new Vector2(recalcX, recalcY);

            CastTextureRayToWorld(localCursor);
        }
    }

    private void CastTextureRayToWorld(Vector2 localCursor)
    {
        Ray textureRay = cam.ScreenPointToRay(new Vector2(localCursor.x * cam.pixelWidth, localCursor.y * cam.pixelHeight));
        RaycastHit textureHit;
        if (Physics.Raycast(textureRay, out textureHit, Mathf.Infinity))
        {
            //Debug.Log("Hover: " + textureHit.collider.gameObject);
            if (IsClicked)
            {
                IsClicked = !IsClicked;
                //Debug.Log("Click: " + textureHit.collider.gameObject);
            }
        }

    }

    void Start()
    {
        if (rawImg == null) rawImg = this.GetComponent<RawImage>();
    }

    private void Update()
    {
        if (IsHovered)
        {
            var screenPoint = Input.mousePosition;
            //Debug.Log(screenPoint);
            HandlePointerRay(screenPoint,null);

            ZoomCamera();
            MoveCamera();
        }
    }

    void ZoomCamera() {
        float fov = cam.fieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        cam.fieldOfView = fov;
    }

    void MoveCamera() {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        Vector3 pos = CameraCache.Main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        Vector3 move = new Vector3(-pos.x * dragSpeed, -pos.y * dragSpeed,0);

        cam.transform.Translate(move, Space.World);
    }

}
