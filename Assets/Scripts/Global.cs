using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;

public struct SetInfo
{
    public int type;//0-와인, 1-맥주
    public bool is_auto_login;
    public bool is_coffee;
    public bool is_id_saved;
    public int app_type;//0-beer 1-wine
    public int self_cnt;
    public int regulator_cnt;
    public UserInfo userinfo;
    public DeviceInfo[] gateways;
    public DeviceInfo[] controllers;
    public DeviceInfo[] regulators;
    public TapInfo[] tapInfoList;
}

public struct UserInfo
{
    public int id;
    public string userID;
    public string password;
    public int pub_id;
    public string pub_name;
    public bool is_open;
}

public struct DeviceInfo
{
    public string no;
    public string name;
    public string id;
    public string mac;
    public string ip;
}

public struct TapInfo
{
    public int tap_id;
    public int serial_number;
    public int tagGWNo;
    public int tagGW_channel;
    public int boardNo;
    public int board_channel;
    public int regulator_no;
    public int regulator_channel;
    public int max_quantity;
    public int flow_sensor;
    public int soldout;
    public int decarbonation;
    public int open_settingtime;
    public int volume_sec;
    public int decarbo_time;
    public int standby_time;
    public int water;
    public int is_error;
    public float pressure;//제어용 압력설정값(start시)
    public float temperature;//제어용 온도설정값
    public float pressure0;//제어용 압력설정값(stop시)
    public float show_press;//현재 압력
    public float show_temp;//현재 온도
    public string err_content;
    public int pstatus;//밸브 제어 0-stop상태, 1-표준상태, 2-error
    public int appType;
    public int appNo;
    public int soldout_state;
    public int valve1_state;
    public int valve2_state;
}

public class Global
{
    public static bool is_pos_run = false;
    //setting information
    public static SetInfo setinfo = new SetInfo();
    public static string filePath = "";
    public static Process posExe = null;
    public static Process udpConnecter = null;
    public static Process tagGWProcess = null;
    public static Process boardProcess = null;
    public static Process regulatorProcess = null;
    //api
    public static bool is_from_splash = false;
    public static string server_address = "";
    public static string api_server_port = "3006";
    public static string api_url = "";
    static string api_prefix = "m-api/device/";
    //self_device셀프단말기용 api
    public static string login_api = api_prefix + "login";
    public static string check_db_api = api_prefix + "check-db";
    public static string save_tapinfo_api = api_prefix + "save-tap-deviceinfo";
    public static string get_tapinfo_api = api_prefix + "get-tap-deviceinfo";
    public static string save_regulatorinfo_api = api_prefix + "save-regulatorinfo";
    public static string set_soldout_api = api_prefix + "set-soldout";
    public static string valve_ctrl_api = api_prefix + "valve-ctrl";
    //socket server
    public static string socket_server = "";
}