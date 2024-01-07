using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraCtr : MonoBehaviour
{
    public RenderTexture RenderTextureRef;

    public GameObject gun;
    public GameObject center;

    public GameObject DL; //Directional light

    Transform gun_parent;

    bool update=false;

    int pre_data_num;
    int data_num = -1;
    int total_data_num;
    int process_percent=1;

    float x_rot;
    float y_rot;
    float z_rot;
    float cam_len;

    public float x_step = 5;
    public float y_step = 5;
    public float z_step = 5;
    public float cam_len_step = 0.2f;

    public float start_x = -90;
    public float start_y = -180;
    public float start_z = 0;
    public float start_cam_len = 1f;

    public float end_x = 90;
    public float end_y = 180;
    public float end_z = 360;
    public float end_cam_len = 3f;

    List<Vector3[]> data = new List<Vector3[]>();

    //List<Vector4> angle_data = new List<Vector4>();
    //List<Vector3> posi_data = new List<Vector3>();

    // Use this for initialization
    void Start () {
        //File.Delete("./Assets/direction.csv");

        if(!Directory.Exists("./Assets/data")){
            Directory.CreateDirectory("./Assets/data");
        }

        if(!File.Exists("./Assets/direction.csv")){
            FileStream fs = File.Create(@"./Assets/direction.csv");
            fs.Close();
        }

        gun_parent = gun.transform.parent;

        pre_data_num = (int)(Directory.GetFiles("./Assets/data", "*", SearchOption.TopDirectoryOnly).Length / 2); //metaデータの分だけ1/2する
        Debug.Log("pre_data_num: "+pre_data_num);

        cam_len = start_cam_len;
        z_rot = start_z;
        x_rot = start_x;
        y_rot = start_y - 2 * y_step;  //一つ目のupdateではなぜか画像が真っ暗になるからその分余計に取って後で消す。

        Camera.main.transform.localEulerAngles = new Vector3(start_x, start_y, start_z);
        Camera.main.transform.position = GetCameraPosition(new Vector3(start_x, start_y, start_z), center.transform.position, start_cam_len);

        total_data_num = (int)((int)Math.Ceiling((end_x-start_x)/x_step-0.001f) * (int)Math.Ceiling((end_y-start_y)/y_step-0.001f) * (int)Math.Ceiling((end_z-start_z)/z_step-0.001f) * (int)Math.Ceiling((end_cam_len - start_cam_len)/cam_len_step-0.001f));  //ここの挙動が安定しない。(int)が3->2にするときもある。=>これは計算精度の問題、（https://dobon.net/vb/dotnet/beginner/floatingpointerror.html)を参照。(int)Math.Ceilingを
        Debug.Log("The number of total data: " + total_data_num);
	}

	
	// Update is called once per frame
	void Update () {

        if(Input.GetKeyDown(KeyCode.S)){
            update=true;

            cam_len = start_cam_len;
            z_rot = start_z;
            x_rot = start_x;
            y_rot = start_y - 2 * y_step;
            pre_data_num = (int)(Directory.GetFiles("./Assets/data", "*", SearchOption.TopDirectoryOnly).Length / 2);

            Debug.Log("Start");
            Camera.main.GetComponent<Camera>().targetTexture = this.RenderTextureRef;
            data = new List<Vector3[]>();
            center.GetComponent<MeshRenderer>().enabled = false;
        }

        if(Input.GetKeyDown(KeyCode.Alpha1)){
            int num_data_csv = System.IO.File.ReadAllLines("./Assets/direction.csv").Length;
            Debug.Log("num data csv: "+ num_data_csv);
        }

        if(update){
            y_rot+=y_step;

            if(y_rot >= end_y){
                y_rot = start_y;
                x_rot += x_step;

                if(x_rot >= end_x){
                    x_rot = start_x;
                    z_rot += z_step;

                    if(z_rot >= end_z){
                        z_rot = start_z;
                        cam_len += cam_len_step;
                    }
                }
            }

            if(cam_len < end_cam_len){

                //Directional lightの向きをランダムに変更する
                System.Random r1 = new System.Random();
                DL.transform.eulerAngles = new Vector3(40, r1.Next(0,360), 0);

                Camera.main.transform.localEulerAngles = new Vector3(x_rot, y_rot, z_rot);
                Camera.main.transform.position = GetCameraPosition(new Vector3(x_rot, y_rot, z_rot), center.transform.position, cam_len);

                gun.transform.parent = Camera.main.transform;
                data.Add(new Vector3[2]{gun.transform.localPosition, gun.transform.localEulerAngles});
                gun.transform.parent = gun_parent;
                
                savePng(this.data_num);

                this.data_num+=1;

                if(this.data_num * 100 / total_data_num >= process_percent){  //100分割くらいで表示
                    Debug.Log((int)(this.data_num * 100 / total_data_num) + "% finished.");
                    process_percent += 1;
                }

            }else{
                Debug.Log("100% finished.");
                update = false;
                CSV_Write(data);
                Camera.main.GetComponent<Camera>().targetTexture = null;

                center.GetComponent<MeshRenderer>().enabled = true;
                
            }
        }
    }


    void savePng(int data_num)
    {

        Texture2D tex = new Texture2D(RenderTextureRef.width, RenderTextureRef.height, TextureFormat.RGB24, false);
        RenderTexture.active = RenderTextureRef;
        tex.ReadPixels(new Rect(0, 0, RenderTextureRef.width, RenderTextureRef.height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        //Write to a file in the project folder
        if(data_num != -1) File.WriteAllBytes("./Assets/data/"+ (pre_data_num + data_num) + ".png", bytes);  //これで黒い画像を弾けるか分からない。多分弾けない気がする

    }


    Vector3 GetCameraPosition(Vector3 eA, Vector3 center, float cam_len){
        double sitax=eA.x*Math.PI/180;
        double sitay=eA.y*Math.PI/180;

        float cosx = (float)Math.Cos(sitax);
        float cosy = (float)Math.Cos(sitay);
        float sinx = (float)Math.Sin(sitax);
        float siny = (float)Math.Sin(sitay);
        return center + cam_len * new Vector3(- cosx * siny, sinx, - cosx * cosy);
    }

    void CSV_Write(List<Vector3[]> list)
    {
        Debug.Log("list count:" + (list.Count-1));

        int num_data_csv = File.ReadAllLines("./Assets/direction.csv").Length;
        //Debug.Log("num data csv: "+ num_data_csv);

        StreamWriter sw = new StreamWriter(@"./Assets/direction.csv", true, Encoding.GetEncoding("Shift_JIS"));

        for(int i=1;i<list.Count;i++){    //最初のupdateはカウントしないから、i=1からスタート
            string line = list[i][0].x +","+ list[i][0].y +","+ list[i][0].z+","+list[i][1].x +","+ list[i][1].y +","+ list[i][1].z;
            sw.WriteLine(line);
        }

        sw.Close();
    }
}
