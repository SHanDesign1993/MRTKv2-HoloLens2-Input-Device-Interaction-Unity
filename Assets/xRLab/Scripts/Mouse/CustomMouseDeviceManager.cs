// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UInput = UnityEngine.Input;

namespace Microsoft.MixedReality.Toolkit.Input.UnityInput
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        (SupportedPlatforms)(-1), // All platforms supported by Unity
        "Custom Mouse Service")]
    public class CustomMouseDeviceManager : BaseInputDeviceManager
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="inputSystemProfile">The input system configuration profile.</param>
        /// <param name="playspace">The <see href="https://docs.unity3d.com/ScriptReference/Transform.html">Transform</see> of the playspace object.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public CustomMouseDeviceManager(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            MixedRealityInputSystemProfile inputSystemProfile,
            Transform playspace,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(registrar, inputSystem, inputSystemProfile, playspace, name, priority, profile) { }

        /// <summary>
        /// Current Mouse Controller.
        /// </summary>
        public MouseController Controller { get; private set; }
        bool enabled = false;
        public bool IsAvailable = false;

        /// <inheritdoc />
        public override void Enable()
        {
            if (!IsAvailable) return;
            if (enabled) return;
            if (!UInput.mousePresent)
            {
                Disable();
                return;
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorWindow.focusedWindow != null)
            {
                UnityEditor.EditorWindow.focusedWindow.ShowNotification(new GUIContent("Press \"ESC\" to regain mouse control"));
            }
#endif
            enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            IMixedRealityInputSource mouseInputSource = null;

            MixedRealityRaycaster.DebugEnabled = true;

            const Handedness handedness = Handedness.Any;
            System.Type controllerType = typeof(MouseController);

            // Make sure that the handedness declared in the controller attribute matches what we expect
            {
                var controllerAttribute = MixedRealityControllerAttribute.Find(controllerType);
                if (controllerAttribute != null)
                {
                    Handedness[] handednesses = controllerAttribute.SupportedHandedness;
                    Debug.Assert(handednesses.Length == 1 && handednesses[0] == Handedness.Any, "Unexpected mouse handedness declared in MixedRealityControllerAttribute");
                }
            }

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;
           
            if (inputSystem != null)
            {
                var pointers = RequestPointers(SupportedControllerType.Mouse, handedness);
                mouseInputSource = inputSystem.RequestNewGenericInputSource("Mouse Input", pointers);
            }
            
            Controller =  new MouseController(TrackingState.NotApplicable, handedness, mouseInputSource);

            if (mouseInputSource != null)
            {
                for (int i = 0; i < mouseInputSource.Pointers.Length; i++)
                {
                    mouseInputSource.Pointers[i].Controller = Controller;
                }
            }

            Controller.SetupConfiguration(typeof(MouseController));
            inputSystem?.RaiseSourceDetected(Controller.InputSource, Controller);
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (UInput.mousePresent && Controller == null) { Enable(); }
            /*
            if (enabled)
            {
                Cursor.visible = !enabled;
                Cursor.lockState = (!Cursor.visible) ? CursorLockMode.Locked : CursorLockMode.None;
            }*/

            Controller?.Update();
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (!IsAvailable) return;
            enabled = false;
            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;
            if (Controller != null)
            {
                inputSystem?.RaiseSourceLost(Controller.InputSource, Controller);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                foreach (var pt in Controller.InputSource.Pointers)
                {
                    pt.BaseCursor = null;
                    var pointer = pt as MousePointer;
                    GameObject.DestroyImmediate(pointer.gameObject);
                }
            }
        }
    }
}
