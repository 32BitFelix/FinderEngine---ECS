using System.Reflection;
using Components.ECS;
using Components.SFX.Tonklang;
using Components.SFX.TonklangHelper;
using Components.Shimshek;
using Components.ShimshekHelper;
using Components.SpacialHierarchy;
using Components.UISystem;
using FinderEngine.Scenes;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Utility.InputHandling;

[Starter] [Scene]
public unsafe class StaticScene
{
    public static void Start()
    {
        Engine.FinderEngine.CurrentWindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;

        Engine.FinderEngine.CurrentCursorState = CursorModeValue.CursorDisabled;


        SystemHandler.SetShouldRun<UIHandler>(true);


        InputHandler.AddInput((int)Keys.Escape, InputPressType.direct, 0);

        InputHandler.AddInput((int)MouseButton.Button1, InputPressType.direct, 1);

        UIHandler.SetPressBind(1);


        mainMenu = EntityHandler.CreateEntity("MAINMENU");

        TransformSystem.CreateTransform(mainMenu.ID, (1, 1, 1), (0, 0, 0), (0, 0, 0));


        camera = EntityHandler.CreateEntity("MENUCAMERA");

        TransformSystem.CreateTransform(camera.ID, (1, 1, 1), (0, 0, 0), (0, 0, 10));

        Renderer.CreateCamera(camera.ID);


        mouseIdle = TextureHandler.CreateTexture("PointerIdle.png");

        mouseHover = TextureHandler.CreateTexture("PointerPoint.png");

        mousePress = mouseHover;

        mouseLocked = TextureHandler.CreateTexture("PointerLocked.png");


        virtualMouse = EntityHandler.CreateEntity("VIRTUALMOUSE");

        TransformSystem.CreateTransform(virtualMouse.ID, (1, 1, 1), (0, 0, 0), (0, 0, 0));

        Renderer.CreateSprite(virtualMouse.ID, mouseIdle);

        UIHandler.CreateVirtualMouse(virtualMouse.ID, mouseIdle, mouseHover,
            mousePress, mouseLocked);


        returnIdle = TextureHandler.CreateTexture("ReturnIdle.png");

        returnHover = TextureHandler.CreateTexture("ReturnHover.png");

        returnPress = returnHover;

        returnLocked = 0;


        CreateMainMenu();

        CreateSettingsMenu();

        CreateAudioSettings();

        CreateGraphicsSettings();

        CreateInputSettings();

        CreateMiscSettings();
    }   


    public static Entity camera;


    public static Entity virtualMouse;

    public static int mouseIdle,
        mouseHover,
        mousePress,
        mouseLocked;



    public static void Update()
    {


        if(TransformSystem.GetEnable(mainMenu.ID))
            MainMenuUpdate();

        if(TransformSystem.GetEnable(settingsMenu.ID))
            SettingsMenuUpdate();


    }
        // Reoccuring resources

        public static int returnIdle,
            returnHover,
            returnPress,
            returnLocked;


        // Main Menu

        public static Entity mainMenu;


        public static int titleCardTex;

        public static Entity titleCard;

        public static int titleMusic;


        public static Entity playButton;

        public static int playButtonIdle,
            playButtonHover,
            playButtonPress,
            playButtonLocked;


        public static Entity settingsButton;

        public static int settingsButtonIdle,
            settingsButtonHover,
            settingsButtonPress,
            settingsButtonLocked;


        public static Entity quitButton;

        public static int quitButtonIdle,
            quitButtonHover,
            quitButtonPress,
            quitButtonLocked;


        private static void CreateMainMenu()
        {
            titleCardTex = TextureHandler.CreateTexture("Title.png"); 


            titleCard = EntityHandler.CreateEntity("TITLECARD");

            TransformSystem.CreateTransform(titleCard.ID, (5, 3.25f, 1), (0, 0, 0), (0, 4, -1));

            TransformSystem.BindChild(mainMenu.ID, titleCard.ID);

            Renderer.CreateSprite(titleCard.ID, titleCardTex);


            titleMusic = AudioReader.ReadData("VetusDiversum.ogg");


            AudioRenderer.CreateAudioObject(titleCard.ID, titleMusic);

            AudioRenderer.PlaySound(titleCard.ID);


            playButtonIdle = TextureHandler.CreateTexture("PlayIdle.png");

            playButtonHover = TextureHandler.CreateTexture("PlayHover.png");

            playButtonPress = playButtonHover;

            playButtonLocked = 0;


            playButton = EntityHandler.CreateEntity("PLAYBUTTON");

            TransformSystem.CreateTransform(playButton.ID, (2f, 0.75f, 1), (0, 0, 0), (0, -1, -1));

            TransformSystem.BindChild(mainMenu.ID, playButton.ID);

            Renderer.CreateSprite(playButton.ID, playButtonIdle);

            UIHandler.CreateButton(playButton.ID, playButtonIdle, playButtonHover,
                playButtonPress, playButtonLocked);


            settingsButtonIdle = TextureHandler.CreateTexture("SettingsIdle.png");

            settingsButtonHover = TextureHandler.CreateTexture("SettingsHover.png");

            settingsButtonPress = settingsButtonHover;

            settingsButtonLocked = 0;


            settingsButton = EntityHandler.CreateEntity("SETTINGSBUTTON");

            TransformSystem.CreateTransform(settingsButton.ID, (3.5f, 0.75f, 1), (0, 0, 0), (0, -3, -1));

            TransformSystem.BindChild(mainMenu.ID, settingsButton.ID);

            Renderer.CreateSprite(settingsButton.ID, settingsButtonIdle);

            UIHandler.CreateButton(settingsButton.ID, settingsButtonIdle, settingsButtonHover,
                settingsButtonPress, settingsButtonLocked);


            quitButtonIdle = TextureHandler.CreateTexture("QuitIdle.png");

            quitButtonHover = TextureHandler.CreateTexture("QuitHover.png");

            quitButtonPress = quitButtonHover;

            quitButtonLocked = 0;


            quitButton = EntityHandler.CreateEntity("QUITBUTTON");

            TransformSystem.CreateTransform(quitButton.ID, (2f, 0.75f, 1), (0, 0, 0), (0, -5, -1));

            TransformSystem.BindChild(mainMenu.ID, quitButton.ID);

            Renderer.CreateSprite(quitButton.ID, quitButtonIdle);

            UIHandler.CreateButton(quitButton.ID, quitButtonIdle, quitButtonHover,
                quitButtonPress, quitButtonLocked);
        }

        private static void MainMenuUpdate()
        {
            Button playB = EntityHandler.GetComponent<Button>(playButton.ID);

            if(playB.pressed)
            {

            }
            

            Button settingsB = EntityHandler.GetComponent<Button>(settingsButton.ID);

            if(settingsB.pressed)
            {
                EntityHandler.SetComponent(mainMenu.ID, new EnableState()
                {
                    enabled = false,
                });

                EntityHandler.SetComponent(settingsMenu.ID, new EnableState()
                {
                    enabled = true,
                });
            }


            Button quitB = EntityHandler.GetComponent<Button>(quitButton.ID);

            if(quitB.pressed || InputHandler.GetInput(0))
                Engine.FinderEngine.EndEngine();
        }

        private static void DeleteMainMenu()
        {
            // Remove camera

            TransformSystem.OnRemoveEntity(mainMenu.ID);


            TransformSystem.OnRemoveEntity(titleCard.ID);

            Renderer.RemoveSprite(titleCard.ID);

            AudioRenderer.RemoveAudioObject(titleCard.ID);


            TransformSystem.OnRemoveEntity(playButton.ID);

            Renderer.RemoveSprite(playButton.ID);


            TransformSystem.OnRemoveEntity(settingsButton.ID);

            Renderer.RemoveSprite(settingsButton.ID);


            TransformSystem.OnRemoveEntity(quitButton.ID);

            Renderer.RemoveSprite(quitButton.ID); 


            TextureHandler.DeleteTexture(titleCardTex);


            TextureHandler.DeleteTexture();
        }

        // Settings Menu

        public static Entity settingsMenu;


        public static Entity settingsReturnButton;


        public static Entity audioSettingsButton;

        public static int audioSettingsButtonIdle,
            audioSettingsButtonHover,
            audioSettingsButtonPress,
            audioSettingsButtonLocked;

        
        public static Entity graphicsSettingsButton;

        public static int graphicsSettingsButtonIdle,
            graphicsSettingsButtonHover,
            graphicsSettingsButtonPress,
            graphicsSettingsButtonLocked;


        public static Entity inputSettingsButton;

        public static int inputSettingsButtonIdle,
            inputSettingsButtonHover,
            inputSettingsButtonPress,
            inputSettingsButtonLocked;


        public static Entity miscSettingsButton;

        public static int miscSettingsButtonIdle,
            miscSettingsButtonHover,
            miscSettingsButtonPress,
            miscSettingsButtonLocked;


        private static void CreateSettingsMenu()
        {
            settingsMenu = EntityHandler.CreateEntity("SETTINGSMENU");

            TransformSystem.CreateTransform(settingsMenu.ID, (1, 1, 1), (0, 0, 0), (0, 0, 0));

            EntityHandler.SetComponent(settingsMenu.ID, new EnableState()
            {
                enabled = false,
            });


            settingsReturnButton = EntityHandler.CreateEntity("SETTINGSRETURNBUTTON");

            TransformSystem.CreateTransform(settingsReturnButton.ID, (3f, 0.75f, 1), (0, 0, 0), (0, -7, -1));

            TransformSystem.BindChild(settingsMenu.ID, settingsReturnButton.ID);

            Renderer.CreateSprite(settingsReturnButton.ID, returnIdle);

            UIHandler.CreateButton(settingsReturnButton.ID, returnIdle, returnHover,
                returnPress, returnLocked);


            audioSettingsButtonIdle = TextureHandler.CreateTexture("AudioIdle.png");

            audioSettingsButtonHover = TextureHandler.CreateTexture("AudioHover.png");

            audioSettingsButtonPress = audioSettingsButtonHover;

            audioSettingsButtonLocked = 0;


            audioSettingsButton = EntityHandler.CreateEntity("AUDIOSETTINGSBUTTON");

            TransformSystem.CreateTransform(audioSettingsButton.ID, (2.5f, 0.75f, 1), (0, 0, 0), (0, 4, -1));

            TransformSystem.BindChild(settingsMenu.ID, audioSettingsButton.ID);

            Renderer.CreateSprite(audioSettingsButton.ID, miscSettingsButtonIdle);

            UIHandler.CreateButton(audioSettingsButton.ID, audioSettingsButtonIdle, audioSettingsButtonHover,
                audioSettingsButtonPress, audioSettingsButtonLocked);


            graphicsSettingsButtonIdle = TextureHandler.CreateTexture("GraphicsIdle.png");

            graphicsSettingsButtonHover = TextureHandler.CreateTexture("GraphicsHover.png");

            graphicsSettingsButtonPress = graphicsSettingsButtonHover;

            graphicsSettingsButtonLocked = 0;


            graphicsSettingsButton = EntityHandler.CreateEntity("GRAPHICSSETTINGSBUTTON");

            TransformSystem.CreateTransform(graphicsSettingsButton.ID, (4f, 0.75f, 1), (0, 0, 0), (0, 2, -1));

            TransformSystem.BindChild(settingsMenu.ID, graphicsSettingsButton.ID);

            Renderer.CreateSprite(graphicsSettingsButton.ID, graphicsSettingsButtonIdle);

            UIHandler.CreateButton(graphicsSettingsButton.ID, graphicsSettingsButtonIdle, graphicsSettingsButtonHover,
                graphicsSettingsButtonPress, graphicsSettingsButtonLocked);


            inputSettingsButtonIdle = TextureHandler.CreateTexture("InputIdle.png");

            inputSettingsButtonHover = TextureHandler.CreateTexture("InputHover.png");

            inputSettingsButtonPress = inputSettingsButtonHover;

            inputSettingsButtonLocked = 0;


            inputSettingsButton = EntityHandler.CreateEntity("INPUTSETTINGSBUTTON");

            TransformSystem.CreateTransform(inputSettingsButton.ID, (2.75f, 0.75f, 1), (0, 0, 0), (0, 0, -1));

            TransformSystem.BindChild(settingsMenu.ID, inputSettingsButton.ID);

            Renderer.CreateSprite(inputSettingsButton.ID, inputSettingsButtonIdle);

            UIHandler.CreateButton(inputSettingsButton.ID, inputSettingsButtonIdle, inputSettingsButtonHover,
                inputSettingsButtonPress, inputSettingsButtonLocked);


            miscSettingsButtonIdle = TextureHandler.CreateTexture("MiscIdle.png");

            miscSettingsButtonHover = TextureHandler.CreateTexture("MiscHover.png");

            miscSettingsButtonPress = miscSettingsButtonHover;

            miscSettingsButtonLocked = 0;


            miscSettingsButton = EntityHandler.CreateEntity("MISCSETTINGSBUTTON");

            TransformSystem.CreateTransform(miscSettingsButton.ID, (2.5f, 0.75f, 1), (0, 0, 0), (0, -2, -1));

            TransformSystem.BindChild(settingsMenu.ID, miscSettingsButton.ID);

            Renderer.CreateSprite(miscSettingsButton.ID, miscSettingsButtonIdle);

            UIHandler.CreateButton(miscSettingsButton.ID, miscSettingsButtonIdle, miscSettingsButtonHover,
                miscSettingsButtonPress, miscSettingsButtonLocked);
        }

        private static void SettingsMenuUpdate()
        {
            Button returnB = EntityHandler.GetComponent<Button>(settingsReturnButton.ID);

            if(InputHandler.GetInput(0) || returnB.pressed)
            {
                EntityHandler.SetComponent(mainMenu.ID, new EnableState()
                {
                    enabled = true,
                });

                EntityHandler.SetComponent(settingsMenu.ID, new EnableState()
                {
                    enabled = false,
                });
            }


        }

        private static void DeleteSettingsMenu()
        {
            
        }

        // Audio Settings
        private static void CreateAudioSettings()
        {

        }

        private static void AudioSettingsUpdate()
        {

        }

        private static void DeleteAudioSettings()
        {
            
        }

        // Graphics Settings
        private static void CreateGraphicsSettings()
        {

        }

        private static void GraphicsSettingsUpdate()
        {

        }

        private static void DeleteGraphicsSettings()
        {
            
        }

        // Input Settings
        private static void CreateInputSettings()
        {

        }

        private static void InputSettingsUpdate()
        {

        }

        private static void DeleteInputSettings()
        {
            
        }

        // Misc settings
        private static void CreateMiscSettings()
        {

        }

        private static void MiscUpdate()
        {
            
        }

        private static void DeleteMiscSettings()
        {

        }


    public static void End()
    {
        DeleteMiscSettings();

        DeleteInputSettings();

        DeleteGraphicsSettings();

        DeleteAudioSettings();

        DeleteSettingsMenu();

        DeleteMainMenu();
    }
}