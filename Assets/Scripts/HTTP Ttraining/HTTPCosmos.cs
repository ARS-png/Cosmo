using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using Newtonsoft.Json;

public class HTTPCosmos : MonoBehaviour
{
    [SerializeField] private string url;

    private IEnumerator SendRequest()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);


        yield return request.SendWebRequest();


    }
    
}
