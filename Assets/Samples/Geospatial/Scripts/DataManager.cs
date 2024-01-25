using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using Newtonsoft.Json;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// Classes for the json deserialization, followes the structure of the json from the server
public class Place
{
    public int id { get; set; }
    public string name { get; set; }
    public string info { get; set; }
    public string url { get; set; }
    public string base64texture { get; set; }
    public string base64custom { get; set; }
    public string customText { get; set; }
    public int rating { get; set; }
    public Location[] locations { get; set; }
    public Autor autor { get; set; }
}

public class Autor
{
    public int id { get; set; }
    public string name { get; set; }
    public int rating { get; set; }
}

public class Location
{
    public int id { get; set; }
    public double lng { get; set; }
    public double lat { get; set; }
    public double lev { get; set; }
    public float rX { get; set; }
    public float rY { get; set; }
    public float rZ { get; set; }
    public float rW { get; set; }
    public int placeId { get; set; }
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    // needed as reference to the geospatialController to call the function to generate a new anchor
    [SerializeField] private GeospatialController geospatialController;
    [SerializeField] private GameObject demoGameObject;
    [SerializeField] private Texture2D demoTexture;

    public bool debug = false;


    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    // our inputs here

    public Shader surface_shader;
    public GameObject simons_debug_thingy;
    private String debug_string = "";

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    private void Start()
    {
#if UNITY_EDITOR
        if (debug)
        {
            SerializeObjectJson();
        }
#endif
    }


    // retrieves all places from the server and generates them
    public void RequestPlacesDataFromServer()
    {
        // Callback to request the Data from the server
        RESTApiClient.Instance.GetPlacesFromServer(placesFromServer =>
        {
            if (placesFromServer == null || placesFromServer.Count == 0)
            {
                Debug.Log("Error loading Places or Empty");
                return;
            }
            //m_Text
            // processes the data from the server
            GeneratePlacesWithPrimitives(placesFromServer);
            Debug.Log("Loaded data remotely from PlaceIT-Api", this);
        });
    }


    private void GeneratePlaces(List<Place> placesFromServer)
    {
        foreach (var place in placesFromServer)
        {
            Debug.Log(place.name);
            //GameObject gltfObject = new GameObject();
            //var gltf = gltfObject.AddComponent<GLTFast.GltfAsset>();

            // here goes the go url from place.url
            //gltf.Url = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF/Duck.gltf";

            // convert base64Texture from response to Texture2D
            var newGo = Instantiate(demoGameObject);

            if (!place.base64texture.Equals(""))
            {
                byte[] imageData = Convert.FromBase64String(place.base64texture);

                Texture2D texture = new Texture2D(1024, 1024);
                texture.filterMode = FilterMode.Trilinear;
                texture.LoadImage(imageData);
                newGo.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
            }

            foreach (var location in place.locations)
            {
                // CALLS a function from the GeospatialController.cs to generate a new anchor
                // This function has to be implemented in the GeospatialController.cs
                // demoGameObject is a placeholder for the gltfObject (must be available in runtime, functionalliy to download the gltf from the server is not implemented yet)

                var newQuaternion = new Quaternion();
                newQuaternion.x = location.rX;
                newQuaternion.y = location.rY;
                newQuaternion.z = location.rZ;
                newQuaternion.w = location.rW;

                //geospatialController.GenerateNewAnchor(location.lat, location.lng, location.lev, newQuaternion, demoGameObject);
                // CALLS a function from the GeospatialController.cs to generate a new anchor
                // This function has to be implemented in the GeospatialController.cs
                // demoGameObject is a placeholder for the gltfObject (must be available in runtime, functionalliy to download the gltf from the server is not implemented yet)
                geospatialController.PlaceFixedGeospatialAnchor(new GeospatialAnchorHistory(DateTime.Now, location.lat, location.lng, location.lev, AnchorType.Terrain, newQuaternion), newGo);
            }
        }
    }


    // AddPlace including Texture2D Data that was captured before with your application. Texture2D automatically is converted to a base64String
    // customBase64 is a custom fiield to submit any kind of data as base64String (conversion has to be done manually)
    public void AddPlaceToDataBase(Group authorName, double lng, double lat, double lev, Quaternion rotation, string placeName,
        string placeInfo, Texture2D texture, string objectURL = "", string customText = "", string customBase64 = "")
    {
        // settings
        var setting = new JsonSerializerSettings();
        setting.Formatting = Formatting.Indented;
        setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        var newAutor = new Autor();
        newAutor.name = authorName.ToString();

        var newLocation = new Location();
        newLocation.lng = lng;
        newLocation.lat = lat;
        newLocation.lev = lev;
        newLocation.rX = rotation.x;
        newLocation.rY = rotation.y;
        newLocation.rZ = rotation.z;
        newLocation.rW = rotation.w;

        var newPlace = new Place();
        newPlace.name = placeName;
        newPlace.info = placeInfo;
        newPlace.url = objectURL;

        // generates an image out of the Texture2D and converts it to a base64String to store it in the db
        var bytes = texture.EncodeToJPG();
        newPlace.base64texture = Convert.ToBase64String(bytes);
        newPlace.base64custom = customBase64;

        newPlace.customText = customText;
        newPlace.locations = new Location[] { newLocation };
        newPlace.autor = newAutor;

        // write
        var serializedJson = JsonConvert.SerializeObject(newPlace, setting);
        Debug.Log(serializedJson);

        RESTApiClient.Instance.UploadSinglePlace(serializedJson);
    }


    // authorName must be 'Group1', 'Group2', 'Group3', 'Group4', 'Group5' or 'Group6'
    public void AddPlaceToDataBase(Group authorName, double lng, double lat, double lev, Quaternion rotation, string placeName, string placeInfo = "", string objectURL = "", string customText = "")
    {
        // settings
        var setting = new JsonSerializerSettings();
        setting.Formatting = Formatting.Indented;
        setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        var newAutor = new Autor();
        newAutor.name = authorName.ToString();

        var newLocation = new Location();
        newLocation.lng = lng;
        newLocation.lat = lat;
        newLocation.lev = lev;
        newLocation.rX = rotation.x;
        newLocation.rY = rotation.y;
        newLocation.rZ = rotation.z;
        newLocation.rW = rotation.w;

        var newPlace = new Place();
        newPlace.name = placeName;
        newPlace.info = placeInfo;
        newPlace.url = objectURL;
        newPlace.base64texture = "";
        newPlace.base64custom = "";
        newPlace.customText = QuaternionToBase64(rotation);//customText;
        newPlace.locations = new Location[] { newLocation };
        newPlace.autor = newAutor;

        // write
        var serializedJson = JsonConvert.SerializeObject(newPlace, setting);
        Debug.Log(serializedJson);

        RESTApiClient.Instance.UploadSinglePlace(serializedJson);
    }




    // DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- DEBUG FUNCTIONS ---- 

    // allows access to demo texture data from custom editor buttons
    public Texture2D GetDemoTexture2D()
    {
        return demoTexture;
    }

    // this is a test function to serialize a object to json and upload it to the server
    // newAutor.name = "Andreas" is a forced field, because the server needs a autor and "Andreas" is a fixed placeholder
    private void SerializeObjectJson()
    {
        // settings
        var setting = new JsonSerializerSettings();
        setting.Formatting = Formatting.Indented;
        setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

        var newAutor = new Autor();
        newAutor.name = Group.All.ToString();
        newAutor.rating = 1;

        var newLocation = new Location();
        newLocation.lng = 5.5555;
        newLocation.lat = 40.4444;
        newLocation.lev = 270;
        newLocation.rX = 0.0f;
        newLocation.rY = 0.0f;
        newLocation.rZ = 0.0f;
        newLocation.rW = 0.0f;

        var newPlace = new Place();
        newPlace.name = "Test-Place";
        newPlace.info = "Test-Info";
        newPlace.url = "https://anfuchs.de";
        newPlace.base64texture = "";
        newPlace.base64custom = "";
        newPlace.customText = "";
        newPlace.locations = new Location[] { newLocation };
        newPlace.autor = newAutor;

        // write
        var serializedJson = JsonConvert.SerializeObject(newPlace, setting);
        Debug.Log(serializedJson);

        RESTApiClient.Instance.UploadSinglePlace(serializedJson);
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    // here our functions and variants of functions
    private void GeneratePlacesWithPrimitives(List<Place> placesFromServer)
    {
        output_debug("number of places: " + placesFromServer.Count.ToString());
        foreach (var place in placesFromServer)
        {
            // convert base64Texture from response to Texture2D
            Material newMat = new Material(surface_shader);
            var newGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            newGo.gameObject.transform.localScale = new Vector3((9.0f / 21.0f) * 0.25f, 1.0f, 0.25f);
            newGo.GetComponent<Renderer>().material = newMat;
            //output_debug(place.id.ToString());

            if (!place.base64texture.Equals(""))
            {
                byte[] imageData = Convert.FromBase64String(place.base64texture);
                Texture2D texture = new Texture2D(1080, 2400);
                texture.filterMode = FilterMode.Trilinear;
                texture.LoadImage(imageData);
                newGo.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
            }

            foreach (var location in place.locations)
            {

                var newQuaternion = place.customText != "" ? Base64ToQuaternion(place.customText) : Quaternion.EulerRotation(0.0f, 90.0f, 0.0f);//new Quaternion();
                //output_debug("converted customText to quaternion");
                /*
                newQuaternion.x = location.rX;
                newQuaternion.y = location.rY;
                newQuaternion.z = location.rZ;
                newQuaternion.w = location.rW;
                */
                output_debug(location.lat.ToString());
                geospatialController.PlaceFixedGeospatialAnchor(new GeospatialAnchorHistory(DateTime.Now, location.lat, location.lng, location.lev, AnchorType.Terrain, newQuaternion), newGo);
            }
            //newGo.SetActive(false);
            Destroy(newGo);
        }
    }

    // Convert a Quaternion to an Base64String
    private String QuaternionToBase64(Quaternion q)
    {
        if (q == null)
            return null;

        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, q);

        return Convert.ToBase64String(ms.ToArray());
    }

    // Convert a Base64String to a Quaternion
    private Quaternion Base64ToQuaternion(String rot_string)
    {
        Quaternion q = new Quaternion();
        //output_debug("created new quaternion");
        //output_debug("|" + rot_string + "|");
        rot_string = rot_string.Substring(1, rot_string.Length - 2);
        //output_debug("cut of the ends of the string");
        //t.Trim('(', ')');
        string[] c = rot_string.Split(", ");
        //output_debug("split values");
        q.x = float.Parse(c[0], CultureInfo.InvariantCulture.NumberFormat);
        q.y = float.Parse(c[1], CultureInfo.InvariantCulture.NumberFormat);
        q.z = float.Parse(c[2], CultureInfo.InvariantCulture.NumberFormat);
        q.w = float.Parse(c[3], CultureInfo.InvariantCulture.NumberFormat);


        Vector3 axis = q * Vector3.up + q * Vector3.back;
        Quaternion sec_rot = new Quaternion(axis.x, axis.y, axis.z, 0);
        //output_debug("parsed strings to floats");
        //output_debug(q.ToString());
        return sec_rot * q;
    }

    private void output_debug(String a)
    {
        simons_debug_thingy.GetComponent<Text>().text += a + "\n";
    }
}