using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class loginManager : MonoBehaviour
{
    public InputField userId;
    public InputField password;
    public Toggle beerType;
    public Toggle wineType;
    public Toggle idSave;
    public Toggle autoSave;
    public GameObject err_popup;
    public Text err_str;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.fullScreen = false;
        if(Global.server_address == "")
        {
            Global.is_from_splash = true;
            SceneManager.LoadScene("setting");
        }
        if (Global.setinfo.is_id_saved)
        {
            userId.text = Global.setinfo.userinfo.userID;
            idSave.isOn = true;
        }
        if (Global.setinfo.is_auto_login)
        {
            autoSave.isOn = true;
        }
    }

    public void Login()
    {
        if (userId.text == "" || password.text == "")
        {
            err_str.text = "로그인 정보를 확인하세요.";
            err_popup.SetActive(true);
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("userID", userId.text);
            form.AddField("password", password.text);
            if (beerType.isOn)
            {
                Global.setinfo.app_type = 0;
            }
            else
            {
                Global.setinfo.app_type = 1;
            }
            form.AddField("type", Global.setinfo.app_type);
            WWW www = new WWW(Global.api_url + Global.login_api, form);
            StartCoroutine(ProcessLogin(www, idSave.isOn, userId.text, autoSave.isOn, password.text));
        }
    }

    IEnumerator ProcessLogin(WWW www, bool is_idsave, string username, bool is_autosave, string password)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode dataMap = JSON.Parse(jsonNode["dataMap"].ToString()/*.Replace("\"", "")*/);
                Global.setinfo.userinfo.id = dataMap["uid"].AsInt;
                if (is_idsave)
                {
                    PlayerPrefs.SetInt("idSave", 1);
                    PlayerPrefs.SetString("id", username);
                    Global.setinfo.is_id_saved = true;
                }
                else
                {
                    PlayerPrefs.SetInt("idSave", 0);
                    Global.setinfo.is_id_saved = false;
                }
                if (is_autosave)
                {
                    Debug.Log("autosave");
                    PlayerPrefs.SetInt("autoSave", 1);
                    PlayerPrefs.SetString("id", username);
                    PlayerPrefs.SetString("pwd", password);
                    Global.setinfo.is_auto_login = true;
                }
                else
                {
                    PlayerPrefs.SetInt("autoSave", 0);
                    Global.setinfo.is_auto_login = false;
                }
                PlayerPrefs.SetInt("type", Global.setinfo.app_type);
                Global.setinfo.userinfo.userID = username;
                Global.setinfo.userinfo.password = password;
                Global.setinfo.userinfo.pub_id = dataMap["pub_id"].AsInt;
                Global.setinfo.userinfo.pub_name = dataMap["pub_name"];
                Global.setinfo.userinfo.is_open = dataMap["is_open"].AsBool;

                JSONNode tapList = JSON.Parse(jsonNode["tapList"].ToString()/*.Replace("\"", "")*/);
                Global.setinfo.tapInfoList = new TapInfo[tapList.Count];
                for (int i = 0; i < tapList.Count; i++)
                {
                    TapInfo tapInfo = new TapInfo();
                    tapInfo.serial_number = tapList[i]["serial_number"].AsInt;
                    tapInfo.tagGWNo = tapList[i]["tagGWNo"].AsInt;
                    tapInfo.tagGW_channel = tapList[i]["tagGW_channel"].AsInt;
                    tapInfo.boardNo = tapList[i]["boardNo"].AsInt;
                    tapInfo.board_channel = tapList[i]["board_channel"].AsInt;
                    tapInfo.regulator_no = tapList[i]["regulator_no"].AsInt;
                    tapInfo.regulator_channel = tapList[i]["regulator_channel"].AsInt;
                    tapInfo.tap_id = tapList[i]["id"].AsInt;
                    tapInfo.temperature = tapList[i]["temperature"].AsFloat;
                    tapInfo.pressure = tapList[i]["pressure"].AsFloat;
                    tapInfo.pressure0 = tapList[i]["pressure0"].AsFloat;
                    tapInfo.is_error = tapList[i]["error"].AsInt;
                    tapInfo.err_content = tapList[i]["err_content"];
                    tapInfo.pstatus = tapList[i]["pstatus"].AsInt;
                    tapInfo.max_quantity = tapList[i]["max_quantity"].AsInt;
                    tapInfo.flow_sensor = tapList[i]["flow_sensor"].AsInt;
                    tapInfo.soldout = tapList[i]["soldout"].AsInt;
                    tapInfo.decarbonation = tapList[i]["decarbonation"].AsInt;
                    tapInfo.open_settingtime = tapList[i]["open_settingtime"].AsInt;
                    tapInfo.appType = tapList[i]["appType"].AsInt;
                    tapInfo.appNo = tapList[i]["appNo"].AsInt;
                    Global.setinfo.tapInfoList[i] = tapInfo;
                }

                for (int i = 0; i < Global.setinfo.tapInfoList.Length - 1; i++)
                {
                    for (int j = i; j < Global.setinfo.tapInfoList.Length; j++)
                    {
                        if (Global.setinfo.tapInfoList[i].serial_number > Global.setinfo.tapInfoList[j].serial_number)
                        {
                            TapInfo temp = Global.setinfo.tapInfoList[i];
                            Global.setinfo.tapInfoList[i] = Global.setinfo.tapInfoList[j];
                            Global.setinfo.tapInfoList[j] = temp;
                        }
                    }
                }
                SceneManager.LoadScene("main");
            }
            else
            {
                err_str.text = jsonNode["msg"];
                err_popup.SetActive(true);
            }
        }
        else
        {
            err_str.text = "인터넷 연결을 확인하세요.";
            err_popup.SetActive(true);
        }
    }

    public void onSet()
    {
        Global.is_from_splash = true;
        SceneManager.LoadScene("setting");
    }

    public void onConfirmErrPopup()
    {
        err_popup.SetActive(false);
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
}
