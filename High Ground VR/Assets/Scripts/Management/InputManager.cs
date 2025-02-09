﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
public enum HandTypes { left, right }; // Handedness
public class InputManager : MonoBehaviour
{
    #region Variables
    private static InputManager s_instance;
    public enum SizeOptions { small, large }; //Whether the player is currently Large or Small.

    [Header("Player Config"), Space(5)]
    public HandTypes Handedness; //The handedness of the player, used to place the book and the laser pointer in the correct hand
    [SerializeField, Tooltip("The height at which the game board should sit relative to the player's height."), Range(0f, 1f)] private float m_playerHeightMultiplier = 0.5f;

    [Header("Input Config"), Space(5)]
    [SerializeField, Tooltip("The GameObject of the entire Rig. Used for scaling and positioning.")] private GameObject m_vrRig;
    [SerializeField, Tooltip("The Gameobject of the players camera.")] private GameObject m_camera;
    [SerializeField, Tooltip("The Gameobject of the players left controller.")] private GameObject m_leftController;
    [SerializeField, Tooltip("The Gameobject of the players right controller.")] private GameObject m_rightController;
    [SerializeField, Tooltip("The Gameobject of the game environment. Used for scaling and positioning.")] private GameObject m_gameEnvironment;
    [SerializeField, Tooltip("GameObject for each of the hand positions. Swapped round when handedness is changed")] private GameObject m_bookHand, m_pointerHand;
    [SerializeField, Tooltip("Pointer Position Object")] private GameObject m_pointerPosition;


    //
    [Header("Point & Click")]
    [SerializeField, Space(10), Tooltip("Material to be used as the Line Renderer")] private Material m_pointerMaterial;
    [SerializeField, Tooltip("Grass Material of the Hexagons. Used when replacing the highlight.")] private Material m_grassMaterial; //Grass Material of the Hex
    [SerializeField, Tooltip("Material to be used when highlighting a Hexagon.")] private Material m_outlineMaterial; //Outline or Highlight Material to apply when hexagon is pointed at.
    [SerializeField, Tooltip("Interaction Manager script. This is used for shooting enemies")] private InteractionManager m_interactionManager;
    private GameObject m_currentlySelectedHex; //The currently pointed at Hexagon. This is used for teleporting and building.

    //
    [Header("Teleportation")]
    [SerializeField, Space(10), Tooltip("Speed at which the player should move to their teleport. Higher is better.")] private float m_rigTeleportationSpeed = 0.8f; //Lerp Speed from posA to posB, 0.8 seems to be the least jarring.
    [SerializeField, Tooltip("Smallest Size the environment can be.")] private Vector3 m_smallestScale = new Vector3(0.6f, 0.6f, 0.6f); //m_environment will be set to this size when the player is large.
    [SerializeField, Tooltip("Largest Size the environment can be.")] private Vector3 m_largestScale = new Vector3(30f, 30f, 30f);//m_environment will be set to this size when the player is small.
    private bool m_teleporterPrimed; //Whether or not the teleporter button is pressed;
    private bool m_enlargePlayer; //Whether the player is being made large by the teleportation
    private Vector3 m_newPosition; //Position at which to move the player to.

    //
    [Header("User Interface,Spells & Building")]
    [SerializeField, Space(10), Tooltip("Prefab of the Book UI Object")] private GameObject m_bookPrefab; //Book UI Object, used for correct handedness placement and spawning buildings;
    [SerializeField, Tooltip("Distance from the hand on which to spawn the book.")] private float m_bookHandOffset = 0.6f; //Float that is added to the hand position so that it floats off the hand slightly.
    private GameObject m_bookObject = null; //Tracks the book Object itself.
    private spellTypes m_currentlySelectedSpell; //The currently selected building option, used in conjuction with the point and click functionality.
    private BuildingOption m_currentlySelectedBuilding = null; //The currently selected building option, used in conjuction with the point and click functionality.

    //
    private float m_maxWorldHeight; //This depends on the player's height, and is calculated on start.    
    private bool m_mainTrigger, m_mainTeleport; //Whether or not the main hand buttons are pressed
    private Vector3 m_mainControllerPos, m_mainControllerPreviousPos; // Position of the controller, used for tracking swiping movements for rotation.
    private LineRenderer m_mainPointer; //The Line Renderer used for pointing 
    private Vector3 m_pointerStartPos;
    private ValidateBuildingLocation m_buildingValidation; //The ValidateBuildingLocation script, which checks the position of a building before it's placed.
    private BarracksBehaviour m_selectedBarracksForUpgrade;
    private List<MeshRenderer> m_objectMeshes = new List<MeshRenderer>(); //List of all of the meshrenderers of the hexagons in the environment.
    private SizeOptions m_currentSize;
    private bool m_bookAttatched = true;
    #endregion

    #region Accessors
    public static InputManager Instance { get => s_instance; set => s_instance = value; }
    public bool LeftGripped { get; set; } //Is the left hand Grip pressed?
    public bool RightGripped { get; set; } //Is the right hand Grip pressed?
    public bool LeftTrigger { get; set; } //Is the left hand Trigger pressed?
    public bool RightTrigger { get; set; } //Is the right hand Trigger pressed?
    public bool LeftTeleport { get; set; } //Is the left hand Teleporter Button pressed?
    public bool RightTeleport { get; set; } //Is the right hand Teleporter Button pressed?
    public GameObject OffHandController { get; set; } //GameObject of the offhand controller
    public GameObject MainController { get; set; } //GameObject of the mainHand controller
    public Vector3 LargestScale { get => m_largestScale; set => m_largestScale = value; }
    public SizeOptions CurrentSize { get => m_currentSize; set => m_currentSize = value; }
    public Vector3 SmallestScale { get => m_smallestScale; set => m_smallestScale = value; }
    public BuildingOption CurrentlySelectedBuilding { get => m_currentlySelectedBuilding; set => m_currentlySelectedBuilding = value; }
    public spellTypes CurrentlySelectedSpell { get => m_currentlySelectedSpell; set => m_currentlySelectedSpell = value; }
    #endregion

    void Start()
    {
        //Handles whether the player is left or right handed.
        if (Handedness == HandTypes.left)
        {
            MainController = m_leftController;
            OffHandController = m_rightController;
        }
        if (Handedness == HandTypes.right)
        {
            MainController = m_rightController;
            OffHandController = m_leftController;
        }

        //Adding a line renderer for the point & click functionality
        m_mainPointer = MainController.AddComponent<LineRenderer>();
        m_mainPointer.enabled = true;
        m_mainPointer.startWidth = 0.05f;
        m_mainPointer.endWidth = 0.00f;
        m_mainPointer.material = m_pointerMaterial;
        m_mainPointer.sortingOrder = 1;

        //Sets the inertiaTensor so that the rotation of the board isn't effected by the amount of collider children within the board.
        //m_gameEnvironment.GetComponent<Rigidbody>().inertiaTensor = new Vector3(0.1f, 0.1f, 0.1f);

        //Current size of the player always starts as Large
        CurrentSize = SizeOptions.large;

        //Ensure the environment is the correct size
        m_gameEnvironment.transform.localScale = m_smallestScale;

        //TeleporterPrimed starts as false
        m_teleporterPrimed = false;

        //Add all of the environment object mesh renderers.
        updateObjectList();

        //Ensures correct hand models based on handedness
        swapHandModels();

        //TryCatch to catch any missing references.
        try { m_buildingValidation = m_gameEnvironment.GetComponent<ValidateBuildingLocation>(); } catch { Debug.LogWarning("Error getting the ValidateBuildingLocation from the gameEnvironment object."); }
        Invoke("updateWorldHeight", 0.01f);
    }
    void Awake()
    {
        //Singleton Implementation
        if (s_instance == null)
        {
            s_instance = this;
        }
        else
        {
            Destroy(s_instance.gameObject);
            s_instance = this;
        }
    }
    void Update()
    {
        //Taking Handedness into consideration.
        if ((LeftTrigger == true && MainController == m_leftController) || (RightTrigger == true && MainController == m_rightController)) { m_mainTrigger = true; }
        else { m_mainTrigger = false; }
        if ((LeftTeleport == true && MainController == m_leftController) || (RightTeleport == true && MainController == m_rightController)) { m_mainTeleport = true; }
        else { m_mainTeleport = false; }

        #region Pointer Handling

        //Raycasting from the controllers
        RaycastHit _hit;

        Debug.DrawRay(m_pointerPosition.transform.position, (MainController.transform.forward - (MainController.transform.up * 0.5f)) * 1000);
      

        //Raycast from the pointer position, out forwards and at a downwards angle. This is a different angle when the player is large.
        if (Physics.Raycast(m_pointerPosition.transform.position, MainController.transform.forward - (MainController.transform.up * 0.5f), out _hit, 1000) && m_currentSize == SizeOptions.large)
        {
            //If it hits an environment Hex, highlight.
            if (_hit.collider.gameObject.tag == "Environment")
            {
                removeHighlight(); //Remove the highlight from all objects.
                m_enlargePlayer = false; //Player is not about to teleport and scale up.

                m_currentlySelectedHex = _hit.collider.gameObject; //Keep track of the currently selected hex.

                MeshRenderer _hitMesh = _hit.collider.gameObject.GetComponent<MeshRenderer>(); //Get the mesh of the _hit object. 
                updateObjectList(_hitMesh);  //Update the list of hexagons to apply the default material to. Exludes the hex the player is pointing at.


                //Show the laser pointer
                m_mainPointer.startWidth = 0.05f;
                m_mainPointer.endWidth = 0.00f;

                //Turn laser colour to blue when you correctly collided with environment.
                m_mainPointer.startColor = Color.blue;
                m_mainPointer.endColor = Color.blue;

                //Apply outline material to the selected object
                Material[] _matArray = _hitMesh.materials;
                List<Material> _matList = new List<Material>();
                _matList = new List<Material>();
                _matList.Add(m_outlineMaterial);
                _hitMesh.materials = _matList.ToArray();

                //If the user is selecting a hex, and they have a building selected, verify it's location and then place the building if it's verified 
                if (m_mainTrigger == true && m_currentlySelectedBuilding != null && m_currentlySelectedHex.GetComponent<NodeComponent>().node.navigability == nodeTypes.navigable)
                {
                    float _angle = headsetToHexAngle();
                    if (m_buildingValidation.verifyBuilding(m_currentlySelectedBuilding, m_currentlySelectedHex.GetComponent<NodeComponent>().node, _angle))
                    {
                        m_buildingValidation.placeBuilding(m_currentlySelectedBuilding, m_currentlySelectedHex.GetComponent<NodeComponent>().node, _angle);
                        RumbleManager.Instance.lightVibration(Handedness);
                    }
                }
                else if (m_mainTrigger == true && m_currentlySelectedHex.GetComponent<NodeComponent>().node.navigability == nodeTypes.barracks)
                {
                    BarracksBehaviour _selectedBarracks = m_currentlySelectedHex.GetComponentInChildren<BarracksBehaviour>();
                    if (_selectedBarracks.isShowingUpgrade)
                    {
                        if (_selectedBarracks.canUpgradeBarracks())
                        {
                            _selectedBarracks.runUpgrade();
                            _selectedBarracks.hideUpgradeDisplay();
                            _selectedBarracks.isShowingUpgrade = false;
                        }
                    }
                    else
                    {
                        if (_selectedBarracks.canUpgradeBarracks())
                        {
                            m_selectedBarracksForUpgrade = _selectedBarracks;
                            _selectedBarracks.showUpgradeDisplay();
                            _selectedBarracks.isShowingUpgrade = true;
                        }
                    }
                }
            }

            if(_hit.collider.gameObject.tag == "MenuCanvas")
            {
                //Show the laser
                m_mainPointer.startWidth = 0.05f;
                m_mainPointer.endWidth = 0.00f;

                //Turn laser colour to blue when you correctly collided with environment.
                m_mainPointer.startColor = Color.blue;
                m_mainPointer.endColor = Color.blue;


                //Update the cursor position
                if(_hit.collider.gameObject.TryGetComponent(out UIPointerBehaviour _pointer))
                {
                    _pointer.updateCursorPos(_hit.point);
                    if(m_mainTrigger == true)
                    {
                        _pointer.isClicked = true;
                    }
                    else
                    {
                        _pointer.isClicked = false;
                    }
                }
            }

            //Update the laser position so it continues to update.
            m_mainPointer.SetPosition(0, m_pointerPosition.transform.position);
            m_mainPointer.SetPosition(1, _hit.point);
        }
        else if (Physics.Raycast(m_pointerPosition.transform.position, MainController.transform.forward, out _hit, 1000) && m_currentSize == SizeOptions.small)
        {
            //If it hits an environment Hex, highlight.
            if (_hit.collider.gameObject.tag == "Environment")
            {
                m_enlargePlayer = false;
                MeshRenderer _hitMesh = _hit.collider.gameObject.GetComponent<MeshRenderer>();
                m_currentlySelectedHex = _hit.collider.gameObject;
                //Removes highlight from all objects not in the m_objectMeshes list
                updateObjectList(_hitMesh);


                //Show the laser
                if (m_currentSize == SizeOptions.large)
                {
                    m_mainPointer.startWidth = 0.05f;
                    m_mainPointer.endWidth = 0.00f;
                }
                else
                {
                    m_mainPointer.startWidth = 0.003f;
                    m_mainPointer.endWidth = 0.00f;
                }


                //Turn laser colour to blue when you correctly collided with environment.
                m_mainPointer.startColor = Color.blue;
                m_mainPointer.endColor = Color.blue;

                //Apply outline material to the selected object
                Material[] _matArray = _hitMesh.materials;
                List<Material> _matList = new List<Material>();
                _matList = new List<Material>();
                _matList.Add(_matArray[0]);
                _matList.Add(m_outlineMaterial);
                _hitMesh.materials = _matList.ToArray();

                //If the user is selecting a hex, and they have a building selected, verify it's location and then place the building if it's verified 
                if (m_mainTrigger == true && m_currentlySelectedBuilding != null && m_currentSize == SizeOptions.large)
                {
                    float _angle = headsetToHexAngle();
                    if (m_buildingValidation.verifyBuilding(m_currentlySelectedBuilding, m_currentlySelectedHex.GetComponent<NodeComponent>().node, _angle))
                    {
                        m_buildingValidation.placeBuilding(m_currentlySelectedBuilding, m_currentlySelectedHex.GetComponent<NodeComponent>().node, _angle);
                    }
                }
            }

            //Update the laser position so it continues to update.
            m_mainPointer.SetPosition(0, m_pointerPosition.transform.position);
            m_mainPointer.SetPosition(1, _hit.point);
        }
        else
        {

            m_currentlySelectedHex = null;
            m_enlargePlayer = true;
            if (m_currentSize == SizeOptions.large)
            {
                m_mainPointer.startWidth = 0;
                m_mainPointer.endWidth = 0;
            }
            else
            {
                m_mainPointer.startWidth = 0.003f;
                m_mainPointer.endWidth = 0;
                m_mainPointer.SetPosition(0, m_pointerStartPos);
                m_mainPointer.SetPosition(1, m_pointerStartPos + MainController.transform.forward * 20f);

            }

            m_mainPointer.startColor = Color.red;
            m_mainPointer.endColor = Color.red;

            ////Removes highlight from all objects
            updateObjectList();

        }

        //Removes the highlight from all objects
        removeHighlight();

        //Clicking on an node, will be used alongside the UI to place buildings
        if ((LeftTrigger == true || RightTrigger == true) && m_currentlySelectedHex != null)
        {
            //Debug.Log(m_currentlySelectedHex);
            LeftTrigger = false;
            RightTrigger = false;
        }

        #endregion

        #region Enemy Interaction Manager
        if (m_mainTrigger == true && m_currentlySelectedBuilding == null)
        {
            m_interactionManager.castSpell(CurrentlySelectedSpell);
        }
        #endregion

        #region Teleporting
        //Teleporting down to tiny size
        if (m_mainTeleport == true && m_teleporterPrimed == false)
        {
            //Debug.Log("Teleporter Primed");
            m_teleporterPrimed = true;
        }


        //Teleport onto the game board
        if (m_teleporterPrimed == true && m_mainTeleport == false && m_currentlySelectedHex != null && m_enlargePlayer == false)
        {
            //If the node is able to be teleported onto, teleport the player.
            if (m_currentlySelectedHex.GetComponent<NodeComponent>().node.navigability == nodeTypes.navigable)
            {
                //Debug.Log("Teleported");
                m_teleporterPrimed = false;

                m_vrRig.transform.localScale = m_smallestScale;
                m_newPosition = new Vector3(m_currentlySelectedHex.transform.position.x, m_currentlySelectedHex.transform.position.y + m_buildingValidation.CurrentHeightOffset, m_currentlySelectedHex.transform.position.z);

                m_currentSize = SizeOptions.small;
                m_mainPointer.startWidth = 0.003f;
                m_mainPointer.endWidth = 0.00f;
                if (m_bookAttatched == false)
                {
                    toggleBookAttatched();
                }
                m_pointerHand.transform.localRotation = Quaternion.Euler(0, 0, 0); // Point the hand back up to match the laser.
            }
        }

        //Teleport off the game board.
        if (m_teleporterPrimed == true && m_mainTeleport == false && m_currentlySelectedHex == null && m_enlargePlayer == true)
        {
            //Debug.Log("Teleported");
            m_teleporterPrimed = false;
            if (m_bookAttatched == false)
            {
                toggleBookAttatched();
            }
            m_pointerHand.transform.localRotation = Quaternion.Euler(36, 0, 0);// Point the hand down to match the laser.
            m_newPosition = new Vector3(0, 0, 0);
            m_vrRig.transform.localScale = m_largestScale;

            //m_gameEnvironment.transform.localScale = m_smallestScale;
            //m_gameEnvironment.transform.position = new Vector3(0, m_maxWorldHeight, 0);


            m_currentSize = SizeOptions.large;
            m_mainPointer.startWidth = 0.05f;
            m_mainPointer.endWidth = 0.00f;
        }
        #endregion

        //Lerp the Rig to it's new position - Teleportation
        m_vrRig.transform.position = Vector3.Lerp(m_vrRig.transform.position, m_newPosition, m_rigTeleportationSpeed);

        if((LeftGripped && Handedness == HandTypes.right) || (RightGripped && Handedness == HandTypes.left))
        {
            toggleBookAttatched();
        }
        else if (RightGripped == true && LeftGripped == true)
        {
            updateWorldHeight();
        }

        LeftTeleport = false;
        RightTeleport = false;
        LeftTrigger = false;
        RightTrigger = false;
        RightGripped = false;
        LeftGripped = false;
    }

    /// <summary>
    /// Updates the m_maxWorldHeight based on the current Y position of the headset. This adjusts the game world to work with any height.
    /// </summary>
    /// 
    [ContextMenu("Update Game Board Height")] //Allows me to press a button within the editor to execute this method.
    public void updateWorldHeight()
    {
        m_maxWorldHeight = m_camera.transform.position.y * m_playerHeightMultiplier;
        m_gameEnvironment.transform.position = new Vector3(0, m_maxWorldHeight, 0);
        for (int i = 0; i < m_gameEnvironment.transform.childCount; i++)
        {
            Transform _targetHex = m_gameEnvironment.transform.GetChild(i).transform;
            _targetHex.position = new Vector3(_targetHex.position.x, m_maxWorldHeight, _targetHex.position.z);
        }
        m_gameEnvironment.transform.position = new Vector3(0, m_maxWorldHeight, 16);
    }

    #region Hexagon Highlighting
    /// <summary>
    /// Updates the list of Hexagon Meshes, without the passed in mesh. Used for highlighting
    /// </summary>
    /// <param name="_selectedMesh">The Mesh to not have in the list</param>
    private void updateObjectList(MeshRenderer _selectedMesh)
    {
        if(GameManager.Instance.GameStarted == true)
        {
            m_objectMeshes = new List<MeshRenderer>();
            //Add all of the environment object mesh renderers.
            int _childCount = m_gameEnvironment.transform.GetChild(0).transform.childCount;
            for (int i = 0; i < _childCount; i++)
            {
                if (m_gameEnvironment.transform.GetChild(0).transform.GetChild(i).tag == "Environment")
                {
                    MeshRenderer _mesh = m_gameEnvironment.transform.GetChild(0).transform.GetChild(i).GetComponent<MeshRenderer>();
                    if (_mesh != _selectedMesh)
                    {
                        m_objectMeshes.Add(_mesh);
                    }
                }
            }

            hideBarracksUpgradeDisplays();
        }
    }

    /// <summary>
    /// Updates the list of Hexagon meshes to include all of them. Used for highlighting
    /// </summary>
    private void updateObjectList()
    {
        if (GameManager.Instance.GameStarted == true)
        {
            m_objectMeshes = new List<MeshRenderer>();
            //Add all of the environment object mesh renderers.
            int _childCount = m_gameEnvironment.transform.GetChild(0).transform.childCount;
            for (int i = 0; i < _childCount; i++)
            {
                if (m_gameEnvironment.transform.GetChild(0).transform.GetChild(i).tag == "Environment")
                {
                    MeshRenderer _mesh = m_gameEnvironment.transform.GetChild(0).transform.GetChild(i).GetComponent<MeshRenderer>();
                    m_objectMeshes.Add(_mesh);
                }
            }

            hideBarracksUpgradeDisplays();
        }
    }

    /// <summary>
    /// Sets the material of all meshes in m_objectMeshes to m_grassMaterial. Used for highlighting.
    /// </summary>
    private void removeHighlight()
    {
        foreach (MeshRenderer _mesh in m_objectMeshes)
        {
            if(_mesh != null)
            {
                List<Material> _matList = new List<Material>();
                _matList.Add(m_grassMaterial);
                _mesh.materials = _matList.ToArray();
            }
        }
    }

    /// <summary>
    /// Hides all of the upgrade displays.
    /// </summary>
    private void hideBarracksUpgradeDisplays()
    {
        //Turn off the displays for all the barracks
        BarracksBehaviour _selectedBarracks = null;
        try
        {
            _selectedBarracks = m_currentlySelectedHex.GetComponentInChildren<BarracksBehaviour>();
        }
        catch { }

        foreach (BarracksBehaviour _barracks in FindObjectsOfType<BarracksBehaviour>())
        {
            if (_selectedBarracks != null)
            {
                if (_barracks != _selectedBarracks && _barracks.isShowingUpgrade == true)
                {
                    _barracks.hideUpgradeDisplay();
                    _barracks.isShowingUpgrade = false;
                }
            }
            else if (_barracks.isShowingUpgrade == true)
            {
                _barracks.hideUpgradeDisplay();
                _barracks.isShowingUpgrade = false;
            }
        }
    }

    #endregion

    #region Book Spawning + Detatching

    /// <summary>
    /// Spawns the book in the players hand
    /// </summary>
    public void SpawnBook()
    {
        if (m_bookObject == null)
        {
            m_bookObject = Instantiate(m_bookPrefab, OffHandController.transform);
            if (Handedness == HandTypes.left)
            {
                //Book in the right hand.
                m_bookObject.transform.localEulerAngles = new Vector3(0, 90, 0);
                m_bookObject.transform.localPosition = new Vector3(-m_bookHandOffset, 0, 0.07f);
            }
            else
            {
                //Book in the left hand
                m_bookObject.transform.localEulerAngles = new Vector3(0, -90, 0);
                m_bookObject.transform.localPosition = new Vector3(m_bookHandOffset, 0, 0.07f);
            }
            m_bookObject.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            GameManager.Instance.moneyBookText = m_bookObject.GetComponent<BookManager>().moneyText;
            GameManager.Instance.timerBookText = m_bookObject.GetComponent<BookManager>().timerText;
            GameManager.Instance.roundBookText = m_bookObject.GetComponent<BookManager>().roundText;
            GameManager.Instance.moneyBookText2 = m_bookObject.GetComponent<BookManager>().moneyText2;
            GameManager.Instance.timerBookText2 = m_bookObject.GetComponent<BookManager>().timerText2;
            GameManager.Instance.roundBookText2 = m_bookObject.GetComponent<BookManager>().roundText2;
        }
    }

    /// <summary>
    /// Removes the book from the player's hand.
    /// </summary>
    public void RemoveBook()
    {
        Destroy(m_bookObject);
    }

    /// <summary>
    /// Removes the book from being attatched to the player's hand.
    /// </summary>
    public void toggleBookAttatched()
    {
        if(m_bookAttatched == true)
        {
            m_bookAttatched = false;
            m_bookObject.transform.SetParent(null);
        }
        else
        {
            m_bookAttatched = true;
            m_bookObject.transform.SetParent(OffHandController.transform);
            if (Handedness == HandTypes.left)
            {
                //Book in the right hand.
                m_bookObject.transform.localEulerAngles = new Vector3(0, 90, 0);
                m_bookObject.transform.localPosition = new Vector3(-m_bookHandOffset, 0, 0.07f);
            }
            else
            {
                //Book in the left hand
                m_bookObject.transform.localEulerAngles = new Vector3(0, -90, 0);
                m_bookObject.transform.localPosition = new Vector3(m_bookHandOffset, 0, 0.07f);
            }
            m_bookObject.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
        } 
    }
    #endregion

    /// <summary>
    /// Retrieve the angle of the headset to the selected hex.
    /// </summary>
    /// <returns>The angle as a float in quaternions, ready for building placement.</returns>
    private float headsetToHexAngle()
    {
        //Retrieve the angle of the headset to the chosen hex, using an equal Y so it's on the same plane.
        Vector3 _hexagonPos = m_currentlySelectedHex.transform.position;
        Vector3 _headsetPos = new Vector3(m_camera.transform.position.x, m_currentlySelectedHex.transform.position.y, m_camera.transform.position.z);

        Vector3 _difference = _headsetPos - _hexagonPos;


        float _rawAngle = Mathf.Atan2(_difference.z, _difference.x) * Mathf.Rad2Deg - 90;

        //float _angle = (Mathf.Floor(_rawAngle / 60.0f) * 60.0f) + 30.0f;

        return -_rawAngle;
    }

    /// <summary>
    /// Makes sure the pointer and book hand models are in the correct places.
    /// </summary>
    public void swapHandModels()
    {
        //Handles whether the player is left or right handed.
        if (Handedness == HandTypes.left)
        {
            MainController = m_leftController;
            OffHandController = m_rightController;
            m_pointerHand.transform.localScale = new Vector3(-1, 1, 1);
            m_bookHand.transform.localScale = new Vector3(-1, 1, 1);
        }
        if (Handedness == HandTypes.right)
        {
            MainController = m_rightController;
            OffHandController = m_leftController;
            m_pointerHand.transform.localScale = new Vector3(1, 1, 1);
            m_bookHand.transform.localScale = new Vector3(1, 1, 1);
        }



        m_pointerHand.transform.SetParent(MainController.transform);
        m_bookHand.transform.SetParent(OffHandController.transform);

        m_pointerHand.transform.localPosition = Vector3.zero;
        m_pointerHand.transform.localRotation = Quaternion.Euler(36.0f, 0, 0);

        m_bookHand.transform.localPosition = Vector3.zero;
        m_bookHand.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }
    

}
