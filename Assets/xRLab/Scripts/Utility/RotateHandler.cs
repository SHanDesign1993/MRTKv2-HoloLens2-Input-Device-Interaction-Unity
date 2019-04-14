using UnityEngine;
namespace Microsoft.MixedReality.Toolkit.SDK.UX
{
    internal class RotateHandler : MonoBehaviour
    {
        private Vector3 rotationFromPositionScale = new Vector3(-300.0f, -300.0f, -300.0f);
        [SerializeField]
        private RotationType rotationType;

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

        RotationAxisEnum GetAxisbyIndex(int index)
        {
            if (index == 0 || index == 2 || index == 4 || index == 6)
                return RotationAxisEnum.X;
            else if (index == 1 || index == 3 || index == 5 || index == 7)
                return RotationAxisEnum.Y;
            else
                return RotationAxisEnum.Z;
        }

        public void ApplyRotationContinuous(int handleIndex, Quaternion initialRotation, Vector3 handleInitialPos, Vector3 handleCurrentPos)
        {
            RotationAxisEnum Axis = GetAxisbyIndex(handleIndex);

            Vector3 initialRay = handleInitialPos - this.transform.position;
            initialRay.Normalize();

            Vector3 currentRay = handleCurrentPos - this.transform.position;
            currentRay.Normalize();

            Vector3 delta = currentRay - initialRay;
            delta.Scale(rotationFromPositionScale);

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
                newEulers = new Vector3(0, 0, delta.y);
            }

            if (rotationType == RotationType.GlobalBased)
            {
                newEulers += initialRotation.eulerAngles;
                this.transform.rotation = Quaternion.Euler(newEulers);
            }
            else if (rotationType == RotationType.ObjectBased)
            {
                Vector3 axis = (Axis == RotationAxisEnum.X ? new Vector3(1, 0, 0) : Axis == RotationAxisEnum.Y ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1));
                this.transform.localRotation = initialRotation;
                float angle = newEulers.x != 0 ? newEulers.x : newEulers.y != 0 ? newEulers.y : newEulers.z;
                this.transform.Rotate(axis, angle * 2.0f);
            }
        }

    }
}

