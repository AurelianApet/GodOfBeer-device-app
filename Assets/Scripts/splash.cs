using System;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;

public class splash : MonoBehaviour
{
    // Start is called before the first frame update
    public float delay_time = 0.5f;

    IEnumerator Start()
    {
        //Screen.fullScreen = false;
        if (PlayerPrefs.GetInt("idSave") == 1)
        {
            Global.setinfo.is_id_saved = true;
            Global.setinfo.userinfo.userID = PlayerPrefs.GetString("id");
        }
        else
        {
            Global.setinfo.is_id_saved = false;
        }
        if(PlayerPrefs.GetInt("is_coffee") == 1)
        {
            Global.setinfo.is_coffee = true;
        }
        else
        {
            Global.setinfo.is_coffee = false;
        }
        Global.setinfo.self_cnt = PlayerPrefs.GetInt("selfCnt");
        Global.setinfo.regulator_cnt = PlayerPrefs.GetInt("regulatorCnt");
        Global.server_address = PlayerPrefs.GetString("ip");
        Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
        Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
        Global.setinfo.app_type = PlayerPrefs.GetInt("type");
        try
        {
            Global.setinfo.gateways = new DeviceInfo[PlayerPrefs.GetInt("gatewayCnt")];
            Global.setinfo.controllers = new DeviceInfo[PlayerPrefs.GetInt("controllerCnt")];
            Global.setinfo.regulators = new DeviceInfo[Global.setinfo.regulator_cnt];
            string nostr = PlayerPrefs.GetString("devicenos");
            string namestr = PlayerPrefs.GetString("devicenames");
            string idstr = PlayerPrefs.GetString("deviceids");
            string ipstr = PlayerPrefs.GetString("deviceips");
            string macstr = PlayerPrefs.GetString("devicemacs");
            string[] nos = nostr.Split(',');
            string[] names = namestr.Split(',');
            string[] ids = idstr.Split(',');
            string[] ips = ipstr.Split(',');
            string[] macs = macstr.Split(',');
            for (int i = 0; i < nos.Length; i++)
            {
                Global.setinfo.gateways[i].no = nos[i];
                Global.setinfo.gateways[i].id = ids[i];
                Global.setinfo.gateways[i].ip = ips[i];
                Global.setinfo.gateways[i].name = names[i];
                Global.setinfo.gateways[i].mac = macs[i];
            }
            nostr = PlayerPrefs.GetString("devicenos1");
            namestr = PlayerPrefs.GetString("devicenames1");
            idstr = PlayerPrefs.GetString("deviceids1");
            ipstr = PlayerPrefs.GetString("deviceips1");
            macstr = PlayerPrefs.GetString("devicemacs1");
            nos = nostr.Split(',');
            names = namestr.Split(',');
            ids = idstr.Split(',');
            ips = ipstr.Split(',');
            macs = macstr.Split(',');
            for (int i = 0; i < nos.Length; i++)
            {
                Global.setinfo.controllers[i].no = nos[i];
                Global.setinfo.controllers[i].id = ids[i];
                Global.setinfo.controllers[i].ip = ips[i];
                Global.setinfo.controllers[i].name = names[i];
                Global.setinfo.controllers[i].mac = macs[i];
            }
            nostr = PlayerPrefs.GetString("devicenos2");
            namestr = PlayerPrefs.GetString("devicenames2");
            idstr = PlayerPrefs.GetString("deviceids2");
            ipstr = PlayerPrefs.GetString("deviceips2");
            macstr = PlayerPrefs.GetString("devicemacs2");
            nos = nostr.Split(',');
            names = namestr.Split(',');
            ids = idstr.Split(',');
            ips = ipstr.Split(',');
            macs = macstr.Split(',');
            for (int i = 0; i < nos.Length; i++)
            {
                Global.setinfo.regulators[i].no = nos[i];
                Global.setinfo.regulators[i].id = ids[i];
                Global.setinfo.regulators[i].ip = ips[i];
                Global.setinfo.regulators[i].name = names[i];
                Global.setinfo.regulators[i].mac = macs[i];
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        if (PlayerPrefs.GetInt("autoSave") == 1)
        {
            Debug.Log("auto save");
            Global.setinfo.is_auto_login = true;
            Global.setinfo.userinfo.userID = PlayerPrefs.GetString("id");
            Global.setinfo.userinfo.password = PlayerPrefs.GetString("pwd");
            WWWForm form = new WWWForm();
            form.AddField("userID", Global.setinfo.userinfo.userID);
            form.AddField("password", Global.setinfo.userinfo.password);
            form.AddField("type", Global.setinfo.app_type);
            WWW www = new WWW(Global.api_url + Global.login_api, form);
            StartCoroutine(ProcessLogin(www));
        }
        else
        {
            Global.setinfo.is_auto_login = false;
            yield return new WaitForSeconds(delay_time);
            SceneManager.LoadScene("login");
        }
    }

    IEnumerator ProcessLogin(WWW www)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                JSONNode dataMap = JSON.Parse(jsonNode["dataMap"].ToString()/*.Replace("\"", "")*/);
                Global.setinfo.userinfo.id = dataMap["uid"].AsInt;
                Global.setinfo.userinfo.pub_id = dataMap["pub_id"];
                Global.setinfo.userinfo.pub_name = dataMap["pub_name"];
                Global.setinfo.userinfo.is_open = dataMap["is_open"].AsBool;
                JSONNode tapList = JSON.Parse(jsonNode["tapList"].ToString()/*.Replace("\"", "")*/);
                Global.setinfo.tapInfoList = new TapInfo[tapList.Count];
                for (int i = 0; i < tapList.Count; i ++)
                {
                    TapInfo tapInfo = new TapInfo();
                    tapInfo.tap_id = tapList[i]["id"].AsInt;
                    tapInfo.serial_number = tapList[i]["serial_number"].AsInt;
                    tapInfo.tagGWNo = tapList[i]["tagGWNo"].AsInt;
                    tapInfo.tagGW_channel = tapList[i]["tagGW_channel"].AsInt;
                    tapInfo.boardNo = tapList[i]["boardNo"].AsInt;
                    tapInfo.board_channel = tapList[i]["board_channel"].AsInt;
                    tapInfo.regulator_no = tapList[i]["regulator_no"].AsInt;
                    tapInfo.regulator_channel = tapList[i]["regulator_channel"].AsInt;
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

                for(int i = 0; i < Global.setinfo.tapInfoList.Length - 1; i++)
                {
                    for(int j = i; j < Global.setinfo.tapInfoList.Length; j++)
                    {
                        if(Global.setinfo.tapInfoList[i].serial_number > Global.setinfo.tapInfoList[j].serial_number)
                        {
                            TapInfo temp = Global.setinfo.tapInfoList[i];
                            Global.setinfo.tapInfoList[i] = Global.setinfo.tapInfoList[j];
                            Global.setinfo.tapInfoList[j] = temp;
                        }
                    }
                }
                yield return new WaitForSeconds(delay_time);
                SceneManager.LoadScene("main");
            }
            else
            {
                yield return new WaitForSeconds(delay_time);
                SceneManager.LoadScene("login");
            }
        }
        else
        {
            yield return new WaitForSeconds(delay_time);
            SceneManager.LoadScene("login");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
