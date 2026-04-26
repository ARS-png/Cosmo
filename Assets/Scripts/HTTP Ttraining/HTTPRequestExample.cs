using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HTTPRequestExample : MonoBehaviour
{
    [SerializeField] private string url;

    private void Start()
    {
        StartCoroutine(SendRequest());
    }


    private IEnumerator SendRequest()
    {
        PostStruct post = new()
        {
            id = 1,
            userId = 23,
            title = "now this is its title",
            body = "now this is its body"
        };

        string json = JsonUtility.ToJson(post);

        UnityWebRequest request = UnityWebRequest.Put(this.url, json);

        request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");

        yield return request.SendWebRequest();

    }


    //private IEnumerator SendRequest()
    //{
    //PostStruct post = new PostStruct()
    //{
    //    userId = 115,
    //    title = "some title",
    //    body = "Ctreated post body"
    //};

    //    string json = JsonUtility.ToJson(post);

    //    UnityWebRequest request = new UnityWebRequest(this.url, "POST");

    //    byte[] postBytes = Encoding.UTF8.GetBytes(json);


    //    UploadHandler uploadHandler = new UploadHandlerRaw(postBytes);
    //    request.uploadHandler = uploadHandler;

    //    request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");

    //    request.downloadHandler = new DownloadHandlerBuffer();

    //    yield return request.SendWebRequest();


    //    PostStruct postFromServer = JsonUtility.FromJson<PostStruct>(request.downloadHandler.text);


    //    Debug.Log("User Id: " + postFromServer.userId);
    //    Debug.Log("id: " + postFromServer.id);
    //    Debug.Log("body: " + postFromServer.body);
    //    Debug.Log("title: " + postFromServer.title);

    //}



    //private IEnumerator SendRequest()
    //{
    //    UnityWebRequest request = UnityWebRequest.Get(url);

    //    yield return request.SendWebRequest();

    //    string wrappedJson = "{\"posts\":" + request.downloadHandler.text + "}";

    //    WrapperStruct response = JsonUtility.FromJson<WrapperStruct>(wrappedJson); //пытается заполнить поля объекта данными json

    //    Debug.Log("UserId : " + response.posts[0].userId);
    //    Debug.Log("UserId : " + response.posts[0].title);
    //    Debug.Log("UserId : " + response.posts[0].body);
    //    Debug.Log("UserId : " + response.posts[0].id);

    //}

}
