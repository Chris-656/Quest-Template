
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;


using UnityEngine.InputSystem;


public class XRManager : MonoBehaviour
{
    // Typisierte Actions-Klasse (wird von Unity generiert)
    private InputSystem_Actions controls;
    //public float heightSpeed = 1.0f; // Geschwindigkeit der Höhenänderung (z.B. per Stick)
    public float heightButtonSpeed = 0.2f; // Geschwindigkeit für Taste X/Y (langsamer)
    public float rotationSpeed = 60f; // Rotationsgeschwindigkeit in Grad pro Sekunde
    public float panSpeed = 1.0f; // Geschwindigkeit für Panning
    public GameObject objectToFollow; 
    public List<GameObject> models = new List<GameObject>();
    private int currentModelIndex = -1;
    private bool prevPrimaryButton = false;
    private bool prevSecondaryButton = false;
    private XROrigin xrOrigin;
    private float targetHeight;
    private float targetYRotation;
    private Vector3 targetPan;

    void Start()
    {
        // Typisierte InputActions-Instanz erzeugen und Events abonnieren
        controls = new InputSystem_Actions();
        controls.XR.PrevModel.performed += ctx => PrevModel();
        controls.XR.NextModel.performed += ctx => NextModel();
        controls.Enable();

        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin != null)
        {
            var pos = xrOrigin.CameraFloorOffsetObject.transform.localPosition;
            targetHeight = pos.y;
            targetYRotation = xrOrigin.CameraFloorOffsetObject.transform.localEulerAngles.y;
            targetPan = new Vector3(pos.x, 0f, pos.z);
        }

        // Modelle aus dem GameObject "Models" im Scene-Hierarchiebaum befüllen
        GameObject modelsParent = GameObject.Find("Models");
        if (modelsParent != null)
        {
            models.Clear();
            foreach (Transform child in modelsParent.transform)
            {
                models.Add(child.gameObject);
            }
            Debug.Log($"[XROriginHeightController] Models gefunden: {models.Count}");
            if (models.Count > 0)
            {
                currentModelIndex = 0;
                ShowOnlyModel(0);
            }
        }
        else
        {
            Debug.LogWarning("[XROriginHeightController] Kein GameObject 'Models' gefunden!");
        }
    }

    void Update()
    {
        // Rechter Thumbstick: Nur links/rechts für Drehung um Y-Achse
        UnityEngine.XR.InputDevice rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 rightThumbstick))
        {
            if (objectToFollow != null)
            {
                float rotY = 0f;
                if (Mathf.Abs(rightThumbstick.x) > 0.1f)
                {
                    rotY = -rightThumbstick.x * rotationSpeed * Time.deltaTime;
                }
                // Nur um Y-Achse drehen
                objectToFollow.transform.Rotate(0f, rotY, 0f, Space.Self);
            }
        }

        // Linker Thumbstick: Objekt im Raum bewegen (X/Z)
        if (objectToFollow != null)
        {
            UnityEngine.XR.InputDevice leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 leftThumbstick))
            {
                if (leftThumbstick.magnitude > 0.1f)
                {
                    Vector3 move = new Vector3(leftThumbstick.x, 0, leftThumbstick.y);
                    move = xrOrigin.Camera.transform.TransformDirection(move);
                    move.y = 0; // Keine Höhenänderung durch Stick
                    objectToFollow.transform.position += move * panSpeed * Time.deltaTime;
                }
            }
        }


        // Keine Positions- oder Höhenänderung mehr, Tracking bleibt vollständig erhalten

        // Rotation direkt setzen
        xrOrigin.CameraFloorOffsetObject.transform.localRotation = Quaternion.Euler(0f, targetYRotation, 0f);


        // ...Model-Cycling entfernt, nur noch Höhenänderung über X/Y-Button...
    }

    private void ShowOnlyModel(int index)
    {
        for (int i = 0; i < models.Count; i++)
        {
            if (models[i] != null)
                models[i].SetActive(i == index);
        }
    }

    private void ShowAllModels()
    {
        for (int i = 0; i < models.Count; i++)
        {
            if (models[i] != null)
                models[i].SetActive(true);
        }
    }

    private void NextModel()
    {
        currentModelIndex++;
        if (currentModelIndex >= models.Count)
        {
            ShowAllModels();
            currentModelIndex = -1;
        }
        else
        {
            ShowOnlyModel(currentModelIndex);
        }
    }

    private void PrevModel()
    {
        if (currentModelIndex == -1)
        {
            currentModelIndex = models.Count - 1;
            ShowOnlyModel(currentModelIndex);
        }
        else
        {
            currentModelIndex--;
            if (currentModelIndex < 0)
            {
                ShowAllModels();
                currentModelIndex = -1;
            }
            else
            {
                ShowOnlyModel(currentModelIndex);
            }
        }
    }
}
