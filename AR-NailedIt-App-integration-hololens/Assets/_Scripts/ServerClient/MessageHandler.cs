﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System;
using System.IO;


public class MessageHandler : MonoBehaviour
{

    public StepManager sManager;
    static readonly ILogger logger = LogFactory.GetLogger(typeof(MessageHandler));
    
    private InputField chatMessage = null;
    private Text chatHistory = null;

    // Variables facilitating data transfer between HoloLens and Desktop
    // HoloLens objects
    public ModelGeneration clientModelGenerator;
    public bool handleTargetFound { get; set; } = false;
    public bool isTransferringHandleData { get; set; } = false;
    public GameObject hololensHandle;
    public bool isHoloLensApp { get; set; } = false; // True if this is the hololens app

    // Desktop objects
    public Toggle logDataToggle;
    private string logDataPath;
    public bool isLoggingHandleData { get; set; } = false;

    // Mobile objects
    public GameObject mobileHandle;
    public GameObject mobileBone;
    public bool isFrameOfReferenceFound { get; set; } = false;

    // Server handle orientation data's containers
    public InputField handleXRotInputField;
    public InputField handleYRotInputField;
    public InputField handleZRotInputField;
    public InputField drillGuideXRotInputField;
    public InputField drillGuideYRotInputField;
    public InputField drillGuideZRotInputField;


    //Buttons for initiating steps
    public Button bStep1;
    public Button bStep2;
    public Button bStep3;

    public void Awake()
    {
        Player.OnMessage += OnPlayerMessage;
        Time.fixedDeltaTime = 0.5f;

        // logDataToggle only exists on the server side
        if(logDataToggle != null)
        {
            logDataToggle.onValueChanged.AddListener(delegate
            {
                ToggleValueChanged(logDataToggle);
            });
        }
        logDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\loggedData.txt";
        StreamWriter sw = File.CreateText(logDataPath);
        sw.Close();

        if( bStep1!= null)
            bStep1.onClick.AddListener(mStep1);

        if (bStep2 != null)
            bStep2.onClick.AddListener(mStep2);

        if (bStep3 != null)
            bStep3.onClick.AddListener(mStep3);

    }

    public void Update()
    {
        //if (handleTargetFound && isTransferringHandleData)
        //{
        //    OnFoundHandleImageTarget();
        //}

        // DELETE LATER
        //if(isFrameOfReferenceFound)
        //{
        //    mobileHandle.transform.position += new Vector3(0.001f, 0, 0);
        //    xPos.SetTextWithoutNotify(mobileHandle.transform.position.x.ToString());
        //    yPos.SetTextWithoutNotify(mobileHandle.transform.position.y.ToString());
        //    zPos.SetTextWithoutNotify(mobileHandle.transform.position.z.ToString());
        //}
        
    }

    public void FixedUpdate()
    {
        if (handleTargetFound && isTransferringHandleData)
        {
            OnFoundHandleImageTarget();
        }
    }

    public void InitializeChatUIComponents()
    {
        if (!isHoloLensApp)
        {
            chatMessage = GameObject.Find("ChatInputField").GetComponent<InputField>();
            chatHistory = GameObject.Find("ChatText").GetComponent<Text>();
        }
    }


    public void NotifyServerOnJoin()
    {
        StartCoroutine(NotifyServerOnJoinRoutine());
    }


    IEnumerator NotifyServerOnJoinRoutine()
    {
        yield return new WaitForSeconds(0.55f);
        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend("JOIN|" + player.playerName);
    }

    public void NotifyServerOnExit()
    {
        StartCoroutine(NotifyServerOnExitRoutine());   
    }

    IEnumerator NotifyServerOnExitRoutine()
    {
        yield return new WaitForSeconds(0.55f);
        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend("EXIT|" + player.playerName);
    }

    /// <summary>
    /// functions for initiating various steps in hololens
    /// </summary>
    public void mStep1()
    {
        StartCoroutine(start_Step1());
    }

    public void mStep2()
    {
        StartCoroutine(start_Step2());
    }

    public void mStep3()
    {
        StartCoroutine(start_Step3());
    }

    IEnumerator start_Step1()
    {
        yield return new WaitForSeconds(0.55f);
        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend("STEP|1");
    }

    IEnumerator start_Step2()
    {
        yield return new WaitForSeconds(0.55f);
        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend("STEP|2");
    }
    IEnumerator start_Step3()
    {
        yield return new WaitForSeconds(0.55f);
        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend("STEP|3");
    }

    void steps(string msg)
    {
        Debug.Log("received message: STEP " + msg);
        if(msg.CompareTo("1") == 0)
        {
            sManager.start_step1();
        }
        else if(msg.CompareTo("2") == 0)
        {
            sManager.start_step2();
        }
        else if(msg.CompareTo("3") == 0)
        {
            sManager.start_step3();
        }
    }

    void OnPlayerMessage(Player player, string message)
    {
        if (chatMessage == null)
        {
            InitializeChatUIComponents();
        }
        string[] messageParts = message.Split('|');
        if (player.isServer)
        {
            // Player is server and is receiving the message
            if (!player.isLocalPlayer)
            {
                switch (messageParts[0])
                {
                    case "HANDLEDATA": // when desktop server receive handle data from HoloLens client
                        // Don't add messages to chatHistory if client is trying to send handle data
                        // Otherwise the chatbox will be overflowed with messages
                        OnReceiveHandleDataServer(messageParts[1]);
                        return;
                    default:
                        break;
                }
            }

        }
        else if (player.isClientOnly)
        {
            // player is client and is sending message
            if (player.isLocalPlayer)
            {
                switch (messageParts[0])
                {
                    case "HANDLEDATA": // When HoloLens client sends handle data to the server and mobile clients
                        // Don't add messages to chatHistory if client is trying to send handle data
                        // Otherwise the chatbox will be overflowed with messages
                        return;
                    case "JOIN":
                        return;
                    case "EXIT":
                        return;
                    default:
                        break;
                }
            }
            // player is client and is receiving message
            else
            {
                switch (messageParts[0])
                {
                    case "PLANNINGDATA": // when Hololens and mobile clients receive planning data from desktop server
                        clientModelGenerator.ParseData(messageParts[1]);
                        break;
                    case "HANDLEDATA": // When mobile clients receive handle data from Hololens client
                                       // Don't add messages to chatHistory if client is trying to send handle data
                                       // Otherwise the chatbox will be overflowed with messages
                        if (isFrameOfReferenceFound)
                            OnReceiveHandleDataMobile(messageParts[1]);
                        return;
                    case "STEP": steps(messageParts[1]);
                        break;
                    default:
                        break;
                }
            }
        }
        string prettyMessage = "";

        // when client connect and disconnect
        if (messageParts[0] == "JOIN")
        {
            prettyMessage = $"<color=green>{messageParts[1] + " joined the server."}</color>";

            Debug.Log("Client joining");
        }
        else if(messageParts[0] == "EXIT")
        {
            prettyMessage = $"<color=red>{messageParts[1] + " exited the server."}</color>";

            Debug.Log("Client exiting");
        }
        // All other messaage types
        else
        {
            prettyMessage = player.isLocalPlayer ?
                $"<color=red>{player.playerName}: </color> {message}" :
                $"<color=blue>{player.playerName}: </color> {message}";
        }
        AppendMessage(prettyMessage);
        logger.Log(message);
    }

    // Player sending the message by pressing the Enter key
    public void OnSend()
    {
        if (chatMessage == null)
        {
            InitializeChatUIComponents();
        }
        if (Input.GetKeyDown(KeyCode.Return)) {
            if (chatMessage.text.Trim() == "")
                return;

            // get our player
            Player player = NetworkClient.connection.identity.GetComponent<Player>();

            // send a message
            player.CmdSend(chatMessage.text.Trim());

            chatMessage.text = "";
        }
    }

    // Player sending the message by pressing the Send button in chatbox panel
    public void OnSendButton()
    {
        if (chatMessage == null)
        {
            InitializeChatUIComponents();
        }
        if (chatMessage.text.Trim() == "")
            return;

        // get our player
        Player player = NetworkClient.connection.identity.GetComponent<Player>();

        // send a message
        player.CmdSend(chatMessage.text.Trim());

        chatMessage.text = "";
    }

    // Server user hitting the send data button
    public void OnSendDataButton(string message)
    {
        if (message.Trim() == "")
            return;

        // get our player
        Player player = NetworkClient.connection.identity.GetComponent<Player>();

        // send data
        player.CmdSend(message);
    }

    // When the HoloLens' handle image target is found and the Send Handle Data button is clicked, the HoloLens sends position and 
    // orientation data to the server and mobile clients
    private void OnFoundHandleImageTarget()
    {
        Vector3 position = hololensHandle.transform.position;
        Vector3 rotation = hololensHandle.transform.eulerAngles;
        string handleData =
                "HANDLEDATA|" + position.x + "," + position.y + "," + position.z + ";" + rotation.x + "," + rotation.y + "," + rotation.z;

        Player player = NetworkClient.connection.identity.GetComponent<Player>();
        player.CmdSend(handleData);

        // Display handle orientation data onto screen at bottom left corner
        handleXRotInputField.SetTextWithoutNotify(rotation.x.ToString());
        handleYRotInputField.SetTextWithoutNotify(rotation.y.ToString());
        handleZRotInputField.SetTextWithoutNotify(rotation.z.ToString());
    }

    // When the server receives handle data from the HoloLens, it processes the data and orient the handle on 
    // the desktop app to match the orientation of the handle on the holoLens app. If the log data checkbox 
    // is checked, the data will also gets logged to a file on the desktop
    private void OnReceiveHandleDataServer(string handleData)
    {
        string[] splitHandleData = handleData.Split(';');
        Vector3 position = Utils.StringToVector3(splitHandleData[0]);
        Vector3 rotation = Utils.StringToVector3(splitHandleData[1]);

        // Modifies the current orientation and position of the arm on desktop to match the one on HoloLens client
        // Make sure the arm is marked visible with the display arm checkbox

        // ArmModelDropdown.selectedArmModel.transform.position = position; --> uncomment to match position
        ArmModelDropdown.selectedArmModel.transform.rotation = Quaternion.Euler(rotation);
        //NailModelDropdown.selectedNailModel.transform.rotation = Quaternion.Euler(rotation);

        handleXRotInputField.SetTextWithoutNotify(position.x.ToString());
        handleYRotInputField.SetTextWithoutNotify(position.y.ToString());
        handleZRotInputField.SetTextWithoutNotify(position.z.ToString());

        drillGuideXRotInputField.SetTextWithoutNotify(rotation.x.ToString());
        drillGuideYRotInputField.SetTextWithoutNotify(rotation.y.ToString());
        drillGuideZRotInputField.SetTextWithoutNotify(rotation.z.ToString());

        // Log the handle data to log file in the current directory
        if (isLoggingHandleData)
        {
            string logData =
            "Handle Image Target\n" +
            "[Position]" + position.x + " " + position.y + " " + position.z + " " + "\n" +
            "[Rotation]" + rotation.x + " " + rotation.y + " " + rotation.z + " " + "\n";
            Utils.LogData(logData, logDataPath, includesTimeStamp: true);
        }
    }

    // When the mobile client receives handle data from the HoloLens, it processes the data and orient the handle on
    // mobile app to match those of the handle on the HoloLens app.
    // This way we don't need to "see" the image target of the handle
    private void OnReceiveHandleDataMobile(string handleData)
    {
        string[] splitHandleData = handleData.Split(';');
        Vector3 position = Utils.StringToVector3(splitHandleData[0]);
        Vector3 rotation = Utils.StringToVector3(splitHandleData[1]);

        mobileHandle.transform.rotation = Quaternion.Euler(rotation);
        //mobileHandle.transform.rotation = new Quaternion(1, 0, 0, 0);
        // TODO: Set position with respect to the frame of reference

        handleXRotInputField.SetTextWithoutNotify(position.x.ToString());
        handleYRotInputField.SetTextWithoutNotify(position.y.ToString());
        handleZRotInputField.SetTextWithoutNotify(position.z.ToString());

        drillGuideXRotInputField.SetTextWithoutNotify(rotation.x.ToString());
        drillGuideYRotInputField.SetTextWithoutNotify(rotation.y.ToString());
        drillGuideZRotInputField.SetTextWithoutNotify(rotation.z.ToString());
    }

    void ToggleValueChanged(Toggle toggle)
    {
        isLoggingHandleData = toggle.isOn;
    }

    internal void AppendMessage(string message)
    {
        StartCoroutine(AppendMessageToScrollView(message));
    }

    IEnumerator AppendMessageToScrollView(string message)
    {
        chatHistory.text += message + "\n";

        // it takes 2 frames for the UI to update ?!?!
        yield return null;
        yield return null;
    }
}
