using UnityEngine;
using System.Collections.Generic;
using EFT.UI;
using EFT;
using SharpDX.XInput;
using HarmonyLib;
using UnityEngine.UI;
using System;
using System.Reflection;
using EFT.InputSystem;
using System.Threading.Tasks;
using EFT.UI.DragAndDrop;
using UnityEngine.EventSystems;
using Comfort.Common;
using EFT.Communications;
using EFT.InventoryLogic;
using Diz.Binding;
using Aki.Common.Utils;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;
using TMPro;
using EFT.Interactive;
using System.Net;
using UnityEngine.Rendering;

namespace AmandsController
{
    public class AmandsControllerClass : MonoBehaviour
    {
        // AmandsController
        public LocalPlayer localPlayer;
        public InputTree inputTree;
        public Player.FirearmController firearmController;
        public bool InRaid = false;

        // SharpDX
        public Controller controller;
        public Gamepad gamepad;
        public bool connected = false;
        public float maxValue = short.MaxValue;

        // Inputs
        public Vector2 LS, RS = Vector2.zero;
        public float LSXYSqrt, RSXYSqrt;

        public float LeftTrigger, RightTrigger;

        public bool LSINPUT = false;
        public bool LSUP = false;
        public bool LSDOWN = false;
        public bool LSLEFT = false;
        public bool LSRIGHT = false;

        public bool RSINPUT = false;
        public bool RSUP = false;
        public bool RSDOWN = false;
        public bool RSLEFT = false;
        public bool RSRIGHT = false;

        public bool A = false;
        public bool B = false;
        public bool X = false;
        public bool Y = false;

        public bool LB = false;
        public bool RB = false;

        public bool RT = false;
        public bool LT = false;

        public bool R = false;
        public bool L = false;

        public bool UP = false;
        public bool DOWN = false;
        public bool LEFT = false;
        public bool RIGHT = false;

        public bool BACK = false;
        public bool MENU = false;

        // Custom Inputs
        public bool SlowLeanLeft;
        public bool SlowLeanRight;

        // Input Sets
        public bool isAiming = false;

        public bool LB_RB = false;
        public bool Interface_LB_RB = false;

        public bool Interface = false;
        public bool ContextMenu = false;

        // EFT Methods
        private MethodInfo TranslateInput;
        private object[] TranslateInputInvokeParameters = new object[3] { new List<ECommand>(), null, ECursorResult.Ignore };
        private MethodInfo ButtonPress;

        // Movement
        private object MovementContextObject;
        private Type MovementContextType;
        private MethodInfo SetCharacterMovementSpeed;
        private object[] MovementInvokeParameters = new object[2] { 0.0, false };

        private bool resetCharacterMovementSpeed = false;
        private float CharacterMovementSpeed = 0f;
        private float StateSpeedLimit = 0f;
        private float MaxSpeed = 0f;

        public Slider speedSlider;

        // Aim
        public Vector2 Aim = Vector2.zero;
        private Vector2 InvertY = new Vector2(100f, -100f);
        private AnimationCurve AimAnimationCurve = new AnimationCurve();
        private Keyframe[] AimKeys = new Keyframe[3] { new Keyframe(0f, 0f), new Keyframe(0.75f, 0.5f, 0.75f, 0.5f), new Keyframe(1f, 1f), };

        // Aim Assist
        private Collider[] colliders;
        public int colliderCount;
        public LayerMask AimAssistLayerMask = LayerMask.GetMask("Player");

        public Dictionary<LocalPlayer, float> AimAssistPlayers = new Dictionary<LocalPlayer, float>();
        private RaycastHit hit;
        private LayerMask HighLayerMask = LayerMask.GetMask("Terrain", "HighPolyCollider");

        private Vector2 ScreenSize = new Vector2(Screen.width, Screen.height);
        private Vector2 ScreenSizeRatioMultiplier = new Vector2(1f, Screen.height / Screen.width);

        private Vector3 AimPosition;
        private Vector3 AimDirection;

        private bool Magnetism;
        private float Stickiness;
        private float StickinessSmooth;
        private Vector2 AutoAim = Vector2.zero;
        private Vector2 AutoAimSmooth = Vector2.zero;

        private float AimAssistAngle;
        private float AimAssistBoneAngle;

        private LocalPlayer AimAssistLocalPlayer = null;
        private LocalPlayer HitAimAssistLocalPlayer = null;
        private Vector2 AimAssistTarget2DPoint = Vector2.zero;
        private Vector2 AimAssistScreenLocalPosition = Vector2.zero;

        public SSAA currentSSAA;
        public float SSAARatio;

        // Binds
        public ControllerPresetJsonClass controllerPresetJsonClass;

        Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> AmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>();
        List<string> ActiveAmandsControllerSets = new List<string>();

        Dictionary<EAmandsControllerButton, AmandsControllerButtonSnapshot> AmandsControllerButtonSnapshots = new Dictionary<EAmandsControllerButton, AmandsControllerButtonSnapshot>();
        Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>> AmandsControllerButtonBinds = new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>();

        List<string> AsyncPress = new List<string>();
        List<string> AsyncHold = new List<string>();

        AmandsControllerButtonBind EmptyBind = new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, -100);

        // UI
        public InventoryScreen inventoryScreen;

        public List<GridView> gridViews = new List<GridView>();
        public GridView SimpleStashGridView;
        public TradingTableGridView tradingTableGridView;
        public List<ContainedGridsView> containedGridsViews = new List<ContainedGridsView>();
        public List<ItemSpecificationPanel> itemSpecificationPanels = new List<ItemSpecificationPanel>();

        public List<SlotView> equipmentSlotViews = new List<SlotView>();
        public List<SlotView> weaponsSlotViews = new List<SlotView>();
        public SlotView armbandSlotView;
        public List<SlotView> containersSlotViews = new List<SlotView>();
        public List<SlotView> lootEquipmentSlotViews = new List<SlotView>();
        public List<SlotView> lootWeaponsSlotViews = new List<SlotView>();
        public SlotView lootArmbandSlotView;
        public List<SlotView> lootContainersSlotViews = new List<SlotView>();
        public SlotView dogtagSlotView;
        public List<SlotView> specialSlotSlotViews = new List<SlotView>();

        public List<SearchButton> searchButtons = new List<SearchButton>();
        public Image SearchButtonImage;

        public List<ContextMenuButton> contextMenuButtons = new List<ContextMenuButton>();

        public SplitDialog splitDialog;

        public List<ScrollRectNoDrag> scrollRectNoDrags = new List<ScrollRectNoDrag>();

        public GridView currentGridView;
        public ModSlotView currentModSlotView;
        public TradingTableGridView currentTradingTableGridView;
        public ContainedGridsView currentContainedGridsView;
        public ItemSpecificationPanel currentItemSpecificationPanel;
        public SlotView currentEquipmentSlotView;
        public SlotView currentWeaponsSlotView;
        public SlotView currentArmbandSlotView;
        public SlotView currentContainersSlotView;
        public SlotView currentDogtagSlotView;
        public SlotView currentSpecialSlotSlotView;
        public SearchButton currentSearchButton;
        public ContextMenuButton currentContextMenuButton;
        public ScrollRectNoDrag currentScrollRectNoDrag;
        public RectTransform currentScrollRectNoDragRectTransform;

        private GridView snapshotGridView;
        private ModSlotView snapshotModSlotView;
        private TradingTableGridView snapshotTradingTableGridView;
        private ContainedGridsView snapshotContainedGridsView;
        private ItemSpecificationPanel snapshotItemSpecificationPanel;
        private SlotView snapshotEquipmentSlotView;
        private SlotView snapshotWeaponsSlotView;
        private SlotView snapshotArmbandSlotView;
        private SlotView snapshotContainersSlotView;
        private SlotView snapshotDogtagSlotView;
        private SlotView snapshotSpecialSlotSlotView;
        private SearchButton snapshotSearchButton;

        public Vector2 globalPosition = Vector2.zero;
        private Vector2 globalSize = Vector2.zero;

        private Vector2Int gridViewLocation = Vector2Int.one;
        private Vector2Int SnapshotGridViewLocation = Vector2Int.one;

        public float ScreenRatio = 1f;
        public float GridSize = 63f;
        public float ModSize = 63f;
        public float SlotSize = 124f;

        public bool LSButtons = false;
        public bool RSButtons = false;

        public EAmandsControllerUseStick InterfaceStick = EAmandsControllerUseStick.None;
        public EAmandsControllerUseStick InterfaceSkipStick = EAmandsControllerUseStick.RS;
        public EAmandsControllerUseStick ScrollStick = EAmandsControllerUseStick.LS;
        public EAmandsControllerUseStick WindowStick = EAmandsControllerUseStick.LS;

        public Dictionary<InventoryScreen.EInventoryTab, Tab> Tabs = new Dictionary<InventoryScreen.EInventoryTab, Tab>();

        // UI AutoMove
        private bool AutoMove = false;
        private bool SplitDialogAutoMove = false;

        private float AutoMoveTime = 0f;
        private float AutoMoveTimeDelay = 0.2f;

        private float InterfaceStickMoveTime = 0f;
        private float InterfaceStickMoveTimeDelay = 0.3f;

        private float InterfaceSkipStickMoveTime = 0f;
        private float InterfaceSkipStickMoveTimeDelay = 0.3f;

        public Vector2Int lastDirection = Vector2Int.zero;
        public int lastIntSliderValue;

        // UI Pointer
        private PointerEventData pointerEventData = null;
        private EventSystem eventSystem = null;
        private ItemView onPointerEnterItemView;

        // UI Drag
        public bool Dragging = false;
        private ItemView DraggingItemView = null;

        // UI Methods
        private MethodInfo ExecuteInteraction;
        private object[] ExecuteInteractionInvokeParameters = new object[1] { EItemInfoButton.Inspect };

        private MethodInfo IsInteractionAvailable;
        private object[] IsInteractionAvailableInvokeParameters = new object[1] { EItemInfoButton.Inspect };

        private MethodInfo ExecuteMiddleClick;
        private MethodInfo QuickFindAppropriatePlace;
        private MethodInfo CanExecute;
        private MethodInfo RunNetworkTransaction;
        private MethodInfo CalculateRotatedSize;
        private MethodInfo DraggedItemViewMethod_2;

        private MethodInfo ItemUIContextMethod_0;
        private object[] ItemUIContextMethod_0InvokeParameters = new object[2] { typeof(Item), EBoundItem.Item4 };

        private MethodInfo ShowContextMenu;
        private object[] ShowContextMenuInvokeParameters = new object[1] { Vector2.zero };

        public bool QuickSkipStick = false;

        // UI Selected Box
        public GameObject SelectedGameObject;
        public RectTransform SelectedRectTransform;
        public Image SelectedImage;
        public LayoutElement SelectedLayoutElement;

        // UI Button Blocks
        public delegate void AmandsControllerButtonState(EAmandsControllerButton Button, bool Pressed);
        public static AmandsControllerButtonState onAmandsControllerButtonState;
        public GameObject AllGameObject;
        public Dictionary<EAmandsControllerButton, AmandsControllerButtonBlock> ButtonBlocks = new Dictionary<EAmandsControllerButton, AmandsControllerButtonBlock>();

        // Files
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        public void OnGUI()
        {
            return;
            GUILayout.BeginArea(new Rect(20, 10, 1280, 720));

            AmandsControllerButtonBind[] amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.A);

            List<string> Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("A " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("A " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("A " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("A " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.X);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("X " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("X " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("X " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("X " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.Y);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("Y " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("Y " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("Y " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("Y " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.B);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("B " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("B " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("B " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("B " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.UP);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("UP " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("UP " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("UP " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("UP " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.DOWN);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("DOWN " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("DOWN " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("DOWN " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("DOWN " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.LEFT);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("LEFT " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("LEFT " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("LEFT " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("LEFT " + Action);
            }

            amandsControllerButtonBind = GetPriorityButtonBinds(EAmandsControllerButton.RIGHT);

            Actions = ControllerGetButtonAction(amandsControllerButtonBind[0]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("RIGHT " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[1]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("RIGHT " + Action);
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[2]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("RIGHT " + Action + " (HOLD)");
            }
            Actions = ControllerGetButtonAction(amandsControllerButtonBind[3]);
            foreach (string Action in Actions)
            {
                if (Action == "") continue;
                GUILayout.Label("RIGHT " + Action);
            }

            GUILayout.EndArea();



            GUIContent gUIContent = new GUIContent();
            if (InRaid && Interface && currentContextMenuButton == null)
            {
                GUI.Box(new Rect(new Vector2(globalPosition.x - (GridSize / 2), (Screen.height) - globalPosition.y - (GridSize / 2)), new Vector2(GridSize, GridSize)), gUIContent);
            }
            if (InRaid && Interface && currentScrollRectNoDrag != null && currentScrollRectNoDragRectTransform != null)
            {
                float height = currentScrollRectNoDrag.content.rect.height;
                Vector2 position = new Vector2(currentScrollRectNoDragRectTransform.position.x + (currentScrollRectNoDragRectTransform.rect.x * ScreenRatio), currentScrollRectNoDragRectTransform.position.y - ((currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f)) * ScreenRatio));
                /*if (globalPosition.x > position.x && globalPosition.x < (position.x + (currentScrollRectNoDragRectTransform.rect.width * ScreenRatio)))
                {
                    if ((globalPosition.y + (GridSize / 2f)) > position.y)
                    {
                        currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                        UpdateGlobalPosition();
                        if (!((globalPosition.y + (GridSize / 2f)) > position.y) && !((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio))))
                        {
                            ControllerUIOnMove(Vector2Int.zero, globalPosition);
                        }
                    }
                    else if ((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
                    {
                        currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((-1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                        UpdateGlobalPosition();
                        if (!((globalPosition.y + (GridSize / 2f)) > position.y) && !((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio))))
                        {
                            ControllerUIOnMove(Vector2Int.zero, globalPosition);
                        }
                    }
                }*/
                GUI.Box(new Rect(new Vector2(position.x, (Screen.height) - position.y), new Vector2(currentScrollRectNoDragRectTransform.rect.width * ScreenRatio, currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)), gUIContent);
            }
        }
        public void Start()
        {
            ItemUIContextMethod_0 = typeof(ItemUiContext).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
            TranslateInput = typeof(InputTree).GetMethod("TranslateInput", BindingFlags.Instance | BindingFlags.Public);
            ButtonPress = typeof(Button).GetMethod("Press", BindingFlags.Instance | BindingFlags.NonPublic);
            ExecuteMiddleClick = typeof(ItemView).GetMethod("ExecuteMiddleClick", BindingFlags.Instance | BindingFlags.Public);
            QuickFindAppropriatePlace = typeof(ItemUiContext).GetMethod("QuickFindAppropriatePlace", BindingFlags.Instance | BindingFlags.Public);
            CanExecute = typeof(TraderControllerClass).GetMethod("CanExecute", BindingFlags.Instance | BindingFlags.Public);
            RunNetworkTransaction = typeof(TraderControllerClass).GetMethod("RunNetworkTransaction", BindingFlags.Instance | BindingFlags.Public);
            ShowContextMenu = typeof(ItemView).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.Public);
            CalculateRotatedSize = typeof(Item).GetMethod("CalculateRotatedSize", BindingFlags.Instance | BindingFlags.Public);
            DraggedItemViewMethod_2 = typeof(DraggedItemView).GetMethod("method_2", BindingFlags.Instance | BindingFlags.Public);

            onAmandsControllerButtonState += ControllerButtonStateMethod;

            if (!File.Exists((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/Default.json")))
            {
                DefaultJSON();
            }
            if (File.Exists((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/Default.json")))
            {
                controllerPresetJsonClass = ReadFromJsonFile<ControllerPresetJsonClass>((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/Default.json"));
            }

            AimAnimationCurve.keys = AimKeys;

            ReloadFiles();

            AmandsControllerPlugin.BlockPosition.SettingChanged += BlockPositionUpdated;
            AmandsControllerPlugin.UserIndex.SettingChanged += UserIndexUpdated;
        }
        public void Update()
        {
            if (!connected) return;

            InRaid = localPlayer != null;

            if (!InRaid) return;

            gamepad = controller.GetState().Gamepad;

            if (LeftTrigger > AmandsControllerPlugin.LTDeadzone.Value)
            {
                if (!LT)
                {
                    LT = true;
                    GeneratePressType(EAmandsControllerButton.LT, true);
                }
            }
            else
            {
                if (LT)
                {
                    LT = false;
                    GeneratePressType(EAmandsControllerButton.LT, false);
                }
            }
            if (RightTrigger > AmandsControllerPlugin.RTDeadzone.Value)
            {
                if (!RT)
                {
                    RT = true;
                    GeneratePressType(EAmandsControllerButton.RT, true);
                }
            }
            else
            {
                if (RT)
                {
                    RT = false;
                    GeneratePressType(EAmandsControllerButton.RT, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
            {
                if (!A)
                {
                    A = true;
                    GeneratePressType(EAmandsControllerButton.A, true);

                }
            }
            else
            {
                if (A)
                {
                    A = false;
                    GeneratePressType(EAmandsControllerButton.A, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
            {
                if (!B)
                {
                    B = true;
                    GeneratePressType(EAmandsControllerButton.B, true);
                }
            }
            else
            {
                if (B)
                {
                    B = false;
                    GeneratePressType(EAmandsControllerButton.B, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
            {
                if (!X)
                {
                    X = true;
                    GeneratePressType(EAmandsControllerButton.X, true);
                }
            }
            else
            {
                if (X)
                {
                    X = false;
                    GeneratePressType(EAmandsControllerButton.X, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Y))
            {
                if (!Y)
                {
                    Y = true;
                    GeneratePressType(EAmandsControllerButton.Y, true);
                }
            }
            else
            {
                if (Y)
                {
                    Y = false;
                    GeneratePressType(EAmandsControllerButton.Y, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
            {
                if (!LB)
                {
                    LB = true;
                    GeneratePressType(EAmandsControllerButton.LB, true);
                    EnableSet("LB");
                }
            }
            else
            {
                if (LB)
                {
                    LB = false;
                    GeneratePressType(EAmandsControllerButton.LB, false);
                    DisableSet("LB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))
            {
                if (!RB)
                {
                    RB = true;
                    GeneratePressType(EAmandsControllerButton.RB, true);
                    EnableSet("RB");
                }
            }
            else
            {
                if (RB)
                {
                    RB = false;
                    GeneratePressType(EAmandsControllerButton.RB, false);
                    DisableSet("RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.RightShoulder))
            {
                if (!LB_RB)
                {
                    LB_RB = true;
                    EnableSet("LB_RB");
                }
            }
            else
            {
                if (LB_RB)
                {
                    LB_RB = false;
                    DisableSet("LB_RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.RightShoulder) && Interface)
            {
                if (!Interface_LB_RB)
                {
                    Interface_LB_RB = true;
                    EnableSet("Interface_LB_RB");
                }
            }
            else
            {
                if (Interface_LB_RB)
                {
                    Interface_LB_RB = false;
                    DisableSet("Interface_LB_RB");
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
            {
                if (!L)
                {
                    L = true;
                    GeneratePressType(EAmandsControllerButton.LS, true);
                }
            }
            else
            {
                if (L)
                {
                    L = false;
                    GeneratePressType(EAmandsControllerButton.LS, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb))
            {
                if (!R)
                {
                    R = true;
                    GeneratePressType(EAmandsControllerButton.RS, true);
                }
            }
            else
            {
                if (R)
                {
                    R = false;
                    GeneratePressType(EAmandsControllerButton.RS, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
            {
                if (!UP)
                {
                    UP = true;
                    GeneratePressType(EAmandsControllerButton.UP, true);
                }
            }
            else
            {
                if (UP)
                {
                    UP = false;
                    GeneratePressType(EAmandsControllerButton.UP, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
            {
                if (!DOWN)
                {
                    DOWN = true;
                    GeneratePressType(EAmandsControllerButton.DOWN, true);
                }
            }
            else
            {
                if (DOWN)
                {
                    DOWN = false;
                    GeneratePressType(EAmandsControllerButton.DOWN, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
            {
                if (!LEFT)
                {
                    LEFT = true;
                    GeneratePressType(EAmandsControllerButton.LEFT, true);
                }
            }
            else
            {
                if (LEFT)
                {
                    LEFT = false;
                    GeneratePressType(EAmandsControllerButton.LEFT, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
            {
                if (!RIGHT)
                {
                    RIGHT = true;
                    GeneratePressType(EAmandsControllerButton.RIGHT, true);
                }
            }
            else
            {
                if (RIGHT)
                {
                    RIGHT = false;
                    GeneratePressType(EAmandsControllerButton.RIGHT, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Back))
            {
                if (!BACK)
                {
                    BACK = true;
                    GeneratePressType(EAmandsControllerButton.BACK, true);
                }
            }
            else
            {
                if (BACK)
                {
                    BACK = false;
                    GeneratePressType(EAmandsControllerButton.BACK, false);
                }
            }
            if (gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
            {
                if (!MENU)
                {
                    MENU = true;
                    GeneratePressType(EAmandsControllerButton.MENU, true);
                }
            }
            else
            {
                if (MENU)
                {
                    MENU = false;
                    GeneratePressType(EAmandsControllerButton.MENU, false);
                }
            }

            LeftTrigger = (float)gamepad.LeftTrigger / 255f;
            RightTrigger = (float)gamepad.RightTrigger / 255f;

            LS.x = (float)gamepad.LeftThumbX / maxValue;
            LS.y = (float)gamepad.LeftThumbY / maxValue;
            LSXYSqrt = Mathf.Sqrt(Mathf.Pow(LS.x, 2) + Mathf.Pow(LS.y, 2));

            RS.x = (float)gamepad.RightThumbX / maxValue;
            RS.y = (float)gamepad.RightThumbY / maxValue;
            RSXYSqrt = Mathf.Sqrt(Mathf.Pow(RS.x, 2) + Mathf.Pow(RS.y, 2));

            if (LSXYSqrt > AmandsControllerPlugin.LSDeadzone.Value)
            {
                if (!LSINPUT)
                {
                    LSINPUT = true;
                }
            }
            else
            {
                if (LSINPUT)
                {
                    LSINPUT = false;
                }
            }
            if (LS.y > AmandsControllerPlugin.LSDeadzone.Value)
            {
                if (!LSUP)
                {
                    LSUP = true;
                    GeneratePressType(EAmandsControllerButton.LSUP, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSUP)
                {
                    LSUP = false;
                    GeneratePressType(EAmandsControllerButton.LSUP, false);
                }
            }
            if (LS.y < -AmandsControllerPlugin.LSDeadzone.Value)
            {
                if (!LSDOWN)
                {
                    LSDOWN = true;
                    GeneratePressType(EAmandsControllerButton.LSDOWN, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSDOWN)
                {
                    LSDOWN = false;
                    GeneratePressType(EAmandsControllerButton.LSDOWN, false);
                }
            }
            if (LS.x > AmandsControllerPlugin.LSDeadzone.Value)
            {
                if (!LSRIGHT)
                {
                    LSRIGHT = true;
                    GeneratePressType(EAmandsControllerButton.LSRIGHT, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSRIGHT)
                {
                    LSRIGHT = false;
                    GeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
            }
            if (LS.x < -AmandsControllerPlugin.LSDeadzone.Value)
            {
                if (!LSLEFT)
                {
                    LSLEFT = true;
                    GeneratePressType(EAmandsControllerButton.LSLEFT, true);
                    if (!LSButtons && InterfaceStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !LSButtons && InterfaceSkipStick == EAmandsControllerUseStick.LS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (LSLEFT)
                {
                    LSLEFT = false;
                    GeneratePressType(EAmandsControllerButton.LSLEFT, false);
                }
            }

            if (RSXYSqrt > AmandsControllerPlugin.RSDeadzone.Value)
            {
                if (!RSINPUT)
                {
                    RSINPUT = true;
                }
            }
            else
            {
                if (RSINPUT)
                {
                    RSINPUT = false;
                }
            }
            if (RS.y > AmandsControllerPlugin.RSDeadzone.Value)
            {
                if (!RSUP)
                {
                    RSUP = true;
                    GeneratePressType(EAmandsControllerButton.RSUP, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, 1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSUP)
                {
                    RSUP = false;
                    GeneratePressType(EAmandsControllerButton.RSUP, false);
                }
            }
            if (RS.y < -AmandsControllerPlugin.RSDeadzone.Value)
            {
                if (!RSDOWN)
                {
                    RSDOWN = true;
                    GeneratePressType(EAmandsControllerButton.RSDOWN, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(0, -1), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSDOWN)
                {
                    RSDOWN = false;
                    GeneratePressType(EAmandsControllerButton.RSDOWN, false);
                }
            }
            if (RS.x > AmandsControllerPlugin.RSDeadzone.Value)
            {
                if (!RSRIGHT)
                {
                    RSRIGHT = true;
                    GeneratePressType(EAmandsControllerButton.RSRIGHT, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSRIGHT)
                {
                    RSRIGHT = false;
                    GeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
            }
            if (RS.x < -AmandsControllerPlugin.RSDeadzone.Value)
            {
                if (!RSLEFT)
                {
                    RSLEFT = true;
                    GeneratePressType(EAmandsControllerButton.RSLEFT, true);
                    if (!RSButtons && InterfaceStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        InterfaceStickMoveTime = 0f;
                        InterfaceStickMoveTimeDelay = 0.15f;
                    }
                    if (QuickSkipStick && !RSButtons && InterfaceSkipStick == EAmandsControllerUseStick.RS)
                    {
                        ControllerUIMove(new Vector2Int(-1, 0), true);
                        InterfaceSkipStickMoveTime = 0f;
                        InterfaceSkipStickMoveTimeDelay = 0.15f;
                    }
                }
            }
            else
            {
                if (RSLEFT)
                {
                    RSLEFT = false;
                    GeneratePressType(EAmandsControllerButton.RSLEFT, false);
                }
            }

            // Interface
            if (Interface)
            {
                // Auto Move
                if (AutoMove || SplitDialogAutoMove)
                {
                    AutoMoveTime += Time.deltaTime;
                    if (AutoMoveTime > AutoMoveTimeDelay)
                    {
                        AutoMoveTime = 0f;
                        AutoMoveTimeDelay = 0.1f;
                        if (SplitDialogAutoMove)
                        {
                            ControllerSplitDialogAdd(lastIntSliderValue);
                        }
                        else if (AutoMove)
                        {
                            ControllerUIMove(lastDirection, false);
                        }
                    }
                }
                else
                {
                    AutoMoveTimeDelay = 0.2f;
                }

                // Window Move
                bool WindowLS = false;
                bool WindowRS = false;
                switch (WindowStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (!LSButtons && LSXYSqrt > AmandsControllerPlugin.LSDeadzone.Value)
                        {
                            if (currentContainedGridsView != null)
                            {
                                currentContainedGridsView.transform.parent.position += new Vector3(LS.x, LS.y, 0f) * 1000f * Time.deltaTime;
                                WindowLS = true;
                            }
                            else if (currentItemSpecificationPanel != null)
                            {
                                currentItemSpecificationPanel.transform.position += new Vector3(LS.x, LS.y, 0f) * 1000f * Time.deltaTime;
                                WindowLS = true;
                            }
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (!RSButtons && currentContainedGridsView != null && RSXYSqrt > AmandsControllerPlugin.RSDeadzone.Value)
                        {
                            if (currentContainedGridsView != null)
                            {
                                currentContainedGridsView.transform.parent.position += new Vector3(RS.x, RS.y, 0f) * 1000f * Time.deltaTime;
                                WindowRS = true;
                            }
                            else if (currentItemSpecificationPanel != null)
                            {
                                currentItemSpecificationPanel.transform.position += new Vector3(RS.x, RS.y, 0f) * 1000f * Time.deltaTime;
                                WindowRS = true;
                            }
                        }
                        break;
                }

                // Stick Move
                switch (InterfaceStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (LSButtons || WindowLS) break;
                        InterfaceStickMoveTime += Time.deltaTime;
                        if (InterfaceStickMoveTime > InterfaceStickMoveTimeDelay && (Mathf.Abs(LS.x) > AmandsControllerPlugin.LSDeadzone.Value || Mathf.Abs(LS.y) > AmandsControllerPlugin.LSDeadzone.Value))
                        {
                            InterfaceStickMoveTime = 0f;
                            InterfaceStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(LS.x > AmandsControllerPlugin.LSDeadzone.Value ? 1 : LS.x < -AmandsControllerPlugin.LSDeadzone.Value ? -1 : 0, LS.y > AmandsControllerPlugin.LSDeadzone.Value ? 1 : LS.y < -AmandsControllerPlugin.LSDeadzone.Value ? -1 : 0), false);
                        }
                        else if (!(Mathf.Abs(LS.x) > AmandsControllerPlugin.LSDeadzone.Value || Mathf.Abs(LS.y) > AmandsControllerPlugin.LSDeadzone.Value))
                        {
                            InterfaceStickMoveTime = 1f;
                            InterfaceStickMoveTimeDelay = 0.3f;
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (RSButtons || WindowRS) break;
                        InterfaceStickMoveTime += Time.deltaTime;
                        if (InterfaceStickMoveTime > InterfaceStickMoveTimeDelay && (Mathf.Abs(RS.x) > AmandsControllerPlugin.RSDeadzone.Value || Mathf.Abs(RS.y) > AmandsControllerPlugin.RSDeadzone.Value))
                        {
                            InterfaceStickMoveTime = 0f;
                            InterfaceStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(RS.x > AmandsControllerPlugin.RSDeadzone.Value ? 1 : RS.x < -AmandsControllerPlugin.RSDeadzone.Value ? -1 : 0, RS.y > AmandsControllerPlugin.RSDeadzone.Value ? 1 : RS.y < -AmandsControllerPlugin.RSDeadzone.Value ? -1 : 0), false);
                        }
                        else if (!(Mathf.Abs(RS.x) > AmandsControllerPlugin.RSDeadzone.Value || Mathf.Abs(RS.y) > AmandsControllerPlugin.RSDeadzone.Value))
                        {
                            InterfaceStickMoveTime = 1f;
                            InterfaceStickMoveTimeDelay = 0.3f;
                        }
                        break;
                }

                // Stick Skip Move
                switch (InterfaceSkipStick)
                {
                    case EAmandsControllerUseStick.LS:
                        if (LSButtons || WindowLS) break;
                        InterfaceSkipStickMoveTime += Time.deltaTime;
                        if (InterfaceSkipStickMoveTime > InterfaceSkipStickMoveTimeDelay && (Mathf.Abs(LS.x) > AmandsControllerPlugin.LSDeadzone.Value || Mathf.Abs(LS.y) > AmandsControllerPlugin.LSDeadzone.Value))
                        {
                            InterfaceSkipStickMoveTime = 0f;
                            InterfaceSkipStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(LS.x > AmandsControllerPlugin.LSDeadzone.Value ? 1 : LS.x < -AmandsControllerPlugin.LSDeadzone.Value ? -1 : 0, LS.y > AmandsControllerPlugin.LSDeadzone.Value ? 1 : LS.y < -AmandsControllerPlugin.LSDeadzone.Value ? -1 : 0), true);
                        }
                        else if (!(Mathf.Abs(LS.x) > AmandsControllerPlugin.LSDeadzone.Value || Mathf.Abs(LS.y) > AmandsControllerPlugin.LSDeadzone.Value))
                        {
                            InterfaceSkipStickMoveTime = 1f;
                            InterfaceSkipStickMoveTimeDelay = 0.3f;
                        }
                        break;
                    case EAmandsControllerUseStick.RS:
                        if (RSButtons || WindowRS) break;
                        InterfaceSkipStickMoveTime += Time.deltaTime;
                        if (InterfaceSkipStickMoveTime > InterfaceSkipStickMoveTimeDelay && (Mathf.Abs(RS.x) > AmandsControllerPlugin.RSDeadzone.Value || Mathf.Abs(RS.y) > AmandsControllerPlugin.RSDeadzone.Value))
                        {
                            InterfaceSkipStickMoveTime = 0f;
                            InterfaceSkipStickMoveTimeDelay = 0.15f;
                            ControllerUIMove(new Vector2Int(RS.x > AmandsControllerPlugin.RSDeadzone.Value ? 1 : RS.x < -AmandsControllerPlugin.RSDeadzone.Value ? -1 : 0, RS.y > AmandsControllerPlugin.RSDeadzone.Value ? 1 : RS.y < -AmandsControllerPlugin.RSDeadzone.Value ? -1 : 0), true);
                        }
                        else if (!(Mathf.Abs(RS.x) > AmandsControllerPlugin.RSDeadzone.Value || Mathf.Abs(RS.y) > AmandsControllerPlugin.RSDeadzone.Value))
                        {
                            InterfaceSkipStickMoveTime = 1f;
                            InterfaceSkipStickMoveTimeDelay = 0.3f;
                        }
                        break;
                }

                // Scroll
                if (currentScrollRectNoDrag != null && currentScrollRectNoDragRectTransform != null && !ContextMenu)
                {
                    switch (ScrollStick)
                    {
                        case EAmandsControllerUseStick.None:
                            ControllerAutoScroll();
                            break;
                        case EAmandsControllerUseStick.LS:
                            if (Mathf.Abs(LS.y) > AmandsControllerPlugin.LSDeadzone.Value && !LSButtons && !WindowLS)
                            {
                                ControllerScroll(LS.y);
                            }
                            else
                            {
                                ControllerAutoScroll();
                            }
                            break;
                        case EAmandsControllerUseStick.RS:
                            if (Mathf.Abs(RS.y) > AmandsControllerPlugin.RSDeadzone.Value && !RSButtons && !WindowRS)
                            {
                                ControllerScroll(RS.y);
                            }
                            else
                            {
                                ControllerAutoScroll();
                            }
                            break;
                    }
                }

                return;
            }

            // Movement
            if (!LSButtons && LSXYSqrt > AmandsControllerPlugin.MovementDeadzone.Value)
            {
                localPlayer.Move(LS.normalized);
                CharacterMovementSpeed = 0f;
                if (MovementContextObject != null)
                {
                    StateSpeedLimit = Traverse.Create(MovementContextObject).Property("StateSpeedLimit").GetValue<float>();
                    MaxSpeed = Traverse.Create(MovementContextObject).Property("MaxSpeed").GetValue<float>();
                    CharacterMovementSpeed = Mathf.Lerp(-AmandsControllerPlugin.MovementDeadzone.Value - AmandsControllerPlugin.DeadzoneBuffer.Value, 1f, LSXYSqrt) * Mathf.Min(StateSpeedLimit, MaxSpeed);
                    MovementInvokeParameters[0] = CharacterMovementSpeed;
                    SetCharacterMovementSpeed.Invoke(MovementContextObject, MovementInvokeParameters);
                }
                if (speedSlider != null)
                {
                    speedSlider.value = Mathf.Floor(((CharacterMovementSpeed + 0.005f) / speedSlider.maxValue) * 20f) * (speedSlider.maxValue / 20f);
                }
                resetCharacterMovementSpeed = true;
            }
            else if (resetCharacterMovementSpeed)
            {
                if (MovementContextObject != null)
                {
                    MovementInvokeParameters[0] = 0f;
                    SetCharacterMovementSpeed.Invoke(MovementContextObject, MovementInvokeParameters);
                }
                if (speedSlider != null)
                {
                    speedSlider.value = 0;
                }
            }

            // Aiming
            if (localPlayer != null && Camera.main != null)
            {
                Magnetism = false;
                Stickiness = 0;
                AutoAim = Vector2.zero;

                AimPosition = Vector3.one;
                AimDirection = Vector3.forward;

                if (firearmController == null)
                {
                    firearmController = localPlayer.HandsController as Player.FirearmController;
                }
                if (firearmController != null)
                {
                    AimPosition = firearmController.CurrentFireport.position;
                    AimDirection = firearmController.WeaponDirection;
                    firearmController.AdjustShotVectors(ref AimPosition, ref AimDirection);
                }
                colliders = new Collider[100];
                colliderCount = Physics.OverlapCapsuleNonAlloc(AimPosition, AimPosition + (AimDirection * 200f), AmandsControllerPlugin.Radius.Value, colliders, AimAssistLayerMask, QueryTriggerInteraction.Ignore);

                ScreenSize = new Vector2(Screen.width, Screen.height);
                ScreenSizeRatioMultiplier = new Vector2(1f, (float)(Screen.height) / (float)(Screen.width));

                AimAssistAngle = 100000f;
                AimAssistLocalPlayer = null;

                for (int i = 0; i < colliderCount; i++)
                {
                    SSAARatio = (float)currentSSAA.GetOutputHeight() / (float)currentSSAA.GetInputHeight();

                    HitAimAssistLocalPlayer = colliders[i].transform.gameObject.GetComponent<LocalPlayer>();
                    if (HitAimAssistLocalPlayer != null && HitAimAssistLocalPlayer != localPlayer)
                    {
                        AimAssistScreenLocalPosition = ((((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (((((Vector2)Camera.main.WorldToScreenPoint(AimPosition + (AimDirection * Vector3.Distance(AimPosition, HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * 0f))))  * SSAARatio) - (((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Head.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(AimPosition, (HitAimAssistLocalPlayer.PlayerBones.Head.position - AimPosition).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Head.position, AimPosition), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                        AimAssistScreenLocalPosition = ((((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (((((Vector2)Camera.main.WorldToScreenPoint(AimPosition + (AimDirection * Vector3.Distance(AimPosition, HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * 0f))))  * SSAARatio) - (((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(AimPosition, (HitAimAssistLocalPlayer.PlayerBones.Ribcage.position - AimPosition).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Ribcage.position, AimPosition), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                        AimAssistScreenLocalPosition = ((((((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (((((Vector2)Camera.main.WorldToScreenPoint(AimPosition + (AimDirection * Vector3.Distance(AimPosition, HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * 0f))))  * SSAARatio) - (((Vector2)Camera.main.WorldToScreenPoint(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position + (HitAimAssistLocalPlayer.Velocity * 0f))  * SSAARatio) - (ScreenSize / 2f))) - (ScreenSize / 2f)) * 2f)) - (ScreenSize / 2f)) / ScreenSize) * ScreenSizeRatioMultiplier);
                        AimAssistBoneAngle = Mathf.Sqrt(Vector2.SqrMagnitude(AimAssistScreenLocalPosition)) / (ScreenSize.y / ScreenSize.x);
                        if (AimAssistBoneAngle < Mathf.Max(AmandsControllerPlugin.MagnetismRadius.Value, AmandsControllerPlugin.StickinessRadius.Value, AmandsControllerPlugin.AutoAimRadius.Value) && AimAssistBoneAngle < AimAssistAngle && !Physics.Raycast(AimPosition, (HitAimAssistLocalPlayer.PlayerBones.Pelvis.position - AimPosition).normalized, out hit, Vector3.Distance(HitAimAssistLocalPlayer.PlayerBones.Pelvis.position, AimPosition), HighLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            AimAssistAngle = AimAssistBoneAngle;
                            AimAssistLocalPlayer = HitAimAssistLocalPlayer;
                            AimAssistTarget2DPoint = AimAssistScreenLocalPosition;
                        }
                    }
                }
                if (AimAssistLocalPlayer != null && firearmController != null)
                {
                    if (AimAssistAngle < AmandsControllerPlugin.MagnetismRadius.Value)
                    {
                        Magnetism = true;
                    }
                    if (AimAssistAngle < AmandsControllerPlugin.StickinessRadius.Value)
                    {
                        Stickiness = Mathf.Lerp(1f, 0f, (Mathf.Clamp(AimAssistAngle / AmandsControllerPlugin.StickinessRadius.Value, 0.5f, 1f) - 0.5f) / (1f - 0.5f));
                    }
                    if (AimAssistAngle < AmandsControllerPlugin.AutoAimRadius.Value)
                    {
                        AutoAim = Vector2.Lerp(Vector2.Lerp(Vector2.zero, Vector2.Lerp(new Vector2(Mathf.Clamp((AimAssistTarget2DPoint.x) * 10f, -0.5f, 0.5f), Mathf.Clamp((AimAssistTarget2DPoint.y) * -5f, -0.5f, 0.5f)) * 100f * Time.deltaTime, Vector2.zero, (Mathf.Clamp(AimAssistAngle / AmandsControllerPlugin.AutoAimRadius.Value, 0.5f, 1f) - 0.5f) / (1f - 0.5f)) * AmandsControllerPlugin.AutoAim.Value, 1f) / firearmController.AimingSensitivity * (firearmController.IsAiming ? 2f : 1f), Vector2.zero, RSXYSqrt);
                    }
                }
            }
            StickinessSmooth += ((Stickiness - StickinessSmooth) * AmandsControllerPlugin.StickinessSmooth.Value) * Time.deltaTime;
            AutoAimSmooth += ((AutoAim - AutoAimSmooth) * AmandsControllerPlugin.AutoAimSmooth.Value) * Time.deltaTime;
            if (!RSButtons && RSXYSqrt > AmandsControllerPlugin.AimDeadzone.Value || Mathf.Sqrt(Mathf.Pow(AutoAimSmooth.x, 2) + Mathf.Pow(AutoAimSmooth.y, 2)) > AmandsControllerPlugin.AimDeadzone.Value)
            {
                Aim.x = RS.x * AimAnimationCurve.Evaluate(RSXYSqrt);
                Aim.y = RS.y * AimAnimationCurve.Evaluate(RSXYSqrt);
                localPlayer.Rotate(((Aim * (isAiming ? AmandsControllerPlugin.AimingSensitivity.Value : AmandsControllerPlugin.Sensitivity.Value) * (AmandsControllerPlugin.InvertY.Value ? Vector2.one * 100f : InvertY) * Time.deltaTime) * Mathf.Lerp(1f, AmandsControllerPlugin.Stickiness.Value, StickinessSmooth)) + AutoAimSmooth, false);
            }

            // Aiming Set
            if (localPlayer.HandsController != null)
            {
                if (localPlayer.HandsController.IsAiming && !isAiming)
                {
                    isAiming = true;
                    EnableSet("Aiming");
                }
                else if (isAiming && !localPlayer.HandsController.IsAiming)
                {
                    isAiming = false;
                    DisableSet("Aiming");
                }
            }

            // Lean
            if (SlowLeanLeft || SlowLeanRight)
            {
                localPlayer.SlowLean(((SlowLeanLeft ? -AmandsControllerPlugin.LeanSensitivity.Value: 0) + (SlowLeanRight ? AmandsControllerPlugin.LeanSensitivity.Value : 0)) * Time.deltaTime);
            }
        }
        private void BlockPositionUpdated(object sender, EventArgs e)
        {
            if (AllGameObject != null && AllGameObject.activeSelf)
            {
                AllGameObject.transform.position = new Vector2(Screen.width, 0f) + AmandsControllerPlugin.BlockPosition.Value;
            }
        }
        private void UserIndexUpdated(object sender, EventArgs e)
        {
            if (localPlayer != null)
            {
                switch (AmandsControllerPlugin.UserIndex.Value)
                {
                    case 1:
                        controller = new Controller(UserIndex.One);
                        connected = controller.IsConnected;
                        break;
                    case 2:
                        controller = new Controller(UserIndex.Two);
                        connected = controller.IsConnected;
                        break;
                    case 3:
                        controller = new Controller(UserIndex.Three);
                        connected = controller.IsConnected;
                        break;
                    case 4:
                        controller = new Controller(UserIndex.Four);
                        connected = controller.IsConnected;
                        break;
                    default:
                        controller = new Controller(UserIndex.One);
                        connected = controller.IsConnected;
                        break;
                }
            }
        }

        public void UpdateController(LocalPlayer Player)
        {
            ScreenRatio = (Screen.height / 1080f);

            eventSystem = FindObjectOfType<EventSystem>();
            pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.button = PointerEventData.InputButton.Left;
            switch (AmandsControllerPlugin.UserIndex.Value)
            {
                case 1:
                    controller = new Controller(UserIndex.One);
                    connected = controller.IsConnected;
                    break;
                case 2:
                    controller = new Controller(UserIndex.Two);
                    connected = controller.IsConnected;
                    break;
                case 3:
                    controller = new Controller(UserIndex.Three);
                    connected = controller.IsConnected;
                    break;
                case 4:
                    controller = new Controller(UserIndex.Four);
                    connected = controller.IsConnected;
                    break;
                default:
                    controller = new Controller(UserIndex.One);
                    connected = controller.IsConnected;
                    break;
            }

            if (Player != null)
            {
                localPlayer = Player;
                //movementContext = localPlayer.MovementContext;
                MovementContextObject = Traverse.Create(localPlayer).Property("MovementContext").GetValue<object>();
                MovementContextType = MovementContextObject.GetType();
                SetCharacterMovementSpeed = MovementContextType.GetMethod("SetCharacterMovementSpeed", BindingFlags.Instance | BindingFlags.Public);
            }
            globalPosition = new Vector2(Screen.width / 2.2f, Screen.height);
            if (controllerPresetJsonClass != null && controllerPresetJsonClass.AmandsControllerButtonBinds != null && controllerPresetJsonClass.AmandsControllerSets != null)
            {
                AmandsControllerSets.Clear();
                AmandsControllerButtonBinds.Clear();
                AmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>(controllerPresetJsonClass.AmandsControllerSets);
                AmandsControllerButtonBinds = new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>(controllerPresetJsonClass.AmandsControllerButtonBinds);
                return;
            }
        }
        public void DefaultJSON()
        {
            Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> AmandsControllerSets = new Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>>();
            Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>> AmandsControllerButtonBinds = new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>();

            AmandsControllerSets.Clear();
            AmandsControllerSets.Add("LB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ThrowGrenade), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondaryWeapon), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondaryWeapon), EAmandsControllerPressType.DoubleClick, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectSecondPrimaryWeapon), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectFirstPrimaryWeapon), EAmandsControllerPressType.Press, 2) });

            /*AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot4), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot5), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot6), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot7), EAmandsControllerPressType.Press, 2) });*/

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot4), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot4), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot5), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot5), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot6), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot6), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot7), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot7), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 2));

            AmandsControllerSets["LB"].Add(EAmandsControllerButton.LS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DropBackpack), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleGoggles), EAmandsControllerPressType.Press, 2) });

            AmandsControllerSets.Add("RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChamberUnload), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckChamber), EAmandsControllerPressType.Hold, 1));
            AmandsControllerSets["RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.UnloadMagazine), EAmandsControllerPressType.DoubleClick, 1));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Movement"), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Movement"), EAmandsControllerPressType.Release, 1));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.FoldStock), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckChamber), EAmandsControllerPressType.Hold, 1));

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleLeanLeft), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleLeanRight), EAmandsControllerPressType.Press, 1) });

            AmandsControllerSets["RB"].Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SwitchHeadLight), EAmandsControllerPressType.Press, 1) });
            AmandsControllerSets["RB"][EAmandsControllerButton.BACK].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleHeadLight), EAmandsControllerPressType.Hold, 1));

            AmandsControllerSets.Add("LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBlindAbove), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BlindShootEnd), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBlindRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BlindShootEnd), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleStepLeft), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReturnFromLeftStep), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleStepRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReturnFromRightStep), EAmandsControllerPressType.Release, 3));

            /*AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot8), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot9), EAmandsControllerPressType.Press, 2) });
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.PressSlot0), EAmandsControllerPressType.Press, 2) });*/

            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot8), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot8), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot9), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot9), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.PressSlot0), new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.SelectFastSlot0), new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "HealingLimbSelector") }, EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["LB_RB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DisplayTimer), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["LB_RB"][EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.DisplayTimerAndExits), EAmandsControllerPressType.DoubleClick, 3));

            AmandsControllerSets.Add("ActionPanel", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.BeginInteracting), EAmandsControllerPressType.Press, 10) });
            AmandsControllerSets["ActionPanel"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.EndInteracting), EAmandsControllerPressType.Release, 10));
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollPrevious), EAmandsControllerPressType.Press, 10) });
            AmandsControllerSets["ActionPanel"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollNext), EAmandsControllerPressType.Press, 10) });

            AmandsControllerSets.Add("HealingLimbSelector", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollNext), EAmandsControllerPressType.Press, 11) });
            AmandsControllerSets["HealingLimbSelector"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ScrollPrevious), EAmandsControllerPressType.Press, 11) });

            AmandsControllerSets.Add("Movement", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LSLEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SlowLeanLeft), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LSLEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndSlowLean), EAmandsControllerPressType.Release, 3));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LSRIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SlowLeanRight), EAmandsControllerPressType.Press, 3) });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LSRIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndSlowLean), EAmandsControllerPressType.Release, 3));
            /*AmandsControllerSets["Movement"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.NextWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand> { ECommand.PreviousWalkPose }, EAmandsControllerCommand.InputTree, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanLeft, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.None }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.SlowLeanRight, EAmandsControllerPressType.Press, 3, "") });
            AmandsControllerSets["Movement"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new List<ECommand> { ECommand.None }, EAmandsControllerCommand.EndSlowLean, EAmandsControllerPressType.Release, 3, ""));
            AmandsControllerSets["Movement"].Add(EAmandsControllerButton.LeftThumb, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<ECommand>(), EAmandsControllerCommand.RestoreLean, EAmandsControllerPressType.Press, 3, "") });*/

            AmandsControllerSets.Add("Aiming", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleBreathing), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming"].Add(EAmandsControllerButton.RB, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Aiming_RB"), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming"][EAmandsControllerButton.RB].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Aiming_RB"), EAmandsControllerPressType.Release, 4));

            AmandsControllerSets.Add("Aiming_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.OpticCalibrationSwitchUp), EAmandsControllerPressType.Press, 4) });
            AmandsControllerSets["Aiming_RB"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.OpticCalibrationSwitchDown), EAmandsControllerPressType.Press, 4) });

            AmandsControllerSets.Add("Interface", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceUp), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDown), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceLeft), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceRight), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceDisableAutoMove), EAmandsControllerPressType.Release, 20));

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.BeginDrag), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.A].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.ShowContextMenu), EAmandsControllerPressType.Hold, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.Escape), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Use), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.UseHold), EAmandsControllerPressType.Hold, 20));
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.QuickMove), EAmandsControllerPressType.Press, 20) });

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Discard), EAmandsControllerPressType.Press, 20) });

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LB, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EnableSet, "Interface_LB"), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"][EAmandsControllerButton.LB].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.DisableSet, "Interface_LB"), EAmandsControllerPressType.Release, 20));

            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.LT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.PreviousTab), EAmandsControllerPressType.Press, 20) });
            AmandsControllerSets["Interface"].Add(EAmandsControllerButton.RT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.NextTab), EAmandsControllerPressType.Press, 20) });

            AmandsControllerSets.Add("OnDrag", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.EndDrag), EAmandsControllerPressType.Press, 23) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.CancelDrag), EAmandsControllerPressType.Press, 23) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.RotateDragged), EAmandsControllerPressType.Press, 23) });
            AmandsControllerSets["OnDrag"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDragged), EAmandsControllerPressType.Press, 23) });

            AmandsControllerSets.Add("Interface_LB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind4), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind5), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind6), EAmandsControllerPressType.Press, 21) });
            AmandsControllerSets["Interface_LB"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind7), EAmandsControllerPressType.Press, 21) });

            AmandsControllerSets.Add("Interface_LB_RB", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind8), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind9), EAmandsControllerPressType.Press, 22) });
            AmandsControllerSets["Interface_LB_RB"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.InterfaceBind10), EAmandsControllerPressType.Press, 22) });

            AmandsControllerSets.Add("SearchButton", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["SearchButton"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.Search), EAmandsControllerPressType.Press, 24) });

            AmandsControllerSets.Add("ContextMenu", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.ContextMenuUse), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });
            AmandsControllerSets["ContextMenu"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 30) });

            AmandsControllerSets.Add("SplitDialog", new Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>());
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAccept), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAdd), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogSubtract), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogSubtract), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogAdd), EAmandsControllerPressType.Press, 50) });
            AmandsControllerSets["SplitDialog"][EAmandsControllerButton.RIGHT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.SplitDialogDisableAutoMove), EAmandsControllerPressType.Release, 50));
            AmandsControllerSets["SplitDialog"].Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.None), EAmandsControllerPressType.Press, 50) });

            AmandsControllerButtonBinds.Clear();
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.ToggleAlternativeShooting), new AmandsControllerCommand(ECommand.EndSprinting), new AmandsControllerCommand(ECommand.TryLowThrow) }, EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.LT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.FinishLowThrow), EAmandsControllerPressType.Release, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.ToggleShooting), new AmandsControllerCommand(ECommand.EndSprinting), new AmandsControllerCommand(ECommand.TryHighThrow) }, EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.RT].Add(new AmandsControllerButtonBind(new List<AmandsControllerCommand> { new AmandsControllerCommand(ECommand.EndShooting), new AmandsControllerCommand(ECommand.FinishHighThrow) }, EAmandsControllerPressType.Release, -1));

            AmandsControllerButtonBinds.Add(EAmandsControllerButton.A, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.Jump), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.B, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleDuck), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.B].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleProne), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.X, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ReloadWeapon), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckAmmo), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds[EAmandsControllerButton.X].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.QuickReloadWeapon), EAmandsControllerPressType.DoubleClick, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.Y, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(EAmandsControllerCommand.QuickSelectWeapon), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.Y].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ExamineWeapon), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleSprinting), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RS, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.QuickKnifeKick), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.RS].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.SelectKnife), EAmandsControllerPressType.Hold, -1));

            AmandsControllerButtonBinds.Add(EAmandsControllerButton.UP, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.NextTacticalDevice), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.UP].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleTacticalDevice), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.DOWN, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeWeaponMode), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.CheckFireMode), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds[EAmandsControllerButton.DOWN].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ForceAutoWeaponMode), EAmandsControllerPressType.DoubleClick, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.LEFT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScopeMagnification), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds[EAmandsControllerButton.LEFT].Add(new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScope), EAmandsControllerPressType.Hold, -1));
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.RIGHT, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ChangeScope), EAmandsControllerPressType.Press, -1) });
            AmandsControllerButtonBinds.Add(EAmandsControllerButton.BACK, new List<AmandsControllerButtonBind> { new AmandsControllerButtonBind(new AmandsControllerCommand(ECommand.ToggleInventory), EAmandsControllerPressType.Press, -1) });

            ControllerPresetJsonClass controllerPresetJsonClass = new ControllerPresetJsonClass();
            controllerPresetJsonClass.AmandsControllerButtonBinds = AmandsControllerButtonBinds;
            controllerPresetJsonClass.AmandsControllerSets = AmandsControllerSets;

            WriteToJsonFile<ControllerPresetJsonClass>((AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/Default.json"), controllerPresetJsonClass, false);
        }
        public void UpdateInterfaceBinds(bool Enabled)
        {
            Interface = Enabled;
            if (Enabled)
            {
                EnableSet("Interface");
            }
            else
            {
                DisableSet("Interface");
                DisableSet("Interface_LB");
                DisableSet("Interface_LB_RB");
                DisableSet("OnDrag");
                DisableSet("SearchButton");
            }
        }
        public void UpdateActionPanelBinds(bool Enabled)
        {
            if (Enabled)
            {
                EnableSet("ActionPanel");
            }
            else
            {
                DisableSet("ActionPanel");
            }
        }
        public void UpdateSplitDialogBinds(bool Enabled)
        {
            if (Enabled)
            {
                EnableSet("SplitDialog");
            }
            else
            {
                DisableSet("SplitDialog");
            }
        }
        public void UpdateContextMenuBinds(bool Enabled)
        {
            ContextMenu = Enabled;
            if (Enabled)
            {
                EnableSet("ContextMenu");
            }
            else
            {
                DisableSet("ContextMenu");
            }
        }
        public void UpdateInterface(InventoryScreen inventoryScreen)
        {
            this.inventoryScreen = inventoryScreen;
            if (AllGameObject != null && !InRaid)
            {
                Destroy(AllGameObject);
            }
            if (AllGameObject != null || !InRaid) return;


            AllGameObject = new GameObject("All");
            RectTransform AllRectTransform = AllGameObject.AddComponent<RectTransform>();
            AllRectTransform.SetParent(inventoryScreen.transform);
            AllRectTransform.pivot = new Vector2(1f,0f);
            AllRectTransform.position = new Vector2(Screen.width, 0f) + AmandsControllerPlugin.BlockPosition.Value;
            AllRectTransform.sizeDelta = new Vector2(2560f, AmandsControllerPlugin.BlockSize.Value.y);
            HorizontalLayoutGroup AllHorizontalLayoutGroup = AllGameObject.AddComponent<HorizontalLayoutGroup>();
            AllHorizontalLayoutGroup.childForceExpandHeight = false;
            AllHorizontalLayoutGroup.childForceExpandWidth = false;
            AllHorizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            AllHorizontalLayoutGroup.spacing = AmandsControllerPlugin.BlockSpacing.Value;

            ButtonBlocks.Clear();

            ButtonBlocks.Add(EAmandsControllerButton.Y, new GameObject("Y").AddComponent<AmandsControllerButtonBlock>());
            ButtonBlocks[EAmandsControllerButton.Y].transform.SetParent(AllRectTransform);
            ButtonBlocks[EAmandsControllerButton.Y].Button = EAmandsControllerButton.Y;

            ButtonBlocks.Add(EAmandsControllerButton.X, new GameObject("X").AddComponent<AmandsControllerButtonBlock>());
            ButtonBlocks[EAmandsControllerButton.X].transform.SetParent(AllRectTransform);
            ButtonBlocks[EAmandsControllerButton.X].Button = EAmandsControllerButton.X;

            //ButtonBlocks.Add(EAmandsControllerButton.B, new GameObject("B").AddComponent<AmandsControllerButtonBlock>());
            //ButtonBlocks[EAmandsControllerButton.B].transform.SetParent(AllRectTransform);
            //ButtonBlocks[EAmandsControllerButton.B].Button = EAmandsControllerButton.B;

            ButtonBlocks.Add(EAmandsControllerButton.A, new GameObject("A").AddComponent<AmandsControllerButtonBlock>());
            ButtonBlocks[EAmandsControllerButton.A].transform.SetParent(AllRectTransform);
            ButtonBlocks[EAmandsControllerButton.A].Button = EAmandsControllerButton.A;

        }

        public void GeneratePressType(EAmandsControllerButton Button, bool Pressed)
        {
            onAmandsControllerButtonState(Button, Pressed);
            if (AmandsControllerButtonSnapshots.ContainsKey(Button))
            {
                AmandsControllerButtonSnapshot AmandsControllerButtonSnapshot = AmandsControllerButtonSnapshots[Button];
                if (Pressed)
                {
                    if (AmandsControllerButtonSnapshot.DoubleClickBind.Priority != -100 && Time.time - AmandsControllerButtonSnapshot.Time <= AmandsControllerPlugin.DoubleClickDelay.Value)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.DoubleClickBind);
                    }
                    AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                    AsyncPress.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                    AmandsControllerButtonSnapshots.Remove(Button);
                }
                else
                {
                    // Temp
                    if (AmandsControllerButtonSnapshot.ReleaseBind.Priority != -100)
                    {
                        AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                        AmandsControllerButtonSnapshots.Remove(Button);
                    }
                    else
                    {
                        // Temp
                        if (AmandsControllerButtonSnapshot.HoldBind.Priority == -100 && AmandsControllerButtonSnapshot.DoubleClickBind.Priority == -100)
                        {
                            if (AmandsControllerButtonSnapshot.ReleaseBind.Priority != -100)
                            {
                                AmandsControllerButton(AmandsControllerButtonSnapshot.ReleaseBind);
                            }
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                        else if (AmandsControllerButtonSnapshot.HoldBind.Priority != -100 || AmandsControllerButtonSnapshot.DoubleClickBind.Priority != -100)
                        {
                            AsyncHold.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                        }
                        if (AmandsControllerButtonSnapshot.DoubleClickBind.Priority == -100 && AmandsControllerButtonSnapshot.ReleaseBind.Priority == -100)
                        {
                            AsyncPress.Remove(Button.ToString() + AmandsControllerButtonSnapshot.Time.ToString());
                            AmandsControllerButton(AmandsControllerButtonSnapshot.PressBind);
                            AmandsControllerButtonSnapshots.Remove(Button);
                        }
                    }
                }
            }
            else if (Pressed)
            {
                float time = Time.time;
                AmandsControllerButtonBind[] Binds = GetPriorityButtonBinds(Button);
                // Temp
                if (Binds[1].Priority != -100)
                {
                    AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    AmandsControllerButton(Binds[0]);
                }
                else
                {
                    // Temp
                    if (Binds[2].Priority == -100 && Binds[3].Priority == -100)
                    {
                        AmandsControllerButton(Binds[0]);
                    }
                    else if (Binds[2].Priority != -100 || Binds[3].Priority != -100)
                    {
                        ButtonTimer(Button.ToString() + time.ToString(), Button);
                    }
                    if (Binds[2].Priority != -100 || Binds[3].Priority != -100)
                    {
                        AmandsControllerButtonSnapshots.Add(Button, new AmandsControllerButtonSnapshot(true, time, Binds[0], Binds[1], Binds[2], Binds[3]));
                    }
                }
            }
        }
        public AmandsControllerButtonBind[] GetPriorityButtonBinds(EAmandsControllerButton Button)
        {
            AmandsControllerButtonBind PressBind = EmptyBind;
            AmandsControllerButtonBind ReleaseBind = EmptyBind;
            AmandsControllerButtonBind HoldBind = EmptyBind;
            AmandsControllerButtonBind DoubleClickBind = EmptyBind;

            int SetPriority = -69;
            string PrioritySet = "";

            foreach (string Set in ActiveAmandsControllerSets)
            {
                if (AmandsControllerSets[Set].ContainsKey(Button))
                {
                    foreach (AmandsControllerButtonBind Bind in AmandsControllerSets[Set][Button])
                    {
                        if (Bind.Priority > SetPriority)
                        {
                            SetPriority = Bind.Priority;
                            PrioritySet = Set;
                        }
                    }
                }
            }
            if (PrioritySet != "")
            {
                foreach (AmandsControllerButtonBind Bind in AmandsControllerSets[PrioritySet][Button])
                {
                    switch (Bind.PressType)
                    {
                        case EAmandsControllerPressType.Press:
                            PressBind = Bind;
                            break;
                        case EAmandsControllerPressType.Release:
                            ReleaseBind = Bind;
                            break;
                        case EAmandsControllerPressType.Hold:
                            HoldBind = Bind;
                            break;
                        case EAmandsControllerPressType.DoubleClick:
                            DoubleClickBind = Bind;
                            break;
                    }
                }
            }
            else
            {
                if (AmandsControllerButtonBinds.ContainsKey(Button))
                {
                    foreach (AmandsControllerButtonBind Bind in AmandsControllerButtonBinds[Button])
                    {
                        switch (Bind.PressType)
                        {
                            case EAmandsControllerPressType.Press:
                                PressBind = Bind;
                                break;
                            case EAmandsControllerPressType.Release:
                                ReleaseBind = Bind;
                                break;
                            case EAmandsControllerPressType.Hold:
                                HoldBind = Bind;
                                break;
                            case EAmandsControllerPressType.DoubleClick:
                                DoubleClickBind = Bind;
                                break;
                        }
                    }
                }
            }
            return new AmandsControllerButtonBind[4] { PressBind, ReleaseBind, HoldBind, DoubleClickBind };
        }
        public void AmandsControllerButton(AmandsControllerButtonBind Bind)
        {
            List<ECommand> Commands = new List<ECommand>();
            foreach (AmandsControllerCommand AmandsControllerCommand in Bind.AmandsControllerCommands)
            {
                if (AmandsControllerCommand.Command == EAmandsControllerCommand.None) continue;
                switch (AmandsControllerCommand.Command)
                {
                    case EAmandsControllerCommand.ToggleSet:
                        ToggleSet(AmandsControllerCommand.AmandsControllerSet);
                        break;
                    case EAmandsControllerCommand.EnableSet:
                        EnableSet(AmandsControllerCommand.AmandsControllerSet);
                        break;
                    case EAmandsControllerCommand.DisableSet:
                        DisableSet(AmandsControllerCommand.AmandsControllerSet);
                        break;
                    case EAmandsControllerCommand.InputTree:
                        if (inputTree != null)
                        {
                            Commands.Add(AmandsControllerCommand.InputTree);
                        }
                        break;
                    case EAmandsControllerCommand.QuickSelectWeapon:
                        break;
                    case EAmandsControllerCommand.SlowLeanLeft:
                        SlowLeanLeft = true;
                        break;
                    case EAmandsControllerCommand.SlowLeanRight:
                        SlowLeanRight = true;
                        break;
                    case EAmandsControllerCommand.EndSlowLean:
                        SlowLeanLeft = false;
                        SlowLeanRight = false;
                        break;
                    case EAmandsControllerCommand.RestoreLean:
                        if (inputTree != null)
                        {
                            Commands.Add(ECommand.EndLeanLeft);
                            Commands.Add(ECommand.EndLeanRight);
                        }
                        break;
                    case EAmandsControllerCommand.InterfaceUp:
                        ControllerUIMove(new Vector2Int(0, 1), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceDown:
                        ControllerUIMove(new Vector2Int(0, -1), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceLeft:
                        ControllerUIMove(new Vector2Int(-1, 0), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceRight:
                        ControllerUIMove(new Vector2Int(1, 0), false);
                        AutoMove = true;
                        AutoMoveTime = 0f;
                        break;
                    case EAmandsControllerCommand.InterfaceDisableAutoMove:
                        AutoMove = false;
                        break;
                    case EAmandsControllerCommand.BeginDrag:
                        ControllerBeginDrag();
                        break;
                    case EAmandsControllerCommand.EndDrag:
                        ControllerEndDrag();
                        break;
                    case EAmandsControllerCommand.RotateDragged:
                        ControllerRotateDragged();
                        break;
                    case EAmandsControllerCommand.SplitDragged:
                        ControllerSplitDragged();
                        break;
                    case EAmandsControllerCommand.CancelDrag:
                        ControllerCancelDrag();
                        break;
                    case EAmandsControllerCommand.Search:
                        ControllerSearch();
                        break;
                    case EAmandsControllerCommand.Use:
                        ControllerUse(false);
                        break;
                    case EAmandsControllerCommand.UseHold:
                        ControllerUse(true);
                        break;
                    case EAmandsControllerCommand.QuickMove:
                        ControllerQuickMove();
                        break;
                    case EAmandsControllerCommand.Discard:
                        ControllerDiscard();
                        break;
                    case EAmandsControllerCommand.InterfaceBind4:
                        ControllerInterfaceBind(EBoundItem.Item4);
                        break;
                    case EAmandsControllerCommand.InterfaceBind5:
                        ControllerInterfaceBind(EBoundItem.Item5);
                        break;
                    case EAmandsControllerCommand.InterfaceBind6:
                        ControllerInterfaceBind(EBoundItem.Item6);
                        break;
                    case EAmandsControllerCommand.InterfaceBind7:
                        ControllerInterfaceBind(EBoundItem.Item7);
                        break;
                    case EAmandsControllerCommand.InterfaceBind8:
                        ControllerInterfaceBind(EBoundItem.Item8);
                        break;
                    case EAmandsControllerCommand.InterfaceBind9:
                        ControllerInterfaceBind(EBoundItem.Item9);
                        break;
                    case EAmandsControllerCommand.InterfaceBind10:
                        ControllerInterfaceBind(EBoundItem.Item10);
                        break;
                    case EAmandsControllerCommand.ShowContextMenu:
                        ControllerShowContextMenu();
                        break;
                    case EAmandsControllerCommand.ContextMenuUse:
                        ControllerContextMenuUse();
                        break;
                    case EAmandsControllerCommand.SplitDialogAccept:
                        ControllerSplitDialogAccept();
                        break;
                    case EAmandsControllerCommand.SplitDialogAdd:
                        ControllerSplitDialogAdd(1);
                        break;
                    case EAmandsControllerCommand.SplitDialogSubtract:
                        ControllerSplitDialogAdd(-1);
                        break;
                    case EAmandsControllerCommand.SplitDialogDisableAutoMove:
                        SplitDialogAutoMove = false;
                        break;
                    case EAmandsControllerCommand.PreviousTab:
                        ControllerPreviousTab();
                        break;
                    case EAmandsControllerCommand.NextTab:
                        ControllerNextTab();
                        break;
                }
            }
            if (Commands.Count != 0 && inputTree != null)
            {
                TranslateInputInvokeParameters[0] = Commands;
                TranslateInput.Invoke(inputTree, TranslateInputInvokeParameters);
            }
        }
        public void ToggleSet(string AmandsControllerSet)
        {
            if (ActiveAmandsControllerSets.Contains(AmandsControllerSet))
            {
                ActiveAmandsControllerSets.Remove(AmandsControllerSet);
                LSRSButtonsCheck();
                ControllerSetState(AmandsControllerSet, false);
            }
            else if (AmandsControllerSets.ContainsKey(AmandsControllerSet))
            {
                ActiveAmandsControllerSets.Add(AmandsControllerSet);
                LSRSButtonsCheck();
                ControllerSetState(AmandsControllerSet, true);
            }
        }
        public void EnableSet(string AmandsControllerSet)
        {
            if (AmandsControllerSets.ContainsKey(AmandsControllerSet) && !ActiveAmandsControllerSets.Contains(AmandsControllerSet))
            {
                ActiveAmandsControllerSets.Add(AmandsControllerSet);
                LSRSButtonsCheck();
                ControllerSetState(AmandsControllerSet, true);
            }
        }
        public void DisableSet(string AmandsControllerSet)
        {
            ActiveAmandsControllerSets.Remove(AmandsControllerSet);
            LSRSButtonsCheck();
            ControllerSetState(AmandsControllerSet, false);
        }
        private async void ButtonTimer(string Token, EAmandsControllerButton Button)
        {
            AsyncPress.Add(Token);
            AsyncHold.Add(Token);
            await Task.Delay((int)(Interface ? AmandsControllerPlugin.HoldDelay.Value * 800 : AmandsControllerPlugin.HoldDelay.Value * 1000));
            if (AsyncHold.Contains(Token))
            {
                AmandsControllerButton(AmandsControllerButtonSnapshots[Button].HoldBind);
                AsyncHold.Remove(Token);
                AmandsControllerButtonSnapshots.Remove(Button);
            }
            else if (AsyncPress.Contains(Token))
            {
                AmandsControllerButton(AmandsControllerButtonSnapshots[Button].PressBind);
                AsyncPress.Remove(Token);
                AmandsControllerButtonSnapshots.Remove(Button);
            }
        }
        private void LSRSButtonsCheck()
        {
            LSButtons = false;
            RSButtons = false;
            foreach (string ActiveSet in ActiveAmandsControllerSets)
            {
                if (AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSUP) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSDOWN) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSLEFT) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.LSRIGHT)) LSButtons = true;
                if (AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSUP) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSDOWN) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSLEFT) || AmandsControllerSets[ActiveSet].ContainsKey(EAmandsControllerButton.RSRIGHT)) RSButtons = true;
            }
            if (!LSButtons)
            {
                if (LSUP)
                {
                    LSUP = false;
                    GeneratePressType(EAmandsControllerButton.LSUP, false);
                }
                if (LSDOWN)
                {
                    LSDOWN = false;
                    GeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
                if (LSLEFT)
                {
                    LSLEFT = false;
                    GeneratePressType(EAmandsControllerButton.LSLEFT, false);
                }
                if (LSRIGHT)
                {
                    LSRIGHT = false;
                    GeneratePressType(EAmandsControllerButton.LSRIGHT, false);
                }
            }
            if (!RSButtons)
            {
                if (RSUP)
                {
                    RSUP = false;
                    GeneratePressType(EAmandsControllerButton.RSUP, false);
                }
                if (RSDOWN)
                {
                    RSDOWN = false;
                    GeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
                if (RSLEFT)
                {
                    RSLEFT = false;
                    GeneratePressType(EAmandsControllerButton.RSLEFT, false);
                }
                if (RSRIGHT)
                {
                    RSRIGHT = false;
                    GeneratePressType(EAmandsControllerButton.RSRIGHT, false);
                }
            }
        }

        // UI Navigation
        private void ResetAllCurrent()
        {
            currentGridView = null;
            currentModSlotView = null;
            currentTradingTableGridView = null;
            currentContainedGridsView = null;
            currentItemSpecificationPanel = null;
            currentEquipmentSlotView = null;
            currentWeaponsSlotView = null;
            currentArmbandSlotView = null;
            currentContainersSlotView = null;
            currentDogtagSlotView = null;
            currentSpecialSlotSlotView = null;
            currentSearchButton = null;
            currentContextMenuButton = null;
        }
        private bool FindGridView(Vector2 Position)
        {
            RectTransform rectTransform;
            // GridViews Window stuff inside needs to be out
            if (containedGridsViews.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ContainedGridsView bestContainedGridsView = null;
                foreach (ContainedGridsView containedGridsView in containedGridsViews)
                {
                    if (containedGridsView == null || containedGridsView == currentContainedGridsView) continue;
                    rectTransform = containedGridsView.GetComponent<RectTransform>();
                    position = new Vector2(containedGridsView.transform.position.x, containedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = containedGridsView.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestContainedGridsView = containedGridsView;
                        }
                    }
                }
                if (bestContainedGridsView != null)
                {
                    rectTransform = bestContainedGridsView.GetComponent<RectTransform>();
                    int GridWidth;
                    int GridHeight;

                    float distance;
                    float bestDistance = 999999f;

                    GridView bestGridView = null;
                    Vector2Int bestGridViewLocation = Vector2Int.zero;

                    foreach (GridView gridView in bestContainedGridsView.GridViews)
                    {
                        GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }

                        distance = Vector2.Distance(Position, (Vector2)gridView.transform.position + position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestGridView = gridView;
                            bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        }
                    }
                    if (bestGridView != null)
                    {
                        ResetAllCurrent();
                        currentGridView = bestGridView;
                        currentContainedGridsView = bestContainedGridsView;
                        gridViewLocation = bestGridViewLocation;
                        globalPosition.x = bestGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = bestGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // Support SlotViews
            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                if (currentItemSpecificationPanel != null)
                {
                    canvasRenderer = currentItemSpecificationPanel.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    if (itemSpecificationPanel == null || itemSpecificationPanel == currentItemSpecificationPanel) continue;
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    rectTransform = bestItemSpecificationPanel.GetComponent<RectTransform>();

                    float distance;
                    float bestDistance = 999999f;

                    ModSlotView bestModSlotView = null;

                    foreach (ModSlotView modSlotView in Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                    {
                        distance = Vector2.Distance(Position, (Vector2)modSlotView.transform.position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestModSlotView = modSlotView;
                        }
                    }
                    if (bestModSlotView != null)
                    {
                        ResetAllCurrent();
                        currentModSlotView = bestModSlotView;
                        currentItemSpecificationPanel = bestItemSpecificationPanel;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentModSlotView.transform.position.x;
                        globalPosition.y = currentModSlotView.transform.position.y;
                        return true;
                    }
                }
            }

            // find gridviews 0 depth
            if (currentContainedGridsView != null || currentItemSpecificationPanel != null)
            {
                foreach (GridView gridView in gridViews)
                {
                    rectTransform = gridView.GetComponent<RectTransform>();
                    if (Position.x > gridView.transform.position.x && Position.x < (gridView.transform.position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < gridView.transform.position.y && Position.y > (gridView.transform.position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        Vector2 position;
                        int GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        int GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        ResetAllCurrent();
                        currentGridView = gridView;
                        gridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        globalPosition.x = gridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = gridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // find tradingtablegridview 0 depth
            if (currentContainedGridsView != null && tradingTableGridView != null)
            {
                rectTransform = tradingTableGridView.GetComponent<RectTransform>();
                Vector2 size = rectTransform.sizeDelta * ScreenRatio;
                Vector2 position = new Vector2(tradingTableGridView.transform.position.x - (size.x / 2f), tradingTableGridView.transform.position.y + (size.y / 2f));
                if (Position.x > position.x && Position.x < (position.x + size.x) && Position.y < position.y && Position.y > (position.y - size.y))
                {
                    int GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    int GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(Position.x - position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(Position.x - position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    ResetAllCurrent();
                    currentTradingTableGridView = tradingTableGridView;
                    gridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                    globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    return true;
                }
            }

            // find equipmentSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in equipmentSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentEquipmentSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentEquipmentSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find weaponsSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in weaponsSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + (314.1622f * ScreenRatio) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio);
                        globalPosition.y = currentWeaponsSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find armbandSlotView 0 depth
            if (currentContainedGridsView != null)
            {
                if (armbandSlotView != null)
                {
                    if (Position.x > armbandSlotView.transform.position.x && Position.x < armbandSlotView.transform.position.x + SlotSize && Position.y < armbandSlotView.transform.position.y && Position.y > (armbandSlotView.transform.position.y - (64f * ScreenRatio)))
                    {
                        ResetAllCurrent();
                        currentArmbandSlotView = armbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentArmbandSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentArmbandSlotView.transform.position.y - (32f * ScreenRatio);
                        return true;
                    }
                }
            }
            // find containersSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in containersSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentContainersSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentContainersSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }

            // find lootEquipmentSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootEquipmentSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentEquipmentSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentEquipmentSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentEquipmentSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find lootWeaponsSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootWeaponsSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + (314.1622f * ScreenRatio) && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentWeaponsSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio);
                        globalPosition.y = currentWeaponsSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            // find lootArmbandSlotView 0 depth
            if (currentContainedGridsView != null)
            {
                if (lootArmbandSlotView != null)
                {
                    if (Position.x > lootArmbandSlotView.transform.position.x && Position.x < lootArmbandSlotView.transform.position.x + SlotSize && Position.y < lootArmbandSlotView.transform.position.y && Position.y > (lootArmbandSlotView.transform.position.y - (64f * ScreenRatio)))
                    {
                        ResetAllCurrent();
                        currentArmbandSlotView = lootArmbandSlotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentArmbandSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentArmbandSlotView.transform.position.y - (32f * ScreenRatio);
                        return true;
                    }
                }
            }
            // find lootContainersSlotViews 0 depth
            if (currentContainedGridsView != null)
            {
                foreach (SlotView slotView in lootContainersSlotViews)
                {
                    if (Position.x > slotView.transform.position.x && Position.x < slotView.transform.position.x + SlotSize && Position.y < slotView.transform.position.y && Position.y > (slotView.transform.position.y - SlotSize))
                    {
                        ResetAllCurrent();
                        currentContainersSlotView = slotView;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentContainersSlotView.transform.position.x + (SlotSize / 2f);
                        globalPosition.y = currentContainersSlotView.transform.position.y - (SlotSize / 2f);
                        return true;
                    }
                }
            }
            return false;
        }
        private bool FindGridWindow(Vector2 Position)
        {
            RectTransform rectTransform;

            if (containedGridsViews.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ContainedGridsView bestContainedGridsView = null;
                if (currentContainedGridsView != null)
                {
                    canvasRenderer = currentContainedGridsView.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ContainedGridsView containedGridsView in containedGridsViews)
                {
                    if (containedGridsView == null || containedGridsView == currentContainedGridsView) continue;
                    rectTransform = containedGridsView.GetComponent<RectTransform>();
                    position = new Vector2(containedGridsView.transform.position.x, containedGridsView.transform.position.y - (rectTransform.sizeDelta.y * (rectTransform.pivot.y - 1f) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = containedGridsView.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestContainedGridsView = containedGridsView;
                        }
                    }
                }
                if (bestContainedGridsView != null)
                {
                    rectTransform = bestContainedGridsView.GetComponent<RectTransform>();
                    int GridWidth;
                    int GridHeight;

                    float distance;
                    float bestDistance = 999999f;

                    GridView bestGridView = null;
                    Vector2Int bestGridViewLocation = Vector2Int.zero;

                    foreach (GridView gridView in bestContainedGridsView.GridViews)
                    {
                        GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                        GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                        if (GridWidth == 1 && GridHeight == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -GridSize / 2f;
                        }
                        else if (GridWidth == 1)
                        {
                            position.x = GridSize / 2f;
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }
                        else if (GridHeight == 1)
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -GridSize / 2f;
                        }
                        else
                        {
                            position.x = Mathf.Clamp(Position.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                            position.y = -Mathf.Clamp(gridView.transform.position.y - Position.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                        }

                        distance = Vector2.Distance(Position, (Vector2)gridView.transform.position + position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestGridView = gridView;
                            bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                        }
                    }
                    if (bestGridView != null)
                    {
                        ResetAllCurrent();
                        currentGridView = bestGridView;
                        currentContainedGridsView = bestContainedGridsView;
                        gridViewLocation = bestGridViewLocation;
                        globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                        globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                        return true;
                    }
                }
            }
            // Support SlotViews Window

            if (itemSpecificationPanels.Count != 0)
            {
                Vector2 position;
                int bestDepth = -1;
                CanvasRenderer canvasRenderer;
                ItemSpecificationPanel bestItemSpecificationPanel = null;
                if (currentItemSpecificationPanel != null)
                {
                    canvasRenderer = currentItemSpecificationPanel.GetComponent<CanvasRenderer>();
                    bestDepth = canvasRenderer.absoluteDepth;
                }
                foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
                {
                    if (itemSpecificationPanel == null || itemSpecificationPanel == currentItemSpecificationPanel) continue;
                    rectTransform = itemSpecificationPanel.GetComponent<RectTransform>();
                    position = new Vector2(itemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x / 2) * ScreenRatio), itemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y / 2) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform.sizeDelta.x * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.sizeDelta.y * ScreenRatio)))
                    {
                        canvasRenderer = itemSpecificationPanel.GetComponent<CanvasRenderer>();
                        if (canvasRenderer != null && canvasRenderer.absoluteDepth > bestDepth)
                        {
                            bestDepth = canvasRenderer.absoluteDepth;
                            bestItemSpecificationPanel = itemSpecificationPanel;
                        }
                    }
                }
                if (bestItemSpecificationPanel != null)
                {
                    rectTransform = bestItemSpecificationPanel.GetComponent<RectTransform>();

                    float distance;
                    float bestDistance = 999999f;

                    ModSlotView bestModSlotView = null;

                    foreach (ModSlotView modSlotView in Traverse.Create(bestItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                    {
                        distance = Vector2.Distance(Position, (Vector2)modSlotView.transform.position);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestModSlotView = modSlotView;
                        }
                    }
                    if (bestModSlotView != null)
                    {
                        ResetAllCurrent();
                        currentModSlotView = bestModSlotView;
                        currentItemSpecificationPanel = bestItemSpecificationPanel;
                        gridViewLocation = new Vector2Int(1, 1);
                        globalPosition.x = currentModSlotView.transform.position.x;
                        globalPosition.y = currentModSlotView.transform.position.y;
                        return true;
                    }
                }
            }
            return false;
        }
        private bool FindScrollRectNoDrag(Vector2 Position)
        {
            currentScrollRectNoDrag = null;
            currentScrollRectNoDragRectTransform = null;

            RectTransform rectTransform;
            Vector2 position;
            foreach (ScrollRectNoDrag scrollRectNoDrag in scrollRectNoDrags)
            {
                rectTransform = scrollRectNoDrag.GetComponent<RectTransform>();
                position = new Vector2(rectTransform.position.x + (rectTransform.rect.x * ScreenRatio), rectTransform.position.y - ((rectTransform.rect.height * (rectTransform.pivot.y - 1f)) * ScreenRatio));
                if (Position.x > position.x && Position.x < (position.x + (rectTransform.rect.width * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform.rect.height * ScreenRatio)))
                {
                    currentScrollRectNoDrag = scrollRectNoDrag;
                    currentScrollRectNoDragRectTransform = rectTransform;
                    return true;
                }
                else
                {
                    RectTransform rectTransform2 = scrollRectNoDrag.content;
                    position = new Vector2(rectTransform2.position.x + (rectTransform2.rect.x * ScreenRatio), rectTransform2.position.y - ((rectTransform2.rect.height * (rectTransform2.pivot.y - 1f)) * ScreenRatio));
                    if (Position.x > position.x && Position.x < (position.x + (rectTransform2.rect.width * ScreenRatio)) && Position.y < position.y && Position.y > (position.y - (rectTransform2.rect.height * ScreenRatio)))
                    {
                        currentScrollRectNoDrag = scrollRectNoDrag;
                        currentScrollRectNoDragRectTransform = rectTransform;
                        return true;
                    }
                }
            }
            return false;
        }
        public void ControllerUIMove(Vector2Int direction, bool Skip)
        {
            lastDirection = direction;

            DisableSet("SearchButton");

            if (SearchButtonImage != null)
            {
                SearchButtonImage.color = Color.white;
                SearchButtonImage = null;
            }

            ScreenRatio = (Screen.height / 1080f);
            GridSize = 63f * ScreenRatio;
            ModSize = 63f * ScreenRatio;
            SlotSize = 125f * ScreenRatio;

            Vector2 position;

            int GridWidth = 1;
            int GridHeight = 1;

            float dot;
            float distance;
            float score;
            float bestScore = 99999f;

            GridView bestGridView = null;
            ModSlotView bestModSlotView = null;
            SlotView bestEquipmentSlotView = null;
            SlotView bestWeaponsSlotView = null;
            SlotView bestArmbandSlotView = null;
            SlotView bestContainersSlotView = null;
            SlotView bestDogtagSlotView = null;
            SlotView bestSpecialSlotSlotView = null;
            ItemSpecificationPanel bestItemSpecificationPanel = null;
            TradingTableGridView bestTradingTableGridView = null;
            ContainedGridsView bestContainedGridsView = null;
            SearchButton bestSearchButton = null;
            ContextMenuButton bestContextMenuButton = null;
            Vector2Int bestGridViewLocation = Vector2Int.one;

            // Exclusive SimpleContextMenuButton Blind Search
            if (contextMenuButtons.Count > 0)
            {
                UpdateGlobalPosition();
                foreach (ContextMenuButton contextMenuButton in contextMenuButtons)
                {
                    if (contextMenuButton == null || contextMenuButton == currentContextMenuButton) continue;

                    position.x = contextMenuButton.transform.position.x;
                    position.y = contextMenuButton.transform.position.y;

                    dot = direction == Vector2Int.zero ? 1f : Vector2.Dot((globalPosition - position).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestContextMenuButton = contextMenuButton;
                    }
                }
                if (bestContextMenuButton == null) return;
                if ((currentContextMenuButton != null && currentContextMenuButton.gameObject.activeSelf))
                {
                    currentContextMenuButton.OnPointerExit(null);
                }
                currentContextMenuButton = bestContextMenuButton;
                UpdateGlobalPosition();
                currentContextMenuButton.OnPointerEnter(null);
                return;
            }

            if ((currentGridView == null || !currentGridView.gameObject.activeSelf) && (currentTradingTableGridView == null || !currentTradingTableGridView.gameObject.activeSelf) && (currentEquipmentSlotView == null || !currentEquipmentSlotView.gameObject.activeSelf) && (currentWeaponsSlotView == null || !currentWeaponsSlotView.gameObject.activeSelf) && (currentArmbandSlotView == null || !currentArmbandSlotView.gameObject.activeSelf) && (currentContainersSlotView == null || !currentContainersSlotView.gameObject.activeSelf) && (currentDogtagSlotView == null || !currentDogtagSlotView.gameObject.activeSelf) && (currentSpecialSlotSlotView == null || !currentSpecialSlotSlotView.gameObject.activeSelf) && (currentModSlotView == null || !currentModSlotView.gameObject.activeSelf) && (currentSearchButton == null || !currentSearchButton.gameObject.activeSelf))
            {
                ControllerUIMoveToClosest(false);
                return;
            }

            if (Skip) goto Skip1;

            // Local GridView Search
            if ((currentGridView != null && currentGridView.gameObject.activeSelf))
            {
                GridWidth = Traverse.Create(Traverse.Create(currentGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(currentGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
            }
            if ((currentGridView != null && currentGridView.gameObject.activeSelf) && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= GridWidth && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= GridHeight)
            {
                gridViewLocation.x += direction.x;
                gridViewLocation.y -= direction.y;

                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                FindGridWindow(globalPosition);
                ControllerUIOnMove(direction, globalPosition);
                return;
            }

            // Local TradingTableGridView Search
            if ((currentTradingTableGridView != null && currentTradingTableGridView.gameObject.activeSelf))
            {
                GridWidth = Traverse.Create(Traverse.Create(currentTradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(currentTradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;
            }
            if ((currentTradingTableGridView != null && currentTradingTableGridView.gameObject.activeSelf) && gridViewLocation.x + direction.x >= 1 && gridViewLocation.x + direction.x <= GridWidth && gridViewLocation.y - direction.y >= 1 && gridViewLocation.y - direction.y <= GridHeight)
            {
                gridViewLocation.x += direction.x;
                gridViewLocation.y -= direction.y;

                Vector2 size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                FindGridWindow(globalPosition);
                ControllerUIOnMove(direction, globalPosition);
                return;
            }

            // Local ContainedGridsView GridView Blind Search
            if ((currentGridView != null && currentGridView.gameObject.activeSelf) && currentContainedGridsView != null)
            {
                globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);

                foreach (GridView gridView in currentContainedGridsView.GridViews)
                {
                    if (gridView == null || gridView == currentGridView) continue;

                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }

                    dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = gridView;
                        bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    }
                }

                if (bestGridView != null)
                {
                    bestContainedGridsView = currentContainedGridsView;
                    ResetAllCurrent();
                    currentGridView = bestGridView;
                    currentContainedGridsView = bestContainedGridsView;
                    gridViewLocation = bestGridViewLocation;

                    globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    FindGridWindow(globalPosition);
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
                Vector2 point = globalPosition + ((Vector2)direction * 1000f);
                RectTransform rectTransform = currentContainedGridsView.GetComponent<RectTransform>();
                position.x = currentContainedGridsView.transform.position.x;
                position.y = currentContainedGridsView.transform.position.y - ((rectTransform.sizeDelta.y * ScreenRatio) * (rectTransform.pivot.y - 1f));
                if (FindGridView(new Vector2(position.x + Mathf.Clamp(point.x - position.x, 0, rectTransform.sizeDelta.x * ScreenRatio) + (direction.x * (ModSize / 2)), position.y - Mathf.Clamp(position.y - point.y, 0, rectTransform.sizeDelta.y * ScreenRatio) + (direction.y * (ModSize / 2)))))
                {
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
            }

            // Local ItemSpecificationPanel ModSlotView Blind Search
            if ((currentModSlotView != null && currentModSlotView.gameObject.activeSelf) && currentItemSpecificationPanel != null)
            {
                globalPosition.x = currentModSlotView.transform.position.x;
                globalPosition.y = currentModSlotView.transform.position.y;

                foreach (ModSlotView modSlotView in Traverse.Create(currentItemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                {
                    if (modSlotView == null || modSlotView == currentModSlotView) continue;

                    dot = Vector2.Dot((globalPosition - ((Vector2)modSlotView.transform.position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)modSlotView.transform.position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestModSlotView = modSlotView;
                    }
                }

                if (bestModSlotView != null)
                {
                    bestItemSpecificationPanel = currentItemSpecificationPanel;
                    ResetAllCurrent();
                    currentModSlotView = bestModSlotView;
                    currentItemSpecificationPanel = bestItemSpecificationPanel;
                    gridViewLocation = new Vector2Int(1, 1);

                    globalPosition.x = currentModSlotView.transform.position.x;
                    globalPosition.y = currentModSlotView.transform.position.y;
                    FindGridWindow(globalPosition);
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }
                Vector2 point = globalPosition + ((Vector2)direction * 1000f);
                RectTransform rectTransform = currentItemSpecificationPanel.GetComponent<RectTransform>();
                position.x = currentItemSpecificationPanel.transform.position.x - ((rectTransform.sizeDelta.x * ScreenRatio) / 2);
                position.y = currentItemSpecificationPanel.transform.position.y + ((rectTransform.sizeDelta.y * ScreenRatio) / 2);
                if (FindGridView(new Vector2(position.x + Mathf.Clamp(point.x - position.x, 0, rectTransform.sizeDelta.x * ScreenRatio) + (direction.x * (ModSize / 2)), position.y - Mathf.Clamp(position.y - point.y, 0, rectTransform.sizeDelta.y * ScreenRatio) + (direction.y * (ModSize / 2)))))
                {
                    ControllerUIOnMove(direction, globalPosition);
                    return;
                }


            }

            Skip1:

            // GlobalPosition
            UpdateGlobalPosition();
            // Global Blind Search

            if (Skip) goto Skip2;

            // GridView Blind Search
            foreach (GridView gridView in gridViews)
            {
                if (gridView == null || gridView == currentGridView) continue;

                GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = gridView;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ContainedGridsView GridView Blind Search
            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                if (containedGridsView == null || containedGridsView == currentContainedGridsView) continue;
                foreach (GridView gridView in containedGridsView.GridViews)
                {
                    if (gridView == null || gridView == currentGridView) continue;

                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }

                    dot = Vector2.Dot((globalPosition - ((Vector2)gridView.transform.position + position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = gridView;
                        bestModSlotView = null;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = null;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = containedGridsView;
                        bestSearchButton = null;
                        bestContextMenuButton = null;
                        bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    }
                }
            }
            // TradingTableGridView Blind Search
            if (tradingTableGridView != null)
            {
                Vector2 size = tradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                Vector2 positionTradingTableGridView = new Vector2(tradingTableGridView.transform.position.x - (size.x / 2f), tradingTableGridView.transform.position.y + (size.y / 2f));

                GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                position.x = Mathf.Clamp(globalPosition.x - positionTradingTableGridView.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                position.y = -Mathf.Clamp(positionTradingTableGridView.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));

                dot = Vector2.Dot((globalPosition - (positionTradingTableGridView + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, positionTradingTableGridView + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = tradingTableGridView;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestContextMenuButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ItemSpecificationPanel ModSlotView Blind Search
            foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
            {
                if (itemSpecificationPanel == null || itemSpecificationPanel == currentItemSpecificationPanel) continue;
                foreach (ModSlotView modSlotView in Traverse.Create(itemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                {
                    if (modSlotView == null || modSlotView == currentModSlotView) continue;

                    dot = Vector2.Dot((globalPosition - ((Vector2)modSlotView.transform.position)).normalized, -direction);
                    distance = Vector2.Distance(globalPosition, (Vector2)modSlotView.transform.position);
                    score = Mathf.Lerp(distance, distance * 0.25f, dot);

                    if (score < bestScore && dot > 0.4f)
                    {
                        bestScore = score;
                        bestGridView = null;
                        bestModSlotView = modSlotView;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = itemSpecificationPanel;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = null;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(1, 1);
                    }
                }
            }
            // SearchButton Blind Search
            foreach (SearchButton searchButton in searchButtons)
            {
                if (searchButton == null || searchButton == currentSearchButton) continue;
                Vector2 size = searchButton.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                position.x = searchButton.transform.position.x;// - (size.x / 2f);
                position.y = searchButton.transform.position.y;// - (size.y / 2f);

                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = searchButton;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // SpecialSlot Blind Search
            foreach (SlotView slotView in specialSlotSlotViews)
            {
                if (slotView == null || slotView == currentSpecialSlotSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (GridSize / 2f), slotView.transform.position.y - (GridSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = slotView;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }

        Skip2:

            // EquipmentSlotView Blind Search
            foreach (SlotView slotView in equipmentSlotViews)
            {
                if (slotView == null || slotView == currentEquipmentSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // WeaponsSlotView Blind Search
            foreach (SlotView slotView in weaponsSlotViews)
            {
                if (slotView == null || slotView == currentWeaponsSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ArmbandSlotView Blind Search
            if (armbandSlotView != null && armbandSlotView != currentArmbandSlotView)
            {
                position = new Vector2(armbandSlotView.transform.position.x + (SlotSize / 2f), armbandSlotView.transform.position.y - (32f * ScreenRatio));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = armbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ContainersSlotView Blind Search
            foreach (SlotView slotView in containersSlotViews)
            {
                if (slotView == null || slotView == currentContainersSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootEquipmentSlotView Blind Search
            foreach (SlotView slotView in lootEquipmentSlotViews)
            {
                if (slotView == null || slotView == currentEquipmentSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootWeaponsSlotView Blind Search
            foreach (SlotView slotView in lootWeaponsSlotViews)
            {
                if (slotView == null || slotView == currentWeaponsSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootArmbandSlotView Blind Search
            if (lootArmbandSlotView != null && lootArmbandSlotView != currentArmbandSlotView)
            {
                position = new Vector2(lootArmbandSlotView.transform.position.x + (SlotSize / 2f), lootArmbandSlotView.transform.position.y - (32f * ScreenRatio));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = lootArmbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootContainersSlotView Blind Search
            foreach (SlotView slotView in lootContainersSlotViews)
            {
                if (slotView == null || slotView == currentContainersSlotView) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Dogtag Blind Search
            if (dogtagSlotView != null)
            {
                position.x = dogtagSlotView.transform.position.x - GridSize - (GridSize / 2f);
                position.y = dogtagSlotView.transform.position.y - (GridSize / 2f);

                dot = Vector2.Dot((globalPosition - position).normalized, -direction);
                distance = Vector2.Distance(globalPosition, position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = dogtagSlotView;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Skip Include SimpleStash
            if (Skip && SimpleStashGridView != null && SimpleStashGridView != currentGridView)
            {
                GridWidth = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                dot = Vector2.Dot((globalPosition - ((Vector2)SimpleStashGridView.transform.position + position)).normalized, -direction);
                distance = Vector2.Distance(globalPosition, (Vector2)SimpleStashGridView.transform.position + position);
                score = Mathf.Lerp(distance, distance * 0.25f, dot);

                if (score < bestScore && dot > 0.4f)
                {
                    bestScore = score;
                    bestGridView = SimpleStashGridView;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // Support

            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null && Skip)
            {
                ControllerUIMove(direction, false);
                return;
            }
            // Set GridView/SlotView
            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null) return;

            currentGridView = bestGridView;
            currentModSlotView = bestModSlotView;
            currentTradingTableGridView = bestTradingTableGridView;
            currentContainedGridsView = bestContainedGridsView;
            currentItemSpecificationPanel = bestItemSpecificationPanel;
            currentEquipmentSlotView = bestEquipmentSlotView;
            currentWeaponsSlotView = bestWeaponsSlotView;
            currentArmbandSlotView = bestArmbandSlotView;
            currentContainersSlotView = bestContainersSlotView;
            currentDogtagSlotView = bestDogtagSlotView;
            currentSpecialSlotSlotView = bestSpecialSlotSlotView;
            currentSearchButton = bestSearchButton;
            gridViewLocation = bestGridViewLocation;

            if ((currentSearchButton != null && currentSearchButton.gameObject.activeSelf))
            {
                EnableSet("SearchButton");
                SearchButtonImage = currentSearchButton.GetComponent<Image>();
                if (SearchButtonImage != null)
                {
                    SearchButtonImage.color = Color.red;
                }
            }

            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(direction, globalPosition);
        }
        public void ControllerUIMoveToClosest(bool Skip)
        {
            DisableSet("SearchButton");

            if (SearchButtonImage != null)
            {
                SearchButtonImage.color = Color.white;
                SearchButtonImage = null;
            }

            ScreenRatio = (Screen.height / 1080f);
            GridSize = 63f * ScreenRatio;
            ModSize = 63f * ScreenRatio;
            SlotSize = 125f * ScreenRatio;

            Vector2 position;

            int GridWidth = 1;
            int GridHeight = 1;

            float distance;
            float bestScore = 99999f;

            GridView bestGridView = null;
            ModSlotView bestModSlotView = null;
            SlotView bestEquipmentSlotView = null;
            SlotView bestWeaponsSlotView = null;
            SlotView bestArmbandSlotView = null;
            SlotView bestContainersSlotView = null;
            SlotView bestDogtagSlotView = null;
            SlotView bestSpecialSlotSlotView = null;
            ItemSpecificationPanel bestItemSpecificationPanel = null;
            TradingTableGridView bestTradingTableGridView = null;
            ContainedGridsView bestContainedGridsView = null;
            SearchButton bestSearchButton = null;
            Vector2Int bestGridViewLocation = Vector2Int.one;

            // GlobalPosition
            UpdateGlobalPosition();
            // Global Blind Search

            if (Skip) goto Skip2;

            // GridView Blind Search
            foreach (GridView gridView in gridViews)
            {
                if (gridView == null) continue;

                GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = gridView;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ContainedGridsView GridView Blind Search
            foreach (ContainedGridsView containedGridsView in containedGridsViews)
            {
                if (containedGridsView == null) continue;
                foreach (GridView gridView in containedGridsView.GridViews)
                {
                    if (gridView == null) continue;

                    GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                    GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                    if (GridWidth == 1 && GridHeight == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -GridSize / 2f;
                    }
                    else if (GridWidth == 1)
                    {
                        position.x = GridSize / 2f;
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }
                    else if (GridHeight == 1)
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -GridSize / 2f;
                    }
                    else
                    {
                        position.x = Mathf.Clamp(globalPosition.x - gridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                        position.y = -Mathf.Clamp(gridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                    }

                    distance = Vector2.Distance(globalPosition, (Vector2)gridView.transform.position + position);

                    if (distance < bestScore)
                    {
                        bestScore = distance;
                        bestGridView = gridView;
                        bestModSlotView = null;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = null;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = containedGridsView;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                    }
                }
            }
            // TradingTableGridView Blind Search
            if (tradingTableGridView != null)
            {
                Vector2 size = tradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                Vector2 positionTradingTableGridView = new Vector2(tradingTableGridView.transform.position.x - (size.x / 2f), tradingTableGridView.transform.position.y + (size.y / 2f));

                GridWidth = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(tradingTableGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                position.x = Mathf.Clamp(globalPosition.x - positionTradingTableGridView.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                position.y = -Mathf.Clamp(positionTradingTableGridView.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));

                distance = Vector2.Distance(globalPosition, positionTradingTableGridView + position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = tradingTableGridView;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // ItemSpecificationPanel ModSlotView Blind Search
            foreach (ItemSpecificationPanel itemSpecificationPanel in itemSpecificationPanels)
            {
                if (itemSpecificationPanel == null) continue;
                foreach (ModSlotView modSlotView in Traverse.Create(itemSpecificationPanel).Field("_modsContainer").GetValue<RectTransform>().GetComponentsInChildren<ModSlotView>())
                {
                    if (modSlotView == null) continue;

                    distance = Vector2.Distance(globalPosition, (Vector2)modSlotView.transform.position);

                    if (distance < bestScore)
                    {
                        bestScore = distance;
                        bestGridView = null;
                        bestModSlotView = modSlotView;
                        bestEquipmentSlotView = null;
                        bestWeaponsSlotView = null;
                        bestArmbandSlotView = null;
                        bestContainersSlotView = null;
                        bestDogtagSlotView = null;
                        bestSpecialSlotSlotView = null;
                        bestItemSpecificationPanel = itemSpecificationPanel;
                        bestTradingTableGridView = null;
                        bestContainedGridsView = null;
                        bestSearchButton = null;
                        bestGridViewLocation = new Vector2Int(1, 1);
                    }
                }
            }
            // SearchButton Blind Search
            foreach (SearchButton searchButton in searchButtons)
            {
                if (searchButton == null) continue;
                Vector2 size = searchButton.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                position.x = searchButton.transform.position.x;// - (size.x / 2f);
                position.y = searchButton.transform.position.y;// - (size.y / 2f);

                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = searchButton;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // SpecialSlot Blind Search
            foreach (SlotView slotView in specialSlotSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (GridSize / 2f), slotView.transform.position.y - (GridSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = slotView;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }

        Skip2:

            // EquipmentSlotView Blind Search
            foreach (SlotView slotView in equipmentSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // WeaponsSlotView Blind Search
            foreach (SlotView slotView in weaponsSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ArmbandSlotView Blind Search
            if (armbandSlotView != null)
            {
                position = new Vector2(armbandSlotView.transform.position.x + (SlotSize / 2f), armbandSlotView.transform.position.y - (32f * ScreenRatio));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = armbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // ContainersSlotView Blind Search
            foreach (SlotView slotView in containersSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootEquipmentSlotView Blind Search
            foreach (SlotView slotView in lootEquipmentSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = slotView;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootWeaponsSlotView Blind Search
            foreach (SlotView slotView in lootWeaponsSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (157.0811f * ScreenRatio), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = slotView;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootArmbandSlotView Blind Search
            if (lootArmbandSlotView != null)
            {
                position = new Vector2(lootArmbandSlotView.transform.position.x + (SlotSize / 2f), lootArmbandSlotView.transform.position.y - (32f * ScreenRatio));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = lootArmbandSlotView;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // LootContainersSlotView Blind Search
            foreach (SlotView slotView in lootContainersSlotViews)
            {
                if (slotView == null) continue;
                position = new Vector2(slotView.transform.position.x + (SlotSize / 2f), slotView.transform.position.y - (SlotSize / 2f));
                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = slotView;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Dogtag Blind Search
            if (dogtagSlotView != null)
            {
                position.x = dogtagSlotView.transform.position.x - GridSize - (GridSize / 2f);
                position.y = dogtagSlotView.transform.position.y - (GridSize / 2f);

                distance = Vector2.Distance(globalPosition, position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = null;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = dogtagSlotView;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(1, 1);
                }
            }
            // Skip Include SimpleStash
            if (Skip && SimpleStashGridView != null)
            {
                GridWidth = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
                GridHeight = Traverse.Create(Traverse.Create(SimpleStashGridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

                if (GridWidth == 1 && GridHeight == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -GridSize / 2f;
                }
                else if (GridWidth == 1)
                {
                    position.x = GridSize / 2f;
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }
                else if (GridHeight == 1)
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -GridSize / 2f;
                }
                else
                {
                    position.x = Mathf.Clamp(globalPosition.x - SimpleStashGridView.transform.position.x, GridSize / 2f, (GridSize * GridWidth) - (GridSize / 2f));
                    position.y = -Mathf.Clamp(SimpleStashGridView.transform.position.y - globalPosition.y, GridSize / 2f, (GridSize * GridHeight) - (GridSize / 2f));
                }

                distance = Vector2.Distance(globalPosition, (Vector2)SimpleStashGridView.transform.position + position);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestGridView = SimpleStashGridView;
                    bestModSlotView = null;
                    bestEquipmentSlotView = null;
                    bestWeaponsSlotView = null;
                    bestArmbandSlotView = null;
                    bestContainersSlotView = null;
                    bestDogtagSlotView = null;
                    bestSpecialSlotSlotView = null;
                    bestItemSpecificationPanel = null;
                    bestTradingTableGridView = null;
                    bestContainedGridsView = null;
                    bestSearchButton = null;
                    bestGridViewLocation = new Vector2Int(Mathf.RoundToInt((position.x + (GridSize / 2f)) / GridSize), -Mathf.RoundToInt((position.y - (GridSize / 2f)) / GridSize));
                }
            }
            // Support

            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null && Skip)
            {
                ControllerUIMoveToClosest(false);
                return;
            }
            // Set GridView/SlotView
            if (bestGridView == null && bestTradingTableGridView == null && bestEquipmentSlotView == null && bestWeaponsSlotView == null && bestArmbandSlotView == null && bestContainersSlotView == null && bestDogtagSlotView == null && bestSpecialSlotSlotView == null && bestModSlotView == null && bestSearchButton == null) return;

            currentGridView = bestGridView;
            currentModSlotView = bestModSlotView;
            currentTradingTableGridView = bestTradingTableGridView;
            currentContainedGridsView = bestContainedGridsView;
            currentItemSpecificationPanel = bestItemSpecificationPanel;
            currentEquipmentSlotView = bestEquipmentSlotView;
            currentWeaponsSlotView = bestWeaponsSlotView;
            currentArmbandSlotView = bestArmbandSlotView;
            currentContainersSlotView = bestContainersSlotView;
            currentDogtagSlotView = bestDogtagSlotView;
            currentSpecialSlotSlotView = bestSpecialSlotSlotView;
            currentSearchButton = bestSearchButton;
            gridViewLocation = bestGridViewLocation;

            if ((currentSearchButton != null && currentSearchButton.gameObject.activeSelf))
            {
                EnableSet("SearchButton");
                SearchButtonImage = currentSearchButton.GetComponent<Image>();
                if (SearchButtonImage != null)
                {
                    SearchButtonImage.color = Color.red;
                }
            }

            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUIMoveSnapshot()
        {
            snapshotGridView = currentGridView;
            snapshotModSlotView = currentModSlotView;
            snapshotTradingTableGridView = currentTradingTableGridView;
            snapshotContainedGridsView = currentContainedGridsView;
            snapshotItemSpecificationPanel = currentItemSpecificationPanel;
            snapshotEquipmentSlotView = currentEquipmentSlotView;
            snapshotWeaponsSlotView = currentWeaponsSlotView;
            snapshotArmbandSlotView = currentArmbandSlotView;
            snapshotContainersSlotView = currentContainersSlotView;
            snapshotDogtagSlotView = currentDogtagSlotView;
            snapshotSpecialSlotSlotView = currentSpecialSlotSlotView;
            snapshotSearchButton = currentSearchButton;
            SnapshotGridViewLocation = gridViewLocation;
        }
        public void ControllerUIMoveToSnapshot()
        {
            currentGridView = snapshotGridView;
            currentModSlotView = snapshotModSlotView;
            currentTradingTableGridView = snapshotTradingTableGridView;
            currentContainedGridsView = snapshotContainedGridsView;
            currentItemSpecificationPanel = snapshotItemSpecificationPanel;
            currentEquipmentSlotView = snapshotEquipmentSlotView;
            currentWeaponsSlotView = snapshotWeaponsSlotView;
            currentArmbandSlotView = snapshotArmbandSlotView;
            currentContainersSlotView = snapshotContainersSlotView;
            currentDogtagSlotView = snapshotDogtagSlotView;
            currentSpecialSlotSlotView = snapshotSpecialSlotSlotView;
            currentSearchButton = snapshotSearchButton;
            gridViewLocation = SnapshotGridViewLocation;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            // OnMove
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        private void ControllerUIOnMove(Vector2Int direction, Vector2 position)
        {
            FindScrollRectNoDrag(position);
            ControllerOnPointerMove();
            ControllerOnDrag();
            UpdateSelector();
        }
        public void UpdateGlobalPosition()
        {
            switch (GetControllerCurrentUI())
            {
                case EAmandsControllerCurrentUI.GridView:
                    globalPosition.x = currentGridView.transform.position.x + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = currentGridView.transform.position.y - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    //globalSize = new Vector2(GridSize, GridSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.TradingTableGridView:
                    Vector2 size = currentTradingTableGridView.GetComponent<RectTransform>().sizeDelta * ScreenRatio;
                    globalPosition.x = (currentTradingTableGridView.transform.position.x - (size.x / 2f)) + (GridSize * gridViewLocation.x) - (GridSize / 2f);
                    globalPosition.y = (currentTradingTableGridView.transform.position.y + (size.y / 2f)) - (GridSize * gridViewLocation.y) + (GridSize / 2f);
                    //globalSize = new Vector2(GridSize, GridSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.ModSlotView:
                    globalPosition = currentModSlotView.transform.position;
                    //globalSize = new Vector2(GridSize, GridSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.EquipmentSlotView:
                    globalPosition = new Vector2(currentEquipmentSlotView.transform.position.x + (SlotSize / 2f), currentEquipmentSlotView.transform.position.y - (SlotSize / 2f));
                    //globalSize = new Vector2(SlotSize, SlotSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.WeaponsSlotView:
                    globalPosition = new Vector2(currentWeaponsSlotView.transform.position.x + (157.0811f * ScreenRatio) - 2f, currentWeaponsSlotView.transform.position.y - (SlotSize / 2f));
                    //globalSize = new Vector2(157.0811f * ScreenRatio * 2f, SlotSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.ArmbandSlotView:
                    globalPosition = new Vector2(currentArmbandSlotView.transform.position.x + (SlotSize / 2f), currentArmbandSlotView.transform.position.y - (32f * ScreenRatio));
                    //globalSize = new Vector2(SlotSize, 32f * ScreenRatio * 2f) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.ContainersSlotView:
                    globalPosition = new Vector2(currentContainersSlotView.transform.position.x + (SlotSize / 2f), currentContainersSlotView.transform.position.y - (SlotSize / 2f));
                    //globalSize = new Vector2(SlotSize, SlotSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.DogtagSlotView:
                    globalPosition = new Vector2(currentDogtagSlotView.transform.position.x - GridSize - (GridSize / 2f), currentDogtagSlotView.transform.position.y - (GridSize / 2f));
                    //globalSize = new Vector2(GridSize, GridSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.SpecialSlotSlotView:
                    globalPosition.x = currentSpecialSlotSlotView.transform.position.x + (GridSize / 2f);
                    globalPosition.y = currentSpecialSlotSlotView.transform.position.y - (GridSize / 2f);
                    //globalSize = new Vector2(GridSize, GridSize) * ScreenRatio;
                    break;
                case EAmandsControllerCurrentUI.SearchButton:
                    globalPosition.x = currentSearchButton.transform.position.x;
                    globalPosition.y = currentSearchButton.transform.position.y;
                    //globalSize = Vector2.zero;
                    break;
                case EAmandsControllerCurrentUI.ContextMenuButton:
                    globalPosition.x = currentContextMenuButton.transform.position.x;
                    globalPosition.y = currentContextMenuButton.transform.position.y;
                    //globalSize = Vector2.zero;
                    break;
            }
        }
        public EAmandsControllerCurrentUI GetControllerCurrentUI()
        {
            if ((currentContextMenuButton != null && currentContextMenuButton.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.ContextMenuButton;
            }
            else if ((currentGridView != null && currentGridView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.GridView;
            }
            else if ((currentTradingTableGridView != null && currentTradingTableGridView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.TradingTableGridView;
            }
            else if ((currentModSlotView != null && currentModSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.ModSlotView;
            }
            else if ((currentEquipmentSlotView != null && currentEquipmentSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.EquipmentSlotView;
            }
            else if ((currentWeaponsSlotView != null && currentWeaponsSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.WeaponsSlotView;
            }
            else if ((currentArmbandSlotView != null && currentArmbandSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.ArmbandSlotView;
            }
            else if ((currentContainersSlotView != null && currentContainersSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.ContainersSlotView;
            }
            else if ((currentDogtagSlotView != null && currentDogtagSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.DogtagSlotView;
            }
            else if ((currentSpecialSlotSlotView != null && currentSpecialSlotSlotView.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.SpecialSlotSlotView;
            }
            else if ((currentSearchButton != null && currentSearchButton.gameObject.activeSelf))
            {
                return EAmandsControllerCurrentUI.SearchButton;
            }
            return EAmandsControllerCurrentUI.None;
        }
        public void ControllerUISelect()
        {
            ResetAllCurrent();
            gridViewLocation = Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(GridView gridView, ItemView itemView)
        {
            ResetAllCurrent();
            currentGridView = gridView;
            gridViewLocation = CalculateItemLocation(gridView, itemView) + Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(GridView gridView)
        {
            ResetAllCurrent();
            currentGridView = gridView;
            gridViewLocation = Vector2Int.one;
            UpdateGlobalPosition();
            FindGridWindow(globalPosition);
            ControllerUIOnMove(Vector2Int.zero, globalPosition);
        }
        public void ControllerUISelect(ContextMenuButton contextMenuButton)
        {
            if ((currentContextMenuButton != null && currentContextMenuButton.gameObject.activeSelf))
            {
                currentContextMenuButton.OnPointerExit(null);
            }
            currentContextMenuButton = contextMenuButton;
            currentContextMenuButton.OnPointerEnter(null);
        }
        public Vector2Int CalculateItemLocation(GridView gridView, ItemView itemView)
        {
            int GridWidth = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridWidth").GetValue<IBindable<int>>().Value;
            int GridHeight = Traverse.Create(Traverse.Create(gridView).Field("Grid").GetValue<object>()).Property("GridHeight").GetValue<IBindable<int>>().Value;

            RectTransform rectTransform = gridView.transform.GetComponent<RectTransform>();
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            Vector2 b = size * pivot;
            Vector2 vector = rectTransform.InverseTransformPoint((Vector2)itemView.transform.position + new Vector2(0f,-64f));
            vector += b;

            object gstruct23 = CalculateRotatedSize.Invoke(itemView.Item, new object[1] { itemView.ItemRotation });

            vector /= 63f;
            vector.y = (float)GridHeight - vector.y;

            vector.y -= (float)Traverse.Create(gstruct23).Field("Y").GetValue<int>();

            return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(vector.x), 0, GridWidth), Mathf.Clamp(Mathf.RoundToInt(vector.y), 0, GridHeight));
        }
        public void UpdateSelector()
        {
            if (SelectedGameObject == null && InRaid)
            {
                SelectedGameObject = new GameObject("SelectorGameObject");
                if (SelectedGameObject != null)
                {
                    SelectedRectTransform = SelectedGameObject.AddComponent<RectTransform>();
                    if (SelectedRectTransform != null)
                    {
                        SelectedRectTransform.sizeDelta = new Vector2(GridSize, GridSize);
                        SelectedImage = SelectedGameObject.AddComponent<Image>();
                        if (SelectedImage != null)
                        {
                            SelectedImage.sprite = LoadedSprites["Grid.png"];
                            SelectedImage.raycastTarget = false;
                            SelectedImage.color = AmandsControllerPlugin.SelectColor.Value;
                        }
                        SelectedLayoutElement = SelectedGameObject.AddComponent<LayoutElement>();
                        if (SelectedLayoutElement != null)
                        {
                            SelectedLayoutElement.ignoreLayout = true;
                        }
                    }
                }
            }
            else if (SelectedGameObject != null && !InRaid)
            {
                Destroy(SelectedGameObject);
            }
            if (SelectedGameObject != null && SelectedRectTransform != null && SelectedImage != null)
            {
                SelectedGameObject.SetActive(false);
                SelectedGameObject.transform.SetParent(null);

                if ((currentGridView != null && currentGridView.gameObject.activeSelf) && currentGridView.gameObject.activeSelf)
                {
                    SelectedImage.sprite = LoadedSprites["Grid.png"];
                    globalSize = new Vector2(GridSize, GridSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentGridView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentTradingTableGridView != null && currentTradingTableGridView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Grid.png"];
                    globalSize = new Vector2(GridSize, GridSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(tradingTableGridView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentModSlotView != null && currentModSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Grid.png"];
                    globalSize = new Vector2(GridSize, GridSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentModSlotView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentEquipmentSlotView != null && currentEquipmentSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Slot.png"];
                    globalSize = new Vector2(SlotSize, SlotSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    RectTransform slotPlace = Traverse.Create(currentEquipmentSlotView).Field("_slotPlace").GetValue<RectTransform>();
                    SelectedRectTransform.SetParent(slotPlace);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentWeaponsSlotView != null && currentWeaponsSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["WeaponSlot.png"];
                    globalSize = new Vector2(157.0811f * ScreenRatio * 2f, SlotSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentWeaponsSlotView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentArmbandSlotView != null && currentArmbandSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["ArmbandSlot.png"];
                    globalSize = new Vector2(SlotSize, 32f * ScreenRatio * 2f);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentArmbandSlotView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentContainersSlotView != null && currentContainersSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Slot.png"];
                    globalSize = new Vector2(SlotSize, SlotSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    RectTransform slotPlace = Traverse.Create(currentContainersSlotView).Field("_slotPlace").GetValue<RectTransform>();
                    SelectedRectTransform.SetParent(slotPlace);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentDogtagSlotView != null && currentDogtagSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Grid.png"];
                    globalSize = new Vector2(GridSize, GridSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentDogtagSlotView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
                else if ((currentSpecialSlotSlotView != null && currentSpecialSlotSlotView.gameObject.activeSelf))
                {
                    SelectedImage.sprite = LoadedSprites["Grid.png"];
                    globalSize = new Vector2(GridSize, GridSize);// * ScreenRatio;
                    SelectedGameObject.SetActive(true);
                    SelectedRectTransform.SetParent(currentSpecialSlotSlotView.transform);
                    SelectedRectTransform.sizeDelta = globalSize;
                    SelectedRectTransform.localPosition = Vector2.zero;
                    SelectedRectTransform.position = globalPosition;
                }
            }
        }
        public InventoryScreen.EInventoryTab CurrentTab()
        {
            if (Tabs != null)
            {
                foreach (KeyValuePair<InventoryScreen.EInventoryTab, Tab> Tab in Tabs)
                {
                    if (Traverse.Create(Tab.Value).Field("_uiSelected").GetValue<bool>()) return Tab.Key;
                }
            }
            return InventoryScreen.EInventoryTab.Unchanged;
        }

        // UI Pointer
        public void ControllerOnPointerMove()
        {
            if (pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                List<RaycastResult> results = new List<RaycastResult>();
                eventSystem.RaycastAll(pointerEventData, results);
                if (onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
                {
                    onPointerEnterItemView.OnPointerExit(pointerEventData);
                }
                if (results.Count > 0)
                {
                    pointerEventData.pointerEnter = results[0].gameObject;
                    if (results[0].gameObject.GetComponentInParent<QuickSlotItemView>() == null)
                    {
                        onPointerEnterItemView = results[0].gameObject.GetComponentInParent<ItemView>();
                        if (onPointerEnterItemView == null)
                        {
                            onPointerEnterItemView = results[0].gameObject.GetComponentInParent<GridItemView>();
                        }
                        if (onPointerEnterItemView == null)
                        {
                            onPointerEnterItemView = results[0].gameObject.GetComponentInParent<SlotItemView>();
                        }
                        if (onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
                        {
                            onPointerEnterItemView.OnPointerEnter(pointerEventData);
                        }
                    }
                }
                else
                {
                    pointerEventData.pointerEnter = null;
                    onPointerEnterItemView = null;
                }
                UpdateControllerBlocks();
            }
        }

        // UI Inputs
        private void ControllerUse(bool Hold)
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                ControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;
                object NewContextInteractionsObject = Traverse.Create(onPointerEnterItemView).Property("NewContextInteractions").GetValue();
                if (NewContextInteractionsObject != null)
                {
                    if (ExecuteInteraction == null)
                    {
                        ExecuteInteraction = NewContextInteractionsObject.GetType().GetMethod("ExecuteInteraction", BindingFlags.Instance | BindingFlags.Public);
                    }
                    if (IsInteractionAvailable == null)
                    {
                        IsInteractionAvailable = NewContextInteractionsObject.GetType().GetMethod("IsInteractionAvailable", BindingFlags.Instance | BindingFlags.Public);
                    }
                }
                if (!onPointerEnterItemView.IsSearched && ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                if (ExecuteInteraction != null && IsInteractionAvailable != null)
                {
                    if (onPointerEnterItemView.Item is FoodClass || onPointerEnterItemView.Item is MedsClass)
                    {
                        ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Use;
                        if (!(bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters))
                        {
                            ExecuteInteractionInvokeParameters[0] = EItemInfoButton.UseAll;
                            ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters);
                        }
                        return;
                    }
                    if (onPointerEnterItemView.Item.IsContainer && !Hold)
                    {
                        ExecuteInteractionInvokeParameters[0] = EItemInfoButton.Open;
                        if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                    }
                    if (Hold && ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                    SimpleTooltip tooltip = ItemUiContext.Tooltip;
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Equip;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                    {
                        ItemUiContext.QuickEquip(onPointerEnterItemView.Item).HandleExceptions();
                        if (tooltip != null)
                        {
                            tooltip.Close();
                        }
                        ControllerOnPointerMove();
                        return;
                    }
                    else
                    {
                        bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        if (IsBeingLoadedMagazine || IsBeingUnloadedMagazine)
                        {
                            ItemController.StopProcesses();
                            return;
                        }
                    }
                    ExecuteInteractionInvokeParameters[0] = EItemInfoButton.CheckMagazine;
                    if ((bool)ExecuteInteraction.Invoke(NewContextInteractionsObject, ExecuteInteractionInvokeParameters)) return;
                    if (ExecuteMiddleClick != null && (bool)ExecuteMiddleClick.Invoke(onPointerEnterItemView, null)) return;
                }
            }
        }
        private void ControllerQuickMove()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                ControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                SimpleTooltip tooltip = ItemUiContext.Tooltip;
                object ItemContext = Traverse.Create(onPointerEnterItemView).Property("ItemContext").GetValue<object>();
                if (ItemContext != null)
                {
                    object gstructObject = QuickFindAppropriatePlace.Invoke(ItemUiContext, new object[5] { ItemContext, ItemController, false, true, true });
                    if (gstructObject != null)
                    {
                        bool Failed = Traverse.Create(gstructObject).Property("Failed").GetValue<bool>();
                        if (Failed) return;
                        object Value = Traverse.Create(gstructObject).Field("Value").GetValue<object>();
                        if (Value != null)
                        {
                            if (!(bool)CanExecute.Invoke(ItemController, new object[1] { Value }))
                            {
                                return;
                            }
                            bool ItemsDestroyRequired = Traverse.Create(Value).Field("ItemsDestroyRequired").GetValue<bool>();
                            if (ItemsDestroyRequired)
                            {
                                NotificationManagerClass.DisplayWarningNotification("DiscardLimit", ENotificationDurationType.Default);
                                return;
                            }
                            string itemSound = onPointerEnterItemView.Item.ItemSound;
                            RunNetworkTransaction.Invoke(ItemController, new object[2] { Value, null });
                            if (tooltip != null)
                            {
                                tooltip.Close();
                            }
                            Singleton<GUISounds>.Instance.PlayItemSound(itemSound, EInventorySoundType.pickup, false);
                            ControllerOnPointerMove();
                        }
                    }
                }
            }
        }
        private void ControllerDiscard()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                ControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return;
                object NewContextInteractionsObject = Traverse.Create(onPointerEnterItemView).Property("NewContextInteractions").GetValue();
                if (NewContextInteractionsObject != null)
                {
                    if (IsInteractionAvailable == null)
                    {
                        IsInteractionAvailable = NewContextInteractionsObject.GetType().GetMethod("IsInteractionAvailable", BindingFlags.Instance | BindingFlags.Public);
                    }
                }
                if (IsInteractionAvailable != null)
                {
                    SimpleTooltip tooltip = ItemUiContext.Tooltip;
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Discard;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                    {
                        ItemUiContext.ThrowItem(onPointerEnterItemView.Item).HandleExceptions();
                        if (tooltip != null)
                        {
                            tooltip.Close();
                        }
                        ControllerOnPointerMove();
                        return;
                    }
                }
            }
        }

        private void ControllerBeginDrag()
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                ControllerOnPointerMove();
            }
            if (!Dragging && pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                if (ItemController != null)
                {
                    bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                    bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                    if (IsBeingLoadedMagazine || IsBeingUnloadedMagazine)
                    {
                        ItemController.StopProcesses();
                        return;
                    }
                }
                ControllerUIMoveSnapshot();
                pointerEventData.position = globalPosition;
                pointerEventData.dragging = false;
                DraggingItemView = onPointerEnterItemView;
                DraggingItemView.OnBeginDrag(pointerEventData);
                pointerEventData.dragging = true;
                Dragging = true;
                EnableSet("OnDrag");
                ControllerOnPointerMove();
                ControllerOnDrag();
            }
        }
        private void ControllerOnDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf && DraggingItemView.BeingDragged)
                {
                    DraggingItemView.OnDrag(pointerEventData);
                }
                else if ((DraggingItemView != null && !DraggingItemView.BeingDragged) || !DraggingItemView.gameObject.activeSelf)
                {
                    ControllerCancelDrag();
                }
            }
        }
        private void ControllerEndDrag()
        {
            if (Dragging && pointerEventData != null)
            {
                pointerEventData.position = globalPosition;
                if (pointerEventData.pointerEnter != null)
                {
                    Dragging = false;
                    pointerEventData.dragging = false;
                    if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                    {
                        DraggingItemView.OnEndDrag(pointerEventData);
                        DraggingItemView = null;
                        ControllerOnPointerMove();
                    }
                    DisableSet("OnDrag");
                }
                else
                {
                    ControllerCancelDrag();
                }
            }
        }
        public void ControllerCancelDrag()
        {
            if (Dragging)
            {
                Dragging = false;
                PointerEventData pointerEventData = new PointerEventData(eventSystem);
                pointerEventData.button = PointerEventData.InputButton.Left;
                pointerEventData.position = globalPosition;
                pointerEventData.pointerEnter = null;
                pointerEventData.dragging = false;
                if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                {
                    DraggingItemView.OnEndDrag(pointerEventData);
                    DraggingItemView = null;
                    ControllerUIMoveToSnapshot();
                }
                DisableSet("OnDrag");
            }
        }
        private void ControllerRotateDragged()
        {
            if (Dragging && DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
            {
                DraggedItemView DraggedItemView = Traverse.Create(DraggingItemView).Property("DraggedItemView").GetValue<DraggedItemView>();
                if (DraggedItemView != null)
                {
                    object ItemContext = Traverse.Create(DraggedItemView).Property("ItemContext").GetValue<object>();
                    if (ItemContext != null)
                    {
                        ItemRotation ItemRotation = Traverse.Create(ItemContext).Field("ItemRotation").GetValue<ItemRotation>();
                        DraggedItemViewMethod_2.Invoke(DraggedItemView, new object[1] { (ItemRotation == ItemRotation.Horizontal ? ItemRotation.Vertical : ItemRotation.Horizontal) });
                        ControllerOnDrag();
                    }
                }
            }
        }
        private void ControllerSplitDragged()
        {
            if (Dragging && pointerEventData != null)
            {
                AmandsControllerPlugin.LeftControl.Enable();
                pointerEventData.position = globalPosition;
                if (pointerEventData.pointerEnter != null)
                {
                    Dragging = false;
                    pointerEventData.dragging = false;
                    if (DraggingItemView != null && DraggingItemView.gameObject.activeSelf)
                    {
                        DraggingItemView.OnEndDrag(pointerEventData);
                        DraggingItemView = null;
                        ControllerOnPointerMove();
                    }
                    DisableSet("OnDrag");
                }
                else
                {
                    ControllerCancelDrag();
                }
                AmandsControllerPlugin.LeftControl.Disable();
            }
        }

        private void ControllerSearch()
        {
            if (Interface && (currentSearchButton != null && currentSearchButton.gameObject.activeSelf))
            {
                ButtonPress.Invoke(currentSearchButton, null);
            }
            else
            {
                DisableSet("SearchButton");
                if (SearchButtonImage != null)
                {
                    SearchButtonImage.color = Color.white;
                    SearchButtonImage = null;
                }
            }
        }
        private void ControllerShowContextMenu()
        {
            if (!ContextMenu && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                UpdateGlobalPosition();
                ShowContextMenuInvokeParameters[0] = globalPosition;
                ShowContextMenu.Invoke(onPointerEnterItemView, ShowContextMenuInvokeParameters);
            }
        }
        private void ControllerContextMenuUse()
        {
            if ((currentContextMenuButton != null && currentContextMenuButton.gameObject.activeSelf))
            {
                Button _button = Traverse.Create(currentContextMenuButton).Field("_button").GetValue<Button>();
                if (_button != null)
                {
                    _button.onClick.Invoke();
                }
            }
        }
        private void ControllerInterfaceBind(EBoundItem bindIndex)
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                ControllerOnPointerMove();
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                pointerEventData.position = globalPosition;
                ItemUiContext ItemUiContext = ItemUiContext.Instance;
                if (ItemUiContext != null && onPointerEnterItemView.Item != null && ItemUIContextMethod_0 != null)
                {
                    ItemUIContextMethod_0InvokeParameters[0] = onPointerEnterItemView.Item;
                    ItemUIContextMethod_0InvokeParameters[1] = bindIndex;
                    ItemUIContextMethod_0.Invoke(ItemUiContext, ItemUIContextMethod_0InvokeParameters);
                }
            }
        }
        private void ControllerSplitDialogAccept()
        {
            if (splitDialog != null)
            {
                splitDialog.Accept();
            }
        }
        private void ControllerSplitDialogAdd(int Value)
        {
            if (splitDialog != null)
            {
                IntSlider _intSlider = Traverse.Create(splitDialog).Field("_intSlider").GetValue<IntSlider>();
                if (_intSlider != null && _intSlider.gameObject.activeSelf)
                {
                    lastIntSliderValue = Value;
                    SplitDialogAutoMove = true;
                    int int_1 = Traverse.Create(_intSlider).Field("int_1").GetValue<int>() - 1;
                    _intSlider.UpdateValue((_intSlider.CurrentValue() + int_1) + ((LB || RB) ? Value * 10 : Value));
                }
                StepSlider _stepSlider = Traverse.Create(splitDialog).Field("_stepSlider").GetValue<StepSlider>();
                if (_stepSlider != null && _stepSlider.gameObject.activeSelf)
                {
                    lastIntSliderValue = Value;
                    SplitDialogAutoMove = true;
                    //Temp
                    int int_0 = Traverse.Create(_stepSlider).Field("int_0").GetValue<int>();
                    int int_1 = Traverse.Create(_stepSlider).Field("int_1").GetValue<int>();
                    _stepSlider.Show(int_0, int_1, (int)_stepSlider.CurrentValue() + ((LB || RB) ? Value * 10 : Value));
                }
            }
        }
        private void ControllerScroll(float Value)
        {
            float height = currentScrollRectNoDrag.content.rect.height * ScreenRatio;
            Vector2 position = new Vector2(currentScrollRectNoDragRectTransform.position.x + (currentScrollRectNoDragRectTransform.rect.x * ScreenRatio), currentScrollRectNoDragRectTransform.position.y - ((currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f)) * ScreenRatio));
            currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + ((((Value * 1000f) / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
            UpdateGlobalPosition();
            if ((globalPosition.y + (GridSize / 2f)) > position.y)
            {
                ControllerUIMove(new Vector2Int(0, -1), false);
            }
            else if ((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
            {
                ControllerUIMove(new Vector2Int(0, 1), false);
            }
        }
        private void ControllerAutoScroll()
        {
            float height = currentScrollRectNoDrag.content.rect.height;
            Vector2 position = new Vector2(currentScrollRectNoDragRectTransform.position.x + (currentScrollRectNoDragRectTransform.rect.x * ScreenRatio), currentScrollRectNoDragRectTransform.position.y - ((currentScrollRectNoDragRectTransform.rect.height * (currentScrollRectNoDragRectTransform.pivot.y - 1f)) * ScreenRatio));
            if (globalPosition.x > position.x && globalPosition.x < (position.x + (currentScrollRectNoDragRectTransform.rect.width * ScreenRatio)))
            {
                if ((globalPosition.y + (GridSize / 2f)) > position.y)
                {
                    currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                    UpdateGlobalPosition();
                    if (!((globalPosition.y + (GridSize / 2f)) > position.y) && !((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio))))
                    {
                        ControllerUIOnMove(Vector2Int.zero, globalPosition);
                    }
                }
                else if ((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio)))
                {
                    currentScrollRectNoDrag.verticalNormalizedPosition = currentScrollRectNoDrag.verticalNormalizedPosition + (((-1000f / height) / height) * 10000f * Time.deltaTime * AmandsControllerPlugin.ScrollSensitivity.Value);
                    UpdateGlobalPosition();
                    if (!((globalPosition.y + (GridSize / 2f)) > position.y) && !((globalPosition.y - (GridSize / 2f)) < (position.y - (currentScrollRectNoDragRectTransform.rect.height * ScreenRatio))))
                    {
                        ControllerUIOnMove(Vector2Int.zero, globalPosition);
                    }
                }
            }
        }
        private void ControllerNextTab()
        {
            Tab tab = null;
            switch (CurrentTab())
            {
                case InventoryScreen.EInventoryTab.Overall:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Gear)) tab = Tabs[InventoryScreen.EInventoryTab.Gear];
                    break;
                case InventoryScreen.EInventoryTab.Gear:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Health)) tab = Tabs[InventoryScreen.EInventoryTab.Health];
                    break;
                case InventoryScreen.EInventoryTab.Health:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Skills)) tab = Tabs[InventoryScreen.EInventoryTab.Skills];
                    break;
                case InventoryScreen.EInventoryTab.Skills:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Map)) tab = Tabs[InventoryScreen.EInventoryTab.Map];
                    break;
                case InventoryScreen.EInventoryTab.Map:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Notes)) tab = Tabs[InventoryScreen.EInventoryTab.Notes];
                    break;
                case InventoryScreen.EInventoryTab.Notes:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Overall)) tab = Tabs[InventoryScreen.EInventoryTab.Overall];
                    break;
            }
            if (tab != null)
            {
                tab.OnPointerClick(null);
            }
        }
        private void ControllerPreviousTab()
        {
            Tab tab = null;
            switch (CurrentTab())
            {
                case InventoryScreen.EInventoryTab.Overall:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Notes)) tab = Tabs[InventoryScreen.EInventoryTab.Notes];
                    break;
                case InventoryScreen.EInventoryTab.Gear:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Overall)) tab = Tabs[InventoryScreen.EInventoryTab.Overall];
                    break;
                case InventoryScreen.EInventoryTab.Health:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Gear)) tab = Tabs[InventoryScreen.EInventoryTab.Gear];
                    break;
                case InventoryScreen.EInventoryTab.Skills:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Health)) tab = Tabs[InventoryScreen.EInventoryTab.Health];
                    break;
                case InventoryScreen.EInventoryTab.Map:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Skills)) tab = Tabs[InventoryScreen.EInventoryTab.Skills];
                    break;
                case InventoryScreen.EInventoryTab.Notes:
                    if (Tabs.ContainsKey(InventoryScreen.EInventoryTab.Map)) tab = Tabs[InventoryScreen.EInventoryTab.Map];
                    break;
            }
            if (tab != null)
            {
                tab.OnPointerClick(null);
            }
        }

        // UI Inputs Actions
        public List<string> ControllerGetButtonAction(AmandsControllerButtonBind Bind)
        {
            List<string> Actions = new List<string>();
            foreach (AmandsControllerCommand AmandsControllerCommand in Bind.AmandsControllerCommands)
            {
                if (AmandsControllerCommand.Command == EAmandsControllerCommand.None) continue;
                switch (AmandsControllerCommand.Command)
                {
                    case EAmandsControllerCommand.ToggleSet:
                        if (ActiveAmandsControllerSets.Contains(AmandsControllerCommand.AmandsControllerSet))
                        {
                            Actions.Add("Enable " + AmandsControllerCommand.AmandsControllerSet);
                        }
                        else if (AmandsControllerSets.ContainsKey(AmandsControllerCommand.AmandsControllerSet))
                        {
                            Actions.Add("Disable " + AmandsControllerCommand.AmandsControllerSet);
                        }
                        break;
                    case EAmandsControllerCommand.EnableSet:
                        if (AmandsControllerSets.ContainsKey(AmandsControllerCommand.AmandsControllerSet) && !ActiveAmandsControllerSets.Contains(AmandsControllerCommand.AmandsControllerSet))
                        {
                            Actions.Add("Enable " + AmandsControllerCommand.AmandsControllerSet);
                        }
                        break;
                    case EAmandsControllerCommand.DisableSet:
                        Actions.Add("Disable " + AmandsControllerCommand.AmandsControllerSet);
                        break;
                    case EAmandsControllerCommand.InputTree:
                        Actions.Add("" + AmandsControllerCommand.InputTree);
                        break;
                    case EAmandsControllerCommand.QuickSelectWeapon:
                        Actions.Add("QuickSelectWeapon");
                        break;
                    case EAmandsControllerCommand.SlowLeanLeft:
                        Actions.Add("SlowLeanLeft");
                        break;
                    case EAmandsControllerCommand.SlowLeanRight:
                        Actions.Add("SlowLeanRight");
                        break;
                    case EAmandsControllerCommand.EndSlowLean:
                        Actions.Add("EndSlowLean");
                        break;
                    case EAmandsControllerCommand.RestoreLean:
                        Actions.Add("RestoreLean");
                        break;
                    case EAmandsControllerCommand.InterfaceUp:
                        Actions.Add("InterfaceUp");
                        break;
                    case EAmandsControllerCommand.InterfaceDown:
                        Actions.Add("InterfaceDown");
                        break;
                    case EAmandsControllerCommand.InterfaceLeft:
                        Actions.Add("InterfaceLeft");
                        break;
                    case EAmandsControllerCommand.InterfaceRight:
                        Actions.Add("InterfaceRight");
                        break;
                    case EAmandsControllerCommand.InterfaceDisableAutoMove:
                        Actions.Add("InterfaceDisableAutoMove");
                        break;
                    case EAmandsControllerCommand.BeginDrag:
                        if (!Dragging && pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Drag");
                        break;
                    case EAmandsControllerCommand.EndDrag:
                        Actions.Add("EndDrag");
                        break;
                    case EAmandsControllerCommand.RotateDragged:
                        Actions.Add("Rotate");
                        break;
                    case EAmandsControllerCommand.SplitDragged:
                        Actions.Add("Split");
                        break;
                    case EAmandsControllerCommand.CancelDrag:
                        Actions.Add("CancelDrag");
                        break;
                    case EAmandsControllerCommand.Search:
                        Actions.Add("Search");
                        break;
                    case EAmandsControllerCommand.Use:
                        Actions.Add(ControllerUseAction(false));
                        break;
                    case EAmandsControllerCommand.UseHold:
                        Actions.Add(ControllerUseAction(true));
                        break;
                    case EAmandsControllerCommand.QuickMove:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("QuickMove");
                        break;
                    case EAmandsControllerCommand.Discard:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Discard");
                        break;
                    case EAmandsControllerCommand.InterfaceBind4:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind4");
                        break;
                    case EAmandsControllerCommand.InterfaceBind5:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind5");
                        break;
                    case EAmandsControllerCommand.InterfaceBind6:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind6");
                        break;
                    case EAmandsControllerCommand.InterfaceBind7:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind7");
                        break;
                    case EAmandsControllerCommand.InterfaceBind8:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind8");
                        break;
                    case EAmandsControllerCommand.InterfaceBind9:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind9");
                        break;
                    case EAmandsControllerCommand.InterfaceBind10:
                        if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("Bind10");
                        break;
                    case EAmandsControllerCommand.ShowContextMenu:
                        if (!ContextMenu && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf) Actions.Add("ContextMenu");
                        break;
                    case EAmandsControllerCommand.ContextMenuUse:
                        Actions.Add("Select");
                        break;
                    case EAmandsControllerCommand.SplitDialogAccept:
                        Actions.Add("Accept");
                        break;
                    case EAmandsControllerCommand.SplitDialogAdd:
                        Actions.Add("SplitDialogAdd");
                        break;
                    case EAmandsControllerCommand.SplitDialogSubtract:
                        Actions.Add("SplitDialogSubtract");
                        break;
                    case EAmandsControllerCommand.SplitDialogDisableAutoMove:
                        Actions.Add("SplitDialogDisableAutoMove");
                        break;
                    case EAmandsControllerCommand.PreviousTab:
                        Actions.Add("PreviousTab");
                        break;
                    case EAmandsControllerCommand.NextTab:
                        Actions.Add("NextTab");
                        break;
                }
            }
            return Actions;
        }
        public string ControllerUseAction(bool Hold)
        {
            if (onPointerEnterItemView == null || (onPointerEnterItemView != null && !onPointerEnterItemView.gameObject.activeSelf))
            {
                return "";
            }
            if (pointerEventData != null && onPointerEnterItemView != null && onPointerEnterItemView.gameObject.activeSelf)
            {
                ItemUiContext ItemUiContext = ItemUiContext.Instance;
                object NewContextInteractionsObject = Traverse.Create(onPointerEnterItemView).Property("NewContextInteractions").GetValue();
                if (NewContextInteractionsObject == null) return "";
                if (ExecuteInteraction == null)
                {
                    ExecuteInteraction = NewContextInteractionsObject.GetType().GetMethod("ExecuteInteraction", BindingFlags.Instance | BindingFlags.Public);
                }
                if (IsInteractionAvailable == null)
                {
                    IsInteractionAvailable = NewContextInteractionsObject.GetType().GetMethod("IsInteractionAvailable", BindingFlags.Instance | BindingFlags.Public);
                }

                if (!onPointerEnterItemView.IsSearched && IsInteractionAvailable != null)
                {
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Examine;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Examine";
                }
                if (ItemUiContext == null || !onPointerEnterItemView.IsSearched) return "";
                TraderControllerClass ItemController = Traverse.Create(onPointerEnterItemView).Field("ItemController").GetValue<TraderControllerClass>();
                if (onPointerEnterItemView.Item != null && ExecuteInteraction != null && IsInteractionAvailable != null && !Hold)
                {
                    if (onPointerEnterItemView.Item is FoodClass)
                    {
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Use;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Consume";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.UseAll;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Consume";
                    }
                    if (onPointerEnterItemView.Item is MedsClass)
                    {
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Use;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Use";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.UseAll;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Use";
                    }
                    if (onPointerEnterItemView.Item.IsContainer)
                    {
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Open;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Open";
                    }
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Equip;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters))
                    {
                        return "Equip";
                    }
                    else
                    {
                        bool IsBeingLoadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingLoadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        bool IsBeingUnloadedMagazine = Traverse.Create(Traverse.Create(onPointerEnterItemView).Property("IsBeingUnloadedMagazine").GetValue<object>()).Field("gparam_0").GetValue<bool>();
                        if (IsBeingLoadedMagazine)
                        {
                            return "Stop Loading Mag";
                        }
                        if (IsBeingUnloadedMagazine)
                        {
                            return "Stop Unloading Mag";
                        }
                    }
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.CheckMagazine;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "CheckMagazine";
                    if (IsInteractionAvailable != null)
                    {
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Examine;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Examine";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Fold;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Fold";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Unfold;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Unfold";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.TurnOn;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "TurnOn";
                        IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.TurnOff;
                        if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "TurnOff";
                    }
                }
                if (onPointerEnterItemView.Item != null && ExecuteInteraction != null && IsInteractionAvailable != null && Hold)
                {
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Fold;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Fold";
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Unfold;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Unfold";
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.TurnOn;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "TurnOn";
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.TurnOff;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "TurnOff";
                    IsInteractionAvailableInvokeParameters[0] = EItemInfoButton.Equip;
                    if ((bool)IsInteractionAvailable.Invoke(NewContextInteractionsObject, IsInteractionAvailableInvokeParameters)) return "Equip";
                }
            }
            return "";
        }

        // UI Blocks
        public void ControllerButtonStateMethod(EAmandsControllerButton Button, bool Pressed)
        {
            if (ButtonBlocks.ContainsKey(Button) && ButtonBlocks[Button] != null) ButtonBlocks[Button].UpdateButtonPressed(Pressed);
        }
        public void ControllerSetState(string AmandsControllerSet, bool Enabled)
        {
            UpdateControllerBlocks();
        }
        public void UpdateControllerBlocks()
        {
            foreach (KeyValuePair<EAmandsControllerButton, AmandsControllerButtonBlock> Block in ButtonBlocks)
            {
                if (Block.Value == null) continue;
                bool PressValid = false;
                bool HoldValid = false;
                bool DoubleValid = false;
                AmandsControllerButtonBind[] Binds = GetPriorityButtonBinds(Block.Key);
                foreach (string Action in ControllerGetButtonAction(Binds[0]))
                {
                    if (Action == "") continue;
                    Block.Value.Press = Action;
                    PressValid = true;
                    break;
                }
                foreach (string Action in ControllerGetButtonAction(Binds[2]))
                {
                    if (Action == "") continue;
                    Block.Value.Hold = Action;
                    HoldValid = true;
                    break;
                }
                foreach (string Action in ControllerGetButtonAction(Binds[3]))
                {
                    if (Action == "") continue;
                    Block.Value.DoubleClick = Action;
                    DoubleValid = true;
                    break;
                }
                Block.Value.HoldGameObject.SetActive(HoldValid);
                Block.Value.DoubleClickGameObject.SetActive(DoubleValid);
                if (PressValid)
                {
                    Block.Value.gameObject.SetActive(true);
                    Block.Value.UpdateCommands();
                }
                else
                {
                    Block.Value.gameObject.SetActive(false);
                }
            }
        }

        // Files
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, Formatting.Indented);//Json.Serialize(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return Json.Deserialize<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        public static void ReloadFiles()
        {
            string[] Files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/images/", "*.png");
            foreach (string File in Files)
            {
                LoadSprite(File);
            }
            string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Controller/sounds/");
            foreach (string File in AudioFiles)
            {
                LoadAudioClip(File);
            }
        }
        private async static void LoadSprite(string path)
        {
            LoadedSprites[Path.GetFileName(path)] = await RequestSprite(path);
        }
        private async static Task<Sprite> RequestSprite(string path)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                return sprite;
            }
        }
        private async static void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }
        private async static Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }
    }
    public class AmandsControllerButtonBlock : MonoBehaviour
    {
        public EAmandsControllerButton Button;
        public bool Pressed;
        public string Press = "";
        public string Hold = "";
        public string DoubleClick = "";

        public RectTransform RectTransform;
        public HorizontalLayoutGroup HorizontalLayoutGroup;

        public GameObject CommandsGameObject;
        public RectTransform CommandsRectTransform;
        public VerticalLayoutGroup CommandsVerticalLayoutGroup;

        public GameObject IconGameObject;
        public RectTransform IconRectTransform;
        public Image IconImage;
        public LayoutElement IconLayoutElement;

        public GameObject PressGameObject;
        public RectTransform PressRectTransform;
        public TextMeshProUGUI PressTextMeshProUGUI;

        public GameObject HoldGameObject;
        public RectTransform HoldRectTransform;
        public TextMeshProUGUI HoldTextMeshProUGUI;

        public GameObject DoubleClickGameObject;
        public RectTransform DoubleClickRectTransform;
        public TextMeshProUGUI DoubleClickTextMeshProUGUI;

        public void Start()
        {
            RectTransform = gameObject.AddComponent<RectTransform>();
            RectTransform.sizeDelta = AmandsControllerPlugin.BlockSize.Value;

            HorizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            HorizontalLayoutGroup.childForceExpandHeight = false;
            HorizontalLayoutGroup.childForceExpandWidth = false;
            HorizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            HorizontalLayoutGroup.spacing = AmandsControllerPlugin.BlockIconSpacing.Value;

            CommandsGameObject = new GameObject("Commands");
            CommandsGameObject.transform.SetParent(RectTransform);
            CommandsRectTransform = CommandsGameObject.AddComponent<RectTransform>();
            CommandsVerticalLayoutGroup = CommandsGameObject.AddComponent<VerticalLayoutGroup>();
            CommandsVerticalLayoutGroup.childForceExpandHeight = false;
            CommandsVerticalLayoutGroup.childForceExpandWidth = false;
            CommandsVerticalLayoutGroup.childAlignment = TextAnchor.MiddleRight;

            PressGameObject = new GameObject("Press");
            PressGameObject.transform.SetParent(CommandsRectTransform);
            PressRectTransform = PressGameObject.AddComponent<RectTransform>();
            PressTextMeshProUGUI = PressGameObject.AddComponent<TextMeshProUGUI>();
            PressTextMeshProUGUI.autoSizeTextContainer = true;
            PressTextMeshProUGUI.horizontalAlignment = HorizontalAlignmentOptions.Right;
            PressTextMeshProUGUI.text = Press;
            PressTextMeshProUGUI.fontSize = AmandsControllerPlugin.PressFontSize.Value;

            HoldGameObject = new GameObject("Hold");
            HoldGameObject.transform.SetParent(CommandsRectTransform);
            HoldRectTransform = HoldGameObject.AddComponent<RectTransform>();
            HoldTextMeshProUGUI = HoldGameObject.AddComponent<TextMeshProUGUI>();
            HoldTextMeshProUGUI.autoSizeTextContainer = true;
            HoldTextMeshProUGUI.horizontalAlignment = HorizontalAlignmentOptions.Right;
            HoldTextMeshProUGUI.text = Hold + " (HOLD)";
            HoldTextMeshProUGUI.fontSize = AmandsControllerPlugin.HoldDoubleClickFontSize.Value;
            HoldTextMeshProUGUI.color = new Color(1f,1f,1f,0.7f);

            DoubleClickGameObject = new GameObject("DoubleClick");
            DoubleClickGameObject.transform.SetParent(CommandsRectTransform);
            DoubleClickRectTransform = DoubleClickGameObject.AddComponent<RectTransform>();
            DoubleClickTextMeshProUGUI = DoubleClickGameObject.AddComponent<TextMeshProUGUI>();
            DoubleClickTextMeshProUGUI.autoSizeTextContainer = true;
            DoubleClickTextMeshProUGUI.horizontalAlignment = HorizontalAlignmentOptions.Right;
            DoubleClickTextMeshProUGUI.text = DoubleClick + " 2x";
            DoubleClickTextMeshProUGUI.fontSize = AmandsControllerPlugin.HoldDoubleClickFontSize.Value;
            DoubleClickTextMeshProUGUI.color = new Color(1f, 1f, 1f, 0.7f);

            IconGameObject = new GameObject("Icon");
            IconGameObject.transform.SetParent(RectTransform);
            IconRectTransform = IconGameObject.AddComponent<RectTransform>();
            IconRectTransform.sizeDelta = AmandsControllerPlugin.BlockSize.Value;
            IconImage = IconGameObject.AddComponent<Image>();
            IconImage.raycastTarget = false;
            if (AmandsControllerClass.LoadedSprites.ContainsKey((AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + Button.ToString() + ".png"))
            {
                IconImage.sprite = AmandsControllerClass.LoadedSprites[(AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + Button.ToString() + ".png"];
            }
            IconLayoutElement = IconGameObject.AddComponent<LayoutElement>();
            IconLayoutElement.preferredWidth = AmandsControllerPlugin.BlockSize.Value.x;
            IconLayoutElement.preferredHeight = AmandsControllerPlugin.BlockSize.Value.y;

        }
        public void UpdateCommands()
        {
            if (PressTextMeshProUGUI != null) PressTextMeshProUGUI.text = Press;

            if (HoldTextMeshProUGUI != null) HoldTextMeshProUGUI.text = Hold + " (HOLD)";

            if (DoubleClickTextMeshProUGUI != null) DoubleClickTextMeshProUGUI.text = DoubleClick + " 2x";
        }
        public void UpdateButtonPressed(bool Pressed)
        {
            this.Pressed = Pressed;

            if (IconImage != null && AmandsControllerClass.LoadedSprites.ContainsKey(Pressed ? (AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + (Button.ToString() + "_PRESSED.png") : (AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + Button.ToString() + ".png"))
            {
                IconImage.sprite = AmandsControllerClass.LoadedSprites[Pressed ? (AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + (Button.ToString() + "_PRESSED.png") : (AmandsControllerPlugin.DualsenseIcons.Value ? "Dualsense" : "") + Button.ToString() + ".png"];
            }

        }
    }
    public enum EAmandsControllerCurrentUI
    {
        None = 0,
        GridView = 1,
        TradingTableGridView = 2,
        ModSlotView = 3,
        EquipmentSlotView = 4,
        WeaponsSlotView = 5,
        ArmbandSlotView = 6,
        ContainersSlotView = 7,
        DogtagSlotView = 8,
        SpecialSlotSlotView = 9,
        SearchButton = 10,
        ContextMenuButton = 11
    }
    public class ControllerPresetJsonClass
    {
        public Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>> AmandsControllerButtonBinds { get; set; }
        public Dictionary<string, Dictionary<EAmandsControllerButton, List<AmandsControllerButtonBind>>> AmandsControllerSets { get; set; }
    }
    public struct AmandsControllerCommand
    {
        public EAmandsControllerCommand Command;
        public ECommand InputTree;
        public string AmandsControllerSet;
        public AmandsControllerCommand(EAmandsControllerCommand Command, ECommand InputTree, string AmandsControllerSet)
        {
            this.Command = Command;
            this.InputTree = InputTree;
            this.AmandsControllerSet = AmandsControllerSet;
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command, ECommand InputTree)
        {
            this.Command = Command;
            this.InputTree = InputTree;
            this.AmandsControllerSet = "";
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command, string AmandsControllerSet)
        {
            this.Command = Command;
            this.InputTree = ECommand.None;
            this.AmandsControllerSet = AmandsControllerSet;
        }
        public AmandsControllerCommand(EAmandsControllerCommand Command)
        {
            this.Command = Command;
            this.InputTree = ECommand.None;
            this.AmandsControllerSet = "";
        }
        public AmandsControllerCommand(ECommand InputTree)
        {
            this.Command = EAmandsControllerCommand.InputTree;
            this.InputTree = InputTree;
            this.AmandsControllerSet = "";
        }
    }
    public struct AmandsControllerButtonBind
    {
        public List<AmandsControllerCommand> AmandsControllerCommands;
        public EAmandsControllerPressType PressType;
        public int Priority;

        public AmandsControllerButtonBind(List<AmandsControllerCommand> AmandsControllerCommands, EAmandsControllerPressType PressType, int Priority)
        {
            this.AmandsControllerCommands = AmandsControllerCommands;
            this.PressType = PressType;
            this.Priority = Priority;
        }
        public AmandsControllerButtonBind(AmandsControllerCommand AmandsControllerCommands, EAmandsControllerPressType PressType, int Priority)
        {
            this.AmandsControllerCommands = new List<AmandsControllerCommand> { AmandsControllerCommands };
            this.PressType = PressType;
            this.Priority = Priority;
        }
    }
    public struct AmandsControllerButtonSnapshot
    {
        public bool Pressed;
        public float Time;
        public AmandsControllerButtonBind PressBind;
        public AmandsControllerButtonBind ReleaseBind;
        public AmandsControllerButtonBind HoldBind;
        public AmandsControllerButtonBind DoubleClickBind;

        public AmandsControllerButtonSnapshot(bool Pressed, float Time, AmandsControllerButtonBind PressBind, AmandsControllerButtonBind ReleaseBind, AmandsControllerButtonBind HoldBind, AmandsControllerButtonBind DoubleClickBind)
        {
            this.Pressed = Pressed;
            this.Time = Time;
            this.PressBind = PressBind;
            this.ReleaseBind = ReleaseBind;
            this.HoldBind = HoldBind;
            this.DoubleClickBind = DoubleClickBind;
        }
    }
    public enum EAmandsControllerButton
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        LB = 4,
        RB = 5,
        LT = 6,
        RT = 7,
        LS = 8,
        RS = 9,
        UP = 10,
        DOWN = 11,
        LEFT = 12,
        RIGHT = 13,
        LSUP = 14,
        LSDOWN = 15,
        LSLEFT = 16,
        LSRIGHT = 17,
        RSUP = 18,
        RSDOWN = 19,
        RSLEFT = 20,
        RSRIGHT = 21,
        BACK = 24,
        MENU = 25,
    }
    public enum EAmandsControllerPressType
    {
        Press = 0,
        Release = 1,
        Hold = 2,
        DoubleClick = 3
    }
    public enum EAmandsControllerCommand
    {
        None = 0,
        ToggleSet = 1,
        EnableSet = 2,
        DisableSet = 3,
        InputTree = 4,
        QuickSelectWeapon = 5,
        SlowLeanLeft = 6,
        SlowLeanRight = 7,
        EndSlowLean = 8,
        RestoreLean = 9,
        InterfaceUp = 10,
        InterfaceDown = 11,
        InterfaceLeft = 12,
        InterfaceRight = 13,
        InterfaceDisableAutoMove = 14,
        BeginDrag = 15,
        EndDrag = 16,
        Search = 17,
        Use = 18,
        UseHold = 19,
        QuickMove = 20,
        Discard = 21,
        InterfaceBind4 = 22,
        InterfaceBind5 = 23,
        InterfaceBind6 = 24,
        InterfaceBind7 = 25,
        InterfaceBind8 = 26,
        InterfaceBind9 = 27,
        InterfaceBind10 = 28,
        ShowContextMenu = 29,
        ContextMenuUse = 30,
        SplitDialogAccept = 31,
        SplitDialogAdd = 32,
        SplitDialogSubtract = 33,
        SplitDialogDisableAutoMove = 34,
        RotateDragged = 35,
        CancelDrag = 36,
        SplitDragged = 37,
        PreviousTab = 38,
        NextTab = 39
    }
    public enum EAmandsControllerUseStick
    {
        None = 0,
        LS = 1,
        RS = 2
    }
}
