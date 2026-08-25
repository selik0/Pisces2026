using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button button;
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(() =>
        {
            GameEvent.Default.Emit(1);
        });
        GameEvent.Default.Subscribe(1, Func);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Func()
    {
        string str = null;
        str.ToString();
        Debug.Log("Test");
    }
}
