using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.SDK.Input.Handlers;
using Microsoft.MixedReality.Toolkit;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBoxCtr : BaseFocusHandler,
        IMixedRealityInputHandler,
        IMixedRealityInputHandler<MixedRealityPose>,
        IMixedRealityPointerHandler,
        IMixedRealitySourceStateHandler
{
    #region Enums
    /// <summary>
    /// Enum which describes how an object's boundingbox is to be flattened.
    /// </summary>
    private enum FlattenModeType
    {
        DoNotFlatten = 0,
        /// <summary>
        /// Flatten the X axis
        /// </summary>
        FlattenX,
        /// <summary>
        /// Flatten the Y axis
        /// </summary>
        FlattenY,
        /// <summary>
        /// Flatten the Z axis
        /// </summary>
        FlattenZ,
        /// <summary>
        /// Flatten the smallest relative axis if it falls below threshold
        /// </summary>
        FlattenAuto,
    }

    /// <summary>
    /// Enum which describes whether a boundingbox handle which has been grabbed, is 
    /// a Rotation Handle (sphere) or a Scale Handle( cube)
    /// </summary>
    private enum HandleType
    {
        None = 0,
        Rotation,
        Scale
    }

    /// <summary>
    /// This enum describes which primitive type the wireframe portion of the boundingbox
    /// consists of. 
    /// </summary>
    /// <remarks>
    /// Wireframe refers to the thin linkage between the handles. When the handles are invisible
    /// the wireframe looks like an outline box around an object.
    /// </remarks> 
    private enum WireframeType
    {
        Cubic = 0,
        Cylindrical
    }

    /// <summary>
    /// This enum defines which of the axes a given rotation handle revolves about.
    /// </summary>
    private enum CardinalAxisType
    {
        X = 0,
        Y,
        Z
    }

    /// <summary>
    /// This enum is used internally to define how an object's bounds are calculated in order to fit the boundingbox
    /// to it.
    /// </summary>
    private enum BoundsCalculationMethod
    {
        Collider = 0,
        Colliders,
        Renderers,
        MeshFilters
    }

    /// <summary>
    /// This enum defines how a particular controller rotates an object when a Rotate handle has been grabbed.
    /// </summary>
    /// <remarks>
    /// a Controller feels more natural when rotation of the controller rotates the object.
    /// the wireframe looks like an outline box around an object.
    /// </remarks> 
    private enum HandleMoveType
    {
        Ray = 0,
        Point
    }
    #endregion Enums

    #region Serialized Fields
    //ADD box helper
    private BoundingBoxHelper helper;

    [Header("Toggle Button Settings")]
    //ADD toggle ctr and settings
    [SerializeField]
    private GameObject togglePrefab;
    [SerializeField]
    private Interactable toggleInstance;
    [SerializeField]
    private DraggingCtr dragHandler;
    /// <summary>
    /// Pushes the app bar away from the object
    /// </summary>
    [SerializeField]
    private float HoverOffsetZ = 0.05f;

    [Header("Bounds Calculation")]
    [Tooltip("For complex objects, automatic bounds calculation may not behave as expected. Use an existing Box Collider (even on a child object) to manually determine bounds of Bounding Box.")]
    [SerializeField]
    private BoxCollider boxColliderToUse = null;

    [Header("Behavior")]
    [SerializeField]
    private bool createToggleOnStart = false;
    private bool isToggleCreated = false;
    private bool activateOnStart = false;

    [SerializeField]
    private float scaleMaximum = 2.0f;

    [SerializeField]
    private float scaleMinimum = 0.2f;

    [Header("Wireframe")]
    [SerializeField]
    private bool wireframeOnly = false;

    /// <summary>
    /// Public Property that displays simple wireframe around an object with no scale or rotate handles.
    /// </summary>
    /// <remarks>
    /// this is useful when outlining an object without being able to edit it is desired.
    /// </remarks>
    public bool WireframeOnly
    {
        get { return wireframeOnly; }
        set
        {
            if (wireframeOnly != value)
            {
                wireframeOnly = value;
                ResetHandleVisibility();
            }
        }
    }

    [SerializeField]
    private Vector3 wireframePadding = Vector3.zero;

    [SerializeField]
    private FlattenModeType flattenAxis = FlattenModeType.DoNotFlatten;

    [SerializeField]
    private WireframeType wireframeShape = WireframeType.Cubic;

    [SerializeField]
    private Material wireframeMaterial;

    [Header("Handles")]
    [Tooltip("Default materials will be created for Handles and Wireframe if none is specified.")]
    [SerializeField]
    private Material handleMaterial;

    [SerializeField]
    private Material handleGrabbedMaterial;

    [SerializeField]
    private bool showScaleHandles = true;

    /// <summary>
    /// Public property to Set the visibility of the corner cube Scaling handles.
    /// This property can be set independent of the Rotate handles.
    /// </summary>
    public bool ShowScaleHandles
    {
        get
        {
            return showScaleHandles;
        }
        set
        {
            if (showScaleHandles != value)
            {
                showScaleHandles = value;
                ResetHandleVisibility();
            }
        }
    }

    [SerializeField]
    private bool showRotateHandles = true;

    /// <summary>
    /// Public property to Set the visibility of the sphere rotating handles.
    /// This property can be set independent of the Scaling handles.
    /// </summary>
    public bool ShowRotateHandles
    {
        get
        {
            return showRotateHandles;
        }
        set
        {
            if (showRotateHandles != value)
            {
                showRotateHandles = value;
                ResetHandleVisibility();
            }
        }
    }

    [SerializeField]
    private float linkRadius = 0.005f;

    [SerializeField]
    private float ballRadius = 0.035f;

    [SerializeField]
    private float cornerRadius = 0.03f;

    [SerializeField]
    private float dragSensitivity = 0.2f;
    #endregion Serialized Fields

    #region Constants
    private const int LeftTopBack = 0;
    private const int LeftTopFront = 1;
    private const int LeftBottomFront = 2;
    private const int LeftBottomBack = 3;
    private const int RightTopBack = 4;
    private const int RightTopFront = 5;
    private const int RightBottonFront = 6;
    private const int RightBottomBack = 7;
    private const int CORNER_COUNT = 8;
    #endregion Constants

    #region Private Properties
    private bool isActive = false;
    /// <summary>
    /// This Public property sets whether the BoundingBox is active (visible)
    /// </summary>
    public bool IsActive
    {
        get
        {
            return isActive;
        }
        set
        {
            if (isActive != value)
            {
                if (value)
                {
                    CreateRig();
                    rigRoot.gameObject.SetActive(true);
                }
                else
                {
                    DestroyRig();
                }

                isActive = value;
                //ADD - close/open drag handler
                dragHandler.SetDragging(!IsActive);
            }
        }
    }
    private IMixedRealityPointer currentPointer;
    private IMixedRealityInputSource currentInputSource;
    private Vector3 initialGazePoint = Vector3.zero;
    private GameObject targetObject;
    private Transform rigRoot;
    private BoxCollider cachedTargetCollider;
    private Vector3[] boundsCorners;
    private Vector3 currentBoundsSize;
    private BoundsCalculationMethod boundsMethod;
    private HandleMoveType handleMoveType = HandleMoveType.Point;
    private List<Transform> links;
    private List<Transform> corners;
    private List<Transform> balls;
    private List<Renderer> cornerRenderers;
    private List<Renderer> ballRenderers;
    private List<Renderer> linkRenderers;
    private List<Collider> cornerColliders;
    private List<Collider> ballColliders;
    private Vector3[] edgeCenters;
    private Ray initialGrabRay;
    private Ray currentGrabRay;
    private float initialGrabMag;
    private Vector3 currentRotationAxis;
    private Vector3 initialScale;
    private Vector3 initialGrabbedPosition;
    private Vector3 initialGrabbedCentroid;
    private Vector3 initialGrabPoint;
    private CardinalAxisType[] edgeAxes;
    private int[] flattenedHandles;
    private Vector3 boundsCentroid;
    private GameObject grabbedHandle;
    private bool usingPose = true;
    private Vector3 currentPosePosition = Vector3.zero;
    private HandleType currentHandleType;
    #endregion Private Properties

    #region MonoBehaviour Methods
    private void Start()
    {
        targetObject = this.gameObject;

        if (MixedRealityToolkit.IsInitialized && MixedRealityToolkit.InputSystem != null)
        {
            MixedRealityToolkit.InputSystem.Register(targetObject);
        }

        //ADD init helper
        helper = new BoundingBoxHelper();
        if (dragHandler == null) dragHandler = this.GetComponent<DraggingCtr>();
        if (boxColliderToUse == null) boxColliderToUse = this.GetComponent<BoxCollider>();

        if (createToggleOnStart == true)
        {
            AlignToggleToBox(false);
        }

        if (activateOnStart == true)
        {
            IsActive = true;
        }
    }

    private void Update()
    {
        if (!isToggleCreated) return;
        //ADD : realtime follow box
        AlignToggleToBox(true);
        //ADD : check status is active 
        if (!IsActive) return;
        
        if (currentInputSource == null)
        {
            UpdateBounds();
        }
        else
        {
            UpdateBounds();
            TransformRig();
        }

        UpdateRigHandles();
    }
    #endregion MonoBehaviour Methods

    #region Private Methods
    private void AlignToggleToBox(bool smooth)
    {
        if (togglePrefab == null) return;

        if (toggleInstance == null)
        {
            isToggleCreated = true;
            var toggleObj = Instantiate(togglePrefab);
            toggleObj.name = this.name + "_ToggleButton";
            toggleInstance = toggleObj.GetComponent<Interactable>();
            toggleInstance?.OnClick.AddListener(ToggleBoundingBox);
        }

        //calculate best follow position for toggleInstance
        Vector3 finalPosition = Vector3.zero;
        Vector3 headPosition = CameraCache.Main.transform.position;
        LayerMask ignoreLayers = new LayerMask();
        List<Vector3> boundsPoints = new List<Vector3>();
        if (this != null)
        {
            // meshes exist. 
            //helper.UpdateNonAABoundingBoxCornerPositions(targetObject, boundsPoints, ignoreLayers);
            // there is no mesh. get box collider corners
            helper.UpdateBoundingBoxCornerPositionsFromColliderVertices(boxColliderToUse, boundsPoints);

            int followingFaceIndex = helper.GetIndexOfForwardFace(headPosition);
            Vector3 faceNormal = helper.GetFaceNormal(followingFaceIndex);
            
            //finally we have new position
            finalPosition = helper.GetFaceBottomCentroid(followingFaceIndex) + (faceNormal * HoverOffsetZ);
        }

        // Follow bounding box
        toggleInstance.transform.position = smooth ? Vector3.Lerp(toggleInstance.transform.position, finalPosition, 0.5f) : finalPosition;
        // Rotate on the y axis
        Vector3 eulerAngles = Quaternion.LookRotation((finalPosition- boxColliderToUse.center).normalized, Vector3.up).eulerAngles;
        eulerAngles.x = 0f;
        eulerAngles.z = 0f;
        //Debug.DrawLine(finalPosition, boxColliderToUse.center, Color.cyan);
        toggleInstance.transform.eulerAngles = eulerAngles;
    }

    private void ToggleBoundingBox() {
        IsActive = !IsActive;
    }

    private void CreateRig()
    {
        DestroyRig();
        SetMaterials();
        InitializeDataStructures();

        SetBoundingBoxCollider();

        UpdateBounds();
        AddCorners();
        AddLinks();
        UpdateRigHandles();
        Flatten();
        ResetHandleVisibility();
        rigRoot.gameObject.SetActive(false);
    }

    private void DestroyRig()
    {
        if (boxColliderToUse == null)
        {
            //ADD- destory gameobject indead transform
            Destroy(cachedTargetCollider.gameObject);
        }
        else
        {
            boxColliderToUse.size -= wireframePadding;
        }

        if (balls != null)
        {
            for (var i = 0; i < balls.Count; i++)
            {
                //ADD- destory gameobject indead transform
                Destroy(balls[i].gameObject);
            }

            balls.Clear();
        }

        if (links != null)
        {
            for (int i = 0; i < links.Count; i++)
            {
                //ADD- destory gameobject indead transform
                Destroy(links[i].gameObject);
            }

            links.Clear();
        }

        if (corners != null)
        {
            for (var i = 0; i < corners.Count; i++)
            {
                //ADD- destory gameobject indead transform
                Destroy(corners[i].gameObject);
            }

            corners.Clear();
        }

        if (rigRoot != null)
        {
            //ADD- destory gameobject indead transform
            Destroy(rigRoot.gameObject);
        }
    }

    private void TransformRig()
    {
        if (usingPose)
        {
            TransformHandleWithPoint();
        }
        else
        {
            Debug.Log(handleMoveType);
            switch (handleMoveType)
            {
                case HandleMoveType.Ray:
                    TransformHandleWithRay();
                    break;
                case HandleMoveType.Point:
                    TransformHandleWithPoint();
                    break;
                default:
                    Debug.LogWarning($"Unexpected handle move type {handleMoveType}");
                    break;
            }
        }
    }

    private void TransformHandleWithRay()
    {
        if (currentHandleType != HandleType.None)
        {
            currentGrabRay = GetHandleGrabbedRay();
            Vector3 grabRayPt = currentGrabRay.origin + (currentGrabRay.direction * initialGrabMag);


            switch (currentHandleType)
            {
                case HandleType.Rotation:
                    //ADD :　custom rotation method
                    ApplyRotation(grabRayPt);
                    //RotateByHandle(grabRayPt);
                    break;
                case HandleType.Scale:
                    //ADD :　custom scale method
                    ApplyScale(grabRayPt);
                    //ScaleByHandle(grabRayPt);
                    break;
                default:
                    Debug.LogWarning($"Unexpected handle type {currentHandleType}");
                    break;
            }
        }
    }

    private void TransformHandleWithPoint()
    {
        if (currentHandleType != HandleType.None)
        {
            Vector3 newGrabbedPosition;

            if (usingPose == false)
            {
                Vector3 newRemotePoint;
                //currentPointer.TryGetPointerPosition(out newRemotePoint);
                newRemotePoint = currentPointer.Position;
                newGrabbedPosition = initialGrabbedPosition + (newRemotePoint - initialGrabPoint);
            }
            else
            {
                if (initialGazePoint == Vector3.zero)
                {
                    return;
                }

                newGrabbedPosition = currentPosePosition;
            }

            if (currentHandleType == HandleType.Rotation)
            {
                //ADD :　custom rotation method
                //RotateByHandle(newGrabbedPosition);
                ApplyRotation(newGrabbedPosition);
            }
            else if (currentHandleType == HandleType.Scale)
            {
                //ADD :　custom scale method
                //ScaleByHandle(newGrabbedPosition);
                ApplyScale(newGrabbedPosition);
            }
        }
    }

    private void RotateByHandle(Vector3 newHandlePosition)
    {
        Vector3 projPt = Vector3.ProjectOnPlane((newHandlePosition - rigRoot.transform.position).normalized, currentRotationAxis);
        Quaternion rotation = Quaternion.FromToRotation((grabbedHandle.transform.position - rigRoot.transform.position).normalized, projPt.normalized);
        Vector3 axis;
        float angle;
        rotation.ToAngleAxis(out angle, out axis);
        targetObject.transform.RotateAround(rigRoot.transform.position, axis, angle);
    }

    private void ScaleByHandle(Vector3 newHandlePosition)
    {
        Vector3 correctedPt = PointToRay(rigRoot.transform.position, grabbedHandle.transform.position, newHandlePosition);
        Vector3 rigCentroid = rigRoot.transform.position;
        float startMag = (initialGrabbedPosition - rigCentroid).magnitude;
        float newMag = (correctedPt - rigCentroid).magnitude;

        bool isClamped;
        float ratio = newMag / startMag;
        Vector3 newScale = ClampScale(initialScale * ratio, out isClamped);
        //scale from object center
        targetObject.transform.localScale = newScale;
    }

    private Vector3 GetRotationAxis(GameObject handle)
    {
        for (int i = 0; i < balls.Count; ++i)
        {
            if (handle == balls[i])
            {
                switch (edgeAxes[i])
                {
                    case CardinalAxisType.X:
                        return rigRoot.transform.right;
                    case CardinalAxisType.Y:
                        return rigRoot.transform.up;
                    default:
                        return rigRoot.transform.forward;
                }
            }
        }

        return Vector3.zero;
    }

    private void AddCorners()
    {
        for (int i = 0; i < boundsCorners.Length; ++i)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"corner_{i}";
            cube.transform.localScale = new Vector3(cornerRadius, cornerRadius, cornerRadius);
            cube.transform.position = boundsCorners[i];
            cube.transform.parent = rigRoot.transform;

            var cubeRenderer = cube.GetComponent<Renderer>();
            cornerRenderers.Add(cubeRenderer);
            cornerColliders.Add(cube.GetComponent<Collider>());
            corners.Add(cube.transform);

            if (handleMaterial != null)
            {
                cubeRenderer.material = handleMaterial;
            }
        }
    }

    private void AddLinks()
    {
        edgeCenters = new Vector3[12];

        CalculateEdgeCenters();

        for (int i = 0; i < edgeCenters.Length; ++i)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = $"midpoint_{i}";
            ball.transform.localScale = new Vector3(ballRadius, ballRadius, ballRadius);
            ball.transform.position = edgeCenters[i];
            ball.transform.parent = rigRoot.transform;

            var ballRenderer = ball.GetComponent<Renderer>();
            ballRenderers.Add(ballRenderer);
            ballColliders.Add(ball.GetComponent<Collider>());
            balls.Add(ball.transform);

            if (handleMaterial != null)
            {
                ballRenderer.material = handleMaterial;
            }
        }

        edgeAxes = new CardinalAxisType[12];

        edgeAxes[0] = CardinalAxisType.X;
        edgeAxes[1] = CardinalAxisType.Y;
        edgeAxes[2] = CardinalAxisType.X;
        edgeAxes[3] = CardinalAxisType.Y;
        edgeAxes[4] = CardinalAxisType.X;
        edgeAxes[5] = CardinalAxisType.Y;
        edgeAxes[6] = CardinalAxisType.X;
        edgeAxes[7] = CardinalAxisType.Y;
        edgeAxes[8] = CardinalAxisType.Z;
        edgeAxes[9] = CardinalAxisType.Z;
        edgeAxes[10] = CardinalAxisType.Z;
        edgeAxes[11] = CardinalAxisType.Z;

        for (int i = 0; i < edgeCenters.Length; ++i)
        {
            var link = GameObject.CreatePrimitive(wireframeShape == WireframeType.Cubic
                ? PrimitiveType.Cube
                : PrimitiveType.Cylinder);
            link.name = $"link_{i}";

            Vector3 linkDimensions = GetLinkDimensions();

            switch (edgeAxes[i])
            {
                case CardinalAxisType.Y:
                    link.transform.localScale = new Vector3(linkRadius, linkDimensions.y, linkRadius);
                    link.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f));
                    break;
                case CardinalAxisType.Z:
                    link.transform.localScale = new Vector3(linkRadius, linkDimensions.z, linkRadius);
                    link.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
                    break;
                default: //X
                    link.transform.localScale = new Vector3(linkRadius, linkDimensions.x, linkRadius);
                    link.transform.Rotate(new Vector3(0.0f, 0.0f, 90.0f));
                    break;
            }

            link.transform.position = edgeCenters[i];
            link.transform.parent = rigRoot.transform;

            var linkRenderer = link.GetComponent<Renderer>();
            linkRenderers.Add(linkRenderer);

            if (wireframeMaterial != null)
            {
                linkRenderer.material = wireframeMaterial;
            }

            links.Add(link.transform);
        }
    }

    private void SetBoundingBoxCollider()
    {
        //Collider.bounds is world space bounding volume.
        //Mesh.bounds is local space bounding volume
        //Renderer.bounds is the same as mesh.bounds but in world space coords

        if (boxColliderToUse != null)
        {
            cachedTargetCollider = boxColliderToUse;
            cachedTargetCollider.transform.hasChanged = true;
        }
        else
        {
            Bounds bounds = GetTargetBounds();
            cachedTargetCollider = targetObject.AddComponent<BoxCollider>();
            switch (boundsMethod)
            {
                case BoundsCalculationMethod.Renderers:
                    cachedTargetCollider.center = bounds.center;
                    cachedTargetCollider.size = bounds.size;
                    break;
                case BoundsCalculationMethod.Colliders:
                    cachedTargetCollider.center = bounds.center;
                    cachedTargetCollider.size = bounds.size;
                    break;
                default:
                    Debug.LogWarning($"Unexpected Bounds Calculation Method {boundsMethod}");
                    break;
            }
        }

        cachedTargetCollider.size += wireframePadding;
    }

    private Bounds GetTargetBounds()
    {
        var bounds = new Bounds();

        if (targetObject.transform.childCount == 0)
        {
            bounds = GetSingleObjectBounds(targetObject);
            boundsMethod = BoundsCalculationMethod.Collider;
            return bounds;
        }

        for (int i = 0; i < targetObject.transform.childCount; ++i)
        {
            if (bounds.size == Vector3.zero)
            {
                bounds = GetSingleObjectBounds(targetObject.transform.GetChild(i).gameObject);
            }
            else
            {
                Bounds childBounds = GetSingleObjectBounds(targetObject.transform.GetChild(i).gameObject);

                if (childBounds.size != Vector3.zero)
                {
                    bounds.Encapsulate(childBounds);
                }
            }
        }

        if (bounds.size != Vector3.zero)
        {
            boundsMethod = BoundsCalculationMethod.Colliders;
            return bounds;
        }

        //simple case: sum of existing colliders
        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            //Collider.bounds is in world space.
            bounds = colliders[0].bounds;

            for (int i = 0; i < colliders.Length; ++i)
            {
                Bounds colliderBounds = colliders[i].bounds;
                if (colliderBounds.size != Vector3.zero)
                {
                    bounds.Encapsulate(colliderBounds);
                }
            }

            if (bounds.size != Vector3.zero)
            {
                boundsMethod = BoundsCalculationMethod.Colliders;
                return bounds;
            }
        }

        //Renderer bounds is local. Requires transform to global coord system.
        Renderer[] childRenderers = targetObject.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            bounds = childRenderers[0].bounds;
            var _corners = new Vector3[CORNER_COUNT];

            for (int i = 0; i < childRenderers.Length; ++i)
            {
                bounds.Encapsulate(childRenderers[i].bounds);
            }

            GetCornerPositionsFromBounds(bounds, ref boundsCorners);

            for (int cornerIndex = 0; cornerIndex < _corners.Length; ++cornerIndex)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = cornerIndex.ToString();
                cube.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                cube.transform.position = boundsCorners[cornerIndex];
            }

            boundsMethod = BoundsCalculationMethod.Renderers;
            return bounds;
        }

        MeshFilter[] meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length > 0)
        {
            //Mesh.bounds is local space bounding volume
            bounds.size = meshFilters[0].mesh.bounds.size;
            bounds.center = meshFilters[0].mesh.bounds.center;

            for (int i = 0; i < meshFilters.Length; ++i)
            {
                bounds.Encapsulate(meshFilters[i].mesh.bounds);
            }

            if (bounds.size != Vector3.zero)
            {
                bounds.center = targetObject.transform.position;
                boundsMethod = BoundsCalculationMethod.MeshFilters;
                return bounds;
            }
        }

        var boxCollider = targetObject.AddComponent<BoxCollider>();
        bounds = boxCollider.bounds;
        Destroy(boxCollider);
        boundsMethod = BoundsCalculationMethod.Collider;
        return bounds;
    }

    private Bounds GetSingleObjectBounds(GameObject boundsObject)
    {
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        Component[] components = boundsObject.GetComponents<Component>();

        if (components.Length < 3)
        {
            return bounds;
        }

        var boxCollider = boundsObject.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            boxCollider = boundsObject.AddComponent<BoxCollider>();
            bounds = boxCollider.bounds;
            Destroy(boxCollider);
        }
        else
        {
            bounds = boxCollider.bounds;
        }

        return bounds;
    }

    private void SetMaterials()
    {
        if (wireframeMaterial == null)
        {
            Shader.EnableKeyword("_InnerGlow");
            Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

            wireframeMaterial = new Material(shader);
            wireframeMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
        }

        if (handleMaterial == null && handleMaterial != wireframeMaterial)
        {
            float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

            Shader.EnableKeyword("_InnerGlow");
            Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

            handleMaterial = new Material(shader);
            handleMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
            handleMaterial.SetFloat("_InnerGlow", 1.0f);
            handleMaterial.SetFloatArray("_InnerGlowColor", color);
        }

        if (handleGrabbedMaterial == null && handleGrabbedMaterial != handleMaterial && handleGrabbedMaterial != wireframeMaterial)
        {
            float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

            Shader.EnableKeyword("_InnerGlow");
            Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

            handleGrabbedMaterial = new Material(shader);
            handleGrabbedMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
            handleGrabbedMaterial.SetFloat("_InnerGlow", 1.0f);
            handleGrabbedMaterial.SetFloatArray("_InnerGlowColor", color);
        }
    }

    private void InitializeDataStructures()
    {
        rigRoot = new GameObject("rigRoot").transform;
        //rigRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

        boundsCorners = new Vector3[8];

        corners = new List<Transform>();
        cornerColliders = new List<Collider>();
        cornerRenderers = new List<Renderer>();
        balls = new List<Transform>();
        ballRenderers = new List<Renderer>();
        ballColliders = new List<Collider>();
        links = new List<Transform>();
        linkRenderers = new List<Renderer>();
    }

    private void CalculateEdgeCenters()
    {
        if (boundsCorners != null && edgeCenters != null)
        {
            edgeCenters[0] = (boundsCorners[0] + boundsCorners[1]) * 0.5f;
            edgeCenters[1] = (boundsCorners[1] + boundsCorners[2]) * 0.5f;
            edgeCenters[2] = (boundsCorners[2] + boundsCorners[3]) * 0.5f;
            edgeCenters[3] = (boundsCorners[3] + boundsCorners[0]) * 0.5f;

            edgeCenters[4] = (boundsCorners[4] + boundsCorners[5]) * 0.5f;
            edgeCenters[5] = (boundsCorners[5] + boundsCorners[6]) * 0.5f;
            edgeCenters[6] = (boundsCorners[6] + boundsCorners[7]) * 0.5f;
            edgeCenters[7] = (boundsCorners[7] + boundsCorners[4]) * 0.5f;

            edgeCenters[8] = (boundsCorners[0] + boundsCorners[4]) * 0.5f;
            edgeCenters[9] = (boundsCorners[1] + boundsCorners[5]) * 0.5f;
            edgeCenters[10] = (boundsCorners[2] + boundsCorners[6]) * 0.5f;
            edgeCenters[11] = (boundsCorners[3] + boundsCorners[7]) * 0.5f;
        }
    }

    private Vector3 ClampScale(Vector3 scale, out bool clamped)
    {
        Vector3 finalScale = scale;
        Vector3 maximumScale = initialScale * scaleMaximum;
        clamped = false;

        if (scale.x > maximumScale.x || scale.y > maximumScale.y || scale.z > maximumScale.z)
        {
            finalScale = maximumScale;
            clamped = true;
        }

        Vector3 minimumScale = initialScale * scaleMinimum;

        if (finalScale.x < minimumScale.x || finalScale.y < minimumScale.y || finalScale.z < minimumScale.z)
        {
            finalScale = minimumScale;
            clamped = true;
        }

        return finalScale;
    }

    private Vector3 GetLinkDimensions()
    {
        float linkLengthAdjustor = wireframeShape == WireframeType.Cubic ? 2.0f : 1.0f - (6.0f * linkRadius);
        return (currentBoundsSize * linkLengthAdjustor) + new Vector3(linkRadius, linkRadius, linkRadius);
    }

    private void ResetHandleVisibility()
    {
        bool isVisible;
        if (!IsActive) return;
        //set balls visibility
        if (balls != null)
        {
            isVisible = (!wireframeOnly && showRotateHandles);

            for (int i = 0; i < ballRenderers.Count; ++i)
            {
                ballRenderers[i].material = handleMaterial;
                ballRenderers[i].enabled = isVisible;
            }
        }

        //set corner visibility
        if (corners != null)
        {
            isVisible = (!wireframeOnly && showScaleHandles);

            for (int i = 0; i < cornerRenderers.Count; ++i)
            {
                cornerRenderers[i].material = handleMaterial;
                cornerRenderers[i].enabled = isVisible;
            }
        }

        SetHiddenHandles();
    }

    private void ShowOneHandle(GameObject handle)
    {
        //turn off all balls
        if (balls != null)
        {
            for (int i = 0; i < ballRenderers.Count; ++i)
            {
                ballRenderers[i].enabled = false;
            }
        }

        //turn off all corners
        if (corners != null)
        {
            for (int i = 0; i < cornerRenderers.Count; ++i)
            {
                cornerRenderers[i].enabled = false;
            }
        }

        //turn on one handle
        if (handle != null)
        {
            var handleRenderer = handle.GetComponent<Renderer>();
            handleRenderer.material = handleGrabbedMaterial;
            handleRenderer.enabled = true;
        }
    }

    private void UpdateBounds()
    {
        Vector3 boundsSize = Vector3.zero;
        Vector3 centroid = Vector3.zero;

        //store current rotation then zero out the rotation so that the bounds
        //are computed when the object is in its 'axis aligned orientation'.
        Quaternion currentRotation = targetObject.transform.rotation;
        targetObject.transform.rotation = Quaternion.identity;

        if (cachedTargetCollider != null)
        {
            Bounds colliderBounds = cachedTargetCollider.bounds;
            boundsSize = Vector3.Scale(cachedTargetCollider.size / 2, cachedTargetCollider.transform.localScale);
            centroid = colliderBounds.center;
        }

        //after bounds are computed, restore rotation...
        targetObject.transform.rotation = currentRotation;

        if (boundsSize != Vector3.zero)
        {
            if (flattenAxis == FlattenModeType.FlattenAuto)
            {
                float min = Mathf.Min(boundsSize.x, Mathf.Min(boundsSize.y, boundsSize.z));
                flattenAxis = min.Equals(boundsSize.x) ? FlattenModeType.FlattenX : (min.Equals(boundsSize.y) ? FlattenModeType.FlattenY : FlattenModeType.FlattenZ);
            }

            boundsSize.x = flattenAxis == FlattenModeType.FlattenX ? 0.0f : boundsSize.x;
            boundsSize.y = flattenAxis == FlattenModeType.FlattenY ? 0.0f : boundsSize.y;
            boundsSize.z = flattenAxis == FlattenModeType.FlattenZ ? 0.0f : boundsSize.z;

            //@TODO: Fix boundingbox size perform abnormally while rotating
            currentBoundsSize = boundsSize;
            boundsCentroid = centroid;
            //Debug.DrawLine(targetObject.transform.position, centroid,Color.cyan);

            boundsCorners[0] = centroid - new Vector3(centroid.x - currentBoundsSize.x, centroid.y - currentBoundsSize.y, centroid.z - currentBoundsSize.z);
            boundsCorners[1] = centroid - new Vector3(centroid.x + currentBoundsSize.x, centroid.y - currentBoundsSize.y, centroid.z - currentBoundsSize.z);
            boundsCorners[2] = centroid - new Vector3(centroid.x + currentBoundsSize.x, centroid.y + currentBoundsSize.y, centroid.z - currentBoundsSize.z);
            boundsCorners[3] = centroid - new Vector3(centroid.x - currentBoundsSize.x, centroid.y + currentBoundsSize.y, centroid.z - currentBoundsSize.z);

            boundsCorners[4] = centroid - new Vector3(centroid.x - currentBoundsSize.x, centroid.y - currentBoundsSize.y, centroid.z + currentBoundsSize.z);
            boundsCorners[5] = centroid - new Vector3(centroid.x + currentBoundsSize.x, centroid.y - currentBoundsSize.y, centroid.z + currentBoundsSize.z);
            boundsCorners[6] = centroid - new Vector3(centroid.x + currentBoundsSize.x, centroid.y + currentBoundsSize.y, centroid.z + currentBoundsSize.z);
            boundsCorners[7] = centroid - new Vector3(centroid.x - currentBoundsSize.x, centroid.y + currentBoundsSize.y, centroid.z + currentBoundsSize.z);

            CalculateEdgeCenters();
        }
    }

    private void UpdateRigHandles()
    {
        if (rigRoot != null && targetObject != null)
        {
            rigRoot.rotation = Quaternion.identity;
            rigRoot.position = Vector3.zero;

            for (int i = 0; i < corners.Count; ++i)
            {
                corners[i].position = boundsCorners[i];
            }

            Vector3 linkDimensions = GetLinkDimensions();

            for (int i = 0; i < edgeCenters.Length; ++i)
            {
                balls[i].position = edgeCenters[i];
                links[i].position = edgeCenters[i];

                if (edgeAxes[i] == CardinalAxisType.X)
                {
                    links[i].localScale = new Vector3(linkRadius, linkDimensions.x, linkRadius);
                }
                else if (edgeAxes[i] == CardinalAxisType.Y)
                {
                    links[i].localScale = new Vector3(linkRadius, linkDimensions.y, linkRadius);
                }
                else
                {
                    links[i].localScale = new Vector3(linkRadius, linkDimensions.z, linkRadius);
                }
            }

            //move rig into position and rotation
            rigRoot.position = cachedTargetCollider.bounds.center;
            rigRoot.rotation = targetObject.transform.rotation;
        }
    }

    private HandleType GetHandleType(GameObject handle)
    {
        //ADD : Fixed not get type correctly issue.
        if (balls.Contains(handle.transform))
            return HandleType.Rotation;
        else if (corners.Contains(handle.transform))
            return HandleType.Scale;
        else
            return HandleType.None;

        for (int i = 0; i < balls.Count; ++i)
        {
            if (handle == balls[i])
            {
                return HandleType.Rotation;
            }
        }

        for (int i = 0; i < corners.Count; ++i)
        {
            if (handle == corners[i])
            {
                return HandleType.Scale;
            }
        }

        return HandleType.None;
    }

    private Collider GetGrabbedCollider(Ray ray, out float distance)
    {
        Collider closestCollider = null;
        float currentDistance;
        float closestDistance = float.MaxValue;


        for (int i = 0; i < cornerColliders.Count; ++i)
        {
            if (cornerRenderers[i].enabled && cornerColliders[i].bounds.IntersectRay(ray, out currentDistance))
            {
                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestCollider = cornerColliders[i];
                }
            }
        }

        for (int i = 0; i < ballColliders.Count; ++i)
        {
            if (ballRenderers[i].enabled && ballColliders[i].bounds.IntersectRay(ray, out currentDistance))
            {
                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestCollider = ballColliders[i];
                }
            }
        }

        distance = closestDistance;
        return closestCollider;
    }

    private Ray GetHandleGrabbedRay()
    {
        Ray pointerRay = new Ray();

        if (currentInputSource.Pointers.Length > 0)
        {
            pointerRay = currentInputSource.Pointers[0].Rays[0];
            //currentInputSource.Pointers[0].TryGetPointingRay(out pointerRay);
        }

        return pointerRay;
    }

    private void Flatten()
    {
        switch (flattenAxis)
        {
            case FlattenModeType.FlattenX:
                flattenedHandles = new[] { 0, 4, 2, 6 };
                break;
            case FlattenModeType.FlattenY:
                flattenedHandles = new[] { 1, 3, 5, 7 };
                break;
            case FlattenModeType.FlattenZ:
                flattenedHandles = new[] { 9, 10, 8, 11 };
                break;
        }

        if (flattenedHandles != null)
        {
            for (int i = 0; i < flattenedHandles.Length; ++i)
            {
                linkRenderers[flattenedHandles[i]].enabled = false;
            }
        }
    }

    private void SetHiddenHandles()
    {
        if (flattenedHandles != null)
        {
            for (int i = 0; i < flattenedHandles.Length; ++i)
            {
                ballRenderers[flattenedHandles[i]].enabled = false;
            }
        }
    }

    private void GetCornerPositionsFromBounds(Bounds bounds, ref Vector3[] positions)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float leftEdge = center.x - extents.x;
        float rightEdge = center.x + extents.x;
        float bottomEdge = center.y - extents.y;
        float topEdge = center.y + extents.y;
        float frontEdge = center.z - extents.z;
        float backEdge = center.z + extents.z;

        if (positions == null || positions.Length != CORNER_COUNT)
        {
            positions = new Vector3[CORNER_COUNT];
        }

        positions[LeftBottomFront] = new Vector3(leftEdge, bottomEdge, frontEdge);
        positions[LeftBottomBack] = new Vector3(leftEdge, bottomEdge, backEdge);
        positions[LeftTopFront] = new Vector3(leftEdge, topEdge, frontEdge);
        positions[LeftTopBack] = new Vector3(leftEdge, topEdge, backEdge);
        positions[RightBottonFront] = new Vector3(rightEdge, bottomEdge, frontEdge);
        positions[RightBottomBack] = new Vector3(rightEdge, bottomEdge, backEdge);
        positions[RightTopFront] = new Vector3(rightEdge, topEdge, frontEdge);
        positions[RightTopBack] = new Vector3(rightEdge, topEdge, backEdge);
    }

    private static Vector3 PointToRay(Vector3 origin, Vector3 end, Vector3 closestPoint)
    {
        Vector3 originToPoint = closestPoint - origin;
        Vector3 originToEnd = end - origin;
        float magnitudeAb = originToEnd.sqrMagnitude;
        float dotProduct = Vector3.Dot(originToPoint, originToEnd);
        float distance = dotProduct / magnitudeAb;
        return origin + (originToEnd * distance);
    }
    #endregion Private Methods

    #region Used Event Handlers
    public void OnInputDown(InputEventData eventData)
    {
        //ADD : check status is active 
        if (!IsActive) return;
        if (currentInputSource == null)
        {
            IMixedRealityPointer pointer = eventData.InputSource.Pointers[0];
            if (pointer == null) return;
            Ray ray = pointer.Rays[0]; 
            if (ray.IsValid())
            {
                handleMoveType = HandleMoveType.Ray;
                float distance;
                Collider grabbedCollider = GetGrabbedCollider(ray, out distance);

                if (grabbedCollider != null)
                {
                    currentInputSource = eventData.InputSource;
                    currentPointer = pointer;
                    grabbedHandle = grabbedCollider.gameObject;
                    currentHandleType = GetHandleType(grabbedHandle);
                    currentRotationAxis = GetRotationAxis(grabbedHandle);
                    initialGrabRay = currentPointer.Rays[0];
                    //currentPointer.TryGetPointingRay(out initialGrabRay);
                    initialGrabMag = distance;
                    initialGrabbedPosition = grabbedHandle.transform.position;
                    initialGrabbedCentroid = targetObject.transform.position;
                    initialScale = targetObject.transform.localScale;

                    //ADD - store initial rotation and axis
                    if (currentHandleType == HandleType.Rotation)
                    {
                        initialRotation = targetObject.transform.rotation;
                        initialRotationAxis = GetAxisbyIndex(balls.IndexOf(grabbedHandle.transform));
                    }
                    
                    //pointer.TryGetPointerPosition(out initialGrabPoint);
                    initialGrabPoint = pointer.Position;
                    ShowOneHandle(grabbedHandle);
                    initialGazePoint = Vector3.zero;
                }
            }
        }
    }

    public void OnInputUp(InputEventData eventData)
    {
        //ADD : check status is active 
        if (!IsActive) return;
        if (currentInputSource != null && eventData.InputSource.SourceId == currentInputSource.SourceId)
        {
            currentInputSource = null;
            currentHandleType = HandleType.None;
            currentPointer = null;
            grabbedHandle = null;
            ResetHandleVisibility();
        }
    }

    public void OnInputChanged(InputEventData<MixedRealityPose> eventData)
    {
        //ADD : check status is active 
        if (!IsActive) return;
        return;
        if (currentInputSource != null && eventData.InputSource.SourceId == currentInputSource.SourceId)
        {
            Vector3 pos = eventData.InputData.Position;
            usingPose = true;
            if (initialGazePoint == Vector3.zero)
            {
                initialGazePoint = pos;
            }
            currentPosePosition = initialGrabbedPosition + (pos - initialGazePoint);
        }
        else
        {
            usingPose = false;
        }
    }

    public void OnSourceLost(SourceStateEventData eventData)
    {
        if (currentInputSource != null && eventData.InputSource.SourceId == currentInputSource.SourceId)
        {
            currentInputSource = null;
            currentHandleType = HandleType.None;
            currentPointer = null;
            grabbedHandle = null;
            ResetHandleVisibility();
        }
    }
    #endregion Used Event Handlers

    #region Unused Event Handlers
    public void OnPointerDown(MixedRealityPointerEventData eventData) { }
    public void OnPointerUp(MixedRealityPointerEventData eventData) { }
    public void OnPointerClicked(MixedRealityPointerEventData eventData) { }
    public void OnInputPressed(InputEventData<float> eventData) { }
    public void OnPositionInputChanged(InputEventData<Vector2> eventData) { }
    public void OnPositionChanged(InputEventData<Vector3> eventData) { }
    public void OnRotationChanged(InputEventData<Quaternion> eventData) { }
    public void OnSourceDetected(SourceStateEventData eventData) { }
    #endregion Unused Event Handlers
    //call Init method later if createToggleOnStart = false
    public void Init()
    {
        if (!isToggleCreated)
        {
            AlignToggleToBox(false);
        }
    }

    //ADD custom rotation method
    public enum RotationType
    {
        ObjectBased,
        GlobalBased
    }
    public enum RotationAxisEnum
    {
        X,       // the X axis
        Y,       // the Y axis
        Z        // the Z axis
    }
    private float minimumScaleNav = .001f;
    private float scaleRate = 1.0f;
    private float maxScale = 5.0f;
    private Quaternion initialRotation;
    private RotationAxisEnum initialRotationAxis;
    private Vector3 rotationFromPositionScale = -150f * Vector3.one;
    [SerializeField]
    private RotationType rotationType;
    /// <summary>
    /// Get Rotation Axis by handle(balls/corners) index (Hard-Coded) 
    /// </summary>
    private RotationAxisEnum GetAxisbyIndex(int index)
    {
        if (index == 0 || index == 2 || index == 4 || index == 6)
            return RotationAxisEnum.X;
        else if (index == 1 || index == 3 || index == 5 || index == 7)
            return RotationAxisEnum.Y;
        else
            return RotationAxisEnum.Z;
    }

    /// <summary>
    /// Compute the change of scale
    /// </summary>
    private Vector3 GetBoundedScaleChange(Vector3 scale)
    {
        Vector3 maximumScale = new Vector3(initialScale.x * maxScale, initialScale.y * maxScale, initialScale.z * maxScale);
        Vector3 intendedFinalScale = new Vector3(initialScale.x, initialScale.y, initialScale.z);
        intendedFinalScale.Scale(scale);
        if (intendedFinalScale.x > maximumScale.x || intendedFinalScale.y > maximumScale.y || intendedFinalScale.z > maximumScale.z)
        {
            return new Vector3(maximumScale.x / initialScale.x, maximumScale.y / initialScale.y, maximumScale.z / initialScale.z);
        }

        return scale;
    }

    /// <summary>
    /// Compute the delta of mouse pos on handle along one axis and apply rotation
    /// </summary>
    public void ApplyRotation(Vector3 currentHandlePosition)
    {
        RotationAxisEnum Axis = initialRotationAxis;

        Vector3 initPos = CameraCache.Main.WorldToScreenPoint(initialGrabbedPosition);
        Vector3 currentPos = CameraCache.Main.WorldToScreenPoint(currentHandlePosition);
        Vector3 delta = initPos - currentPos;

        Vector3 newEulers = new Vector3(0, 0, 0);
        if (Axis == RotationAxisEnum.X)
        {
            newEulers = new Vector3(-delta.y, 0, 0);
        }
        else if (Axis == RotationAxisEnum.Y)
        {
            newEulers = new Vector3(0, delta.x, 0);
        }
        else if (Axis == RotationAxisEnum.Z)
        {
            newEulers = new Vector3(0, 0, -delta.x);
        }

        if (rotationType == RotationType.GlobalBased)
        {
            newEulers += initialRotation.eulerAngles * dragSensitivity;
            this.transform.rotation = Quaternion.Euler(newEulers);
        }
        else if (rotationType == RotationType.ObjectBased)
        {
            Vector3 axis = (Axis == RotationAxisEnum.X ? new Vector3(1, 0, 0) : Axis == RotationAxisEnum.Y ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1));
            this.transform.localRotation = initialRotation;
            float angle = newEulers.x != 0 ? newEulers.x : newEulers.y != 0 ? newEulers.y : newEulers.z;
            this.transform.Rotate(axis, angle * dragSensitivity);
        }
    }

    /// <summary>
    /// Compute the delta of mouse pos on handle and apply scale
    /// </summary>
    public void ApplyScale(Vector3 currentHandlePosition)
    {
        if ((this.transform.position - initialGrabbedPosition).magnitude > minimumScaleNav)
        {
            Vector3 initPos = CameraCache.Main.WorldToScreenPoint(initialGrabbedPosition);
            Vector3 currentPos = CameraCache.Main.WorldToScreenPoint(currentHandlePosition);
            Vector3 target = CameraCache.Main.WorldToScreenPoint(this.transform.position);

            float scaleScalar = (currentPos - target).magnitude / (target - initPos).magnitude;
            scaleScalar = Mathf.Pow(scaleScalar, scaleRate);
            Vector3 changeScale = new Vector3(scaleScalar, scaleScalar, scaleScalar);
            changeScale = GetBoundedScaleChange(changeScale);

            Vector3 newScale = changeScale;
            newScale.Scale(initialScale);

            //scale from object center
            this.transform.localScale = newScale;
        }
    }
}
public class BoundingBoxHelper
{
    readonly int[] face0 = { 0, 1, 3, 2 };
    readonly int[] face1 = { 1, 5, 7, 3 };
    readonly int[] face2 = { 5, 4, 6, 7 };
    readonly int[] face3 = { 4, 0, 2, 6 };
    readonly int[] face4 = { 6, 2, 3, 7 };
    readonly int[] face5 = { 1, 0, 4, 5 };
    readonly int[] noFaceIndices = { };
    readonly Vector3[] noFaceVertices = { };

    private List<Vector3> rawBoundingCorners = new List<Vector3>();
    private List<Vector3> worldBoundingCorners = new List<Vector3>();
    private GameObject targetObject;
    private bool rawBoundingCornersObtained = false;

    public bool CheckMeshesInside(GameObject target) {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Objects that align to an target's bounding box can call this function in the object's UpdateLoop
    /// to get current bound points;
    /// </summary>
    /// <param name="target"></param>
    /// <param name="boundsPoints"></param>
    /// <param name="ignoreLayers"></param>
    public void UpdateNonAABoundingBoxCornerPositions(GameObject target, List<Vector3> boundsPoints, LayerMask ignoreLayers)
    {
        if (target != targetObject || rawBoundingCornersObtained == false)
        {
            GetRawBBCorners(target, ignoreLayers);
        }

        if (target == targetObject && rawBoundingCornersObtained)
        {
            boundsPoints.Clear();
            for (int i = 0; i < rawBoundingCorners.Count; ++i)
            {
                boundsPoints.Add(target.transform.localToWorldMatrix.MultiplyPoint(rawBoundingCorners[i]));
            }

            worldBoundingCorners.Clear();
            worldBoundingCorners.AddRange(boundsPoints);
        }
    }

    public void UpdateBoundingBoxCornerPositionsFromColliderVertices(BoxCollider collider, List<Vector3> boundsPoints)
    {
        //check this in world
        Vector3 colliderCentre = collider.center;
        Vector3 colliderExtents = collider.size/2;
        boundsPoints.Clear();
        for (int i = 0; i != 8; ++i)
        {
            Vector3 extent = colliderExtents;

            extent.Scale(new Vector3((i & 1) == 0 ? 1 : -1, (i & 2) == 0 ? 1 : -1, (i & 4) == 0 ? 1 : -1));

            Vector3 vertexPosLocal = colliderCentre + extent;

            Vector3 vertexPosGlobal = collider.transform.TransformPoint(vertexPosLocal);

            // display vector3 to six decimal places
            // Debug.Log("Vertex " + i + " @ " + vertexPosGlobal.ToString("F6"));
            boundsPoints.Add(vertexPosGlobal);
        }
        worldBoundingCorners.Clear();
        worldBoundingCorners.AddRange(boundsPoints);

        /* DrawLines to check corners.
        for (int i = 0; i < worldBoundingCorners.Count; i++)
        {
            Debug.DrawLine(worldBoundingCorners[i], collider.transform.position, Color.cyan);
        }
        */
    }

    /// <summary>
    /// This function gets the untransformed bounding box corner points of a GameObject.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="ignoreLayers"></param>
    public void GetRawBBCorners(GameObject target, LayerMask ignoreLayers)
    {
        targetObject = target;
        rawBoundingCorners.Clear();
        rawBoundingCornersObtained = false;

        GetUntransformedCornersFromObject(target, rawBoundingCorners, ignoreLayers);

        if (rawBoundingCorners != null && rawBoundingCorners.Count >= 4)
        {
            rawBoundingCornersObtained = true;
        }
    }

    /// <summary>
    /// this function gets the indices of the bounding cube corners that make up a face.
    /// </summary>
    /// <param name="index">the face index of the bounding cube 0-5</param>
    /// <returns>an array of four integer indices</returns>
    public int[] GetFaceIndices(int index)
    {
        switch (index)
        {
            case 0:
                return face0;
            case 1:
                return face1;
            case 2:
                return face2;
            case 3:
                return face3;
            case 4:
                return face4;
            case 5:
                return face5;
        }

        return noFaceIndices;
    }

    /// <summary>
    /// This function returns the midpoints of each of the edges of the face of the bounding box
    /// </summary>
    /// <param name="index">the index of the face of the bounding cube- 0-5</param>
    /// <returns>four Vector3 points</returns>
    public Vector3[] GetFaceEdgeMidpoints(int index)
    {
        Vector3[] corners = GetFaceCorners(index);
        Vector3[] midpoints = new Vector3[4];
        midpoints[0] = (corners[0] + corners[1]) * 0.5f;
        midpoints[1] = (corners[1] + corners[2]) * 0.5f;
        midpoints[2] = (corners[2] + corners[3]) * 0.5f;
        midpoints[3] = (corners[3] + corners[0]) * 0.5f;

        return midpoints;
    }

    /// <summary>
    /// Get the normal of the face of the bounding cube specified by index
    /// </summary>
    /// <param name="index">the index of the face of the bounding cube 0-5</param>
    /// <returns>a vector3 representing the face normal</returns>
    public Vector3 GetFaceNormal(int index)
    {
        int[] face = GetFaceIndices(index);
        
        if (face.Length == 4)
        {
            Vector3 ab = (worldBoundingCorners[face[1]] - worldBoundingCorners[face[0]]).normalized;
            Vector3 ac = (worldBoundingCorners[face[2]] - worldBoundingCorners[face[0]]).normalized;

            return Vector3.Cross(ab, ac).normalized;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// This function returns the centroid of a face of the bounding cube of an object specified
    /// by the index parameter;
    /// </summary>
    /// <param name="index">an index into the list of faces of a boundingcube. 0-5</param>
    /// <returns></returns>
    public Vector3 GetFaceCentroid(int index)
    {
        int[] faceIndices = GetFaceIndices(index);

        if (faceIndices.Length == 4)
        {
            return (worldBoundingCorners[faceIndices[0]] +
                    worldBoundingCorners[faceIndices[1]] +
                    worldBoundingCorners[faceIndices[2]] +
                    worldBoundingCorners[faceIndices[3]]) * 0.25f;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get the center of the bottom edge of a face of the bounding box determined by index
    /// </summary>
    /// <param name="index">parameter indicating which face is used. 0-5</param>
    /// <returns>a vector representing the bottom most edge center of the face</returns>
    public Vector3 GetFaceBottomCentroid(int index)
    {
        Vector3[] edgeCentroids = GetFaceEdgeMidpoints(index);

        Vector3 leastYPoint = edgeCentroids[0];
        for (int i = 1; i < 4; ++i)
        {
            leastYPoint = edgeCentroids[i].y < leastYPoint.y ? edgeCentroids[i] : leastYPoint;
        }
        return leastYPoint;
    }

    /// <summary>
    /// This function returns the four couners of a face of a bounding cube specified by index.
    /// </summary>
    /// <param name="index">the index of the face of the bounding cube. 0-5</param>
    /// <returns>an array of 4 vectors</returns>
    public Vector3[] GetFaceCorners(int index)
    {
        int[] faceIndices = GetFaceIndices(index);

        if (faceIndices.Length == 4)
        {
            Vector3[] face = new Vector3[4];
            face[0] = worldBoundingCorners[faceIndices[0]];
            face[1] = worldBoundingCorners[faceIndices[1]];
            face[2] = worldBoundingCorners[faceIndices[2]];
            face[3] = worldBoundingCorners[faceIndices[3]];
            return face;
        }

        return noFaceVertices;
    }

    /// <summary>
    /// This function gets the index of the face of the bounding cube that is most facing the lookAtPoint.
    /// This could be the headPosition or camera position if the face that was facing the view is desired.
    /// </summary>
    /// <param name="lookAtPoint">the world coordinate to test which face is desired</param>
    /// <returns>an integer representing the index of the bounding box faces</returns>
    public int GetIndexOfForwardFace(Vector3 lookAtPoint)
    {
        int highestDotIndex = -1;
        float hightestDotValue = float.MinValue;
        for (int i = 0; i < 6; ++i)
        {
            Vector3 a = (lookAtPoint - GetFaceCentroid(i)).normalized;
            Vector3 b = GetFaceNormal(i);
            float dot = Vector3.Dot(a, b);
            if (hightestDotValue < dot)
            {
                hightestDotValue = dot;
                highestDotIndex = i;
            }
        }
        return highestDotIndex;
    }

    /// <summary>
    /// This is the static function to call to get the non-Axis-aligned bounding box corners one time only.
    /// Use this function if the calling object only needs the info once. To get an updated boundingbox use the
    /// function above---UpdateNonAABoundingBoxCornerPositions(...);
    /// </summary>
    /// <param name="target">The gameObject whose bounding box is desired</param>
    /// <param name="boundsPoints">the array of 8 points that will be filled</param>
    /// <param name="ignoreLayers">a LayerMask variable</param>
    public static void GetNonAABoundingBoxCornerPositions(GameObject target, List<Vector3> boundsPoints, LayerMask ignoreLayers)
    {
        //get untransformed points
        GetUntransformedCornersFromObject(target, boundsPoints, ignoreLayers);

        //transform the points
        for (int i = 0; i < boundsPoints.Count; ++i)
        {
            boundsPoints[i] = target.transform.localToWorldMatrix.MultiplyPoint(boundsPoints[i]);
        }
    }

    /// <summary>
    /// static function that performs one-time non-persistent calculation of boundingbox of object without transformation.
    /// </summary>
    /// <param name="target">The gameObject whose bounding box is desired</param>
    /// <param name="boundsPoints">the array of 8 points that will be filled</param>
    /// <param name="ignoreLayers">a LayerMask variable</param>
    public static void GetUntransformedCornersFromObject(GameObject target, List<Vector3> boundsPoints, LayerMask ignoreLayers)
    {
        GameObject clone = GameObject.Instantiate(target);
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.position = Vector3.zero;
        clone.transform.localScale = Vector3.one;
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; ++i)
        {
            var rendererObj = renderers[i];
            if (ignoreLayers == (1 << rendererObj.gameObject.layer | ignoreLayers))
            {
                continue;
            }
            Vector3[] corners = null;
            rendererObj.bounds.GetCornerPositionsFromRendererBounds(ref corners);
            AddAABoundingBoxes(boundsPoints, corners);
        }

        GameObject.Destroy(clone);
    }
    /// <summary>
    /// This function expands the box defined by the first param 'points' to include the second bounding box 'pointsToAdd'. The
    /// result is found in the points variable.
    /// </summary>
    /// <param name="points">the bounding box points representing box A</param>
    /// <param name="pointsToAdd">the bounding box points representing box B</param>
    public static void AddAABoundingBoxes(List<Vector3> points, Vector3[] pointsToAdd)
    {
        if (points.Count < 8)
        {
            points.Clear();
            points.AddRange(pointsToAdd);
            return;
        }

        for (int i = 0; i < pointsToAdd.Length; ++i)
        {
            if (pointsToAdd[i].x < points[0].x)
            {
                points[0].Set(pointsToAdd[i].x, points[0].y, points[0].z);
                points[1].Set(pointsToAdd[i].x, points[1].y, points[1].z);
                points[2].Set(pointsToAdd[i].x, points[2].y, points[2].z);
                points[3].Set(pointsToAdd[i].x, points[3].y, points[3].z);
            }
            if (pointsToAdd[i].x > points[4].x)
            {
                points[4].Set(pointsToAdd[i].x, points[4].y, points[4].z);
                points[5].Set(pointsToAdd[i].x, points[5].y, points[5].z);
                points[6].Set(pointsToAdd[i].x, points[6].y, points[6].z);
                points[7].Set(pointsToAdd[i].x, points[7].y, points[7].z);
            }

            if (pointsToAdd[i].y < points[0].y)
            {
                points[0].Set(points[0].x, pointsToAdd[i].y, points[0].z);
                points[1].Set(points[1].x, pointsToAdd[i].y, points[1].z);
                points[4].Set(points[4].x, pointsToAdd[i].y, points[4].z);
                points[5].Set(points[5].x, pointsToAdd[i].y, points[5].z);
            }
            if (pointsToAdd[i].y > points[2].y)
            {
                points[2].Set(points[2].x, pointsToAdd[i].y, points[2].z);
                points[3].Set(points[3].x, pointsToAdd[i].y, points[3].z);
                points[6].Set(points[6].x, pointsToAdd[i].y, points[6].z);
                points[7].Set(points[7].x, pointsToAdd[i].y, points[7].z);
            }

            if (pointsToAdd[i].z < points[0].z)
            {
                points[0].Set(points[0].x, points[0].y, pointsToAdd[i].z);
                points[2].Set(points[2].x, points[2].y, pointsToAdd[i].z);
                points[6].Set(points[6].x, points[6].y, pointsToAdd[i].z);
                points[4].Set(points[4].x, points[4].y, pointsToAdd[i].z);
            }
            if (pointsToAdd[i].z > points[1].z)
            {
                points[1].Set(points[1].x, points[1].y, pointsToAdd[i].z);
                points[5].Set(points[5].x, points[5].y, pointsToAdd[i].z);
                points[7].Set(points[7].x, points[7].y, pointsToAdd[i].z);
                points[3].Set(points[3].x, points[3].y, pointsToAdd[i].z);
            }
        }
    }
}


