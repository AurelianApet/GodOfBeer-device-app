using SimpleJSON;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SocketIO;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class MainManager : MonoBehaviour
{
    public Text mainContent;
    public GameObject deviceSetObj;
    public GameObject selfSetObj;
    public GameObject regulatorSetObj;
    public Image gatewayImage;
    public Image controllerImage;
    public Image regulatorImage;
    public GameObject deviceItem;
    public GameObject deviceParent;
    public InputField deviceNoTxt;
    public InputField deviceNameTxt;
    public InputField deviceIDTxt;
    public InputField deviceIpTxt;
    public InputField deviceMacTxt;
    public Dropdown selfTapDrop;
    public InputField GwNoTxt;
    public InputField ControllerNoTxt;
    public InputField MaxQuantityTxt;
    public InputField SoldoutTxt;
    public InputField VolumeSecTxt;
    public InputField DecarbotimeTxt;
    public InputField StandbytimeTxt;
    public InputField WaterTxt;
    public InputField GwChTxt;
    public InputField ControllerChTxt;
    public InputField FlowSensorTxt;
    public InputField DecarboncationTxt;
    public Image soldoutBtnImg;
    public Image valveABtnImg;
    public Image valveBBtnImg;
    public GameObject constantNoticeObj;
    public GameObject constantObj;
    public GameObject thresholdNoticeObj;
    public GameObject thresholdObj;
    public Text statusNotice;
    public Image refreshBtnImg;
    public Image settingBtnImg;
    public Image monitorBtnImg;
    public GameObject channelItem;
    public GameObject channelParent;
    public GameObject select_popup;
    public Text select_str;
    public GameObject err_popup;
    public Text err_str;
    public GameObject progress_popup;
    public Text progress_str;
    public GameObject socketPrefab;
    GameObject socketObj;
    SocketIOComponent socket;
    bool is_socket_open = false;
    GameObject[] channelObj;
    bool is_udp_run = false;//UDP세팅용 EXE실행중
    bool is_tagGW = false;//TagGW Exe 소켓 접속중
    bool is_board = false;//Board Exe 소켓 접속중
    bool is_regulator = false;//Regulaotr Exe 소켓 접속중
    int cur_selected_deviceIndex = -1;
    int cur_selected_deviceType = 0;//0-gateway, 1-controller, 2-regulator
    DeviceInfo curSelectedDevice = new DeviceInfo();
    int cur_selected_serial_number = -1;
    int cur_selected_tap_index = -1;
    bool is_exist = false;
    int regulator_mode = 0;//0-setting, 1-monitor
    TapInfo cur_tapInfo = new TapInfo();
    float response_delay_time = 5f;
    float time = 0f;
    private bool is_search = false;
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    void KillExes(int type = 0)
    {
        try
        {
            if (Global.posExe != null && type == 0)
            {
                Global.posExe.Kill();
                Global.posExe.CloseMainWindow();
            }
        }catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
        try
        {
            if (Global.udpConnecter != null)
            {
                Global.udpConnecter.Kill();
                Global.udpConnecter.CloseMainWindow();
            }
        }catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
        try
        {
            if (Global.tagGWProcess != null)
            {
                Global.tagGWProcess.Kill();
                Global.tagGWProcess.CloseMainWindow();
            }
        }catch(Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
        try
        {
            if (Global.boardProcess != null)
            {
                Global.boardProcess.Kill();
                Global.boardProcess.CloseMainWindow();
            }
        }catch(Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
        try
        {
            if (Global.regulatorProcess != null)
            {
                Global.regulatorProcess.Kill();
                Global.regulatorProcess.CloseMainWindow();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Screen.fullScreen = false;
        KillExes();
        try
        {
            UnityEngine.Debug.Log(Global.setinfo.gateways.Length + ", " + Global.setinfo.controllers.Length);
            if (Global.setinfo.gateways != null && Global.setinfo.gateways.Length > 0)
            {
                Global.tagGWProcess = new Process();
                UnityEngine.Debug.Log("Occured TagGW");
                string path = @"/tagGW/device.exe";
                string cmd = "\"" + Global.server_address + "\"";
                for (int i = 0; i < Global.setinfo.gateways.Length; i++)
                {
                    cmd += " \"" + Global.setinfo.gateways[i].ip + "\"";
                    cmd += " \"" + Global.setinfo.gateways[i].no + "\"";
                }
                UnityEngine.Debug.Log("taggw : " + cmd);
                StartCoroutine(StartAndWaitForProcess(path, cmd, 3));
            }
            if (Global.setinfo.controllers != null && Global.setinfo.controllers.Length > 0)
            {
                Global.boardProcess = new Process();
                UnityEngine.Debug.Log("Occured Board");
                string path = @"/board/device.exe";
                string cmd = "\"" + Global.server_address + "\"";
                for (int i = 0; i < Global.setinfo.controllers.Length; i++)
                {
                    cmd += " \"" + Global.setinfo.controllers[i].ip + "\"";
                    cmd += " \"" + Global.setinfo.controllers[i].no + "\"";
                }
                UnityEngine.Debug.Log("board : " + cmd);
                StartCoroutine(StartAndWaitForProcess(path, cmd, 4));
            }
            if (Global.setinfo.regulators != null && Global.setinfo.regulators.Length > 0)
            {
                Global.regulatorProcess = new Process();
                UnityEngine.Debug.Log("Occured Board");
                string path = @"/regulator/device.exe";
                string cmd = "\"" + Global.server_address + "\"";
                for (int i = 0; i < Global.setinfo.regulators.Length; i++)
                {
                    cmd += " \"" + Global.setinfo.regulators[i].ip + "\"";
                    cmd += " \"" + Global.setinfo.regulators[i].no + "\"";
                }
                UnityEngine.Debug.Log("regulator : " + cmd);
                StartCoroutine(StartAndWaitForProcess(path, cmd, 5));
            }
            socketObj = Instantiate(socketPrefab);
            socket = socketObj.GetComponent<SocketIOComponent>();
            socket.On("open", socketOpen);
            socket.On("error", socketError);
            socket.On("deviceAlert", socketAlert);
            socket.On("close", socketClose);
            socket.On("deviceSettingInfo", socketSetting);
            socket.On("existExeSetFinish", posExeSetFinish);
            socket.On("udpConnecterSetFinish", udpConnecterSetFinish);
            socket.On("udpConnecterKill", udpConnecterKill);
            socket.On("tagGWSetFinish", tagGWSetFinish);
            socket.On("boardSetFinish", boardSetFinish);
            socket.On("regulatorSetFinish", regulatorSetFinish);
            socket.On("getDeviceStatusResponse", getDeviceStatusResponse);
            socket.On("SetRegulatorResponse", SetRegulatorResponse);
            socket.On("setRegulatorValveStatusResponse", setRegulatorValveStatusResponse);
            socket.On("shopOpen", tapOpen);
            socket.On("shopClose", tapClose);
            UnityEngine.Debug.Log(Global.is_pos_run);
            if (!Global.is_pos_run)
            {
                Global.is_pos_run = true;
                string filePath = @"/pos_run/device.exe";
                string command = "\"" + Global.setinfo.userinfo.pub_id.ToString() + "\" \"" + Global.setinfo.userinfo.pub_name + "\" \"" + Global.setinfo.userinfo.is_open.ToString() + "\" \"" + Global.server_address + "\"";
                StartCoroutine(StartAndWaitForProcess(filePath, command, 0));
                //progress_popup.SetActive(true);
                //progress_str.text = "기존단말기용 Exe 로딩중입니다.";
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator StartAndWaitForProcess(string filePath, string command, int type, int index = 0)
    {
        bool programFinished = false;
        var waitItem = new WaitUntil(() => programFinished);
        Process MyProcess = new Process();
        string projectCurrentDir = Directory.GetCurrentDirectory();
        MyProcess.StartInfo.FileName = projectCurrentDir + filePath;
        MyProcess.StartInfo.Arguments = command;
        MyProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
        //Sets the bool to true when the event fires.
        MyProcess.Exited += (obj, args) => programFinished = true;

        MyProcess.Start();
        if (type == 0)
        {
            Global.posExe = MyProcess;
        }
        else if(type == 1 || type == 2)
        {
            Global.udpConnecter = MyProcess;
        }
        else if(type == 3)
        {
            Global.tagGWProcess = MyProcess;
        }
        else if(type == 4)
        {
            Global.boardProcess = MyProcess;
        }
        else if(type == 5)
        {
            Global.regulatorProcess = MyProcess;
        }
        yield return waitItem;
    }

    public void socketOpen(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
        if (is_socket_open)
            return;
        is_socket_open = true;
        string pubid = "{\"pub_id\":\"" + Global.setinfo.userinfo.pub_id + "\"}";
        UnityEngine.Debug.Log(pubid);
        socket.Emit("deviceSetInfo", JSONObject.Create(pubid));
    }

    public void socketError(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void socketClose(SocketIOEvent e)
    {
        is_socket_open = false;
        UnityEngine.Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }

    public void socketAlert(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] Alert received: " + e.name + " " + e.data);
        err_str.text = "세팅할 장비만 전원을 켜고 검색을 하세요.";
        err_popup.SetActive(true);
    }

    public void posExeSetFinish(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] ExistSelf Exe Setting Finish received: " + e.name + " " + e.data);
        progress_popup.SetActive(false);
    }

    public void udpConnecterSetFinish(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] UdpConnecter Exe Setting Finish received: " + e.name + " " + e.data);
        progress_popup.SetActive(false);
    }

    public void tagGWSetFinish(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] TagGW Exe Setting Finish received: " + e.name + " " + e.data);
        is_tagGW = false;
        progress_popup.SetActive(false);
    }

    public void regulatorSetFinish(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] Regulator Exe Setting Finish received: " + e.name + " " + e.data);
        is_regulator = false;
        progress_popup.SetActive(false);
    }

    public void boardSetFinish(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] Board Exe Setting Finish received: " + e.name + " " + e.data);
        is_board = false;
        progress_popup.SetActive(false);
    }

    public void getDeviceStatusResponse(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] REQ_GET_DEVICE_STATUS Response received: " + e.name + " " + e.data);
        JSONNode data = JSON.Parse(e.data.ToString());
        int serial_number = data["serial_number"].AsInt;
        int valve1_state = data["valve1_state"].AsInt;
        int valve2_state = data["valve2_state"].AsInt;
        if(!is_exist)
        {
            return;
        }
        if(cur_selected_serial_number == serial_number)
        {
            for(int i = 0; i < Global.setinfo.tapInfoList.Length; i ++)
            {
                if(Global.setinfo.tapInfoList[i].serial_number == cur_selected_serial_number)
                {
                    cur_tapInfo = Global.setinfo.tapInfoList[i];
                    cur_selected_tap_index = i;
                    break;
                }
            }
            cur_tapInfo.valve1_state = valve1_state;
            cur_tapInfo.valve2_state = valve2_state;
            Global.setinfo.tapInfoList[cur_selected_tap_index] = cur_tapInfo;
            if (valve1_state == 0)
            {
                valveABtnImg.sprite = Resources.Load<Sprite>("valvea");
            }
            else
            {
                valveABtnImg.sprite = Resources.Load<Sprite>("valvea1");
            }

            if(valve2_state == 0)
            {
                valveBBtnImg.sprite = Resources.Load<Sprite>("valveb");
            }
            else
            {
                valveBBtnImg.sprite = Resources.Load<Sprite>("valveb1");
            }
        }
    }

    public void SetRegulatorResponse(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] REQ_GET_PRESSURE_VALUE Response received: " + e.name + " " + e.data);
        JSONNode data = JSON.Parse(e.data.ToString());
        int regulator_no = data["regulator_no"].AsInt;
        int regulator_channel = data["regulator_channel"].AsInt;
        float pressure = data["pressure"].AsFloat;
        float temperature = data["temperature"].AsFloat;
        for(int i = 0; i < Global.setinfo.tapInfoList.Length; i ++)
        {
            if(Global.setinfo.tapInfoList[i].regulator_no == regulator_no 
                && Global.setinfo.tapInfoList[i].regulator_channel == regulator_channel)
            {
                Global.setinfo.tapInfoList[i].show_press = pressure;
                Global.setinfo.tapInfoList[i].show_temp = temperature;
                break;
            }
        }
        StartCoroutine(LoadRegulatorInfo());
    }

    public void setRegulatorValveStatusResponse(SocketIOEvent e)
    {
        if (!is_exist)
        {
            return;
        }
        try
        {
            UnityEngine.Debug.Log("[SocketIO] REQ_SET_VALVE_STATUS Response received: " + e.name + " " + e.data);
            JSONNode data = JSON.Parse(e.data.ToString());
            int regulator_no = data["regulator_no"].AsInt;
            int ch_value = data["ch_value"].AsInt;
            int valve = data["valve"].AsInt;
            int is_valve_error = data["is_valve_error"].AsInt;
            int error_status = data["error_status"].AsInt;
            string err_content = data["error_content"];
            for(int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
            {
                if(Global.setinfo.tapInfoList[i].regulator_no == regulator_no
                    && Global.setinfo.tapInfoList[i].regulator_channel == ch_value)
                {
                    Global.setinfo.tapInfoList[i].is_error = is_valve_error;
                    Global.setinfo.tapInfoList[i].err_content = err_content;
                    break;
                }
            }
            StartCoroutine(LoadRegulatorInfo());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    public void tapOpen(SocketIOEvent e)
    {
        if (!is_exist)
        {
            return;
        }
        try
        {
            UnityEngine.Debug.Log("[SocketIO] tapOpen Response received: " + e.name + " " + e.data);
            for (int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
            {
                Global.setinfo.tapInfoList[i].pstatus = 1;
                string data = "{\"regulator_no\":\"" + Global.setinfo.tapInfoList[i].regulator_no + "\"," +
                    "\"ch_value\":\"" + Global.setinfo.tapInfoList[i].regulator_channel + "\"," +
                    "\"pressure\":\"" + Global.setinfo.tapInfoList[i].pressure + "\"," +
                    "\"temperature\":\"" + Global.setinfo.tapInfoList[i].temperature + "\"," +
                    "\"constant\":\"" + constantObj.GetComponent<InputField>().text + "\"," +
                    "\"tolerance\":\"" + thresholdObj.GetComponent<InputField>().text + "\"," +
                    "\"ctrl_state\":\"" + Global.setinfo.tapInfoList[i].pstatus + "\"}";
                socket.Emit("regulatorInfo", JSONObject.Create(data));
                channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("unsel");
                channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Stop";
                channelObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("normal");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    public void tapClose(SocketIOEvent e)
    {
        if (!is_exist)
        {
            return;
        }
        try
        {
            for (int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
            {
                Global.setinfo.tapInfoList[i].pstatus = 0;
                string data = "{\"regulator_no\":\"" + Global.setinfo.tapInfoList[i].regulator_no + "\"," +
                    "\"ch_value\":\"" + Global.setinfo.tapInfoList[i].regulator_channel + "\"," +
                    "\"pressure\":\"" + Global.setinfo.tapInfoList[i].pressure + "\"," +
                    "\"temperature\":\"" + Global.setinfo.tapInfoList[i].temperature + "\"," +
                    "\"constant\":\"" + constantObj.GetComponent<InputField>().text + "\"," +
                    "\"tolerance\":\"" + thresholdObj.GetComponent<InputField>().text + "\"," +
                    "\"ctrl_state\":\"" + Global.setinfo.tapInfoList[i].pstatus + "\"}";
                socket.Emit("regulatorInfo", JSONObject.Create(data));
                channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("sel");
                channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Start";
                channelObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("stop");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    public void udpConnecterKill(SocketIOEvent e)
    {
        UnityEngine.Debug.Log("[SocketIO] UdpConnecter Exe Kill received: " + e.name + " " + e.data);
        err_str.text = "디바이스 세팅 완료되였습니다.";
        err_popup.SetActive(true);
        is_udp_run = false;
        if (Global.udpConnecter != null)
        {
            Global.udpConnecter.Kill();
            Global.udpConnecter.CloseMainWindow();
        }
        curSelectedDevice.ip = deviceIpTxt.text;
        curSelectedDevice.id = deviceIDTxt.text;
        curSelectedDevice.mac = deviceMacTxt.text;
        if(cur_selected_deviceType == 0)
        {
            string filePath = @"/tagGW/device.exe";
            string command = "\"" + Global.server_address + "\"";
            for(int i = 0; i < Global.setinfo.gateways.Length; i++)
            {
                command += " \"" + Global.setinfo.gateways[i].ip + "\"";
                command += " \"" + Global.setinfo.gateways[i].no + "\"";
            }
            UnityEngine.Debug.Log("taggw : " + command);
            StartCoroutine(StartAndWaitForProcess(filePath, command, 3, cur_selected_deviceIndex));
            is_tagGW = true;
            //progress_popup.SetActive(true);
            //progress_str.text = "TagGW용 Exe 로딩중입니다.";
        } else if(cur_selected_deviceType == 1)
        {
            //controller
            string filePath = @"/board/device.exe";
            string command = "\"" + Global.server_address + "\"";
            for (int i = 0; i < Global.setinfo.controllers.Length; i++)
            {
                command += " \"" + Global.setinfo.controllers[i].ip + "\"";
                command += " \"" + Global.setinfo.controllers[i].no + "\"";
            }
            UnityEngine.Debug.Log("board : " + command);
            StartCoroutine(StartAndWaitForProcess(filePath, command, 4, cur_selected_deviceIndex));
            is_board = true;
            //progress_popup.SetActive(true);
            //progress_str.text = "제어보드용 Exe 로딩중입니다.";
        }
        else
        {
            string filePath = @"/regulator/device.exe";
            string command = "\"" + Global.server_address + "\"";
            for (int i = 0; i < Global.setinfo.regulators.Length; i++)
            {
                command += " \"" + Global.setinfo.regulators[i].ip + "\"";
                command += " \"" + Global.setinfo.regulators[i].no + "\"";
            }
            UnityEngine.Debug.Log("regulator : " + command);
            StartCoroutine(StartAndWaitForProcess(filePath, command, 5, cur_selected_deviceIndex));
            is_regulator = true;
            //progress_popup.SetActive(true);
            //progress_str.text = "리귤레이터용 Exe 로딩중입니다.";
        }
    }

    public void socketSetting(SocketIOEvent e)
    {
        try
        {
            UnityEngine.Debug.Log("[SocketIO] Setting received: " + e.name + " " + e.data);
            JSONNode data = JSON.Parse(e.data.ToString());
            curSelectedDevice.id = data["id"];
            curSelectedDevice.ip = data["ip"];
            curSelectedDevice.mac = data["mac"];
            deviceIDTxt.text = curSelectedDevice.id;
            deviceIpTxt.text = curSelectedDevice.ip;
            deviceMacTxt.text = curSelectedDevice.mac;
            is_search = true;
        } catch(Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    void FixedUpdate()
    {
        if (!Input.anyKey)
        {
            time += Time.deltaTime;
        }
        else
        {
            if (time != 0f)
            {
                GameObject.Find("touch").GetComponent<AudioSource>().Play();
                time = 0f;
            }
        }
    }

    IEnumerator Destroy_Object(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }

    public void onRegulator()
    {
        selfSetObj.SetActive(false);
        deviceSetObj.SetActive(false);
        regulatorSetObj.SetActive(true);
        regulator_mode = 0;
        constantNoticeObj.SetActive(true);
        thresholdNoticeObj.SetActive(true);
        constantObj.SetActive(true);
        thresholdObj.SetActive(true);
        settingBtnImg.sprite = Resources.Load<Sprite>("sel");
        monitorBtnImg.sprite = Resources.Load<Sprite>("unsel");
        refreshBtnImg.sprite = Resources.Load<Sprite>("savechanges");
        statusNotice.text = "Pressure0";
        if (Global.setinfo.regulators == null)
            return;
        StartCoroutine(LoadRegulatorInfo());
    }

    IEnumerator LoadRegulatorInfo()
    {
        if (regulator_mode == 0)
        {
            settingBtnImg.sprite = Resources.Load<Sprite>("sel");
            monitorBtnImg.sprite = Resources.Load<Sprite>("unsel");
            refreshBtnImg.sprite = Resources.Load<Sprite>("savechanges");
            constantNoticeObj.SetActive(true);
            thresholdNoticeObj.SetActive(true);
            constantObj.SetActive(true);
            constantObj.GetComponent<InputField>().text = PlayerPrefs.GetFloat("constant").ToString();
            thresholdObj.SetActive(true);
            thresholdObj.GetComponent<InputField>().text = PlayerPrefs.GetFloat("threshold").ToString();
            statusNotice.text = "Pressure0";
        }
        else
        {
            refreshBtnImg.sprite = Resources.Load<Sprite>("refresh");
            settingBtnImg.sprite = Resources.Load<Sprite>("unsel");
            monitorBtnImg.sprite = Resources.Load<Sprite>("sel");
            constantNoticeObj.SetActive(false);
            thresholdNoticeObj.SetActive(false);
            constantObj.SetActive(false);
            thresholdObj.SetActive(false);
            statusNotice.text = "Status";
        }
        channelObj = new GameObject[Global.setinfo.tapInfoList.Length];
        //ui
        while (channelParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(channelParent.transform.GetChild(0).gameObject));
        }
        while (channelParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        for (int i = 0; i < Global.setinfo.tapInfoList.Length; i ++)
        {
            try
            {
                channelObj[i] = Instantiate(channelItem);
                channelObj[i].transform.SetParent(channelParent.transform);
                channelObj[i].transform.Find("channel").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].regulator_channel.ToString();
                channelObj[i].transform.Find("no").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].serial_number.ToString();
                if (regulator_mode == 0)
                {
                    channelObj[i].transform.Find("temperature").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].temperature.ToString();
                    channelObj[i].transform.Find("pressure").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].pressure.ToString();
                    channelObj[i].transform.Find("pressure0").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].pressure0.ToString();
                }
                else
                {
                    channelObj[i].transform.Find("temperature").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].show_temp.ToString();
                    channelObj[i].transform.Find("pressure").GetComponent<InputField>().text = Global.setinfo.tapInfoList[i].show_press.ToString();
                }
                channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().options.Clear();
                for (int j = 0; j < Global.setinfo.regulators.Length; j++)
                {
                    Dropdown.OptionData option = new Dropdown.OptionData();
                    option.text = (j + 1).ToString();
                    channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().options.Add(option);
                }
                channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().value = Global.setinfo.tapInfoList[i].regulator_no - 1;
                int _i = i;
                int status = Global.setinfo.tapInfoList[i].pstatus;
                if (status == 0)
                {
                    channelObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("normal");
                    channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("sel");
                    channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Start";
                }
                else if (status == 1)
                {
                    channelObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("stop");
                    channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("unsel");
                    channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Stop";
                }
                else
                {
                    channelObj[i].transform.Find("status").GetComponent<Image>().sprite = Resources.Load<Sprite>("error");
                    channelObj[i].transform.Find("content").GetComponent<Text>().text = Global.setinfo.tapInfoList[i].err_content;
                }
                channelObj[i].transform.Find("btn").GetComponent<Button>().onClick.RemoveAllListeners();
                channelObj[i].transform.Find("btn").GetComponent<Button>().onClick.AddListener(delegate () {
                    onActChannel(_i);
                });
                if (regulator_mode == 0)
                {
                    channelObj[i].transform.Find("status").gameObject.SetActive(false);
                    channelObj[i].transform.Find("content").gameObject.SetActive(false);
                    channelObj[i].transform.Find("btn").gameObject.SetActive(true);
                    channelObj[i].transform.Find("pressure0").gameObject.SetActive(true);
                }
                else
                {
                    channelObj[i].transform.Find("status").gameObject.SetActive(true);
                    channelObj[i].transform.Find("content").gameObject.SetActive(true);
                    channelObj[i].transform.Find("btn").gameObject.SetActive(false);
                    channelObj[i].transform.Find("pressure0").gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }
        }
    }

    void onActChannel(int i)
    {
        try
        {
            if(Global.setinfo.tapInfoList[i].pstatus != 1 &&
                Global.setinfo.tapInfoList[i].pstatus != 0)
            {
                return;
            }
            Global.setinfo.tapInfoList[i].regulator_no = channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().value + 1;
            Global.setinfo.tapInfoList[i].regulator_channel = int.Parse(channelObj[i].transform.Find("channel").GetComponent<InputField>().text);
            if(Global.setinfo.tapInfoList[i].regulator_channel == 0)
            {
                return;
            }
            Global.setinfo.tapInfoList[i].pressure = float.Parse(channelObj[i].transform.Find("pressure").GetComponent<InputField>().text);
            Global.setinfo.tapInfoList[i].pressure0 = float.Parse(channelObj[i].transform.Find("pressure0").GetComponent<InputField>().text);
            Global.setinfo.tapInfoList[i].temperature = float.Parse(channelObj[i].transform.Find("temperature").GetComponent<InputField>().text);
            float pressure = 0f;
            if (Global.setinfo.tapInfoList[i].pstatus == 0)
            {
                pressure = Global.setinfo.tapInfoList[i].pressure;
            }
            else
            {
                pressure = Global.setinfo.tapInfoList[i].pressure0;
            }
            if (Global.setinfo.tapInfoList[i].pstatus == 0)
            {
                //현재 stop상태->start
                channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("unsel");
                channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Stop";
                Global.setinfo.tapInfoList[i].pstatus = 1;
            }
            else if (Global.setinfo.tapInfoList[i].pstatus == 1)
            {
                //현재 start상태->stop
                channelObj[i].transform.Find("btn").GetComponent<Image>().sprite = Resources.Load<Sprite>("sel");
                channelObj[i].transform.Find("btn/title").GetComponent<Text>().text = "Start";
                Global.setinfo.tapInfoList[i].pstatus = 0;
            }
            string data = "{\"regulator_no\":\"" + Global.setinfo.tapInfoList[i].regulator_no + "\"," +
                "\"ch_value\":\"" + Global.setinfo.tapInfoList[i].regulator_channel + "\"," +
                "\"pressure\":\"" + pressure + "\"," +
                "\"temperature\":\"" + Global.setinfo.tapInfoList[i].temperature + "\"," +
                "\"constant\":\"" + constantObj.GetComponent<InputField>().text + "\"," +
                "\"tolerance\":\"" + thresholdObj.GetComponent<InputField>().text + "\"," +
                "\"ctrl_state\":\"" + Global.setinfo.tapInfoList[i].pstatus + "\"}";
            socket.Emit("regulatorInfo", JSONObject.Create(data));
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    public void onRegulatorSetting()
    {
        regulator_mode = 0;
        StartCoroutine(LoadRegulatorInfo());
    }

    public void onRegulatorMonitor()
    {
        regulator_mode = 1;
        StartCoroutine(LoadRegulatorInfo());
    }

    public void onSaveRegulator()
    {
        if(regulator_mode == 0)
        {
            //setting
            PlayerPrefs.SetFloat("constant", float.Parse(constantObj.GetComponent<InputField>().text));
            PlayerPrefs.SetFloat("threshold", float.Parse(thresholdObj.GetComponent<InputField>().text));
            string cinfo = "[";
            int index = 0;
            for (int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
            {
                try
                {
                    int regulator_channel = int.Parse(channelObj[i].transform.Find("channel").GetComponent<InputField>().text);
                    if (regulator_channel != 0)
                    {
                        if (index == 0)
                        {
                            cinfo += "{";
                        }
                        else
                        {
                            cinfo += ",{";
                        }
                        index++;
                        cinfo += "\"id\":\"" + Global.setinfo.tapInfoList[i].tap_id + "\""
                            + ",\"regulator_no\":\"" + (channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().value + 1) + "\""
                            + ",\"regulator_channel\":\"" + channelObj[i].transform.Find("channel").GetComponent<InputField>().text + "\""
                            + ",\"pressure\":\"" + channelObj[i].transform.Find("pressure").GetComponent<InputField>().text + "\""
                            + ",\"pressure0\":\"" + channelObj[i].transform.Find("pressure0").GetComponent<InputField>().text + "\""
                            + ",\"temperature\":\"" + channelObj[i].transform.Find("temperature").GetComponent<InputField>().text + "\"}";
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log(ex);
                }
            }
            cinfo += "]";
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.setinfo.userinfo.pub_id);
            form.AddField("info", cinfo);
            WWW www = new WWW(Global.api_url + Global.save_regulatorinfo_api, form);
            StartCoroutine(SaveRegulatorInfo(www));
        }
        else
        {
            //refresh monitor
            for(int i = 1; i <= Global.setinfo.regulator_cnt; i++)
            {
                string ch_values = "";
                int index = 0;
                for(int j = 0; j < Global.setinfo.tapInfoList.Length; j ++)
                {
                    if(Global.setinfo.tapInfoList[j].regulator_no == i)
                    {
                        if (index != 0)
                        {
                            ch_values += ",";
                        }
                        ch_values += Global.setinfo.tapInfoList[j].regulator_channel;
                        index++;
                    }
                }
                string data = "{\"regulator_no\":\"" + i + "\"," + "\"ch_values\":\"" + ch_values + "\"}";
                socket.Emit("sendRegulatorRefresh", JSONObject.Create(data));
            }
            //StartCoroutine(LoadRegulatorInfo());
        }
    }

    IEnumerator SaveRegulatorInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                for(int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
                {
                    Global.setinfo.tapInfoList[i].regulator_no = channelObj[i].transform.Find("regulator").GetComponent<Dropdown>().value + 1;
                    Global.setinfo.tapInfoList[i].regulator_channel = int.Parse(channelObj[i].transform.Find("channel").GetComponent<InputField>().text);
                    Global.setinfo.tapInfoList[i].pressure = float.Parse(channelObj[i].transform.Find("pressure").GetComponent<InputField>().text);
                    Global.setinfo.tapInfoList[i].pressure0 = float.Parse(channelObj[i].transform.Find("pressure0").GetComponent<InputField>().text);
                    Global.setinfo.tapInfoList[i].temperature = float.Parse(channelObj[i].transform.Find("temperature").GetComponent<InputField>().text);
                }
                err_str.text = "성공적으로 보관되였습니다.";
                err_popup.SetActive(true);
            }
            else
            {
                err_str.text = "보관에 실패하였습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "보관에 실패하였습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onSetting()
    {
        Global.is_from_splash = false;
        StartCoroutine(GotoScene("setting"));
    }

    public void onGatewayBtn()
    {
        UnityEngine.Debug.Log("gateway button selected");
        gatewayImage.sprite = Resources.Load<Sprite>("sel");
        controllerImage.sprite = Resources.Load<Sprite>("unsel");
        regulatorImage.sprite = Resources.Load<Sprite>("unsel");
        StartCoroutine(LoadGateways(0));
    }

    public void onSaveDeviceInfo()
    {
        if(cur_selected_deviceIndex == -1)
        {
            return;
        }
        try
        {
            string nos = "";
            string names = "";
            string ids = "";
            string ips = "";
            string macs = "";
            if (cur_selected_deviceType == 0)
            {
                Global.setinfo.gateways[cur_selected_deviceIndex].name = deviceNameTxt.text;
                Global.setinfo.gateways[cur_selected_deviceIndex].id = deviceIDTxt.text;
                Global.setinfo.gateways[cur_selected_deviceIndex].ip = deviceIpTxt.text;
                Global.setinfo.gateways[cur_selected_deviceIndex].mac = deviceMacTxt.text;
                
                for (int i = 0; i < Global.setinfo.gateways.Length; i++)
                {
                    nos += (i + 1).ToString();
                    names += Global.setinfo.gateways[i].name;
                    ids += Global.setinfo.gateways[i].id;
                    ips += Global.setinfo.gateways[i].ip;
                    macs += Global.setinfo.gateways[i].mac;
                    if (i != Global.setinfo.gateways.Length - 1)
                    {
                        nos += ",";
                        names += ",";
                        ids += ",";
                        ips += ",";
                        macs += ",";
                    }
                }
                PlayerPrefs.SetString("devicenos", nos);
                PlayerPrefs.SetString("devicenames", names);
                PlayerPrefs.SetString("deviceids", ids);
                PlayerPrefs.SetString("deviceips", ips);
                PlayerPrefs.SetString("devicemacs", macs);
            }
            else if(cur_selected_deviceType == 1)
            {
                //controller
                Global.setinfo.controllers[cur_selected_deviceIndex].name = deviceNameTxt.text;
                Global.setinfo.controllers[cur_selected_deviceIndex].id = deviceIDTxt.text;
                Global.setinfo.controllers[cur_selected_deviceIndex].ip = deviceIpTxt.text;
                Global.setinfo.controllers[cur_selected_deviceIndex].mac = deviceMacTxt.text;

                nos = "";
                names = "";
                ids = "";
                ips = "";
                macs = "";
                for (int i = 0; i < Global.setinfo.controllers.Length; i++)
                {
                    nos += (i + 1).ToString();
                    names += Global.setinfo.controllers[i].name;
                    ids += Global.setinfo.controllers[i].id;
                    ips += Global.setinfo.controllers[i].ip;
                    macs += Global.setinfo.controllers[i].mac;
                    if (i != Global.setinfo.controllers.Length - 1)
                    {
                        nos += ",";
                        names += ",";
                        ids += ",";
                        ips += ",";
                        macs += ",";
                    }
                }
                PlayerPrefs.SetString("devicenos1", nos);
                PlayerPrefs.SetString("devicenames1", names);
                PlayerPrefs.SetString("deviceids1", ids);
                PlayerPrefs.SetString("deviceips1", ips);
                PlayerPrefs.SetString("devicemacs1", macs);
            }
            else
            {
                //regulator
                Global.setinfo.regulators[cur_selected_deviceIndex].name = deviceNameTxt.text;
                Global.setinfo.regulators[cur_selected_deviceIndex].id = deviceIDTxt.text;
                Global.setinfo.regulators[cur_selected_deviceIndex].ip = deviceIpTxt.text;
                Global.setinfo.regulators[cur_selected_deviceIndex].mac = deviceMacTxt.text;

                nos = "";
                names = "";
                ids = "";
                ips = "";
                macs = "";
                for (int i = 0; i < Global.setinfo.regulators.Length; i++)
                {
                    nos += (i + 1).ToString();
                    names += Global.setinfo.regulators[i].name;
                    ids += Global.setinfo.regulators[i].id;
                    ips += Global.setinfo.regulators[i].ip;
                    macs += Global.setinfo.regulators[i].mac;
                    if (i != Global.setinfo.regulators.Length - 1)
                    {
                        nos += ",";
                        names += ",";
                        ids += ",";
                        ips += ",";
                        macs += ",";
                    }
                }
                PlayerPrefs.SetString("devicenos2", nos);
                PlayerPrefs.SetString("devicenames2", names);
                PlayerPrefs.SetString("deviceids2", ids);
                PlayerPrefs.SetString("deviceips2", ips);
                PlayerPrefs.SetString("devicemacs2", macs);
            }
            for(int i = 0; i < deviceParent.transform.childCount; i++)
            {
                if(deviceParent.transform.transform.GetChild(i).Find("no").GetComponent<Text>().text == Global.setinfo.gateways[cur_selected_deviceIndex].no)
                {
                    deviceParent.transform.GetChild(i).GetComponent<Text>().text = deviceNameTxt.text;
                    deviceParent.transform.GetChild(i).GetComponent<Text>().color = Color.black;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    public void onSaveSelfInfo()
    {
        try
        {
            if(cur_selected_serial_number == -1)
            {
                err_str.text = "Self TAP 을 선택하세요.";
                err_popup.SetActive(true);
                return;
            }
            WWWForm form = new WWWForm();
            form.AddField("pub_id", Global.setinfo.userinfo.pub_id);
            form.AddField("serial_number", cur_selected_serial_number);
            form.AddField("tagGWNo", int.Parse(GwNoTxt.text));
            form.AddField("tagGW_channel", int.Parse(GwChTxt.text));
            form.AddField("boardNo", int.Parse(ControllerNoTxt.text));
            form.AddField("board_channel", int.Parse(ControllerChTxt.text));
            form.AddField("max_quantity", int.Parse(MaxQuantityTxt.text));
            form.AddField("flow_sensor", int.Parse(FlowSensorTxt.text));
            form.AddField("soldout", int.Parse(SoldoutTxt.text));
            form.AddField("decarbonation", int.Parse(DecarboncationTxt.text));
            form.AddField("volume_sec", int.Parse(VolumeSecTxt.text));
            form.AddField("decarbo_time", int.Parse(DecarbotimeTxt.text));
            form.AddField("standby_time", int.Parse(StandbytimeTxt.text));
            form.AddField("water", int.Parse(WaterTxt.text));
            if (Global.setinfo.is_coffee)
            {
                form.AddField("is_coffee", 1);
            }
            else
            {
                form.AddField("is_coffee", 0);
            }
            WWW www = new WWW(Global.api_url + Global.save_tapinfo_api, form);
            StartCoroutine(SaveTapInfo(www));
        } catch(Exception ex)
        {
            UnityEngine.Debug.Log(ex);
        }
    }

    IEnumerator SaveTapInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                err_str.text = "성공적으로 보관되였습니다.";
                err_popup.SetActive(true);
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "서버와의 접속이 원활하지 않습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onSoldout()
    {
        //soldout 조작
        /* soldout 설정이 되어잇으면*/
        if(!is_exist)
        {
            return;
        }
        if (cur_tapInfo.serial_number == 0 || cur_tapInfo.tagGWNo == 0 || cur_tapInfo.tagGW_channel == 0)
        {
            err_str.text = "셀프 세팅 정보를 입력하세요.";
            err_popup.SetActive(true);
            return;
        }

        int is_soldout = 0;
        int tag_state = 0;
        if (cur_tapInfo.soldout_state == 0)
        {
            //이미 솔드아웃 설정됨.
            soldoutBtnImg.sprite = Resources.Load<Sprite>("soldout1");
            is_soldout = 1;
            tag_state = 0;
        }
        else
        {
            soldoutBtnImg.sprite = Resources.Load<Sprite>("soldout");
            is_soldout = 0;
            tag_state = 1;
        }
        cur_tapInfo.soldout_state = is_soldout;

        string data = "{\"tagGW_no\":\"" + cur_tapInfo.tagGWNo + "\"," +
            "\"ch_value\":\"" + cur_tapInfo.tagGW_channel + "\"," +
            "\"status\":\"" + tag_state + "\"}";
        socket.Emit("deviceTagLock", JSONObject.Create(data));

        WWWForm form = new WWWForm();
        form.AddField("serial_number", cur_tapInfo.serial_number);
        form.AddField("is_soldout", is_soldout);
        WWW www = new WWW(Global.api_url + Global.set_soldout_api, form);
        StartCoroutine(ProcessSoldout(www));
    }

    IEnumerator ProcessSoldout(WWW www)
    {
        yield return www;
    }

    public void onValveA()
    {
        if(!is_exist)
        {
            return;
        }
        UnityEngine.Debug.Log(cur_tapInfo.serial_number);
        if(cur_tapInfo.boardNo == 0 || cur_tapInfo.board_channel == 0)
        {
            err_str.text = "셀프 세팅 정보를 입력하세요.";
            err_popup.SetActive(true);
            return;
        }

        int status;
        if(cur_tapInfo.valve1_state == 0)
        {
            //이미 설정됨.
            valveABtnImg.sprite = Resources.Load<Sprite>("valvea1");
            status = 1;
        }
        else
        {
            valveABtnImg.sprite = Resources.Load<Sprite>("valvea");
            status = 0;
        }
        cur_tapInfo.valve1_state = status;

        string data = "{\"board_no\":\"" + cur_tapInfo.boardNo + "\"," +
            "\"ch_value\":\"" + cur_tapInfo.board_channel + "\"," +
            "\"valve\":\"" + 0 + "\"," +
            "\"status\":\"" + status + "\"}";
        socket.Emit("boardValveCtrl", JSONObject.Create(data));

        if(status == 0)
        {
            //밸브 close 시
            data = "{\"tagGW_no\":\"" + cur_tapInfo.tagGWNo + "\"," +
                "\"ch_value\":\"" + cur_tapInfo.tagGW_channel + "\"," +
                "\"status\":\"" + 1 + "\"}";
            socket.Emit("deviceTagLock", JSONObject.Create(data));
        }
        else
        {
            data = "{\"tagGW_no\":\"" + cur_tapInfo.tagGWNo + "\"," +
                "\"ch_value\":\"" + cur_tapInfo.tagGW_channel + "\"," +
                "\"status\":\"" + 0 + "\"}";
            socket.Emit("deviceTagLock", JSONObject.Create(data));
        }
    }

    IEnumerator ValveCtrl(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                Global.setinfo.tapInfoList[cur_selected_tap_index] = cur_tapInfo;
            }
            else
            {
                err_str.text = "서버와의 접속이 원활하지 않습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "서버와의 접속이 원활하지 않습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onValveB()
    {
        if (!is_exist)
        {
            return;
        }
        if (cur_tapInfo.boardNo == 0 || cur_tapInfo.board_channel == 0)
        {
            err_str.text = "셀프 세팅 정보를 입력하세요.";
            err_popup.SetActive(true);
            return;
        }

        int status;
        if (cur_tapInfo.valve2_state == 0)
        {
            valveBBtnImg.sprite = Resources.Load<Sprite>("valveb1");
            status = 1;
        }
        else
        {
            valveBBtnImg.sprite = Resources.Load<Sprite>("valveb");
            status = 0;
        }
        cur_tapInfo.valve2_state = status;

        string data = "{\"board_no\":\"" + cur_tapInfo.boardNo + "\"," +
            "\"ch_value\":\"" + cur_tapInfo.board_channel + "\"," +
            "\"valve\":\"" + 1 + "\"," +
            "\"status\":\"" + status + "\"}";
        socket.Emit("boardValveCtrl", JSONObject.Create(data));

        if (status == 0)
        {
            //밸브 close 시
            data = "{\"tagGW_no\":\"" + cur_tapInfo.tagGWNo + "\"," +
                "\"ch_value\":\"" + cur_tapInfo.tagGW_channel + "\"," +
                "\"status\":\"" + 1 + "\"}";
            socket.Emit("deviceTagLock", JSONObject.Create(data));
        }
        else
        {
            data = "{\"tagGW_no\":\"" + cur_tapInfo.tagGWNo + "\"," +
                "\"ch_value\":\"" + cur_tapInfo.tagGW_channel + "\"," +
                "\"status\":\"" + 0 + "\"}";
            socket.Emit("deviceTagLock", JSONObject.Create(data));
        }

    }

    void SelectTap(int value)
    {
        //loading values from the selected tap.
        if(value == 0)
        {

            GwNoTxt.text = "";
            GwChTxt.text = "";
            ControllerNoTxt.text = "";
            ControllerChTxt.text = "";
            MaxQuantityTxt.text = "";
            FlowSensorTxt.text = "";
            SoldoutTxt.text = "";
            DecarboncationTxt.text = "";
            VolumeSecTxt.text = "";
            DecarbotimeTxt.text = "";
            StandbytimeTxt.text = "";
            WaterTxt.text = "";

            soldoutBtnImg.sprite = Resources.Load<Sprite>("soldout");
            valveABtnImg.sprite = Resources.Load<Sprite>("valvea");
            valveBBtnImg.sprite = Resources.Load<Sprite>("valveb");
            return;
        }
        cur_selected_serial_number = value;
        WWWForm form = new WWWForm();
        form.AddField("pub_id", Global.setinfo.userinfo.pub_id);
        form.AddField("serial_number", cur_selected_serial_number);
        WWW www = new WWW(Global.api_url + Global.get_tapinfo_api, form);
        StartCoroutine(GetTapInfo(www));
    }

    IEnumerator GetTapInfo(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            int result = jsonNode["suc"].AsInt;
            if (result == 1)
            {
                int serial_number = cur_selected_serial_number;
                int index = -1;
                TapInfo tapInfo = new TapInfo();
                for (int i = 0; i < Global.setinfo.tapInfoList.Length; i++)
                {
                    if (Global.setinfo.tapInfoList[i].serial_number == serial_number)
                    {
                        index = i;
                        tapInfo = Global.setinfo.tapInfoList[i];
                        break;
                    }
                }
                if (index == -1)
                    yield return null;

                tapInfo.tagGWNo = jsonNode["tagGWNo"].AsInt;
                tapInfo.tagGW_channel = jsonNode["tagGW_channel"].AsInt;
                tapInfo.boardNo = jsonNode["boardNo"].AsInt;
                tapInfo.board_channel = jsonNode["board_channel"].AsInt;
                tapInfo.max_quantity = jsonNode["max_quantity"].AsInt;
                tapInfo.flow_sensor = jsonNode["flow_sensor"].AsInt;
                tapInfo.soldout = jsonNode["soldout"].AsInt;
                tapInfo.decarbonation = jsonNode["decarbonation"].AsInt;
                tapInfo.volume_sec = jsonNode["volume_sec"].AsInt;
                tapInfo.decarbo_time = jsonNode["decarbo_time"].AsInt;
                tapInfo.standby_time = jsonNode["standby_time"].AsInt;
                tapInfo.water = jsonNode["water"].AsInt;
                tapInfo.soldout_state = jsonNode["is_soldout"].AsInt;
                Global.setinfo.tapInfoList[index] = tapInfo;
                cur_tapInfo = tapInfo;
                is_exist = true;
                GwNoTxt.text = jsonNode["tagGWNo"];
                GwChTxt.text = jsonNode["tagGW_channel"];
                ControllerNoTxt.text = jsonNode["boardNo"];
                ControllerChTxt.text = jsonNode["board_channel"];
                MaxQuantityTxt.text = jsonNode["max_quantity"];
                FlowSensorTxt.text = jsonNode["flow_sensor"];
                SoldoutTxt.text = jsonNode["soldout"];
                DecarboncationTxt.text = jsonNode["decarbonation"];
                VolumeSecTxt.text = jsonNode["volume_sec"];
                DecarbotimeTxt.text = jsonNode["decarbo_time"];
                StandbytimeTxt.text = jsonNode["standby_time"];
                WaterTxt.text = jsonNode["water"];
                if (cur_tapInfo.soldout_state == 0)
                {
                    soldoutBtnImg.sprite = Resources.Load<Sprite>("soldout");
                }
                else
                {
                    soldoutBtnImg.sprite = Resources.Load<Sprite>("soldout1");
                }
                valveABtnImg.sprite = Resources.Load<Sprite>("valvea");
                valveBBtnImg.sprite = Resources.Load<Sprite>("valveb");
            }
            else
            {
                is_exist = false;
                err_str.text = "서버와의 접속이 원활하지 않습니다.";
                err_popup.SetActive(true);
            }
        }
        else
        {
            is_exist = false;
            err_str.text = "서버와의 접속이 원활하지 않습니다.";
            err_popup.SetActive(true);
        }
    }

    public void onSetSelf()
    {
        regulatorSetObj.SetActive(false);
        deviceSetObj.SetActive(false);
        selfSetObj.SetActive(true);
        selfTapDrop.options.Clear();
        Dropdown.OptionData option = new Dropdown.OptionData();
        option.text = "";
        selfTapDrop.options.Add(option);
        for (int i = 0; i < Global.setinfo.self_cnt; i++)
        {
            option = new Dropdown.OptionData();
            option.text = (i + 1).ToString();
            selfTapDrop.options.Add(option);
        }
        selfTapDrop.onValueChanged.RemoveAllListeners();
        selfTapDrop.onValueChanged.AddListener((value) => {
            SelectTap(value);
        }
        );
    }

    IEnumerator LoadGateways(int type)
    {
        while (deviceParent.transform.childCount > 0)
        {
            StartCoroutine(Destroy_Object(deviceParent.transform.GetChild(0).gameObject));
        }
        while (deviceParent.transform.childCount > 0)
        {
            yield return new WaitForFixedUpdate();
        }
        if(type == 0){
            //gateway
            if(Global.setinfo.gateways != null)
            {
                for (int i = 0; i < Global.setinfo.gateways.Length; i++)
                {
                    GameObject tmp = Instantiate(deviceItem);
                    tmp.transform.SetParent(deviceParent.transform);
                    tmp.GetComponent<Text>().text = Global.setinfo.gateways[i].name;
                    tmp.transform.Find("no").GetComponent<Text>().text = Global.setinfo.gateways[i].no;
                    if (Global.setinfo.gateways[i].name == "")
                    {
                        tmp.GetComponent<Text>().text = "Not connected";
                        tmp.GetComponent<Text>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
                    }
                    else
                    {
                        tmp.GetComponent<Text>().color = Color.black;
                    }
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelectDevice(_i, 0); });
                }
            }
        }
        else if(type == 1)
        {
            //controller
            if(Global.setinfo.controllers != null)
            {
                for (int i = 0; i < Global.setinfo.controllers.Length; i++)
                {
                    GameObject tmp = Instantiate(deviceItem);
                    tmp.transform.SetParent(deviceParent.transform);
                    tmp.GetComponent<Text>().text = Global.setinfo.controllers[i].name;
                    tmp.transform.Find("no").GetComponent<Text>().text = Global.setinfo.controllers[i].no;
                    if (Global.setinfo.controllers[i].name == "")
                    {
                        tmp.GetComponent<Text>().text = "Not connected";
                        tmp.GetComponent<Text>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
                    }
                    else
                    {
                        tmp.GetComponent<Text>().color = Color.black;
                    }
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelectDevice(_i, 1); });
                }
            }
        }
        else
        {
            //regulator
            if (Global.setinfo.regulators != null)
            {
                for (int i = 0; i < Global.setinfo.regulators.Length; i++)
                {
                    GameObject tmp = Instantiate(deviceItem);
                    tmp.transform.SetParent(deviceParent.transform);
                    tmp.GetComponent<Text>().text = Global.setinfo.regulators[i].name;
                    tmp.transform.Find("no").GetComponent<Text>().text = Global.setinfo.regulators[i].no;
                    if (Global.setinfo.regulators[i].name == "")
                    {
                        tmp.GetComponent<Text>().text = "Not connected";
                        tmp.GetComponent<Text>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
                    }
                    else
                    {
                        tmp.GetComponent<Text>().color = Color.black;
                    }
                    tmp.GetComponent<Button>().onClick.RemoveAllListeners();
                    int _i = i;
                    tmp.GetComponent<Button>().onClick.AddListener(delegate () { onSelectDevice(_i, 2); });
                }
            }
        }
    }

    void onSelectDevice(int index, int type)
    {
        cur_selected_deviceIndex = index;
        cur_selected_deviceType = type;
        if(type == 0)
        {
            //gateway
            deviceNoTxt.text = Global.setinfo.gateways[index].no;
            deviceNameTxt.text = Global.setinfo.gateways[index].name;
            deviceIpTxt.text = Global.setinfo.gateways[index].ip;
            deviceIDTxt.text = Global.setinfo.gateways[index].id;
            deviceMacTxt.text = Global.setinfo.gateways[index].mac;
            curSelectedDevice = Global.setinfo.gateways[index];
        }
        else if(type == 1)
        {
            //controller
            deviceNoTxt.text = Global.setinfo.controllers[index].no;
            deviceNameTxt.text = Global.setinfo.controllers[index].name;
            deviceIpTxt.text = Global.setinfo.controllers[index].ip;
            deviceIDTxt.text = Global.setinfo.controllers[index].id;
            deviceMacTxt.text = Global.setinfo.controllers[index].mac;
            curSelectedDevice = Global.setinfo.controllers[index];
        }
        else
        {
            //regulator
            deviceNoTxt.text = Global.setinfo.regulators[index].no;
            deviceNameTxt.text = Global.setinfo.regulators[index].name;
            deviceIpTxt.text = Global.setinfo.regulators[index].ip;
            deviceIDTxt.text = Global.setinfo.regulators[index].id;
            deviceMacTxt.text = Global.setinfo.regulators[index].mac;
            curSelectedDevice = Global.setinfo.regulators[index];
        }
    }

    public void onControllerBtn()
    {
        gatewayImage.sprite = Resources.Load<Sprite>("unsel");
        controllerImage.sprite = Resources.Load<Sprite>("sel");
        regulatorImage.sprite = Resources.Load<Sprite>("unsel");
        StartCoroutine(LoadGateways(1));
    }

    public void onRegulatorBtn()
    {
        gatewayImage.sprite = Resources.Load<Sprite>("unsel");
        controllerImage.sprite = Resources.Load<Sprite>("unsel");
        regulatorImage.sprite = Resources.Load<Sprite>("sel");
        StartCoroutine(LoadGateways(2));
    }

    public void onSearch()
    {
        KillExes(1);
        if (cur_selected_deviceIndex == -1)
        {
            err_str.text = "세팅할 장비를 선택하세요.";
            err_popup.SetActive(true);
            return;
        }
        if (is_udp_run)
        {
            err_str.text = "디바이스 세팅중입니다.";
            err_popup.SetActive(true);
            return;
        }
        is_udp_run = true;
        string filePath = @"/udp_connecter/device.exe";
        //progress_popup.SetActive(true);
        //progress_str.text = "디바이스 세팅용 Exe 로딩중입니다.";
        if (cur_selected_deviceType == 0)//gateway
        {
            string command = "\"0\" \"" + Global.server_address + "\"";
            StartCoroutine(StartAndWaitForProcess(filePath, command, 1));
            StartCoroutine(checkConnectedTagGW());
        }
        else if(cur_selected_deviceType == 1)//controllers
        {
            string command = "\"1\" \"" + Global.server_address + "\"";
            StartCoroutine(StartAndWaitForProcess(filePath, command, 2));
            StartCoroutine(checkConnectedBoard());
        }
        else
        {
            //regulator
            string command = "\"2\" \"" + Global.server_address + "\"";
            StartCoroutine(StartAndWaitForProcess(filePath, command, 2));
            StartCoroutine(checkConnectedRegulator());
        }
    }

    IEnumerator checkConnectedTagGW()
    {
        yield return new WaitForSeconds(response_delay_time);
        if (!is_search)
        {
            err_str.text = "Connecting to Gateway";
            err_popup.SetActive(true);
        }
    }

    IEnumerator checkConnectedBoard()
    {
        yield return new WaitForSeconds(response_delay_time);
        if (!is_search)
        {
            err_str.text = "Connecting to Board";
            err_popup.SetActive(true);
        }
    }

    IEnumerator checkConnectedRegulator()
    {
        yield return new WaitForSeconds(response_delay_time);
        if (!is_search)
        {
            err_str.text = "Connecting to Regulator";
            err_popup.SetActive(true);
        }
    }

    public void onReset()
    {
        if(is_search)
        {
            if(deviceIDTxt.text == "")
            {
                err_str.text = "IP를 입력하세요.";
                err_popup.SetActive(true);
                return;
            }
            if(deviceIpTxt.text == "")
            {
                err_str.text = "ID를 입력하세요.";
                err_popup.SetActive(true);
                return;
            }
            if(deviceMacTxt.text == "")
            {
                err_str.text = "MAC 주소를 입력하세요.";
                err_popup.SetActive(true);
                return;
            }
            UnityEngine.Debug.Log("Alert");
            string data = "{\"pub_id\":\"" + Global.setinfo.userinfo.pub_id + "\"," +
                   "\"id\":\"" + deviceIDTxt.text + "\"," +
                   "\"ip\":\"" + deviceIpTxt.text + "\"," +
                   "\"mac\":\"" + deviceMacTxt.text + "\"}";
            socket.Emit("sendUdpResponse", JSONObject.Create(data));

            is_search = false;
        }
        else
        {
            err_str.text = "먼저 검색을 진행하세요.";
            err_popup.SetActive(true);
            return;
        }
    }

    void OnApplicationQuit()
    {
        if(socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
        }
        KillExes();
    }

    public void exit()
    {
        KillExes();
        Application.Quit();
    }

    public void minWindow()
    {
        ShowWindow(GetActiveWindow(), 2);
    }

    public void onErrPopup()
    {
        err_popup.SetActive(false);
    }

    public void onCloseSelectPopup()
    {
        select_popup.SetActive(false);
    }

    public void onSetDevice()
    {
        selfSetObj.SetActive(false);
        regulatorSetObj.SetActive(false);
        deviceSetObj.SetActive(true);
        onGatewayBtn();
    }

    public void onCloseSet()
    {
        deviceSetObj.SetActive(false);
    }

    public void onCloseReg()
    {
        regulatorSetObj.SetActive(false);
    }

    public void onCloseSelfSet()
    {
        selfSetObj.SetActive(false);
    }

    IEnumerator GotoScene(string sceneName)
    {
        if (socket != null)
        {
            socket.Close();
            socket.OnDestroy();
            socket.OnApplicationQuit();
        }
        if (socketObj != null)
        {
            DestroyImmediate(socketObj);
        }
        yield return new WaitForFixedUpdate();
        SceneManager.LoadScene(sceneName);
    }
}