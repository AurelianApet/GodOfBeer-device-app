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

public class settingManager : MonoBehaviour
{
    public Toggle autologin;
    public Toggle noauto;
    public Toggle isCoffee;
    public InputField selfTapCntTxt;
    public InputField getwayTxt;
    public InputField controllerTxt;
    public InputField ipTxt;
    public InputField regulatorTxt;
    public GameObject err_popup;
    public Text err_str;

    // Start is called before the first frame update
    void Start()
    {
        getwayTxt.text = PlayerPrefs.GetInt("gatewayCnt").ToString();
        controllerTxt.text = PlayerPrefs.GetInt("controllerCnt").ToString();
        selfTapCntTxt.text = PlayerPrefs.GetInt("selfCnt").ToString();
        regulatorTxt.text = PlayerPrefs.GetInt("regulatorCnt").ToString();
        ipTxt.text = PlayerPrefs.GetString("ip");
        int is_coffee = PlayerPrefs.GetInt("is_coffee");
        if (is_coffee == 1)
        {
            isCoffee.isOn = true;
        }
        else
        {
            isCoffee.isOn = false;
        }
        int auto_save = PlayerPrefs.GetInt("autoSave");
        if (auto_save == 1)
        {
            autologin.isOn = true;
        }
        else
        {
            autologin.isOn = false;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    float time = 0f;
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

    public void onCloseLogSet()
    {
        if (Global.is_from_splash)
        {
            SceneManager.LoadScene("login");
        }
        else
        {
            SceneManager.LoadScene("main");
        }
    }

    public void onSaveSetInfo()
    {
        if (getwayTxt.text == "" && controllerTxt.text == "" && regulatorTxt.text == "" || selfTapCntTxt.text == "")
        {
            err_popup.SetActive(true);
            err_str.text = "설정값들을 정확히 입력하세요.";
            return;
        }
        if (ipTxt.text == "")
        {
            err_str.text = "ip를 정확히 입력하세요.";
            err_popup.SetActive(true);
            return;
        }
        Global.server_address = ipTxt.text.Trim();
        PlayerPrefs.SetString("ip", Global.server_address);
        Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
        Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
        WWWForm form = new WWWForm();
        WWW www = new WWW(Global.api_url + Global.check_db_api, form);
        StartCoroutine(ProcessCheckConnect(www));
    }
    
    IEnumerator ProcessCheckConnect(WWW www)
    {
        yield return www;
        if(www.error == null)
        {
            if (autologin.isOn)
            {
                Global.setinfo.is_auto_login = true;
                PlayerPrefs.SetInt("autoSave", 1);
            }
            else
            {
                Global.setinfo.is_auto_login = false;
                PlayerPrefs.SetInt("autoSave", 0);
            }
            if (isCoffee.isOn)
            {
                Global.setinfo.is_coffee = true;
                PlayerPrefs.SetInt("is_coffee", 1);
            }
            else
            {
                Global.setinfo.is_coffee = false;
                PlayerPrefs.SetInt("is_coffee", 0);
            }
            try
            {
                Global.setinfo.self_cnt = int.Parse(selfTapCntTxt.text);
                Global.setinfo.regulator_cnt = int.Parse(regulatorTxt.text);
                if(int.Parse(getwayTxt.text) > 0)
                {
                    Global.setinfo.gateways = new DeviceInfo[int.Parse(getwayTxt.text)];
                    Global.tagGWProcess = new Process();
                    PlayerPrefs.SetInt("gatewayCnt", Global.setinfo.gateways.Length);
                }
                else
                {
                    PlayerPrefs.SetInt("gatewayCnt", 0);
                }
                if (int.Parse(controllerTxt.text) > 0)
                {
                    Global.setinfo.controllers = new DeviceInfo[int.Parse(controllerTxt.text)];
                    Global.boardProcess = new Process();
                    PlayerPrefs.SetInt("controllerCnt", Global.setinfo.controllers.Length);
                }
                else
                {
                    PlayerPrefs.SetInt("controllerCnt", 0);
                }
                if (Global.setinfo.regulator_cnt > 0)
                {
                    Global.setinfo.regulators = new DeviceInfo[Global.setinfo.regulator_cnt];
                    Global.regulatorProcess = new Process();
                }
                PlayerPrefs.SetInt("regulatorCnt", Global.setinfo.regulator_cnt);
                PlayerPrefs.SetInt("selfCnt", Global.setinfo.self_cnt);
                string nos = "";
                string names = "";
                string ids = "";
                string ips = "";
                string macs = "";
                for (int i = 0; i < Global.setinfo.gateways.Length; i++)
                {
                    Global.setinfo.gateways[i].no = (i + 1).ToString();
                    Global.setinfo.gateways[i].name = "";
                    Global.setinfo.gateways[i].id = "";
                    Global.setinfo.gateways[i].ip = "";
                    Global.setinfo.gateways[i].mac = "";
                    nos += (i + 1).ToString();
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
                nos = "";
                names = "";
                ids = "";
                ips = "";
                macs = "";
                for (int i = 0; i < Global.setinfo.controllers.Length; i++)
                {
                    Global.setinfo.controllers[i].no = (i + 1).ToString();
                    Global.setinfo.controllers[i].name = "";
                    Global.setinfo.controllers[i].id = "";
                    Global.setinfo.controllers[i].ip = "";
                    Global.setinfo.controllers[i].mac = "";
                    nos += (i + 1).ToString();
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
                nos = "";
                names = "";
                ids = "";
                ips = "";
                macs = "";
                for (int i = 0; i < Global.setinfo.regulators.Length; i++)
                {
                    Global.setinfo.regulators[i].no = (i + 1).ToString();
                    Global.setinfo.regulators[i].name = "";
                    Global.setinfo.regulators[i].id = "";
                    Global.setinfo.regulators[i].ip = "";
                    Global.setinfo.regulators[i].mac = "";
                    nos += (i + 1).ToString();
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
                Global.server_address = ipTxt.text.Trim();
                PlayerPrefs.SetString("ip", Global.server_address);
                Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
                Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
                Global.is_pos_run = false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }
            err_popup.SetActive(true);
            err_str.text = "성공적으로 저장되었습니다.";
        }
        else
        {
            err_str.text = "IP를 확인하세요.";
            err_popup.SetActive(true);
        }
    }

    public void onErrPopup()
    {
        err_popup.SetActive(false);
    }
}