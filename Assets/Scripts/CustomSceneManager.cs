using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class CustomSceneManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;

    [SerializeField]
    private Dropdown dropdown;

    [SerializeField]
    private Camera arCamera; //Used for Raycasting

    [SerializeField]
    // Button For product Button Page
    private Button button;

    [SerializeField]
    private TextMeshProUGUI text;

    [SerializeField]
    private Button dragButton;

    [SerializeField]
    private TextMeshProUGUI loadingText;

    // List of Objects for DropDown.
    public List<string> consoleObjects = new List<string>();

    private Vector3 rotationSpeed = Vector3.zero;
    // Instance of Objects in Dropdown
    private CustomBehaviour[] products;

    private bool objectLoadingComplete = false;

    private bool dropDownVisible = false;

    // Current Index of Dropdown
    private int current = -1;

    private bool dragging = false;

    // Default URL 
    private string DefaultURL = "https://www.echoar.xyz/";

    // URL for the product.
    private string objectURL;

    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    private bool isRotating = false;
    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject spawnedObject { get; private set; }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();

        // Attach Listner to dopdown and buttons
        dropdown.onValueChanged.AddListener(delegate {
            updateCurrent(dropdown);  
        });
        button.onClick.AddListener(OpenButtonURL);
        dragButton.onClick.AddListener(ToggleDraging);

        dropdown.ClearOptions();
        consoleObjects.Add("Please Choose Outfit");
    }

    void Start() {

        // Deactivate all UI Components before initiating Objects
        dropdown.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
        dragButton.gameObject.SetActive(false);
        loadingText.text = "Not complete";
    }

    // Check if touch is over UI
    bool isOverUI(Vector2 touchPosition)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
        PointerEventData eventPosition = new PointerEventData(EventSystem.current);
        eventPosition.position = new Vector2(touchPosition.x, touchPosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventPosition, results);

        return results.Count > 0;
    }


    // Check if al 3D models are completely loaded
    void checkObjectLoading(){
        if(objectLoadingComplete) return;
        foreach (CustomBehaviour customObject in products){
            if(!customObject.completelyLoaded) return;
        }
        objectLoadingComplete = true;

        // If all 3D models are loaded set Dropdown active
        dropdown.gameObject.SetActive(true);
        loadingText.gameObject.SetActive(false);
        dropdown.AddOptions(consoleObjects);
        dropdown.value = 0;
    }

    /// Populate Dropdown Options
    void PopulateDropDownOptions(){
         
        if (consoleObjects.Count <= 1)
        {   button.gameObject.SetActive(false);
            // Find objects of type custombehaviour
            products  = FindObjectsOfType<CustomBehaviour>();
            if(products == null || products.Length == 0) return;
            foreach (CustomBehaviour customObject in products){
                consoleObjects.Add(customObject.name);
            }
        }
        checkObjectLoading();

    }

    void Update()
    {   
        if(Input.touchCount > 1 && current > 0){
            if (spawnedObject != null) {
                // Store both touches.
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);
                // Calculate previous position
                Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
                // Find the magnitude of the vector (the distance) between the touches in each frame.
                float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
                // Find the difference in the distances between each frame.
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                float pinchAmount = deltaMagnitudeDiff * 0.02f * Time.deltaTime;
                spawnedObject.transform.localScale -= new Vector3(pinchAmount, pinchAmount, pinchAmount);
                // Clamp scale
                float Min = 0.005f;
                float Max = 3f;
                spawnedObject.transform.localScale = new Vector3(
                    Mathf.Clamp(spawnedObject.transform.localScale.x, Min, Max),
                    Mathf.Clamp(spawnedObject.transform.localScale.y, Min, Max),
                    Mathf.Clamp(spawnedObject.transform.localScale.z, Min, Max)
                );
            }
        }else if(Input.touchCount == 1) {
            Touch touch = Input.GetTouch(0);

            if(isOverUI(touch.position)) return;

            if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = s_Hits[0].pose;

                if (spawnedObject == null)
                {
                    spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
                    loadingText.text = "Loading...";
                    loadingText.color = new Color(loadingText.color.r, loadingText.color.g, loadingText.color.b, Mathf.PingPong(Time.time, 1));
                }
            }
         
            if(touch.phase == TouchPhase.Moved && current > 0){

                // If fingers are moving check if user is dragging or rotating.
                if(dragging){

                    // If dragging transform position of object to touch position.
                    m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon);
                    var hitPose = s_Hits[0].pose;
                    spawnedObject.transform.position = hitPose.position;
                }else {

                    // If Swiping rotate the object
                    float x = -touch.deltaPosition.x;
                    if(x < 0){
                        products[current-1].gameObject.transform.localRotation *= Quaternion.AngleAxis(-Time.deltaTime * 150, Vector3.up); 
                    }else {
                        products[current-1].gameObject.transform.localRotation *= Quaternion.AngleAxis(Time.deltaTime * 150, Vector3.up); 
                    }
                }
                
            }
        }

        // If object is spawned but Dropdown is empty populate dropdown option
        if(spawnedObject != null && consoleObjects.Count <= 1){
            PopulateDropDownOptions();
        }else if(spawnedObject != null && consoleObjects.Count > 1) {
            // If object is spawned and dropdown is populated check if all 3D-Models are rendered.
            checkObjectLoading();
        }
    }

    public void updateCurrent(Dropdown dropdown){

        // If value haven't changed return
        if(dropdown.value == current) return;

        // If it was not a default option change current active model to false;
        if(current >= 1){
            products[current-1].active = false;
        }

        current = dropdown.value;

        if(current <= 0){
            // If no product is selected deactivate button.
            button.gameObject.SetActive(false);
            dragButton.gameObject.SetActive(false);
            dragging = false;
            objectURL = DefaultURL;
        }else{

            // Set current product active
            products[current-1].active = true;

            // Set button active
            button.gameObject.SetActive(true);
            dragButton.gameObject.SetActive(true);
            if(string.IsNullOrEmpty(products[current-1].productURL)){
                // If product URL is empty make button unclickable and change text.
                text.text = "Item Not Available";
                button.interactable = false;
            }else {
                // If URL then change current button URL and make button clickable.
                text.text = "Buy Now For $" + products[current-1].priceOfProduct;
                objectURL = products[current-1].productURL;
                button.interactable = true;
            }
        }
    }

    // Open URL Of button
    public void OpenButtonURL(){
        Application.OpenURL(objectURL);
    }


    // Toggle Draggin
    private void ToggleDraging(){
        if(dragging){
            // If not dragging change color to White
            dragging = false;
            dragButton.gameObject.GetComponent<Image>().color = Color.white;
        }else {

            // If dragging change color to Red
            dragging = true;
            dragButton.gameObject.GetComponent<Image>().color = Color.red;
        }
    }

   

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    ARRaycastManager m_RaycastManager;
}